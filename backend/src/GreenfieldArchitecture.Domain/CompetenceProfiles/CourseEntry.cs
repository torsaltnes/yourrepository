namespace GreenfieldArchitecture.Domain.CompetenceProfiles;

/// <summary>
/// Domain entity representing a completed professional course or training.
/// Immutable after construction; mutations return new instances.
/// </summary>
public sealed class CourseEntry
{
    private CourseEntry(
        Guid id,
        string name,
        string provider,
        DateOnly completionDate,
        IReadOnlyList<string> skillsAcquired,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        Id = id;
        Name = name;
        Provider = provider;
        CompletionDate = completionDate;
        SkillsAcquired = skillsAcquired;
        CreatedUtc = createdUtc;
        UpdatedUtc = updatedUtc;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string Provider { get; }
    public DateOnly CompletionDate { get; }

    /// <summary>Normalized, distinct, trimmed list of skills acquired during the course.</summary>
    public IReadOnlyList<string> SkillsAcquired { get; }

    public DateTimeOffset CreatedUtc { get; }
    public DateTimeOffset UpdatedUtc { get; }

    /// <summary>Creates a new <see cref="CourseEntry"/> with validated and normalized inputs.</summary>
    public static CourseEntry Create(
        string name,
        string provider,
        DateOnly completionDate,
        IEnumerable<string>? skillsAcquired,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(provider, nameof(provider));

        return new CourseEntry(
            Guid.NewGuid(),
            name.Trim(),
            provider.Trim(),
            completionDate,
            NormalizeSkills(skillsAcquired),
            now,
            now);
    }

    /// <summary>Returns an updated copy, preserving the original <see cref="CreatedUtc"/>.</summary>
    public CourseEntry Update(
        string name,
        string provider,
        DateOnly completionDate,
        IEnumerable<string>? skillsAcquired,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(provider, nameof(provider));

        return new CourseEntry(
            Id,
            name.Trim(),
            provider.Trim(),
            completionDate,
            NormalizeSkills(skillsAcquired),
            CreatedUtc,
            now);
    }

    /// <summary>
    /// Trims, deduplicates (case-insensitive), and drops blank entries.
    /// </summary>
    private static IReadOnlyList<string> NormalizeSkills(IEnumerable<string>? skills)
    {
        if (skills is null) return [];

        return [.. skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
