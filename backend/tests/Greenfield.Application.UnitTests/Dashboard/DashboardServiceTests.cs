using FluentAssertions;
using Greenfield.Application.Abstractions;
using Greenfield.Application.Dashboard;
using Greenfield.Domain.Deviations;
using Moq;
using Xunit;

namespace Greenfield.Application.UnitTests.Dashboard;

/// <summary>
/// Unit tests for <see cref="DashboardService"/> covering summary aggregations,
/// breakdowns, monthly trend, and recent-deviations ordering.
/// </summary>
public sealed class DashboardServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    private static Deviation Make(
        Guid? id = null,
        DeviationStatus status = DeviationStatus.Registered,
        DeviationSeverity severity = DeviationSeverity.Medium,
        DeviationCategory category = DeviationCategory.Quality,
        DateTimeOffset? dueDate = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? updatedAt = null)
    {
        var d = new Deviation
        {
            Id       = id ?? Guid.NewGuid(),
            Title    = "Test",
            Status   = status,
            Severity = severity,
            Category = category,
            DueDate  = dueDate,
        };

        if (createdAt.HasValue)
            typeof(Deviation)
                .GetProperty(nameof(Deviation.CreatedAt))!
                .SetValue(d, createdAt.Value);

        if (updatedAt.HasValue)
            d.UpdatedAt = updatedAt.Value;

        return d;
    }

    private static (DashboardService svc, Mock<IDeviationRepository> repo) Build(
        IEnumerable<Deviation>? seed = null)
    {
        var list = (seed ?? []).ToList();
        var mock = new Mock<IDeviationRepository>();

        mock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => list.AsReadOnly());

        return (new DashboardService(mock.Object), mock);
    }

    // ── GetSummaryAsync – totals ───────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_EmptyStore_ReturnsAllZeroTotals()
    {
        var (svc, _) = Build();

        var summary = await svc.GetSummaryAsync();

        summary.TotalDeviations.Should().Be(0);
        summary.OpenDeviations.Should().Be(0);
        summary.OverdueDeviations.Should().Be(0);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsTotal()
    {
        var seed = new[] { Make(), Make(), Make() };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.TotalDeviations.Should().Be(3);
    }

    [Fact]
    public async Task GetSummaryAsync_OpenDeviations_ExcludesClosed()
    {
        var seed = new[]
        {
            Make(status: DeviationStatus.Registered),
            Make(status: DeviationStatus.Closed),
            Make(status: DeviationStatus.UnderAssessment),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.OpenDeviations.Should().Be(2);
    }

    [Fact]
    public async Task GetSummaryAsync_OverdueDeviations_OpenAndPastDueDate()
    {
        var past = DateTimeOffset.UtcNow.AddDays(-1);
        var future = DateTimeOffset.UtcNow.AddDays(5);

        var seed = new[]
        {
            Make(status: DeviationStatus.Registered,      dueDate: past),    // overdue
            Make(status: DeviationStatus.UnderAssessment, dueDate: future),  // not overdue
            Make(status: DeviationStatus.Closed,          dueDate: past),    // closed – excluded
            Make(status: DeviationStatus.Registered,      dueDate: null),    // no due date
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.OverdueDeviations.Should().Be(1);
    }

    // ── GetSummaryAsync – breakdowns ──────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ByStatus_CorrectCounts()
    {
        var seed = new[]
        {
            Make(status: DeviationStatus.Registered),
            Make(status: DeviationStatus.Registered),
            Make(status: DeviationStatus.Closed),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.ByStatus["Registered"].Should().Be(2);
        summary.ByStatus["Closed"].Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_BySeverity_CorrectCounts()
    {
        var seed = new[]
        {
            Make(severity: DeviationSeverity.Critical),
            Make(severity: DeviationSeverity.Critical),
            Make(severity: DeviationSeverity.Low),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.BySeverity["Critical"].Should().Be(2);
        summary.BySeverity["Low"].Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_ByCategory_CorrectCounts()
    {
        var seed = new[]
        {
            Make(category: DeviationCategory.Safety),
            Make(category: DeviationCategory.Quality),
            Make(category: DeviationCategory.Safety),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.ByCategory["Safety"].Should().Be(2);
        summary.ByCategory["Quality"].Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_Breakdowns_EmptyStore_ReturnsEmptyDictionaries()
    {
        var (svc, _) = Build();

        var summary = await svc.GetSummaryAsync();

        summary.ByStatus.Should().BeEmpty();
        summary.BySeverity.Should().BeEmpty();
        summary.ByCategory.Should().BeEmpty();
    }

    // ── GetSummaryAsync – monthly trend ───────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_MonthlyTrend_ReturnsSixPoints()
    {
        var (svc, _) = Build();

        var summary = await svc.GetSummaryAsync();

        summary.MonthlyTrend.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetSummaryAsync_MonthlyTrend_OldestFirst()
    {
        var (svc, _) = Build();

        var summary = await svc.GetSummaryAsync();
        var months = summary.MonthlyTrend.Select(m => m.Month).ToList();

        months.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetSummaryAsync_MonthlyTrend_MonthFormatIsYyyyMm()
    {
        var (svc, _) = Build();

        var summary = await svc.GetSummaryAsync();

        summary.MonthlyTrend.Should().AllSatisfy(p =>
            p.Month.Should().MatchRegex(@"^\d{4}-\d{2}$"));
    }

    [Fact]
    public async Task GetSummaryAsync_MonthlyTrend_CountsDeviationsCreatedInEachMonth()
    {
        var now = DateTimeOffset.UtcNow;
        var thisMonth  = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var lastMonth  = thisMonth.AddMonths(-1);

        var seed = new[]
        {
            Make(createdAt: thisMonth),
            Make(createdAt: thisMonth),
            Make(createdAt: lastMonth),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        var current = $"{now.Year:D4}-{now.Month:D2}";
        var previous = $"{lastMonth.Year:D4}-{lastMonth.Month:D2}";

        summary.MonthlyTrend.Single(p => p.Month == current).Count.Should().Be(2);
        summary.MonthlyTrend.Single(p => p.Month == previous).Count.Should().Be(1);
    }

    // ── GetSummaryAsync – recent deviations ───────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_RecentDeviations_LimitedToFive()
    {
        var seed = Enumerable.Range(1, 8).Select(_ => Make()).ToList();
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.RecentDeviations.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetSummaryAsync_RecentDeviations_OrderedByUpdatedAtDescending()
    {
        var now = DateTimeOffset.UtcNow;
        var seed = new[]
        {
            Make(updatedAt: now.AddDays(-3)),
            Make(updatedAt: now.AddDays(-1)),
            Make(updatedAt: now.AddDays(-2)),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        var timestamps = summary.RecentDeviations.Select(d => d.UpdatedAt).ToList();
        timestamps.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetSummaryAsync_RecentDeviations_LessThanFive_ReturnsAll()
    {
        var seed = new[] { Make(), Make() };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.RecentDeviations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSummaryAsync_RecentDeviations_ContainsCorrectIds()
    {
        var now = DateTimeOffset.UtcNow;
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var seed = new[]
        {
            Make(id: id1, updatedAt: now.AddDays(-1)),
            Make(id: id2, updatedAt: now.AddDays(-2)),
        };
        var (svc, _) = Build(seed);

        var summary = await svc.GetSummaryAsync();

        summary.RecentDeviations[0].Id.Should().Be(id1);
        summary.RecentDeviations[1].Id.Should().Be(id2);
    }
}
