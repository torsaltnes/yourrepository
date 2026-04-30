namespace Greenfield.Application.Dashboard;

/// <summary>A single calendar-month's deviation count, used to drive the trend chart.</summary>
/// <param name="Month">Year-month in <c>YYYY-MM</c> format, e.g. <c>2026-04</c>.</param>
/// <param name="Count">Number of deviations created in that month.</param>
public sealed record MonthlyTrendPoint(string Month, int Count);
