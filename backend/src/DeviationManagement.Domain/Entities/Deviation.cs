using DeviationManagement.Domain.Enums;

namespace DeviationManagement.Domain.Entities;

public sealed class Deviation
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DeviationSeverity Severity { get; private set; }
    public DeviationStatus Status { get; private set; }
    public string ReportedBy { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF / serializer constructor
    private Deviation() { }

    public Deviation(
        Guid id,
        string title,
        string description,
        DeviationSeverity severity,
        DeviationStatus status,
        string reportedBy,
        DateTimeOffset occurredAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Severity = severity;
        Status = status;
        ReportedBy = reportedBy;
        OccurredAt = occurredAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public void Update(
        string title,
        string description,
        DeviationSeverity severity,
        DeviationStatus status,
        string reportedBy,
        DateTimeOffset occurredAt)
    {
        Title = title;
        Description = description;
        Severity = severity;
        Status = status;
        ReportedBy = reportedBy;
        OccurredAt = occurredAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
