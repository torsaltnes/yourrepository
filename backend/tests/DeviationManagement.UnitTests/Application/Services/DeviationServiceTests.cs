using DeviationManagement.Application.Abstractions.Persistence;
using DeviationManagement.Application.DTOs;
using DeviationManagement.Application.Services;
using DeviationManagement.Application.Validation;
using DeviationManagement.Domain.Entities;
using DeviationManagement.Domain.Enums;
using Moq;
using Xunit;

namespace DeviationManagement.UnitTests.Application.Services;

public sealed class DeviationServiceTests
{
    private readonly Mock<IDeviationRepository> _repoMock;
    private readonly DeviationValidator _validator;
    private readonly DeviationService _sut;

    public DeviationServiceTests()
    {
        _repoMock = new Mock<IDeviationRepository>();
        _validator = new DeviationValidator();
        _sut = new DeviationService(_repoMock.Object, _validator);
    }

    private static SaveDeviationRequest ValidRequest() => new(
        "Test Title",
        "Some description",
        DeviationSeverity.Medium,
        DeviationStatus.Open,
        "Jane Doe",
        DateTimeOffset.UtcNow.AddDays(-1));

    private static Deviation BuildDeviation(Guid? id = null) =>
        new(id ?? Guid.NewGuid(),
            "Test Title",
            "Some description",
            DeviationSeverity.Medium,
            DeviationStatus.Open,
            "Jane Doe",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow);

    // ─── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllDtos()
    {
        var entities = new List<Deviation> { BuildDeviation(), BuildDeviation() };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entities);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        _repoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_MapsReportedAtCorrectly()
    {
        var reportedAt = DateTimeOffset.UtcNow.AddDays(-5);
        var entity = new Deviation(Guid.NewGuid(), "T", "", DeviationSeverity.Low, DeviationStatus.Open, "A", reportedAt, DateTimeOffset.UtcNow);
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([entity]);

        var result = await _sut.GetAllAsync();
        var dto = result.Single();

        Assert.Equal(reportedAt, dto.ReportedAt);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = BuildDeviation();
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDtoWithGeneratedId()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        var request = ValidRequest();
        var (dto, errors) = await _sut.CreateAsync(request);

        Assert.Null(errors);
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.Id);
        Assert.Equal(request.Title, dto.Title);
        Assert.Equal(request.ReportedAt, dto.ReportedAt);
        Assert.True(dto.UpdatedAt > DateTimeOffset.MinValue);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ReturnsValidationErrors()
    {
        var badRequest = new SaveDeviationRequest(
            string.Empty,  // missing title
            string.Empty,
            DeviationSeverity.Low,
            DeviationStatus.Open,
            string.Empty,  // missing reportedBy
            DateTimeOffset.UtcNow);

        var (dto, errors) = await _sut.CreateAsync(badRequest);

        Assert.Null(dto);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("title"));
        Assert.True(errors.ContainsKey("reportedBy"));
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFoundResult()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation?)null);

        var (dto, notFound, errors) = await _sut.UpdateAsync(Guid.NewGuid(), ValidRequest());

        Assert.True(notFound);
        Assert.Null(dto);
        Assert.Null(errors);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedDto()
    {
        var entity = BuildDeviation();
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        var (dto, notFound, errors) = await _sut.UpdateAsync(entity.Id, ValidRequest());

        Assert.False(notFound);
        Assert.Null(errors);
        Assert.NotNull(dto);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAtOnEntity()
    {
        var entity = BuildDeviation();
        var beforeUpdate = entity.UpdatedAt;

        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        await Task.Delay(10); // Ensure time advances
        await _sut.UpdateAsync(entity.Id, ValidRequest());

        Assert.True(entity.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public async Task UpdateAsync_InvalidRequest_ReturnsValidationErrors()
    {
        var badRequest = new SaveDeviationRequest(string.Empty, "", DeviationSeverity.Low, DeviationStatus.Open, string.Empty, DateTimeOffset.UtcNow);

        var (dto, notFound, errors) = await _sut.UpdateAsync(Guid.NewGuid(), badRequest);

        Assert.Null(dto);
        Assert.False(notFound);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("title"));
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(id);

        Assert.True(result);
        _repoMock.Verify(r => r.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
