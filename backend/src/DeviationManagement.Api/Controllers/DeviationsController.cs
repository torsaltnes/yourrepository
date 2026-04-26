using System.Security.Claims;
using DeviationManagement.Api.Contracts.Requests;
using DeviationManagement.Api.Mapping;
using DeviationManagement.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeviationManagement.Api.Controllers;

[ApiController]
[Route("api/deviations")]
[Authorize]
public sealed class DeviationsController(IDeviationService deviationService) : ControllerBase
{
    /// <summary>
    /// The authenticated caller's subject identifier (JWT 'sub' claim).
    /// Used for object-level ownership checks on every request.
    /// </summary>
    private string OwnerId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await deviationService.GetAllAsync(OwnerId, cancellationToken);
        return Ok(items.Select(DeviationApiMapper.ToApiResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await deviationService.GetByIdAsync(id, OwnerId, cancellationToken);
        if (dto is null)
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Deviation with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(DeviationApiMapper.ToApiResponse(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveDeviationApiRequest request, CancellationToken cancellationToken)
    {
        var appRequest = DeviationApiMapper.ToApplicationRequest(request);
        var (dto, validationErrors) = await deviationService.CreateAsync(appRequest, OwnerId, cancellationToken);

        if (validationErrors is not null)
            return ValidationProblem(new ValidationProblemDetails(validationErrors));

        var response = DeviationApiMapper.ToApiResponse(dto!);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveDeviationApiRequest request, CancellationToken cancellationToken)
    {
        var appRequest = DeviationApiMapper.ToApplicationRequest(request);
        var (dto, notFound, forbidden, validationErrors) =
            await deviationService.UpdateAsync(id, appRequest, OwnerId, cancellationToken);

        if (validationErrors is not null)
            return ValidationProblem(new ValidationProblemDetails(validationErrors));

        if (forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "You do not have permission to modify this deviation.",
                Status = StatusCodes.Status403Forbidden
            });

        if (notFound)
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Deviation with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });

        return Ok(DeviationApiMapper.ToApiResponse(dto!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var (deleted, forbidden) = await deviationService.DeleteAsync(id, OwnerId, cancellationToken);

        if (forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "You do not have permission to delete this deviation.",
                Status = StatusCodes.Status403Forbidden
            });

        if (!deleted)
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Deviation with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });

        return NoContent();
    }
}
