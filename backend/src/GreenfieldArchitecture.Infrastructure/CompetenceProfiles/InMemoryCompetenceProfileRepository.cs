using System.Collections.Concurrent;
using GreenfieldArchitecture.Application.Abstractions.CompetenceProfiles;
using GreenfieldArchitecture.Domain.CompetenceProfiles;

namespace GreenfieldArchitecture.Infrastructure.CompetenceProfiles;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ICompetenceProfileRepository"/>.
/// Registered as a singleton; data persists only for the lifetime of the process.
/// </summary>
/// <remarks>
/// Replace with a database-backed implementation before deploying to production.
/// </remarks>
public sealed class InMemoryCompetenceProfileRepository : ICompetenceProfileRepository
{
    private readonly ConcurrentDictionary<string, CompetenceProfile> _store =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<CompetenceProfile?> GetByEmployeeIdAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(employeeId, out var profile);
        return Task.FromResult(profile);
    }

    public Task SaveAsync(
        CompetenceProfile profile,
        CancellationToken cancellationToken = default)
    {
        _store[profile.EmployeeId] = profile;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all profiles. Used only by integration-test infrastructure.
    /// </summary>
    public void Clear() => _store.Clear();
}
