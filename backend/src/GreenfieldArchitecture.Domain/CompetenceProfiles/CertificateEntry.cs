namespace GreenfieldArchitecture.Domain.CompetenceProfiles;

/// <summary>
/// Domain entity representing a professional certificate or credential.
/// Immutable after construction; mutations return new instances.
/// </summary>
public sealed class CertificateEntry
{
    private CertificateEntry(
        Guid id,
        string name,
        string issuingOrganization,
        DateOnly issueDate,
        DateOnly? expirationDate,
        DateTimeOffset createdUtc,
        DateTimeOffset updatedUtc)
    {
        Id = id;
        Name = name;
        IssuingOrganization = issuingOrganization;
        IssueDate = issueDate;
        ExpirationDate = expirationDate;
        CreatedUtc = createdUtc;
        UpdatedUtc = updatedUtc;
    }

    public Guid Id { get; }
    public string Name { get; }
    public string IssuingOrganization { get; }
    public DateOnly IssueDate { get; }
    public DateOnly? ExpirationDate { get; }
    public DateTimeOffset CreatedUtc { get; }
    public DateTimeOffset UpdatedUtc { get; }

    /// <summary>Creates a new <see cref="CertificateEntry"/> with validated inputs.</summary>
    public static CertificateEntry Create(
        string name,
        string issuingOrganization,
        DateOnly issueDate,
        DateOnly? expirationDate,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(issuingOrganization, nameof(issuingOrganization));
        ValidateExpirationDate(issueDate, expirationDate);

        return new CertificateEntry(
            Guid.NewGuid(),
            name.Trim(),
            issuingOrganization.Trim(),
            issueDate,
            expirationDate,
            now,
            now);
    }

    /// <summary>Returns an updated copy, preserving the original <see cref="CreatedUtc"/>.</summary>
    public CertificateEntry Update(
        string name,
        string issuingOrganization,
        DateOnly issueDate,
        DateOnly? expirationDate,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(issuingOrganization, nameof(issuingOrganization));
        ValidateExpirationDate(issueDate, expirationDate);

        return new CertificateEntry(
            Id,
            name.Trim(),
            issuingOrganization.Trim(),
            issueDate,
            expirationDate,
            CreatedUtc,
            now);
    }

    private static void ValidateExpirationDate(DateOnly issueDate, DateOnly? expirationDate)
    {
        if (expirationDate.HasValue && expirationDate.Value < issueDate)
            throw new ArgumentException(
                "Expiration date cannot be earlier than the issue date.",
                nameof(expirationDate));
    }
}
