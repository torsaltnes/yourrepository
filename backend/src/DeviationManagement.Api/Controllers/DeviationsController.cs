using DeviationManagement.Api.Contracts.Requests;
using DeviationManagement.Api.Mapping;
using DeviationManagement.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeviationManagement.Api.Controllers;

[ApiController]
[Route("api/deviations")]
public sealed class DeviationsController(IDeviationService deviationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await deviationService.GetAllAsync(cancellationToken);
        return Ok(items.Select(DeviationApiMapper.ToApiResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await deviationService.GetByIdAsync(id, cancellationToken);
        if (dto is null)
            return NotFound();

        return Ok(DeviationApiMapper.ToApiResponse(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveDeviationApiRequest request, CancellationToken cancellationToken)
    {
        var appRequest = DeviationApiMapper.ToApplicationRequest(request);
        var (dto, validationErrors) = await deviationService.CreateAsync(appRequest, cancellationToken);

        if (validationErrors is not null)
            return ValidationProblem(new ValidationProblemDetails(validationErrors));

        var response = DeviationApiMapper.ToApiResponse(dto!);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveDeviationApiRequest request, CancellationToken cancellationToken)
    {
        var appRequest = DeviationApiMapper.ToApplicationRequest(request);
        var (dto, notFound, validationErrors) = await deviationService.UpdateAsync(id, appRequest, cancellationToken);

        if (validationErrors is not null)
            return ValidationProblem(new ValidationProblemDetails(validationErrors));

        if (notFound)
            return NotFound();

        return Ok(DeviationApiMapper.ToApiResponse(dto!));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await deviationService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
