using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using GreenfieldArchitecture.Api.Tests.Infrastructure;
using GreenfieldArchitecture.Application.CompetenceProfiles.Dtos;
using GreenfieldArchitecture.Application.CompetenceProfiles.Requests;
using Xunit;

namespace GreenfieldArchitecture.Api.Tests.CompetenceProfiles;

public sealed class CompetenceProfileEndpointsTests : IClassFixture<GreenfieldArchitectureApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly GreenfieldArchitectureApiFactory _factory;
    private readonly HttpClient _client;

    public CompetenceProfileEndpointsTests(GreenfieldArchitectureApiFactory factory)
    {
        _factory = factory;
        _factory.ResetCompetenceProfileRepository();
        _client = factory.CreateClient();
    }

    // ── GET /api/me/competence-profile ────────────────────────────────────────

    [Fact]
    public async Task GetProfile_Returns200WithEmptyGroupsForNewUser()
    {
        var response = await _client.GetAsync("/api/me/competence-profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<CompetenceProfileDto>(body, JsonOptions);

        dto.Should().NotBeNull();
        dto!.Education.Should().BeEmpty();
        dto.Certificates.Should().BeEmpty();
        dto.Courses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProfile_Returns401_ForUnauthenticatedRequest()
    {
        var unauthClient = _factory.CreateUnauthenticatedClient();
        var response = await unauthClient.GetAsync("/api/me/competence-profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/me/competence-profile/education ─────────────────────────────

    [Fact]
    public async Task PostEducation_Returns201WithEntryAndLocation()
    {
        var request = new CreateEducationRequest("BSc Computer Science", "MIT", 2020, "With distinction");

        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/education", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var dto = await DeserializeAsync<EducationEntryDto>(response);
        dto.Should().NotBeNull();
        dto!.Id.Should().NotBe(Guid.Empty);
        dto.Degree.Should().Be("BSc Computer Science");
        dto.Institution.Should().Be("MIT");
        dto.GraduationYear.Should().Be(2020);
    }

    [Fact]
    public async Task PostEducation_Returns400_ForBlankDegree()
    {
        var payload = new { degree = "   ", institution = "MIT", graduationYear = 2020 };
        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/education", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEducation_Returns400_ForInvalidYear()
    {
        var request = new CreateEducationRequest("BSc", "MIT", 1800, null);
        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/education", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostEducation_Returns401_ForUnauthenticatedRequest()
    {
        var unauthClient = _factory.CreateUnauthenticatedClient();
        var request = new CreateEducationRequest("BSc", "MIT", 2020, null);
        var response = await unauthClient.PostAsJsonAsync("/api/me/competence-profile/education", request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/me/competence-profile/education/{id} ─────────────────────────

    [Fact]
    public async Task PutEducation_Returns200WithUpdatedEntry()
    {
        var created = await CreateEducationEntryAsync("Original Degree", "Old Uni", 2015);

        var update = new UpdateEducationRequest("Updated MSc", "New Uni", 2018, "Honours");
        var response = await _client.PutAsJsonAsync(
            $"/api/me/competence-profile/education/{created.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await DeserializeAsync<EducationEntryDto>(response);
        dto!.Degree.Should().Be("Updated MSc");
        dto.Institution.Should().Be("New Uni");
        dto.GraduationYear.Should().Be(2018);
        dto.CreatedUtc.Should().Be(created.CreatedUtc);
    }

    [Fact]
    public async Task PutEducation_Returns404_ForUnknownEntryId()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/me/competence-profile/education/{Guid.NewGuid()}",
            new UpdateEducationRequest("BSc", "Uni", 2020, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/me/competence-profile/education/{id} ─────────────────────

    [Fact]
    public async Task DeleteEducation_Returns204_AndSubsequentGetShowsRemoval()
    {
        var created = await CreateEducationEntryAsync("BSc to Delete", "Uni", 2019);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/me/competence-profile/education/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync("/api/me/competence-profile");
        var profile = await DeserializeAsync<CompetenceProfileDto>(getResponse);
        profile!.Education.Should().NotContain(e => e.Id == created.Id);
    }

    [Fact]
    public async Task DeleteEducation_Returns404_ForUnknownEntryId()
    {
        var response = await _client.DeleteAsync(
            $"/api/me/competence-profile/education/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/me/competence-profile/certificates ──────────────────────────

    [Fact]
    public async Task PostCertificate_Returns201WithEntry()
    {
        var request = new CreateCertificateRequest(
            "AWS Solutions Architect",
            "Amazon",
            new DateOnly(2023, 3, 10),
            new DateOnly(2026, 3, 10));

        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/certificates", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await DeserializeAsync<CertificateEntryDto>(response);
        dto!.Name.Should().Be("AWS Solutions Architect");
        dto.ExpirationDate.Should().Be(new DateOnly(2026, 3, 10));
    }

    [Fact]
    public async Task PostCertificate_Returns400_WhenExpirationBeforeIssueDate()
    {
        var request = new CreateCertificateRequest(
            "Cert",
            "Org",
            new DateOnly(2023, 6, 1),
            new DateOnly(2022, 1, 1));  // before issue

        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/certificates", request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/me/competence-profile/courses ───────────────────────────────

    [Fact]
    public async Task PostCourse_Returns201WithNormalizedSkills()
    {
        var request = new CreateCourseRequest(
            "Clean Architecture",
            "Pluralsight",
            new DateOnly(2024, 4, 1),
            ["C#", " c# ", "SOLID", ""]);

        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/courses", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await DeserializeAsync<CourseEntryDto>(response);
        dto!.SkillsAcquired.Should().HaveCount(2);
        dto.SkillsAcquired.Should().Contain("C#");
        dto.SkillsAcquired.Should().Contain("SOLID");
    }

    [Fact]
    public async Task DeleteCourse_Returns204_ForExistingEntry()
    {
        var course = await CreateCourseEntryAsync("Course to Delete", "Provider");

        var response = await _client.DeleteAsync($"/api/me/competence-profile/courses/{course.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Cross-user isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task UserBCannotDeleteUserAsEntry()
    {
        // User A (test-user-id) creates an education entry.
        var entryA = await CreateEducationEntryAsync("User A Degree", "Uni", 2020);

        // User B (different NameIdentifier) attempts to delete it.
        using var userBClient = _factory.CreateClientForUser("user-b-id");
        var response = await userBClient.DeleteAsync(
            $"/api/me/competence-profile/education/{entryA.Id}");

        // Should be 404 — user B has no profile / that entry doesn't belong to them.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<EducationEntryDto> CreateEducationEntryAsync(
        string degree, string institution, int year)
    {
        var request = new CreateEducationRequest(degree, institution, year, null);
        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/education", request);
        response.EnsureSuccessStatusCode();
        return (await DeserializeAsync<EducationEntryDto>(response))!;
    }

    private async Task<CourseEntryDto> CreateCourseEntryAsync(string name, string provider)
    {
        var request = new CreateCourseRequest(name, provider, new DateOnly(2024, 1, 1), null);
        var response = await _client.PostAsJsonAsync("/api/me/competence-profile/courses", request);
        response.EnsureSuccessStatusCode();
        return (await DeserializeAsync<CourseEntryDto>(response))!;
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
