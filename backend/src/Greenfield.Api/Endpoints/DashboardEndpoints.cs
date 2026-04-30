using Greenfield.Application.Abstractions;
using Greenfield.Application.Dashboard;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Greenfield.Api.Endpoints;

/// <summary>
/// Registers all <c>/api/dashboard</c> endpoints using Minimal API route groups.
/// </summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/dashboard")
            .WithTags("Dashboard");

        group.MapGet("/summary", GetSummary)
             .WithName("GetDashboardSummary");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────

    private static async Task<Ok<DashboardSummaryDto>> GetSummary(
        IDashboardService service,
        CancellationToken ct)
    {
        var summary = await service.GetSummaryAsync(ct);
        return TypedResults.Ok(summary);
    }
}
