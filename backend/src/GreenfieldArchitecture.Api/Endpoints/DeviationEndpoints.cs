using GreenfieldArchitecture.Api.Filters;
using GreenfieldArchitecture.Application.Deviations.Abstractions;
using GreenfieldArchitecture.Application.Deviations.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GreenfieldArchitecture.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the deviations domain slice.
/// </summary>
public static class DeviationEndpoints
{
    public static IEndpointRouteBuilder MapDeviationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/deviations").WithTags("Deviations");

        // Read endpoints are intentionally left open (shared, non-sensitive data).
        group.MapGet("/", GetAllAsync)
            .WithName("GetAllDeviations")
            .WithSummary("Returns all deviations ordered by most recently updated.");

        group.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetDeviationById")
            .WithSummary("Returns a single deviation by ID.");

        // Mutation endpoints require a verified caller identity to prevent
        // anonymous tampering with shared deviation records (OWASP A01).
        group.MapPost("/", CreateAsync)
            .WithName("CreateDeviation")
            .WithSummary("Creates a new deviation.")
            .AddEndpointFilter<RequireUserIdentityFilter>();

        group.MapPut("/{id:guid}", UpdateAsync)
            .WithName("UpdateDeviation")
            .WithSummary("Updates an existing deviation.")
            .AddEndpointFilter<RequireUserIdentityFilter>();

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithName("DeleteDeviation")
            .WithSummary("Deletes a deviation.")
            .AddEndpointFilter<RequireUserIdentityFilter>();

        return routes;
    }

    private static async Task<Ok<IReadOnlyList<DeviationDto>>> GetAllAsync(
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<DeviationDto>, NotFound>> GetByIdAsync(
        Guid id,
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<DeviationDto>, BadRequest<string>>> CreateAsync(
        CreateDeviationRequest request,
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
            return TypedResults.Created($"/api/deviations/{result.Id}", result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<DeviationDto>, NotFound, BadRequest<string>>> UpdateAsync(
        Guid id,
        UpdateDeviationRequest request,
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false);
            return result is not null
                ? TypedResults.Ok(result)
                : TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id,
        IDeviationService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
