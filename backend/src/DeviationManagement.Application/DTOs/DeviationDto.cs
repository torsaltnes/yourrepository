using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Application.DTOs;

public sealed record DeviationDto(
    Guid Id,
    string Title,
    string Description,
    DeviationSeverity Severity,
    DeviationStatus Status,
    string ReportedBy,
    DateTimeOffset ReportedAt,
    DateTimeOffset UpdatedAt);
