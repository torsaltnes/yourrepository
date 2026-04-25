using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Api.Contracts.Responses;

public sealed record DeviationApiResponse(
    Guid Id,
    string Title,
    string Description,
    DeviationSeverity Severity,
    DeviationStatus Status,
    string ReportedBy,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
