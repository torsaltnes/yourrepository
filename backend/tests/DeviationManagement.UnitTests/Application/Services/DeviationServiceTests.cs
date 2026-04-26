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
    private const string DefaultOwnerId = "owner-001";
    private const string OtherOwnerId = "owner-other-999";

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

    private static Deviation BuildDeviation(Guid? id = null, string? ownerId = null) =>
        new(id ?? Guid.NewGuid(),
            "Test Title",
            "Some description",
            DeviationSeverity.Medium,
            DeviationStatus.Open,
            "Jane Doe",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            ownerId ?? DefaultOwnerId);

    // ─── GetAll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyOwnersDtos()
    {
        var ownedEntity = BuildDeviation(ownerId: DefaultOwnerId);
        var otherEntity = BuildDeviation(ownerId: OtherOwnerId);
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([ownedEntity, otherEntity]);

        var result = await _sut.GetAllAsync(DefaultOwnerId);

        Assert.Single(result);
        Assert.Equal(ownedEntity.Id, result.First().Id);
    }

    [Fact]
    public async Task GetAllAsync_DifferentOwner_ReturnsEmpty()
    {
        var entities = new List<Deviation> { BuildDeviation(ownerId: DefaultOwnerId) };
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entities);

        var result = await _sut.GetAllAsync(OtherOwnerId);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_MapsReportedAtCorrectly()
    {
        var reportedAt = DateTimeOffset.UtcNow.AddDays(-5);
        var entity = new Deviation(Guid.NewGuid(), "T", "", DeviationSeverity.Low, DeviationStatus.Open, "A", reportedAt, DateTimeOffset.UtcNow, DefaultOwnerId);
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync([entity]);

        var result = await _sut.GetAllAsync(DefaultOwnerId);
        var dto = result.Single();

        Assert.Equal(reportedAt, dto.ReportedAt);
    }

    // ─── GetById ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_SameOwner_ReturnsDto()
    {
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id, DefaultOwnerId);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), DefaultOwnerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_DifferentOwner_ReturnsNull()
    {
        // Exists in repository but belongs to a different owner — must not be exposed
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(entity.Id, OtherOwnerId);

        Assert.Null(result);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDtoWithGeneratedId()
    {
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        var request = ValidRequest();
        var (dto, errors) = await _sut.CreateAsync(request, DefaultOwnerId);

        Assert.Null(errors);
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.Id);
        Assert.Equal(request.Title, dto.Title);
        Assert.Equal(request.ReportedAt, dto.ReportedAt);
        Assert.True(dto.UpdatedAt > DateTimeOffset.MinValue);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_StoresOwnerIdOnEntity()
    {
        Deviation? capturedEntity = null;
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .Callback<Deviation, CancellationToken>((e, _) => capturedEntity = e)
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        await _sut.CreateAsync(ValidRequest(), DefaultOwnerId);

        Assert.NotNull(capturedEntity);
        Assert.Equal(DefaultOwnerId, capturedEntity!.OwnerId);
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

        var (dto, errors) = await _sut.CreateAsync(badRequest, DefaultOwnerId);

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

        var (dto, notFound, forbidden, errors) = await _sut.UpdateAsync(Guid.NewGuid(), ValidRequest(), DefaultOwnerId);

        Assert.True(notFound);
        Assert.False(forbidden);
        Assert.Null(dto);
        Assert.Null(errors);
    }

    [Fact]
    public async Task UpdateAsync_DifferentOwner_ReturnsForbidden()
    {
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        var (dto, notFound, forbidden, errors) = await _sut.UpdateAsync(entity.Id, ValidRequest(), OtherOwnerId);

        Assert.False(notFound);
        Assert.True(forbidden);
        Assert.Null(dto);
        Assert.Null(errors);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_ReturnsUpdatedDto()
    {
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        var (dto, notFound, forbidden, errors) = await _sut.UpdateAsync(entity.Id, ValidRequest(), DefaultOwnerId);

        Assert.False(notFound);
        Assert.False(forbidden);
        Assert.Null(errors);
        Assert.NotNull(dto);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_SetsUpdatedAtOnEntity()
    {
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        var beforeUpdate = entity.UpdatedAt;

        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Deviation>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation e, CancellationToken _) => e);

        await Task.Delay(10); // Ensure time advances
        await _sut.UpdateAsync(entity.Id, ValidRequest(), DefaultOwnerId);

        Assert.True(entity.UpdatedAt >= beforeUpdate);
    }

    [Fact]
    public async Task UpdateAsync_InvalidRequest_ReturnsValidationErrors()
    {
        var badRequest = new SaveDeviationRequest(string.Empty, "", DeviationSeverity.Low, DeviationStatus.Open, string.Empty, DateTimeOffset.UtcNow);

        var (dto, notFound, forbidden, errors) = await _sut.UpdateAsync(Guid.NewGuid(), badRequest, DefaultOwnerId);

        Assert.Null(dto);
        Assert.False(notFound);
        Assert.False(forbidden);
        Assert.NotNull(errors);
        Assert.True(errors!.ContainsKey("title"));
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingId_SameOwner_ReturnsDeleted()
    {
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);
        _repoMock.Setup(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var (deleted, forbidden) = await _sut.DeleteAsync(entity.Id, DefaultOwnerId);

        Assert.True(deleted);
        Assert.False(forbidden);
        _repoMock.Verify(r => r.DeleteAsync(entity.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DifferentOwner_ReturnsForbidden()
    {
        var entity = BuildDeviation(ownerId: DefaultOwnerId);
        _repoMock.Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        var (deleted, forbidden) = await _sut.DeleteAsync(entity.Id, OtherOwnerId);

        Assert.False(deleted);
        Assert.True(forbidden);
        _repoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsNotDeleted()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Deviation?)null);

        var (deleted, forbidden) = await _sut.DeleteAsync(Guid.NewGuid(), DefaultOwnerId);

        Assert.False(deleted);
        Assert.False(forbidden);
    }
}
