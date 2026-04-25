using DeviationManagement.Application.DTOs;

namespace DeviationManagement.Application.Abstractions.Services;

public interface IDeviationService
{
    Task<IReadOnlyCollection<DeviationDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeviationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(DeviationDto? Dto, Dictionary<string, string[]>? ValidationErrors)> CreateAsync(SaveDeviationRequest request, CancellationToken cancellationToken = default);
    Task<(DeviationDto? Dto, bool NotFound, Dictionary<string, string[]>? ValidationErrors)> UpdateAsync(Guid id, SaveDeviationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
