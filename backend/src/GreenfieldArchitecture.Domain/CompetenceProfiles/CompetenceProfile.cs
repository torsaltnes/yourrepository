namespace GreenfieldArchitecture.Domain.CompetenceProfiles;

/// <summary>
/// Aggregate root representing the competence profile of a single employee.
/// Contains typed collections for education, certificates, and courses.
/// Ownership is defined by <see cref="EmployeeId"/>.
/// </summary>
public sealed class CompetenceProfile
{
    private readonly List<EducationEntry> _education = [];
    private readonly List<CertificateEntry> _certificates = [];
    private readonly List<CourseEntry> _courses = [];

    public CompetenceProfile(string employeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeId, nameof(employeeId));
        EmployeeId = employeeId;
    }

    public string EmployeeId { get; }

    public IReadOnlyList<EducationEntry> Education => _education;
    public IReadOnlyList<CertificateEntry> Certificates => _certificates;
    public IReadOnlyList<CourseEntry> Courses => _courses;

    // ── Education ──────────────────────────────────────────────────────────────

    public void AddEducation(EducationEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        _education.Add(entry);
    }

    /// <returns><c>true</c> when the entry was found and replaced; <c>false</c> when not found.</returns>
    public bool UpdateEducation(EducationEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        var idx = _education.FindIndex(e => e.Id == entry.Id);
        if (idx < 0) return false;
        _education[idx] = entry;
        return true;
    }

    /// <returns><c>true</c> when the entry was found and removed; <c>false</c> when not found.</returns>
    public bool DeleteEducation(Guid entryId) => _education.RemoveAll(e => e.Id == entryId) > 0;

    // ── Certificates ───────────────────────────────────────────────────────────

    public void AddCertificate(CertificateEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        _certificates.Add(entry);
    }

    /// <returns><c>true</c> when the entry was found and replaced; <c>false</c> when not found.</returns>
    public bool UpdateCertificate(CertificateEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        var idx = _certificates.FindIndex(c => c.Id == entry.Id);
        if (idx < 0) return false;
        _certificates[idx] = entry;
        return true;
    }

    /// <returns><c>true</c> when the entry was found and removed; <c>false</c> when not found.</returns>
    public bool DeleteCertificate(Guid entryId) => _certificates.RemoveAll(c => c.Id == entryId) > 0;

    // ── Courses ────────────────────────────────────────────────────────────────

    public void AddCourse(CourseEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        _courses.Add(entry);
    }

    /// <returns><c>true</c> when the entry was found and replaced; <c>false</c> when not found.</returns>
    public bool UpdateCourse(CourseEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry, nameof(entry));
        var idx = _courses.FindIndex(c => c.Id == entry.Id);
        if (idx < 0) return false;
        _courses[idx] = entry;
        return true;
    }

    /// <returns><c>true</c> when the entry was found and removed; <c>false</c> when not found.</returns>
    public bool DeleteCourse(Guid entryId) => _courses.RemoveAll(c => c.Id == entryId) > 0;
}
