using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Greenfield.Application.Dashboard;
using Greenfield.Infrastructure.Deviations;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Greenfield.Api.IntegrationTests.Dashboard;

/// <summary>
/// Integration tests for <c>GET /api/dashboard/summary</c>.
/// Spins up the full ASP.NET Core pipeline with the in-memory store pre-seeded
/// by <see cref="DeviationSeedData"/> (6 seed deviations).
/// </summary>
public sealed class DashboardEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public DashboardEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/dashboard/summary ────────────────────────────────────────

    [Fact]
    public async Task GetSummary_Returns200()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSummary_ResponseIsJson()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");

        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");
    }

    [Fact]
    public async Task GetSummary_TotalDeviations_MatchesSeedCount()
    {
        var summary = await FetchSummaryAsync();

        // Seed data has 6 deviations (DEV-001 through DEV-006).
        summary.TotalDeviations.Should().Be(6);
    }

    [Fact]
    public async Task GetSummary_OpenDeviations_ExcludesClosedSeedDeviation()
    {
        var summary = await FetchSummaryAsync();

        // DEV-001 is Closed; the remaining 5 are open.
        summary.OpenDeviations.Should().Be(5);
    }

    [Fact]
    public async Task GetSummary_ByStatus_ContainsAllStatuses()
    {
        var summary = await FetchSummaryAsync();

        // Seed covers: Closed(1), CorrectiveAction(1), UnderInvestigation(1),
        //              UnderAssessment(1), Registered(2).
        summary.ByStatus.Should().ContainKey("Closed");
        summary.ByStatus.Should().ContainKey("CorrectiveAction");
        summary.ByStatus.Should().ContainKey("UnderInvestigation");
        summary.ByStatus.Should().ContainKey("UnderAssessment");
        summary.ByStatus.Should().ContainKey("Registered");
    }

    [Fact]
    public async Task GetSummary_ByStatus_RegisteredCountIsTwo()
    {
        var summary = await FetchSummaryAsync();

        // DEV-005 and DEV-006 are Registered.
        summary.ByStatus["Registered"].Should().Be(2);
    }

    [Fact]
    public async Task GetSummary_BySeverity_ContainsExpectedKeys()
    {
        var summary = await FetchSummaryAsync();

        // Seed covers Critical(2), High(1), Medium(2), Low(1).
        summary.BySeverity.Should().ContainKey("Critical");
        summary.BySeverity.Should().ContainKey("High");
        summary.BySeverity.Should().ContainKey("Medium");
        summary.BySeverity.Should().ContainKey("Low");
    }

    [Fact]
    public async Task GetSummary_ByCategory_ContainsExpectedKeys()
    {
        var summary = await FetchSummaryAsync();

        // Seed covers Quality(2), Safety(1), Environmental(1), Process(1), Product(1).
        summary.ByCategory.Should().ContainKey("Quality");
        summary.ByCategory.Should().ContainKey("Safety");
        summary.ByCategory.Should().ContainKey("Environmental");
        summary.ByCategory.Should().ContainKey("Process");
        summary.ByCategory.Should().ContainKey("Product");
    }

    [Fact]
    public async Task GetSummary_MonthlyTrend_ReturnsSixPoints()
    {
        var summary = await FetchSummaryAsync();

        summary.MonthlyTrend.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetSummary_MonthlyTrend_MonthsAreInAscendingOrder()
    {
        var summary = await FetchSummaryAsync();

        var months = summary.MonthlyTrend.Select(p => p.Month).ToList();
        months.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetSummary_MonthlyTrend_CurrentMonthPresent()
    {
        var summary = await FetchSummaryAsync();

        var thisMonth = $"{DateTimeOffset.UtcNow:yyyy-MM}";
        summary.MonthlyTrend.Should().Contain(p => p.Month == thisMonth);
    }

    [Fact]
    public async Task GetSummary_RecentDeviations_AtMostFive()
    {
        var summary = await FetchSummaryAsync();

        // Seed has 6 deviations – recent list capped at 5.
        summary.RecentDeviations.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetSummary_RecentDeviations_OrderedByUpdatedAtDescending()
    {
        var summary = await FetchSummaryAsync();

        var timestamps = summary.RecentDeviations.Select(d => d.UpdatedAt).ToList();
        timestamps.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetSummary_RecentDeviations_HaveNonEmptyIds()
    {
        var summary = await FetchSummaryAsync();

        summary.RecentDeviations.Should().AllSatisfy(d => d.Id.Should().NotBeEmpty());
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    private async Task<DashboardSummaryDto> FetchSummaryAsync()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        summary.Should().NotBeNull();
        return summary!;
    }
}
