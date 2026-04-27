namespace GreenfieldArchitecture.Application.CompetenceProfiles.Dtos;

// ── Grouped profile read model ────────────────────────────────────────────────

/// <summary>Grouped read model returned by GET /api/me/competence-profile.</summary>
public sealed record CompetenceProfileDto(
    IReadOnlyList<EducationEntryDto> Education,
    IReadOnlyList<CertificateEntryDto> Certificates,
    IReadOnlyList<CourseEntryDto> Courses);

// ── Entry DTOs ────────────────────────────────────────────────────────────────

public sealed record EducationEntryDto(
    Guid Id,
    string Degree,
    string Institution,
    int GraduationYear,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record CertificateEntryDto(
    Guid Id,
    string Name,
    string IssuingOrganization,
    DateOnly IssueDate,
    DateOnly? ExpirationDate,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record CourseEntryDto(
    Guid Id,
    string Name,
    string Provider,
    DateOnly CompletionDate,
    IReadOnlyList<string> SkillsAcquired,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);
