using DeviationManagement.Application.DTOs;

namespace DeviationManagement.Application.Abstractions.Services;

public interface IDeviationService
{
    /// <summary>Returns only deviations owned by <paramref name="ownerId"/>.</summary>
    Task<IReadOnlyCollection<DeviationDto>> GetAllAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the deviation if it exists AND belongs to <paramref name="ownerId"/>;
    /// otherwise returns <c>null</c> (treats "not found" and "forbidden" identically to
    /// avoid information leakage about other users' records).
    /// </summary>
    Task<DeviationDto?> GetByIdAsync(Guid id, string ownerId, CancellationToken cancellationToken = default);

    Task<(DeviationDto? Dto, Dictionary<string, string[]>? ValidationErrors)> CreateAsync(
        SaveDeviationRequest request, string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>Forbidden</c> is <c>true</c> when the record exists but belongs to a different owner.
    /// </summary>
    Task<(DeviationDto? Dto, bool NotFound, bool Forbidden, Dictionary<string, string[]>? ValidationErrors)> UpdateAsync(
        Guid id, SaveDeviationRequest request, string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>Deleted</c> is <c>false</c> and <c>Forbidden</c> is <c>true</c> when the record
    /// exists but belongs to a different owner.
    /// </summary>
    Task<(bool Deleted, bool Forbidden)> DeleteAsync(Guid id, string ownerId, CancellationToken cancellationToken = default);
}
