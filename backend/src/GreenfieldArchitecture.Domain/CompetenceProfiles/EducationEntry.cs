namespace GreenfieldArchitecture.Domain.CompetenceProfiles;

/// <summary>
/// Domain entity representing an employee's completed degree or qualification.
/// Immutable after construction; mutations return new instances.
/// </summary>
public sealed class EducationEntry
{
    private EducationEntry(
        Guid id,
        string degree,
        string institution,
        int graduationYear,
        string? notes,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        Id = id;
        Degree = degree;
        Institution = institution;
        GraduationYear = graduationYear;
        Notes = notes;
        CreatedUtc = createdUtc;
        UpdatedUtc = updatedUtc;
    }

    public Guid Id { get; }
    public string Degree { get; }
    public string Institution { get; }
    public int GraduationYear { get; }
    public string? Notes { get; }
    public DateTimeOffset CreatedUtc { get; }
    public DateTimeOffset UpdatedUtc { get; }

    /// <summary>Creates a new <see cref="EducationEntry"/> with validated inputs.</summary>
    public static EducationEntry Create(
        string degree,
        string institution,
        int graduationYear,
        string? notes,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(degree, nameof(degree));
        ArgumentException.ThrowIfNullOrWhiteSpace(institution, nameof(institution));
        ValidateGraduationYear(graduationYear, now);

        return new EducationEntry(
            Guid.NewGuid(),
            degree.Trim(),
            institution.Trim(),
            graduationYear,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            now,
            now);
    }

    /// <summary>Returns an updated copy, preserving the original <see cref="CreatedUtc"/>.</summary>
    public EducationEntry Update(
        string degree,
        string institution,
        int graduationYear,
        string? notes,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(degree, nameof(degree));
        ArgumentException.ThrowIfNullOrWhiteSpace(institution, nameof(institution));
        ValidateGraduationYear(graduationYear, now);

        return new EducationEntry(
            Id,
            degree.Trim(),
            institution.Trim(),
            graduationYear,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedUtc,
            now);
    }

    private static void ValidateGraduationYear(int year, DateTimeOffset now)
    {
        var maxYear = now.Year + 1;
        if (year < 1900 || year > maxYear)
            throw new ArgumentOutOfRangeException(
                nameof(year),
                $"Graduation year must be between 1900 and {maxYear}.");
    }
}
