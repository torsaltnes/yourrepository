namespace GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

/// <summary>API write model for updating an existing certificate entry.</summary>
public sealed record UpdateCertificateRequest(
    string? Name,
    string? IssuingOrganization,
    DateOnly IssueDate,
    DateOnly? ExpirationDate);
