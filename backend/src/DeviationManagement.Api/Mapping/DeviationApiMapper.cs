using DeviationManagement.Api.Contracts.Requests;
using DeviationManagement.Api.Contracts.Responses;
using DeviationManagement.Application.DTOs;

namespace DeviationManagement.Api.Mapping;

public static class DeviationApiMapper
{
    public static SaveDeviationRequest ToApplicationRequest(SaveDeviationApiRequest apiRequest) => new(
        apiRequest.Title,
        apiRequest.Description,
        apiRequest.Severity,
        apiRequest.Status,
        apiRequest.ReportedBy,
        apiRequest.ReportedAt);

    public static DeviationApiResponse ToApiResponse(DeviationDto dto) => new(
        dto.Id,
        dto.Title,
        dto.Description,
        dto.Severity,
        dto.Status,
        dto.ReportedBy,
        dto.ReportedAt,
        dto.UpdatedAt);
}
