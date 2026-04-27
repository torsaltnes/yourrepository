namespace GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

/// <summary>API write model for updating an existing course entry.</summary>
public sealed record UpdateCourseRequest(
    string? Name,
    string? Provider,
    DateOnly CompletionDate,
    IReadOnlyList<string>? SkillsAcquired);
