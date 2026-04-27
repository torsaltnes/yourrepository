namespace GreenfieldArchitecture.Api.Abstractions;

/// <summary>
/// Resolves the authenticated employee's identifier from the current HTTP request context.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// The stable identifier of the currently authenticated employee.
    /// Sourced from the <c>sub</c> / <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/> JWT claim.
    /// </summary>
    string EmployeeId { get; }
}
