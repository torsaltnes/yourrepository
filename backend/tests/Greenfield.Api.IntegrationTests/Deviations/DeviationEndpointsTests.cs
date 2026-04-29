using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Greenfield.Application.Common;
using Greenfield.Application.Deviations;
using Greenfield.Domain.Deviations;
using Greenfield.Infrastructure.Deviations;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Greenfield.Api.IntegrationTests.Deviations;

/// <summary>
/// Integration tests for the <c>/api/deviations</c> endpoint group.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> for an in-process server
/// backed by the seeded in-memory repository.
/// </summary>
public sealed class DeviationEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private HttpClient Client => factory.CreateClient();

    // ── GET /api/deviations ───────────────────────────────────────────────

    [Fact]
    public async Task GetDeviations_ReturnsOk_WithPagedResult()
    {
        var response = await Client.GetAsync("/api/deviations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("items",      out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("totalCount", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("page",       out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("pageSize",   out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetDeviations_SeedDataIncluded_TotalCountAtLeastSix()
    {
        var response = await Client.GetAsync("/api/deviations?pageSize=100");

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().BeGreaterThanOrEqualTo(6);
    }

    [Fact]
    public async Task GetDeviations_WithStatusFilter_ReturnsOnlyMatchingDeviations()
    {
        var response = await Client.GetAsync("/api/deviations?status=Registered");

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<DeviationSummaryDto>>(body, JsonOpts)!;

        result.Items.Should().AllSatisfy(d => d.Status.Should().Be(DeviationStatus.Registered));
    }

    [Fact]
    public async Task GetDeviations_Pagination_ReturnsCorrectPageSize()
    {
        var response = await Client.GetAsync("/api/deviations?page=1&pageSize=2");

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<DeviationSummaryDto>>(body, JsonOpts)!;

        result.Items.Should().HaveCount(2);
        result.PageSize.Should().Be(2);
    }

    // ── GET /api/deviations/{id} ──────────────────────────────────────────

    [Fact]
    public async Task GetDeviationById_SeedId_ReturnsOk_WithFullDto()
    {
        var response = await Client.GetAsync($"/api/deviations/{DeviationSeedData.Dev001Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("id").GetString()
            .Should().Be(DeviationSeedData.Dev001Id.ToString());
        doc.RootElement.TryGetProperty("timeline",    out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("attachments", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetDeviationById_UnknownId_ReturnsNotFound()
    {
        var response = await Client.GetAsync($"/api/deviations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/deviations ──────────────────────────────────────────────

    [Fact]
    public async Task CreateDeviation_ValidRequest_ReturnsCreated_WithLocation()
    {
        var request = new CreateDeviationRequest(
            Title: "Integration test deviation",
            Description: "Created from integration test",
            Severity: DeviationSeverity.Low,
            Category: DeviationCategory.Other,
            ReportedBy: "integrationtest@example.com");

        var response = await Client.PostAsJsonAsync("/api/deviations", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await response.Content.ReadFromJsonAsync<DeviationDto>(JsonOpts);
        dto!.Title.Should().Be("Integration test deviation");
        dto.Status.Should().Be(DeviationStatus.Registered);
    }

    [Fact]
    public async Task CreateDeviation_EmptyTitle_ReturnsBadRequest()
    {
        var request = new CreateDeviationRequest(
            Title: "   ",
            Description: "desc",
            Severity: DeviationSeverity.Low,
            Category: DeviationCategory.Other,
            ReportedBy: "user@example.com");

        var response = await Client.PostAsJsonAsync("/api/deviations", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PUT /api/deviations/{id} ──────────────────────────────────────────

    [Fact]
    public async Task UpdateDeviation_ValidRequest_ReturnsOk_WithUpdatedFields()
    {
        var request = new UpdateDeviationRequest(
            Title: "Updated title",
            Description: "Updated description",
            Severity: DeviationSeverity.High,
            Category: DeviationCategory.Safety,
            UpdatedBy: "updater@example.com");

        var response = await Client.PutAsJsonAsync(
            $"/api/deviations/{DeviationSeedData.Dev005Id}", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<DeviationDto>(JsonOpts);
        dto!.Title.Should().Be("Updated title");
        dto.Severity.Should().Be(DeviationSeverity.High);
    }

    [Fact]
    public async Task UpdateDeviation_UnknownId_ReturnsNotFound()
    {
        var request = new UpdateDeviationRequest(
            Title: "T",
            Description: "D",
            Severity: DeviationSeverity.Low,
            Category: DeviationCategory.Other);

        var response = await Client.PutAsJsonAsync(
            $"/api/deviations/{Guid.NewGuid()}", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/deviations/{id} ───────────────────────────────────────

    [Fact]
    public async Task DeleteDeviation_ExistingId_ReturnsNoContent()
    {
        // Create then delete so we don't disturb seed data used by other tests.
        var create = await Client.PostAsJsonAsync("/api/deviations",
            new CreateDeviationRequest("To delete", "desc", DeviationSeverity.Low,
                DeviationCategory.Other, "user@example.com"), JsonOpts);
        var dto = await create.Content.ReadFromJsonAsync<DeviationDto>(JsonOpts);

        var response = await Client.DeleteAsync($"/api/deviations/{dto!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteDeviation_UnknownId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync($"/api/deviations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/deviations/{id}/transition ──────────────────────────────

    [Fact]
    public async Task TransitionDeviation_ValidTransition_ReturnsOk_WithNewStatus()
    {
        var request = new TransitionDeviationRequest(
            NewStatus: DeviationStatus.UnderAssessment,
            PerformedBy: "supervisor@example.com",
            Comment: "Escalating for assessment");

        var response = await Client.PostAsJsonAsync(
            $"/api/deviations/{DeviationSeedData.Dev006Id}/transition", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await response.Content.ReadFromJsonAsync<DeviationDto>(JsonOpts);
        dto!.Status.Should().Be(DeviationStatus.UnderAssessment);
    }

    [Fact]
    public async Task TransitionDeviation_InvalidTransition_ReturnsBadRequest()
    {
        // DEV-001 is Closed; cannot go directly to UnderAssessment.
        var request = new TransitionDeviationRequest(
            NewStatus: DeviationStatus.UnderAssessment,
            PerformedBy: "user@example.com");

        var response = await Client.PostAsJsonAsync(
            $"/api/deviations/{DeviationSeedData.Dev001Id}/transition", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransitionDeviation_UnknownId_ReturnsNotFound()
    {
        var request = new TransitionDeviationRequest(
            NewStatus: DeviationStatus.UnderAssessment,
            PerformedBy: "user@example.com");

        var response = await Client.PostAsJsonAsync(
            $"/api/deviations/{Guid.NewGuid()}/transition", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/deviations/{id}/timeline ─────────────────────────────────

    [Fact]
    public async Task GetTimeline_SeedDeviation_ReturnsOk_WithActivities()
    {
        var response = await Client.GetAsync(
            $"/api/deviations/{DeviationSeedData.Dev001Id}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetTimeline_UnknownId_ReturnsNotFound()
    {
        var response = await Client.GetAsync(
            $"/api/deviations/{Guid.NewGuid()}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/deviations/{id}/comments ───────────────────────────────

    [Fact]
    public async Task AddComment_ValidRequest_ReturnsCreated_WithActivity()
    {
        var request = new AddCommentRequest(
            "This is a comment from integration test.",
            "commenter@example.com");

        var response = await Client.PostAsJsonAsync(
            $"/api/deviations/{DeviationSeedData.Dev004Id}/comments", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var activity = await response.Content.ReadFromJsonAsync<ActivityDto>(JsonOpts);
        activity!.Type.Should().Be(ActivityType.CommentAdded);
        activity.Description.Should().Be("This is a comment from integration test.");
    }

    [Fact]
    public async Task AddComment_UnknownId_ReturnsNotFound()
    {
        var request = new AddCommentRequest("comment", "user@example.com");

        var response = await Client.PostAsJsonAsync(
            $"/api/deviations/{Guid.NewGuid()}/comments", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/deviations/{id}/attachments ──────────────────────────────

    [Fact]
    public async Task GetAttachments_SeedDeviationWithAttachments_ReturnsOk_WithList()
    {
        var response = await Client.GetAsync(
            $"/api/deviations/{DeviationSeedData.Dev001Id}/attachments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        doc.RootElement.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAttachments_UnknownId_ReturnsNotFound()
    {
        var response = await Client.GetAsync(
            $"/api/deviations/{Guid.NewGuid()}/attachments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/deviations/{id}/attachments ─────────────────────────────

    [Fact]
    public async Task UploadAttachment_ValidBase64_ReturnsCreated()
    {
        var content = Convert.ToBase64String("Hello, attachment!"u8.ToArray());
        var request = new UploadAttachmentRequest(
            FileName: "test.txt",
            ContentType: "text/plain",
            Base64Content: content,
            UploadedBy: "tester@example.com");

        var response = await Client.PostAsJsonAsync(
            $"/api/deviations/{DeviationSeedData.Dev005Id}/attachments", request, JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await response.Content.ReadFromJsonAsync<AttachmentDto>(JsonOpts);
        dto!.FileName.Should().Be("test.txt");
        dto.ContentType.Should().Be("text/plain");
    }

    // ── DELETE /api/deviations/{id}/attachments/{attachmentId} ───────────

    [Fact]
    public async Task RemoveAttachment_UnknownAttachmentId_ReturnsNotFound()
    {
        var response = await Client.DeleteAsync(
            $"/api/deviations/{DeviationSeedData.Dev001Id}/attachments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/deviations/export ────────────────────────────────────────

    [Fact]
    public async Task ExportCsv_ReturnsOk_WithCsvContentType()
    {
        var response = await Client.GetAsync("/api/deviations/export");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportCsv_ContainsHeaderRow()
    {
        var response = await Client.GetAsync("/api/deviations/export");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().StartWith("Id,Title,Status,Severity,Category,ReportedBy");
    }
}
