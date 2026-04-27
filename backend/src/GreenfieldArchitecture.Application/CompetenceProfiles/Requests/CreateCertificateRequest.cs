namespace GreenfieldArchitecture.Application.CompetenceProfiles.Requests;

/// <summary>API write model for adding a new certificate entry.</summary>
public sealed record CreateCertificateRequest(
    string? Name,
    string? IssuingOrganization,
    DateOnly IssueDate,
    DateOnly? ExpirationDate);
