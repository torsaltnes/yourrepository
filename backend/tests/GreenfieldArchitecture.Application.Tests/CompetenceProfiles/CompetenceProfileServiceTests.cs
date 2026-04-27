using FluentAssertions;
using GreenfieldArchitecture.Application.Abstractions.CompetenceProfiles;
using GreenfieldArchitecture.Application.CompetenceProfiles.Requests;
using GreenfieldArchitecture.Application.CompetenceProfiles.Services;
using GreenfieldArchitecture.Domain.CompetenceProfiles;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GreenfieldArchitecture.Application.Tests.CompetenceProfiles;

public sealed class CompetenceProfileServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2024, 6, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly Mock<ICompetenceProfileRepository> _repoMock;
    private readonly Mock<TimeProvider> _timeProviderMock;
    private readonly Mock<ILogger<CompetenceProfileService>> _loggerMock;
    private readonly CompetenceProfileService _sut;

    public CompetenceProfileServiceTests()
    {
        _repoMock = new Mock<ICompetenceProfileRepository>(MockBehavior.Strict);
        _timeProviderMock = new Mock<TimeProvider>();
        _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(FixedNow);
        _loggerMock = new Mock<ILogger<CompetenceProfileService>>();

        _sut = new CompetenceProfileService(
            _repoMock.Object,
            _timeProviderMock.Object,
            _loggerMock.Object);
    }

    // ── GetMyProfile ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfileAsync_ReturnsEmptyProfile_WhenNoProfileExists()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        // Act
        var dto = await _sut.GetMyProfileAsync("emp-1");

        // Assert
        dto.Education.Should().BeEmpty();
        dto.Certificates.Should().BeEmpty();
        dto.Courses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyProfileAsync_ReturnsGroupedAndSortedProfile_WhenProfileExists()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");
        profile.AddEducation(EducationEntry.Create("BSc", "Uni A", 2015, null, FixedNow));
        profile.AddEducation(EducationEntry.Create("MSc", "Uni B", 2018, null, FixedNow));

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var dto = await _sut.GetMyProfileAsync("emp-1");

        // Assert — education sorted descending by GraduationYear
        dto.Education.Should().HaveCount(2);
        dto.Education[0].GraduationYear.Should().Be(2018);
        dto.Education[1].GraduationYear.Should().Be(2015);
    }

    // ── AddEducation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddEducationAsync_CreatesProfileAndReturnsDto_WhenNoProfileExists()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CreateEducationRequest("BSc Computer Science", "MIT", 2020, "With distinction");

        // Act
        var dto = await _sut.AddEducationAsync("emp-1", request);

        // Assert
        dto.Id.Should().NotBe(Guid.Empty);
        dto.Degree.Should().Be("BSc Computer Science");
        dto.Institution.Should().Be("MIT");
        dto.GraduationYear.Should().Be(2020);
        dto.Notes.Should().Be("With distinction");
        dto.CreatedUtc.Should().Be(FixedNow);
        dto.UpdatedUtc.Should().Be(FixedNow);
    }

    [Theory]
    [InlineData("", "MIT", 2020)]
    [InlineData("  ", "MIT", 2020)]
    [InlineData("BSc", "", 2020)]
    [InlineData("BSc", "  ", 2020)]
    public async Task AddEducationAsync_ThrowsArgumentException_ForBlankRequiredFields(
        string degree, string institution, int year)
    {
        // Arrange
        var request = new CreateEducationRequest(degree, institution, year, null);

        // Act
        var act = () => _sut.AddEducationAsync("emp-1", request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(9999)]
    public async Task AddEducationAsync_ThrowsArgumentOutOfRangeException_ForInvalidYear(int year)
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CreateEducationRequest("BSc", "MIT", year, null);

        // Act
        var act = () => _sut.AddEducationAsync("emp-1", request);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ── UpdateEducation ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateEducationAsync_ReturnsNull_WhenEntryNotFound()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var request = new UpdateEducationRequest("PhD", "Harvard", 2022, null);

        // Act
        var result = await _sut.UpdateEducationAsync("emp-1", Guid.NewGuid(), request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEducationAsync_PreservesCreatedUtcAndUpdatesOtherFields()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");
        var entry = EducationEntry.Create("BSc", "Old Uni", 2010, null, FixedNow.AddDays(-30));
        profile.AddEducation(entry);

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var later = FixedNow.AddHours(2);
        _timeProviderMock.Setup(tp => tp.GetUtcNow()).Returns(later);

        var request = new UpdateEducationRequest("MSc", "New Uni", 2012, "Honours");

        // Act
        var result = await _sut.UpdateEducationAsync("emp-1", entry.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Degree.Should().Be("MSc");
        result.Institution.Should().Be("New Uni");
        result.GraduationYear.Should().Be(2012);
        result.Notes.Should().Be("Honours");
        result.CreatedUtc.Should().Be(entry.CreatedUtc);
        result.UpdatedUtc.Should().Be(later);
    }

    // ── DeleteEducation ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteEducationAsync_ReturnsFalse_WhenEntryNotFound()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _sut.DeleteEducationAsync("emp-1", Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteEducationAsync_ReturnsTrue_WhenEntryDeleted()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");
        var entry = EducationEntry.Create("BSc", "Uni", 2015, null, FixedNow);
        profile.AddEducation(entry);

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteEducationAsync("emp-1", entry.Id);

        // Assert
        result.Should().BeTrue();
    }

    // ── Certificate: date validation ──────────────────────────────────────────

    [Fact]
    public async Task AddCertificateAsync_ThrowsArgumentException_WhenExpirationBeforeIssueDate()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var issueDate = new DateOnly(2023, 6, 1);
        var expirationDate = new DateOnly(2022, 1, 1); // before issue

        var request = new CreateCertificateRequest("AWS SAA", "Amazon", issueDate, expirationDate);

        // Act
        var act = () => _sut.AddCertificateAsync("emp-1", request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddCertificateAsync_Succeeds_WithNullExpirationDate()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CreateCertificateRequest("PMP", "PMI", new DateOnly(2023, 1, 1), null);

        // Act
        var dto = await _sut.AddCertificateAsync("emp-1", request);

        // Assert
        dto.ExpirationDate.Should().BeNull();
        dto.Name.Should().Be("PMP");
    }

    // ── Course: skills normalization ──────────────────────────────────────────

    [Fact]
    public async Task AddCourseAsync_NormalizesSkills_TrimsDeduplicatesAndDropsBlanks()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CreateCourseRequest(
            "Clean Architecture",
            "Pluralsight",
            new DateOnly(2024, 3, 15),
            ["  C#  ", "C#", " ", "SOLID", "solid"]);

        // Act
        var dto = await _sut.AddCourseAsync("emp-1", request);

        // Assert — blank removed, "C#" deduplicated, "solid" collapsed with "SOLID"
        dto.SkillsAcquired.Should().HaveCount(2);
        dto.SkillsAcquired.Should().Contain("C#");
        dto.SkillsAcquired.Should().Contain("SOLID");
    }

    [Fact]
    public async Task AddCourseAsync_ReturnsEmptySkills_WhenNullSkillsProvided()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompetenceProfile?)null);

        _repoMock
            .Setup(r => r.SaveAsync(It.IsAny<CompetenceProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new CreateCourseRequest(
            "Intro to Agile",
            "Udemy",
            new DateOnly(2024, 1, 10),
            null);

        // Act
        var dto = await _sut.AddCourseAsync("emp-1", request);

        // Assert
        dto.SkillsAcquired.Should().BeEmpty();
    }

    // ── Sorted read model ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProfileAsync_SortsCertificatesDescendingByIssueDate()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");
        profile.AddCertificate(CertificateEntry.Create("Old Cert", "Org", new DateOnly(2020, 1, 1), null, FixedNow));
        profile.AddCertificate(CertificateEntry.Create("New Cert", "Org", new DateOnly(2023, 6, 1), null, FixedNow));

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var dto = await _sut.GetMyProfileAsync("emp-1");

        // Assert
        dto.Certificates.Should().HaveCount(2);
        dto.Certificates[0].Name.Should().Be("New Cert");
        dto.Certificates[1].Name.Should().Be("Old Cert");
    }

    [Fact]
    public async Task GetMyProfileAsync_SortsCoursesDescendingByCompletionDate()
    {
        // Arrange
        var profile = new CompetenceProfile("emp-1");
        profile.AddCourse(CourseEntry.Create("Early Course", "Provider", new DateOnly(2019, 5, 1), null, FixedNow));
        profile.AddCourse(CourseEntry.Create("Recent Course", "Provider", new DateOnly(2024, 2, 1), null, FixedNow));

        _repoMock
            .Setup(r => r.GetByEmployeeIdAsync("emp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var dto = await _sut.GetMyProfileAsync("emp-1");

        // Assert
        dto.Courses.Should().HaveCount(2);
        dto.Courses[0].Name.Should().Be("Recent Course");
        dto.Courses[1].Name.Should().Be("Early Course");
    }
}
