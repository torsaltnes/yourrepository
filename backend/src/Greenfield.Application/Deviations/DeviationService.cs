using System.Text;
using Greenfield.Application.Abstractions;
using Greenfield.Application.Common;
using Greenfield.Domain.Deviations;

namespace Greenfield.Application.Deviations;

/// <summary>
/// Application-layer service implementing all deviation management use-cases.
/// Depends only on <see cref="IDeviationRepository"/>; contains no infrastructure concerns.
/// </summary>
public sealed class DeviationService(IDeviationRepository repository) : IDeviationService
{
    // ── Workflow transition table ──────────────────────────────────────────
    private static readonly IReadOnlyDictionary<DeviationStatus, IReadOnlySet<DeviationStatus>>
        ValidTransitions = new Dictionary<DeviationStatus, IReadOnlySet<DeviationStatus>>
        {
            [DeviationStatus.Registered]         = new HashSet<DeviationStatus>([DeviationStatus.UnderAssessment, DeviationStatus.Closed]),
            [DeviationStatus.UnderAssessment]     = new HashSet<DeviationStatus>([DeviationStatus.UnderInvestigation, DeviationStatus.Closed]),
            [DeviationStatus.UnderInvestigation]  = new HashSet<DeviationStatus>([DeviationStatus.CorrectiveAction, DeviationStatus.Closed]),
            [DeviationStatus.CorrectiveAction]    = new HashSet<DeviationStatus>([DeviationStatus.Closed]),
            [DeviationStatus.Closed]              = new HashSet<DeviationStatus>([DeviationStatus.Registered]),
        };

    // ── Query ──────────────────────────────────────────────────────────────

    public async Task<PagedResult<DeviationSummaryDto>> GetDeviationsAsync(
        DeviationListQuery query, CancellationToken ct = default)
    {
        var all = await repository.GetAllAsync(ct).ConfigureAwait(false);

        IEnumerable<Deviation> filtered = all;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLowerInvariant();
            filtered = filtered.Where(d =>
                d.Title.ToLowerInvariant().Contains(term) ||
                d.Description.ToLowerInvariant().Contains(term) ||
                d.ReportedBy.ToLowerInvariant().Contains(term));
        }

        if (query.Status.HasValue)
            filtered = filtered.Where(d => d.Status == query.Status.Value);

        if (query.Severity.HasValue)
            filtered = filtered.Where(d => d.Severity == query.Severity.Value);

        if (query.Category.HasValue)
            filtered = filtered.Where(d => d.Category == query.Category.Value);

        if (!string.IsNullOrWhiteSpace(query.AssignedTo))
            filtered = filtered.Where(d =>
                string.Equals(d.AssignedTo, query.AssignedTo, StringComparison.OrdinalIgnoreCase));

        filtered = query.SortBy.ToLowerInvariant() switch
        {
            "title"     => query.SortDescending ? filtered.OrderByDescending(d => d.Title)     : filtered.OrderBy(d => d.Title),
            "severity"  => query.SortDescending ? filtered.OrderByDescending(d => d.Severity)  : filtered.OrderBy(d => d.Severity),
            "status"    => query.SortDescending ? filtered.OrderByDescending(d => d.Status)    : filtered.OrderBy(d => d.Status),
            "updatedat" => query.SortDescending ? filtered.OrderByDescending(d => d.UpdatedAt) : filtered.OrderBy(d => d.UpdatedAt),
            _           => query.SortDescending ? filtered.OrderByDescending(d => d.CreatedAt) : filtered.OrderBy(d => d.CreatedAt),
        };

        var list = filtered.ToList();
        var totalCount = list.Count;
        var pageSize = Math.Max(1, Math.Min(query.PageSize, 100));
        var page     = Math.Max(1, query.Page);

        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToSummaryDto)
            .ToList();

        return new PagedResult<DeviationSummaryDto>(items, totalCount, page, pageSize);
    }

    public async Task<DeviationDto?> GetDeviationByIdAsync(Guid id, CancellationToken ct = default)
    {
        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        return deviation is null ? null : MapToDto(deviation);
    }

    // ── Commands ───────────────────────────────────────────────────────────

    public async Task<DeviationDto> CreateDeviationAsync(
        CreateDeviationRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ReportedBy);

        var deviation = new Deviation
        {
            Title       = request.Title.Trim(),
            Description = request.Description.Trim(),
            Severity    = request.Severity,
            Category    = request.Category,
            ReportedBy  = request.ReportedBy.Trim(),
            AssignedTo  = request.AssignedTo?.Trim(),
            DueDate     = request.DueDate,
            Tags        = request.Tags is null ? [] : [.. request.Tags],
        };

        deviation.Timeline.Add(new DeviationActivity
        {
            DeviationId  = deviation.Id,
            Type         = ActivityType.Created,
            Description  = $"Deviation registered by {deviation.ReportedBy}.",
            PerformedBy  = deviation.ReportedBy,
            NewStatus    = DeviationStatus.Registered.ToString(),
        });

        var saved = await repository.AddAsync(deviation, ct).ConfigureAwait(false);
        return MapToDto(saved);
    }

    public async Task<DeviationDto?> UpdateDeviationAsync(
        Guid id, UpdateDeviationRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Description);

        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return null;

        deviation.Title            = request.Title.Trim();
        deviation.Description      = request.Description.Trim();
        deviation.Severity         = request.Severity;
        deviation.Category         = request.Category;
        deviation.AssignedTo       = request.AssignedTo?.Trim();
        deviation.DueDate          = request.DueDate;
        deviation.Tags             = request.Tags is null ? [] : [.. request.Tags];
        deviation.RootCause        = request.RootCause;
        deviation.CorrectiveAction = request.CorrectiveAction;
        deviation.ClosureNotes     = request.ClosureNotes;
        deviation.UpdatedAt        = DateTimeOffset.UtcNow;

        deviation.Timeline.Add(new DeviationActivity
        {
            DeviationId = deviation.Id,
            Type        = ActivityType.Updated,
            Description = $"Deviation details updated by {request.UpdatedBy}.",
            PerformedBy = request.UpdatedBy,
        });

        var saved = await repository.UpdateAsync(deviation, ct).ConfigureAwait(false);
        return MapToDto(saved);
    }

    public async Task<bool> DeleteDeviationAsync(Guid id, CancellationToken ct = default)
        => await repository.DeleteAsync(id, ct).ConfigureAwait(false);

    // ── Workflow transition ────────────────────────────────────────────────

    public async Task<(DeviationDto? Deviation, string? Error)> TransitionDeviationAsync(
        Guid id, TransitionDeviationRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PerformedBy);

        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return (null, null);

        if (!ValidTransitions.TryGetValue(deviation.Status, out var allowed) ||
            !allowed.Contains(request.NewStatus))
        {
            var allowedList = ValidTransitions.TryGetValue(deviation.Status, out var a)
                ? string.Join(", ", a)
                : "none";
            return (null, $"Cannot transition from '{deviation.Status}' to '{request.NewStatus}'. " +
                          $"Allowed: {allowedList}.");
        }

        var previous = deviation.Status;
        deviation.Status    = request.NewStatus;
        deviation.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.RootCause        is not null) deviation.RootCause        = request.RootCause;
        if (request.CorrectiveAction is not null) deviation.CorrectiveAction = request.CorrectiveAction;
        if (request.ClosureNotes     is not null) deviation.ClosureNotes     = request.ClosureNotes;

        var desc = new StringBuilder($"Status changed from '{previous}' to '{request.NewStatus}'.");
        if (!string.IsNullOrWhiteSpace(request.Comment))
            desc.Append($" Comment: {request.Comment}");

        deviation.Timeline.Add(new DeviationActivity
        {
            DeviationId    = deviation.Id,
            Type           = ActivityType.StatusChanged,
            Description    = desc.ToString(),
            PerformedBy    = request.PerformedBy,
            PreviousStatus = previous.ToString(),
            NewStatus      = request.NewStatus.ToString(),
        });

        var saved = await repository.UpdateAsync(deviation, ct).ConfigureAwait(false);
        return (MapToDto(saved), null);
    }

    // ── Timeline ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ActivityDto>> GetTimelineAsync(
        Guid id, CancellationToken ct = default)
    {
        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return [];

        return [.. deviation.Timeline.OrderBy(a => a.Timestamp).Select(MapToActivityDto)];
    }

    public async Task<ActivityDto?> AddCommentAsync(
        Guid id, AddCommentRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Comment);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.PerformedBy);

        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return null;

        var activity = new DeviationActivity
        {
            DeviationId = deviation.Id,
            Type        = ActivityType.CommentAdded,
            Description = request.Comment.Trim(),
            PerformedBy = request.PerformedBy.Trim(),
        };

        deviation.Timeline.Add(activity);
        deviation.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(deviation, ct).ConfigureAwait(false);
        return MapToActivityDto(activity);
    }

    // ── Attachments ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<AttachmentDto>?> GetAttachmentsAsync(
        Guid id, CancellationToken ct = default)
    {
        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return null;

        return [.. deviation.Attachments.Select(MapToAttachmentDto)];
    }

    public async Task<AttachmentDto?> AddAttachmentAsync(
        Guid id, UploadAttachmentRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ContentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Base64Content);

        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return null;

        byte[] content;
        try
        {
            content = Convert.FromBase64String(request.Base64Content);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Base64Content is not valid base-64.", nameof(request));
        }

        var attachment = new DeviationAttachment
        {
            DeviationId  = deviation.Id,
            FileName     = request.FileName.Trim(),
            ContentType  = request.ContentType.Trim(),
            SizeBytes    = content.Length,
            UploadedBy   = request.UploadedBy.Trim(),
            Content      = content,
        };

        deviation.Attachments.Add(attachment);
        deviation.UpdatedAt = DateTimeOffset.UtcNow;

        deviation.Timeline.Add(new DeviationActivity
        {
            DeviationId = deviation.Id,
            Type        = ActivityType.AttachmentAdded,
            Description = $"Attachment '{attachment.FileName}' added by {attachment.UploadedBy}.",
            PerformedBy = attachment.UploadedBy,
        });

        await repository.UpdateAsync(deviation, ct).ConfigureAwait(false);
        return MapToAttachmentDto(attachment);
    }

    public async Task<bool> RemoveAttachmentAsync(
        Guid id, Guid attachmentId, CancellationToken ct = default)
    {
        var deviation = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (deviation is null) return false;

        var attachment = deviation.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment is null) return false;

        deviation.Attachments.Remove(attachment);
        deviation.UpdatedAt = DateTimeOffset.UtcNow;

        deviation.Timeline.Add(new DeviationActivity
        {
            DeviationId = deviation.Id,
            Type        = ActivityType.AttachmentRemoved,
            Description = $"Attachment '{attachment.FileName}' removed.",
            PerformedBy = "system",
        });

        await repository.UpdateAsync(deviation, ct).ConfigureAwait(false);
        return true;
    }

    // ── Export ────────────────────────────────────────────────────────────

    public async Task<string> ExportToCsvAsync(
        DeviationListQuery query, CancellationToken ct = default)
    {
        // Reuse filtering logic; fetch everything (no pagination).
        var unlimited = query with { Page = 1, PageSize = int.MaxValue };
        var paged = await GetDeviationsAsync(unlimited, ct).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Status,Severity,Category,ReportedBy,AssignedTo,CreatedAt,UpdatedAt,DueDate,Tags");

        foreach (var d in paged.Items)
        {
            sb.AppendLine(string.Join(',',
                d.Id,
                EscapeCsv(d.Title),
                d.Status,
                d.Severity,
                d.Category,
                EscapeCsv(d.ReportedBy),
                EscapeCsv(d.AssignedTo ?? string.Empty),
                d.CreatedAt.ToString("O"),
                d.UpdatedAt.ToString("O"),
                d.DueDate?.ToString("O") ?? string.Empty,
                EscapeCsv(string.Join(';', d.Tags))));
        }

        return sb.ToString();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────

    private static DeviationDto MapToDto(Deviation d) => new(
        d.Id,
        d.Title,
        d.Description,
        d.Status,
        d.Severity,
        d.Category,
        d.ReportedBy,
        d.AssignedTo,
        d.CreatedAt,
        d.UpdatedAt,
        d.DueDate,
        [.. d.Tags],
        d.RootCause,
        d.CorrectiveAction,
        d.ClosureNotes,
        [.. d.Timeline.OrderBy(a => a.Timestamp).Select(MapToActivityDto)],
        [.. d.Attachments.Select(MapToAttachmentDto)]);

    private static DeviationSummaryDto MapToSummaryDto(Deviation d) => new(
        d.Id,
        d.Title,
        d.Status,
        d.Severity,
        d.Category,
        d.ReportedBy,
        d.AssignedTo,
        d.CreatedAt,
        d.UpdatedAt,
        d.DueDate,
        [.. d.Tags],
        d.Attachments.Count,
        d.Timeline.Count(a => a.Type == ActivityType.CommentAdded));

    private static ActivityDto MapToActivityDto(DeviationActivity a) => new(
        a.Id,
        a.DeviationId,
        a.Type,
        a.Description,
        a.PerformedBy,
        a.Timestamp,
        a.PreviousStatus,
        a.NewStatus);

    private static AttachmentDto MapToAttachmentDto(DeviationAttachment a) => new(
        a.Id,
        a.DeviationId,
        a.FileName,
        a.ContentType,
        a.SizeBytes,
        a.UploadedBy,
        a.UploadedAt);

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
