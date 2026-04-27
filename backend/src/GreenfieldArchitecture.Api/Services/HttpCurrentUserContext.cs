using System.Security.Claims;
using GreenfieldArchitecture.Api.Abstractions;

namespace GreenfieldArchitecture.Api.Services;

/// <summary>
/// Resolves the authenticated employee identifier from the current <see cref="IHttpContextAccessor"/>.
/// </summary>
public sealed class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public string EmployeeId
    {
        get
        {
            var id = httpContextAccessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException(
                    "No authenticated employee identifier found in the current request context.");

            return id;
        }
    }
}
