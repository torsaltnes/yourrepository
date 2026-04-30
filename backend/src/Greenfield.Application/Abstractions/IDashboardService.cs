using Greenfield.Application.Dashboard;

namespace Greenfield.Application.Abstractions;

/// <summary>Computes aggregated metrics for the dashboard summary endpoint.</summary>
public interface IDashboardService
{
    /// <summary>Returns a point-in-time summary of all deviations in the system.</summary>
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct = default);
}
