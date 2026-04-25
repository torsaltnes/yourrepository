using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Api.Contracts.Requests;

public sealed record SaveDeviationApiRequest(
    string Title,
    string Description,
    DeviationSeverity Severity,
    DeviationStatus Status,
    string ReportedBy,
    DateTimeOffset ReportedAt);
