namespace GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

/// <summary>API write model for updating an existing education entry.</summary>
public sealed record UpdateEducationRequest(
    string? Degree,
    string? Institution,
    int GraduationYear,
    string? Notes);
