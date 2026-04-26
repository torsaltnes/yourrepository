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
    public async Task<IReadOnlyCollection<DeviationDto>> GetAllAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await repository.GetAllAsync(cancellationToken);
        return [.. entities.Where(e => e.OwnerId == ownerId).Select(ToDto)];
    }

    public async Task<DeviationDto?> GetByIdAsync(
        Guid id,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        // Return null for both "not found" and "different owner" to prevent information leakage
        if (entity is null || entity.OwnerId != ownerId)
            return null;

        return ToDto(entity);
    }

    public async Task<(DeviationDto? Dto, Dictionary<string, string[]>? ValidationErrors)> CreateAsync(
        SaveDeviationRequest request,
        string ownerId,
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
            request.ReportedAt,
            now,
            ownerId);

        var created = await repository.CreateAsync(entity, cancellationToken);
        return (ToDto(created), null);
    }

    public async Task<(DeviationDto? Dto, bool NotFound, bool Forbidden, Dictionary<string, string[]>? ValidationErrors)> UpdateAsync(
        Guid id,
        SaveDeviationRequest request,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = validator.ValidateForSave(request);
        if (validationErrors is not null)
            return (null, false, false, validationErrors);

        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
            return (null, true, false, null);

        if (existing.OwnerId != ownerId)
            return (null, false, true, null);

        existing.Update(
            request.Title,
            request.Description,
            request.Severity,
            request.Status,
            request.ReportedBy,
            request.ReportedAt);

        var updated = await repository.UpdateAsync(existing, cancellationToken);
        return updated is null ? (null, true, false, null) : (ToDto(updated), false, false, null);
    }

    public async Task<(bool Deleted, bool Forbidden)> DeleteAsync(
        Guid id,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return (false, false);

        if (entity.OwnerId != ownerId)
            return (false, true);

        var deleted = await repository.DeleteAsync(id, cancellationToken);
        return (deleted, false);
    }

    private static DeviationDto ToDto(Deviation entity) => new(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.Severity,
        entity.Status,
        entity.ReportedBy,
        entity.ReportedAt,
        entity.UpdatedAt);
}
