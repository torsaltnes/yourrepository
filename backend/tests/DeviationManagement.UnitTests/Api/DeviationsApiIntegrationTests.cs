using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DeviationManagement.Application.Abstractions.Persistence;
using DeviationManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DeviationManagement.UnitTests.Api;

public sealed class DeviationsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public DeviationsApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Override the repository so each test-class instance gets a fresh store
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing singleton repository and replace with a fresh one per test run
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IDeviationRepository));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IDeviationRepository,
                    DeviationManagement.Infrastructure.Persistence.InMemory.InMemoryDeviationRepository>();
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static object BuildCreatePayload(string title = "Test Deviation") => new
    {
        title,
        description = "Integration test description",
        severity = "Medium",
        status = "Open",
        reportedBy = "Integration Tester",
        reportedAt = DateTimeOffset.UtcNow.AddDays(-2).ToString("O")
    };

    // ─── Full CRUD flow ───────────────────────────────────────────────────────

    [Fact]
    public async Task FullCrudFlow_CreateReadUpdateDelete()
    {
        var client = CreateClient();

        // Create
        var createResponse = await client.PostAsJsonAsync("/api/deviations", BuildCreatePayload("CRUD Test"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var id = created.GetProperty("id").GetString();
        Assert.NotNull(id);
        Assert.Equal("CRUD Test", created.GetProperty("title").GetString());
        Assert.Equal("Medium", created.GetProperty("severity").GetString());

        // Read by Id
        var getResponse = await client.GetAsync($"/api/deviations/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal(id, fetched.GetProperty("id").GetString());

        // List
        var listResponse = await client.GetAsync("/api/deviations");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement[]>(JsonOpts);
        Assert.NotNull(list);
        Assert.Contains(list, d => d.GetProperty("id").GetString() == id);

        // Update
        var updatePayload = new
        {
            title = "Updated Deviation",
            description = "Updated description",
            severity = "High",
            status = "InProgress",
            reportedBy = "Integration Tester",
            reportedAt = DateTimeOffset.UtcNow.AddDays(-1).ToString("O")
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/deviations/{id}", updatePayload);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Updated Deviation", updated.GetProperty("title").GetString());
        Assert.Equal("High", updated.GetProperty("severity").GetString());
        Assert.Equal("InProgress", updated.GetProperty("status").GetString());

        // Delete
        var deleteResponse = await client.DeleteAsync($"/api/deviations/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Confirm gone
        var afterDeleteResponse = await client.GetAsync($"/api/deviations/{id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }

    // ─── Enum string serialization ────────────────────────────────────────────

    [Fact]
    public async Task Create_ReturnsSeverityAndStatusAsStrings()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/deviations", BuildCreatePayload("Enum Test"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // Severity should be serialized as string "Medium", not integer
        Assert.Contains("\"Medium\"", body);
        Assert.Contains("\"Open\"", body);
        Assert.DoesNotContain("\"severity\":1", body);
    }

    // ─── 8-field contract ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ResponseContainsExactly8Fields()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/deviations", BuildCreatePayload("Fields Test"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var propNames = body.EnumerateObject().Select(p => p.Name.ToLowerInvariant()).ToList();

        Assert.Contains("id", propNames);
        Assert.Contains("title", propNames);
        Assert.Contains("description", propNames);
        Assert.Contains("severity", propNames);
        Assert.Contains("status", propNames);
        Assert.Contains("reportedby", propNames);
        Assert.Contains("reportedat", propNames);
        Assert.Contains("updatedat", propNames);
        // Old fields must not appear
        Assert.DoesNotContain("createdat", propNames);
        Assert.DoesNotContain("occurredat", propNames);
    }

    // ─── Validation error shape ───────────────────────────────────────────────

    [Fact]
    public async Task Create_InvalidRequest_ReturnsProblemJsonWithErrors()
    {
        var client = CreateClient();
        var invalidPayload = new
        {
            title = "",               // required
            description = "",
            severity = "Medium",
            status = "Open",
            reportedBy = "",          // required
            reportedAt = DateTimeOffset.UtcNow.AddDays(-1).ToString("O")
        };

        var response = await client.PostAsJsonAsync("/api/deviations", invalidPayload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Contains("problem+json", contentType ?? "", StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("title", out _));
    }

    // ─── 404 returns ProblemDetails ───────────────────────────────────────────

    [Fact]
    public async Task GetById_NotFound_ReturnsProblemDetails()
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/api/deviations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var status));
        Assert.Equal(404, status.GetInt32());
    }

    // ─── CORS headers ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Options_WithFrontendOrigin_ReturnsCorsHeaders()
    {
        var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/deviations");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            "Expected Access-Control-Allow-Origin header");

        var origin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal("http://localhost:4200", origin);
    }

    // ─── Update non-existent returns 404 ProblemDetails ──────────────────────

    [Fact]
    public async Task Update_NotFound_ReturnsProblemDetails()
    {
        var client = CreateClient();
        var payload = new
        {
            title = "Updated",
            description = "",
            severity = "Low",
            status = "Open",
            reportedBy = "Tester",
            reportedAt = DateTimeOffset.UtcNow.AddDays(-1).ToString("O")
        };

        var response = await client.PutAsJsonAsync($"/api/deviations/{Guid.NewGuid()}", payload);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var status));
        Assert.Equal(404, status.GetInt32());
    }

    // ─── Delete non-existent returns 404 ProblemDetails ──────────────────────

    [Fact]
    public async Task Delete_NotFound_ReturnsProblemDetails()
    {
        var client = CreateClient();
        var response = await client.DeleteAsync($"/api/deviations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("status", out var status));
        Assert.Equal(404, status.GetInt32());
    }
}
