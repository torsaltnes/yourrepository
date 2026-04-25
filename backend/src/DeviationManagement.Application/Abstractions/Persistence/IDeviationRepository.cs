using DeviationManagement.Domain.Entities;

namespace DeviationManagement.Application.Abstractions.Persistence;

public interface IDeviationRepository
{
    Task<IReadOnlyCollection<Deviation>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Deviation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Deviation> CreateAsync(Deviation entity, CancellationToken cancellationToken = default);
    Task<Deviation?> UpdateAsync(Deviation entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
