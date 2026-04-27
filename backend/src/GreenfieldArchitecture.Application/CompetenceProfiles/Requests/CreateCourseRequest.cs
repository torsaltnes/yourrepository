namespace GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

/// <summary>API write model for adding a new course entry.</summary>
public sealed record CreateCourseRequest(
    string? Name,
    string? Provider,
    DateOnly CompletionDate,
    IReadOnlyList<string>? SkillsAcquired);
