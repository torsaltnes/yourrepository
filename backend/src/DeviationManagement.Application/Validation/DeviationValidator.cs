using DeviationManagement.Application.DTOs;
using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Application.Validation;

public sealed class DeviationValidator
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 2000;
    private const int MaxReportedByLength = 100;

    public Dictionary<string, string[]>? ValidateForSave(SaveDeviationRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Title is required."];
        else if (request.Title.Length > MaxTitleLength)
            errors["title"] = [$"Title must not exceed {MaxTitleLength} characters."];

        if (request.Description.Length > MaxDescriptionLength)
            errors["description"] = [$"Description must not exceed {MaxDescriptionLength} characters."];

        if (string.IsNullOrWhiteSpace(request.ReportedBy))
            errors["reportedBy"] = ["ReportedBy is required."];
        else if (request.ReportedBy.Length > MaxReportedByLength)
            errors["reportedBy"] = [$"ReportedBy must not exceed {MaxReportedByLength} characters."];

        if (!Enum.IsDefined(typeof(DeviationSeverity), request.Severity))
            errors["severity"] = ["Severity is not a valid value."];

        if (!Enum.IsDefined(typeof(DeviationStatus), request.Status))
            errors["status"] = ["Status is not a valid value."];

        if (request.ReportedAt == default)
            errors["reportedAt"] = ["ReportedAt must be a valid date."];

        return errors.Count > 0 ? errors : null;
    }
}
