using GreenfieldArchitecture.Application.CompetenceProfiles.Dtos;
using GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

namespace GreenfieldArchitecture.Application.Abstractions.CompetenceProfiles;

/// <summary>
/// Use-case abstraction for the competence profile domain.
/// All operations are scoped to the authenticated employee identified by <c>employeeId</c>.
/// </summary>
public interface ICompetenceProfileService
{
    // ── Profile ───────────────────────────────────────────────────────────────

    Task<CompetenceProfileDto> GetMyProfileAsync(
        string employeeId,
        CancellationToken cancellationToken = default);

    // ── Education ─────────────────────────────────────────────────────────────

    Task<EducationEntryDto> AddEducationAsync(
        string employeeId,
        CreateEducationRequest request,
        CancellationToken cancellationToken = default);

    /// <returns>Updated DTO, or <c>null</c> if the entry was not found.</returns>
    Task<EducationEntryDto?> UpdateEducationAsync(
        string employeeId,
        Guid entryId,
        UpdateEducationRequest request,
        CancellationToken cancellationToken = default);

    /// <returns><c>true</c> when deleted; <c>false</c> when not found.</returns>
    Task<bool> DeleteEducationAsync(
        string employeeId,
        Guid entryId,
        CancellationToken cancellationToken = default);

    // ── Certificates ──────────────────────────────────────────────────────────

    Task<CertificateEntryDto> AddCertificateAsync(
        string employeeId,
        CreateCertificateRequest request,
        CancellationToken cancellationToken = default);

    /// <returns>Updated DTO, or <c>null</c> if the entry was not found.</returns>
    Task<CertificateEntryDto?> UpdateCertificateAsync(
        string employeeId,
        Guid entryId,
        UpdateCertificateRequest request,
        CancellationToken cancellationToken = default);

    /// <returns><c>true</c> when deleted; <c>false</c> when not found.</returns>
    Task<bool> DeleteCertificateAsync(
        string employeeId,
        Guid entryId,
        CancellationToken cancellationToken = default);

    // ── Courses ───────────────────────────────────────────────────────────────

    Task<CourseEntryDto> AddCourseAsync(
        string employeeId,
        CreateCourseRequest request,
        CancellationToken cancellationToken = default);

    /// <returns>Updated DTO, or <c>null</c> if the entry was not found.</returns>
    Task<CourseEntryDto?> UpdateCourseAsync(
        string employeeId,
        Guid entryId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken = default);

    /// <returns><c>true</c> when deleted; <c>false</c> when not found.</returns>
    Task<bool> DeleteCourseAsync(
        string employeeId,
        Guid entryId,
        CancellationToken cancellationToken = default);
}
