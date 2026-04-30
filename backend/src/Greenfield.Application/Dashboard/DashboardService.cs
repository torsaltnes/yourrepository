using Greenfield.Application.Abstractions;
using Greenfield.Domain.Deviations;

namespace Greenfield.Application.Dashboard;

/// <summary>
/// Application-layer service that computes the dashboard summary from the
/// live deviations store.  Depends only on <see cref="IDeviationRepository"/>.
/// </summary>
public sealed class DashboardService(IDeviationRepository repository) : IDashboardService
{
    /// <summary>Number of recent deviations included in the summary.</summary>
    private const int RecentDeviationCount = 5;

    /// <summary>Number of calendar months included in the monthly trend.</summary>
    private const int TrendMonthCount = 6;

    /// <inheritdoc/>
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default)
    {
        var all = await repository.GetAllAsync(ct).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        var totalDeviations = all.Count;

        var openDeviations = all.Count(d => d.Status != DeviationStatus.Closed);

        var overdueDeviations = all.Count(d =>
            d.Status != DeviationStatus.Closed &&
            d.DueDate.HasValue &&
            d.DueDate.Value < now);

        // ── Breakdowns ────────────────────────────────────────────────────
        var byStatus   = BuildBreakdown(all, d => d.Status.ToString());
        var bySeverity = BuildBreakdown(all, d => d.Severity.ToString());
        var byCategory = BuildBreakdown(all, d => d.Category.ToString());

        // ── Monthly trend (oldest → newest, last TrendMonthCount months) ──
        var monthlyTrend = BuildMonthlyTrend(all, now);

        // ── Recent deviations (newest UpdatedAt first, capped at limit) ───
        IReadOnlyList<Deviations.DeviationSummaryDto> recentDeviations = [..
            all.OrderByDescending(d => d.UpdatedAt)
               .Take(RecentDeviationCount)
               .Select(MapToSummaryDto)];

        return new DashboardSummaryDto(
            totalDeviations,
            openDeviations,
            overdueDeviations,
            byStatus,
            bySeverity,
            byCategory,
            monthlyTrend,
            recentDeviations);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private static IReadOnlyDictionary<string, int> BuildBreakdown(
        IReadOnlyList<Deviation> deviations,
        Func<Deviation, string> keySelector)
    {
        var dict = new Dictionary<string, int>();
        foreach (var d in deviations)
        {
            var key = keySelector(d);
            dict[key] = dict.TryGetValue(key, out var existing) ? existing + 1 : 1;
        }
        return dict;
    }

    private static IReadOnlyList<MonthlyTrendPoint> BuildMonthlyTrend(
        IReadOnlyList<Deviation> deviations,
        DateTimeOffset now)
    {
        // Build the ordered list of the last TrendMonthCount calendar months.
        var months = Enumerable.Range(0, TrendMonthCount)
            .Select(offset => now.AddMonths(-(TrendMonthCount - 1) + offset))
            .Select(dto => (dto.Year, dto.Month))
            .ToList();

        return [.. months.Select(m =>
        {
            var count = deviations.Count(d =>
                d.CreatedAt.Year == m.Year && d.CreatedAt.Month == m.Month);

            return new MonthlyTrendPoint($"{m.Year:D4}-{m.Month:D2}", count);
        })];
    }

    private static Deviations.DeviationSummaryDto MapToSummaryDto(Deviation d) => new(
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
}
