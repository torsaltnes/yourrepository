using DeviationManagement.Application.DTOs;
using DeviationManagement.Application.Validation;
using DeviationManagement.Domain.Enums;
using Xunit;

namespace DeviationManagement.UnitTests.Application.Validation;

public sealed class DeviationValidatorTests
{
    private readonly DeviationValidator _sut = new();

    private static SaveDeviationRequest ValidRequest() => new(
        "Valid Title",
        "Valid description",
        DeviationSeverity.Low,
        DeviationStatus.Open,
        "Reporter Name",
        DateTimeOffset.UtcNow.AddDays(-1));

    [Fact]
    public void ValidateForSave_ValidRequest_ReturnsNull()
    {
        var result = _sut.ValidateForSave(ValidRequest());
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateForSave_EmptyTitle_ReturnsError(string title)
    {
        var request = ValidRequest() with { Title = title };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("title"));
    }

    [Fact]
    public void ValidateForSave_TitleTooLong_ReturnsError()
    {
        var request = ValidRequest() with { Title = new string('A', 201) };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("title"));
    }

    [Fact]
    public void ValidateForSave_DescriptionTooLong_ReturnsError()
    {
        var request = ValidRequest() with { Description = new string('X', 2001) };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateForSave_EmptyReportedBy_ReturnsError(string reportedBy)
    {
        var request = ValidRequest() with { ReportedBy = reportedBy };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("reportedBy"));
    }

    [Fact]
    public void ValidateForSave_ReportedByTooLong_ReturnsError()
    {
        var request = ValidRequest() with { ReportedBy = new string('R', 101) };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("reportedBy"));
    }

    [Fact]
    public void ValidateForSave_InvalidSeverity_ReturnsError()
    {
        var request = ValidRequest() with { Severity = (DeviationSeverity)99 };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("severity"));
    }

    [Fact]
    public void ValidateForSave_InvalidStatus_ReturnsError()
    {
        var request = ValidRequest() with { Status = (DeviationStatus)99 };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("status"));
    }

    [Fact]
    public void ValidateForSave_DefaultReportedAt_ReturnsError()
    {
        var request = ValidRequest() with { ReportedAt = default };
        var result = _sut.ValidateForSave(request);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey("reportedAt"));
    }

    [Theory]
    [InlineData(DeviationSeverity.Low)]
    [InlineData(DeviationSeverity.Medium)]
    [InlineData(DeviationSeverity.High)]
    [InlineData(DeviationSeverity.Critical)]
    public void ValidateForSave_AllValidSeverities_ReturnsNull(DeviationSeverity severity)
    {
        var request = ValidRequest() with { Severity = severity };
        var result = _sut.ValidateForSave(request);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(DeviationStatus.Open)]
    [InlineData(DeviationStatus.InProgress)]
    [InlineData(DeviationStatus.Resolved)]
    [InlineData(DeviationStatus.Closed)]
    public void ValidateForSave_AllValidStatuses_ReturnsNull(DeviationStatus status)
    {
        var request = ValidRequest() with { Status = status };
        var result = _sut.ValidateForSave(request);
        Assert.Null(result);
    }
}
