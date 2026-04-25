using DeviationManagement.Api.Contracts.Requests;
using DeviationManagement.Api.Controllers;
using DeviationManagement.Application.Abstractions.Services;
using DeviationManagement.Application.DTOs;
using DeviationManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DeviationManagement.UnitTests.Api.Controllers;

public sealed class DeviationsControllerTests
{
    private readonly Mock<IDeviationService> _serviceMock;
    private readonly DeviationsController _sut;

    public DeviationsControllerTests()
    {
        _serviceMock = new Mock<IDeviationService>();
        _sut = new DeviationsController(_serviceMock.Object);
    }

    private static DeviationDto BuildDto(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "Title",
        "Description",
        DeviationSeverity.Low,
        DeviationStatus.Open,
        "Reporter",
        DateTimeOffset.UtcNow.AddDays(-1),
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow);

    private static SaveDeviationApiRequest ValidApiRequest() => new(
        "Title",
        "Description",
        DeviationSeverity.Low,
        DeviationStatus.Open,
        "Reporter",
        DateTimeOffset.UtcNow.AddDays(-1));

    // ─── GET /api/deviations ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsOkWithItems()
    {
        var dtos = new List<DeviationDto> { BuildDto(), BuildDto() };
        _serviceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dtos);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    // ─── GET /api/deviations/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var dto = BuildDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dto);

        var result = await _sut.GetById(dto.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetById_MissingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((DeviationDto?)null);

        var result = await _sut.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ─── POST /api/deviations ─────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreatedAtAction()
    {
        var dto = BuildDto();
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<SaveDeviationRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((dto, (Dictionary<string, string[]>?)null));

        var result = await _sut.Create(ValidApiRequest(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRequest_ReturnsBadRequest()
    {
        var errors = new Dictionary<string, string[]> { ["title"] = ["Title is required."] };
        _serviceMock.Setup(s => s.CreateAsync(It.IsAny<SaveDeviationRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(((DeviationDto?)null, errors));

        var result = await _sut.Create(ValidApiRequest(), CancellationToken.None);

        var objResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objResult.StatusCode);
    }

    // ─── PUT /api/deviations/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidRequest_ReturnsOk()
    {
        var dto = BuildDto();
        _serviceMock.Setup(s => s.UpdateAsync(dto.Id, It.IsAny<SaveDeviationRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((dto, false, (Dictionary<string, string[]>?)null));

        var result = await _sut.Update(dto.Id, ValidApiRequest(), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<SaveDeviationRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(((DeviationDto?)null, true, (Dictionary<string, string[]>?)null));

        var result = await _sut.Update(Guid.NewGuid(), ValidApiRequest(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_InvalidRequest_ReturnsBadRequest()
    {
        var errors = new Dictionary<string, string[]> { ["title"] = ["Title is required."] };
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<SaveDeviationRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(((DeviationDto?)null, false, errors));

        var result = await _sut.Update(Guid.NewGuid(), ValidApiRequest(), CancellationToken.None);

        var objResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(400, objResult.StatusCode);
    }

    // ─── DELETE /api/deviations/{id} ──────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingId_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);

        var result = await _sut.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_MissingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

        var result = await _sut.Delete(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
