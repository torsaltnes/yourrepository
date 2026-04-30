using Greenfield.Application.Deviations;

namespace Greenfield.Application.Dashboard;

/// <summary>
/// Aggregated summary returned by <c>GET /api/dashboard/summary</c>.
/// All counts are derived from the live deviations store at the time of the request.
/// </summary>
/// <param name="TotalDeviations">Total number of deviations in the system.</param>
/// <param name="OpenDeviations">Deviations whose status is not <c>Closed</c>.</param>
/// <param name="OverdueDeviations">Open deviations whose <c>DueDate</c> is in the past.</param>
/// <param name="ByStatus">Deviation counts keyed by status name (e.g. <c>"Registered"</c>).</param>
/// <param name="BySeverity">Deviation counts keyed by severity name (e.g. <c>"Critical"</c>).</param>
/// <param name="ByCategory">Deviation counts keyed by category name (e.g. <c>"Safety"</c>).</param>
/// <param name="MonthlyTrend">Creation counts for the last six calendar months, oldest first.</param>
/// <param name="RecentDeviations">The five most-recently-updated deviations.</param>
public sealed record DashboardSummaryDto(
    int TotalDeviations,
    int OpenDeviations,
    int OverdueDeviations,
    IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyDictionary<string, int> BySeverity,
    IReadOnlyDictionary<string, int> ByCategory,
    IReadOnlyList<MonthlyTrendPoint> MonthlyTrend,
    IReadOnlyList<DeviationSummaryDto> RecentDeviations);
