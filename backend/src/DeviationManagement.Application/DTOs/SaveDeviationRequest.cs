using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Application.DTOs;

public sealed record SaveDeviationRequest(
    string Title,
    string Description,
    DeviationSeverity Severity,
    DeviationStatus Status,
    string ReportedBy,
    DateTimeOffset OccurredAt);
