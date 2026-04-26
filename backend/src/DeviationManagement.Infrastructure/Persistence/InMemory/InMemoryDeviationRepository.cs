using System.Collections.Concurrent;
using DeviationManagement.Application.Abstractions.Persistence;
using DeviationManagement.Domain.Entities;

namespace DeviationManagement.Infrastructure.Persistence.InMemory;

public sealed class InMemoryDeviationRepository : IDeviationRepository
{
    private readonly ConcurrentDictionary<Guid, Deviation> _store = new();

    public Task<IReadOnlyCollection<Deviation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Deviation> result = [.. _store.Values.OrderByDescending(d => d.ReportedAt)];
        return Task.FromResult(result);
    }

    public Task<Deviation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Deviation> CreateAsync(Deviation entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<Deviation?> UpdateAsync(Deviation entity, CancellationToken cancellationToken = default)
    {
        if (!_store.ContainsKey(entity.Id))
            return Task.FromResult<Deviation?>(null);

        _store[entity.Id] = Clone(entity);
        return Task.FromResult<Deviation?>(_store[entity.Id]);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var removed = _store.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    private static Deviation Clone(Deviation source) => new(
        source.Id,
        source.Title,
        source.Description,
        source.Severity,
        source.Status,
        source.ReportedBy,
        source.ReportedAt,
        source.UpdatedAt,
        source.OwnerId);
}
