using GreenfieldArchitecture.Domain.CompetenceProfiles;

namespace GreenfieldArchitecture.Application.Abstractions.CompetenceProfiles;

/// <summary>
/// Persistence abstraction for the competence profile aggregate.
/// Infrastructure provides a concrete implementation.
/// </summary>
public interface ICompetenceProfileRepository
{
    /// <summary>Returns the profile for <paramref name="employeeId"/>, or <c>null</c> if none exists yet.</summary>
    Task<CompetenceProfile?> GetByEmployeeIdAsync(
        string employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts or replaces the profile for the owning employee.</summary>
    Task SaveAsync(
        CompetenceProfile profile,
        CancellationToken cancellationToken = default);
}
