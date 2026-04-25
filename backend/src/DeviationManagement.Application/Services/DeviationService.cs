using DeviationManagement.Application.Abstractions.Persistence;
using DeviationManagement.Application.Abstractions.Services;
using DeviationManagement.Application.DTOs;
using DeviationManagement.Application.Validation;
using DeviationManagement.Domain.Entities;

namespace DeviationManagement.Application.Services;

public sealed class DeviationService(
    IDeviationRepository repository,
    DeviationValidator validator) : IDeviationService
{
    public async Task<IReadOnlyCollection<DeviationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await repository.GetAllAsync(cancellationToken);
        return [.. entities.Select(ToDto)];
    }

    public async Task<DeviationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<(DeviationDto? Dto, Dictionary<string, string[]>? ValidationErrors)> CreateAsync(
        SaveDeviationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = validator.ValidateForSave(request);
        if (validationErrors is not null)
            return (null, validationErrors);

        var now = DateTimeOffset.UtcNow;
        var entity = new Deviation(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Severity,
            request.Status,
            request.ReportedBy,
            request.OccurredAt,
            now,
            now);

        var created = await repository.CreateAsync(entity, cancellationToken);
        return (ToDto(created), null);
    }

    public async Task<(DeviationDto? Dto, bool NotFound, Dictionary<string, string[]>? ValidationErrors)> UpdateAsync(
        Guid id,
        SaveDeviationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = validator.ValidateForSave(request);
        if (validationErrors is not null)
            return (null, false, validationErrors);

        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return (null, true, null);

        existing.Update(
            request.Title,
            request.Description,
            request.Severity,
            request.Status,
            request.ReportedBy,
            request.OccurredAt);

        var updated = await repository.UpdateAsync(existing, cancellationToken);
        return updated is null ? (null, true, null) : (ToDto(updated), false, null);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await repository.DeleteAsync(id, cancellationToken);
    }

    private static DeviationDto ToDto(Deviation entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.Severity,
        entity.Status,
        entity.ReportedBy,
        entity.OccurredAt,
        entity.CreatedAt,
        entity.UpdatedAt);
}
