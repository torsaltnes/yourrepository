using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Domain.Entities;

public sealed class Deviation
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DeviationSeverity Severity { get; private set; }
    public DeviationStatus Status { get; private set; }
    public string ReportedBy { get; private set; } = string.Empty;
    public DateTimeOffset ReportedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// The subject identifier (JWT 'sub' claim) of the user who created this deviation.
    /// Used for object-level ownership checks.
    /// </summary>
    public string OwnerId { get; private set; } = string.Empty;

    // EF / serializer constructor
    private Deviation() { }

    public Deviation(
        Guid id,
        string title,
        string description,
        DeviationSeverity severity,
        DeviationStatus status,
        string reportedBy,
        DateTimeOffset reportedAt,
        DateTimeOffset updatedAt,
        string ownerId)
    {
        Id = id;
        Title = title;
        Description = description;
        Severity = severity;
        Status = status;
        ReportedBy = reportedBy;
        ReportedAt = reportedAt;
        UpdatedAt = updatedAt;
        OwnerId = ownerId;
    }

    public void Update(
        string title,
        string description,
        DeviationSeverity severity,
        DeviationStatus status,
        string reportedBy,
        DateTimeOffset reportedAt)
    {
        Title = title;
        Description = description;
        Severity = severity;
        Status = status;
        ReportedBy = reportedBy;
        ReportedAt = reportedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
