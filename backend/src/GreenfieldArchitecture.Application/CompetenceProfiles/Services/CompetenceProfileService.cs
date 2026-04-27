using GreenfieldArchitecture.Application.Abstractions.CompetenceProfiles;
using GreenfieldArchitecture.Application.CompetenceProfiles.Dtos;
using GreenfieldArchitecture.Application.CompetenceProfiles.Requests;
using GreenfieldArchitecture.Domain.CompetenceProfiles;
using Microsoft.Extensions.Logging;

namespace GreenfieldArchitecture.Application.CompetenceProfiles.Services;

/// <summary>
/// Orchestrates competence-profile use-cases: get-or-create, CRUD on each entry type, DTO mapping.
/// </summary>
public sealed class CompetenceProfileService(
    ICompetenceProfileRepository repository,
    TimeProvider timeProvider,
    ILogger<CompetenceProfileService> logger) : ICompetenceProfileService
{
    // ── Profile ───────────────────────────────────────────────────────────────

    public async Task<CompetenceProfileDto> GetMyProfileAsync(
        string employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));

        logger.LogInformation("Fetching competence profile for employee {EmployeeId}", employeeId);

        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        return profile is null
            ? new CompetenceProfileDto([], [], [])
            : MapToDto(profile);
    }

    // ── Education ─────────────────────────────────────────────────────────────

    public async Task<EducationEntryDto> AddEducationAsync(
        string employeeId,
        CreateEducationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Degree, nameof(request.Degree));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Institution, nameof(request.Institution));

        var now = timeProvider.GetUtcNow();
        var profile = await GetOrCreateProfileAsync(employeeId, cancellationToken).ConfigureAwait(false);
        var entry = EducationEntry.Create(request.Degree, request.Institution, request.GraduationYear, request.Notes, now);

        profile.AddEducation(entry);

        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Added education entry {EntryId} for employee {EmployeeId}", entry.Id, employeeId);

        return ToDto(entry);
    }

    public async Task<EducationEntryDto?> UpdateEducationAsync(
        string employeeId,
        Guid entryId,
        UpdateEducationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Degree, nameof(request.Degree));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Institution, nameof(request.Institution));

        var now = timeProvider.GetUtcNow();
        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            logger.LogWarning("Education update failed: no profile for employee {EmployeeId}", employeeId);
            return null;
        }

        var existing = profile.Education.FirstOrDefault(e => e.Id == entryId);
        if (existing is null)
        {
            logger.LogWarning("Education entry {EntryId} not found for employee {EmployeeId}", entryId, employeeId);
            return null;
        }

        var updated = existing.Update(request.Degree, request.Institution, request.GraduationYear, request.Notes, now);
        profile.UpdateEducation(updated);

        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated education entry {EntryId} for employee {EmployeeId}", entryId, employeeId);

        return ToDto(updated);
    }

    public async Task<bool> DeleteEducationAsync(
        string employeeId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));

        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null) return false;

        var deleted = profile.DeleteEducation(entryId);
        if (deleted)
            await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Delete education {EntryId} for {EmployeeId}: {Result}", entryId, employeeId, deleted);

        return deleted;
    }

    // ── Certificates ──────────────────────────────────────────────────────────

    public async Task<CertificateEntryDto> AddCertificateAsync(
        string employeeId,
        CreateCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name, nameof(request.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IssuingOrganization, nameof(request.IssuingOrganization));

        var now = timeProvider.GetUtcNow();
        var profile = await GetOrCreateProfileAsync(employeeId, cancellationToken).ConfigureAwait(false);
        var entry = CertificateEntry.Create(request.Name, request.IssuingOrganization, request.IssueDate, request.ExpirationDate, now);

        profile.AddCertificate(entry);

        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Added certificate entry {EntryId} for employee {EmployeeId}", entry.Id, employeeId);

        return ToDto(entry);
    }

    public async Task<CertificateEntryDto?> UpdateCertificateAsync(
        string employeeId,
        Guid entryId,
        UpdateCertificateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name, nameof(request.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.IssuingOrganization, nameof(request.IssuingOrganization));

        var now = timeProvider.GetUtcNow();
        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            logger.LogWarning("Certificate update failed: no profile for employee {EmployeeId}", employeeId);
            return null;
        }

        var existing = profile.Certificates.FirstOrDefault(c => c.Id == entryId);
        if (existing is null)
        {
            logger.LogWarning("Certificate entry {EntryId} not found for employee {EmployeeId}", entryId, employeeId);
            return null;
        }

        var updated = existing.Update(request.Name, request.IssuingOrganization, request.IssueDate, request.ExpirationDate, now);
        profile.UpdateCertificate(updated);

        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated certificate entry {EntryId} for employee {EmployeeId}", entryId, employeeId);

        return ToDto(updated);
    }

    public async Task<bool> DeleteCertificateAsync(
        string employeeId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));

        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null) return false;

        var deleted = profile.DeleteCertificate(entryId);
        if (deleted)
            await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Delete certificate {EntryId} for {EmployeeId}: {Result}", entryId, employeeId, deleted);

        return deleted;
    }

    // ── Courses ───────────────────────────────────────────────────────────────

    public async Task<CourseEntryDto> AddCourseAsync(
        string employeeId,
        CreateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name, nameof(request.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Provider, nameof(request.Provider));

        var now = timeProvider.GetUtcNow();
        var profile = await GetOrCreateProfileAsync(employeeId, cancellationToken).ConfigureAwait(false);
        var entry = CourseEntry.Create(request.Name, request.Provider, request.CompletionDate, request.SkillsAcquired, now);

        profile.AddCourse(entry);

        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Added course entry {EntryId} for employee {EmployeeId}", entry.Id, employeeId);

        return ToDto(entry);
    }

    public async Task<CourseEntryDto?> UpdateCourseAsync(
        string employeeId,
        Guid entryId,
        UpdateCourseRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Name, nameof(request.Name));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Provider, nameof(request.Provider));

        var now = timeProvider.GetUtcNow();
        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            logger.LogWarning("Course update failed: no profile for employee {EmployeeId}", employeeId);
            return null;
        }

        var existing = profile.Courses.FirstOrDefault(c => c.Id == entryId);
        if (existing is null)
        {
            logger.LogWarning("Course entry {EntryId} not found for employee {EmployeeId}", entryId, employeeId);
            return null;
        }

        var updated = existing.Update(request.Name, request.Provider, request.CompletionDate, request.SkillsAcquired, now);
        profile.UpdateCourse(updated);

        await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated course entry {EntryId} for employee {EmployeeId}", entryId, employeeId);

        return ToDto(updated);
    }

    public async Task<bool> DeleteCourseAsync(
        string employeeId,
        Guid entryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));

        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null) return false;

        var deleted = profile.DeleteCourse(entryId);
        if (deleted)
            await repository.SaveAsync(profile, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Delete course {EntryId} for {EmployeeId}: {Result}", entryId, employeeId, deleted);

        return deleted;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<CompetenceProfile> GetOrCreateProfileAsync(
        string employeeId,
        CancellationToken cancellationToken)
    {
        var profile = await repository.GetByEmployeeIdAsync(employeeId, cancellationToken)
            .ConfigureAwait(false);

        return profile ?? new CompetenceProfile(employeeId);
    }

    private static CompetenceProfileDto MapToDto(CompetenceProfile profile) => new(
        Education: [.. profile.Education
            .OrderByDescending(e => e.GraduationYear)
            .Select(ToDto)],
        Certificates: [.. profile.Certificates
            .OrderByDescending(c => c.IssueDate)
            .Select(ToDto)],
        Courses: [.. profile.Courses
            .OrderByDescending(c => c.CompletionDate)
            .Select(ToDto)]);

    private static EducationEntryDto ToDto(EducationEntry e) => new(
        e.Id, e.Degree, e.Institution, e.GraduationYear, e.Notes, e.CreatedUtc, e.UpdatedUtc);

    private static CertificateEntryDto ToDto(CertificateEntry c) => new(
        c.Id, c.Name, c.IssuingOrganization, c.IssueDate, c.ExpirationDate, c.CreatedUtc, c.UpdatedUtc);

    private static CourseEntryDto ToDto(CourseEntry c) => new(
        c.Id, c.Name, c.Provider, c.CompletionDate, c.SkillsAcquired, c.CreatedUtc, c.UpdatedUtc);
}
