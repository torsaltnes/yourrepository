using GreenfieldArchitecture.Api.Abstractions;
using GreenfieldArchitecture.Application.Abstractions.CompetenceProfiles;
using GreenfieldArchitecture.Application.CompetenceProfiles.Dtos;
using GreenfieldArchitecture.Application.CompetenceProfiles.Requests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GreenfieldArchitecture.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for the employee competence profile feature.
/// All endpoints require authentication; the authenticated user's profile is resolved via
/// <see cref="ICurrentUserContext"/> — client-submitted employee IDs are never accepted.
/// </summary>
public static class CompetenceProfileEndpoints
{
    public static IEndpointRouteBuilder MapCompetenceProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes
            .MapGroup("/api/me/competence-profile")
            .WithTags("Competence Profile")
            .RequireAuthorization();

        // ── Profile ────────────────────────────────────────────────────────────
        group.MapGet("/", GetProfileAsync)
            .WithName("GetCompetenceProfile")
            .WithSummary("Returns the authenticated employee's grouped competence profile.")
            .Produces<CompetenceProfileDto>(StatusCodes.Status200OK);

        // ── Education ──────────────────────────────────────────────────────────
        group.MapPost("/education", AddEducationAsync)
            .WithName("AddEducation")
            .WithSummary("Adds an education entry to the profile.")
            .Produces<EducationEntryDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/education/{entryId:guid}", UpdateEducationAsync)
            .WithName("UpdateEducation")
            .WithSummary("Updates an existing education entry.")
            .Produces<EducationEntryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/education/{entryId:guid}", DeleteEducationAsync)
            .WithName("DeleteEducation")
            .WithSummary("Removes an education entry.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // ── Certificates ───────────────────────────────────────────────────────
        group.MapPost("/certificates", AddCertificateAsync)
            .WithName("AddCertificate")
            .WithSummary("Adds a certificate entry to the profile.")
            .Produces<CertificateEntryDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/certificates/{entryId:guid}", UpdateCertificateAsync)
            .WithName("UpdateCertificate")
            .WithSummary("Updates an existing certificate entry.")
            .Produces<CertificateEntryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/certificates/{entryId:guid}", DeleteCertificateAsync)
            .WithName("DeleteCertificate")
            .WithSummary("Removes a certificate entry.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        // ── Courses ────────────────────────────────────────────────────────────
        group.MapPost("/courses", AddCourseAsync)
            .WithName("AddCourse")
            .WithSummary("Adds a course entry to the profile.")
            .Produces<CourseEntryDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/courses/{entryId:guid}", UpdateCourseAsync)
            .WithName("UpdateCourse")
            .WithSummary("Updates an existing course entry.")
            .Produces<CourseEntryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapDelete("/courses/{entryId:guid}", DeleteCourseAsync)
            .WithName("DeleteCourse")
            .WithSummary("Removes a course entry.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return routes;
    }

    // ── Profile handlers ──────────────────────────────────────────────────────

    private static async Task<Ok<CompetenceProfileDto>> GetProfileAsync(
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        var dto = await service.GetMyProfileAsync(currentUser.EmployeeId, cancellationToken)
            .ConfigureAwait(false);
        return TypedResults.Ok(dto);
    }

    // ── Education handlers ────────────────────────────────────────────────────

    private static async Task<Results<Created<EducationEntryDto>, BadRequest<ProblemDetails>>> AddEducationAsync(
        CreateEducationRequest request,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await service.AddEducationAsync(currentUser.EmployeeId, request, cancellationToken)
                .ConfigureAwait(false);
            return TypedResults.Created($"/api/me/competence-profile/education/{dto.Id}", dto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(Problem(ex.Message));
        }
    }

    private static async Task<Results<Ok<EducationEntryDto>, NotFound, BadRequest<ProblemDetails>>> UpdateEducationAsync(
        Guid entryId,
        UpdateEducationRequest request,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await service.UpdateEducationAsync(currentUser.EmployeeId, entryId, request, cancellationToken)
                .ConfigureAwait(false);
            return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(Problem(ex.Message));
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteEducationAsync(
        Guid entryId,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteEducationAsync(currentUser.EmployeeId, entryId, cancellationToken)
            .ConfigureAwait(false);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    // ── Certificate handlers ──────────────────────────────────────────────────

    private static async Task<Results<Created<CertificateEntryDto>, BadRequest<ProblemDetails>>> AddCertificateAsync(
        CreateCertificateRequest request,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await service.AddCertificateAsync(currentUser.EmployeeId, request, cancellationToken)
                .ConfigureAwait(false);
            return TypedResults.Created($"/api/me/competence-profile/certificates/{dto.Id}", dto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(Problem(ex.Message));
        }
    }

    private static async Task<Results<Ok<CertificateEntryDto>, NotFound, BadRequest<ProblemDetails>>> UpdateCertificateAsync(
        Guid entryId,
        UpdateCertificateRequest request,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await service.UpdateCertificateAsync(currentUser.EmployeeId, entryId, request, cancellationToken)
                .ConfigureAwait(false);
            return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(Problem(ex.Message));
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteCertificateAsync(
        Guid entryId,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteCertificateAsync(currentUser.EmployeeId, entryId, cancellationToken)
            .ConfigureAwait(false);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    // ── Course handlers ───────────────────────────────────────────────────────

    private static async Task<Results<Created<CourseEntryDto>, BadRequest<ProblemDetails>>> AddCourseAsync(
        CreateCourseRequest request,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await service.AddCourseAsync(currentUser.EmployeeId, request, cancellationToken)
                .ConfigureAwait(false);
            return TypedResults.Created($"/api/me/competence-profile/courses/{dto.Id}", dto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(Problem(ex.Message));
        }
    }

    private static async Task<Results<Ok<CourseEntryDto>, NotFound, BadRequest<ProblemDetails>>> UpdateCourseAsync(
        Guid entryId,
        UpdateCourseRequest request,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await service.UpdateCourseAsync(currentUser.EmployeeId, entryId, request, cancellationToken)
                .ConfigureAwait(false);
            return dto is null ? TypedResults.NotFound() : TypedResults.Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(Problem(ex.Message));
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteCourseAsync(
        Guid entryId,
        ICurrentUserContext currentUser,
        ICompetenceProfileService service,
        CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteCourseAsync(currentUser.EmployeeId, entryId, cancellationToken)
            .ConfigureAwait(false);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ProblemDetails Problem(string detail) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Bad Request",
        Detail = detail,
    };
}
