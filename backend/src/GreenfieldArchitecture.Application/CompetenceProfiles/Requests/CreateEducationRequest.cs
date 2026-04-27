namespace GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

/// <summary>API write model for adding a new education entry.</summary>
public sealed record CreateEducationRequest(
    string? Degree,
    string? Institution,
    int GraduationYear,
    string? Notes);
