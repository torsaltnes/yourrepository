using GreenfieldArchitecture.Application.Profile.Abstractions;
using Microsoft.AspNetCore.Http;

namespace GreenfieldArchitecture.Api.Context;

/// <summary>
/// Resolves the current employee identity from the active HTTP request.
/// Resolution order:
///   1. <c>HttpContext.User.Identity.Name</c> — populated by real auth middleware (JWT, cookie, etc.)
///   2. <c>X-Employee-Id</c> request header — used for development / demo scenarios.
/// Throws <see cref="InvalidOperationException"/> when neither source yields an identity
/// so that downstream code always receives a non-null, non-empty user id.
/// </summary>
/// <remarks>
/// This class lives in the Api project (not Infrastructure) because it has a direct
/// dependency on <see cref="IHttpContextAccessor"/>, which is an ASP.NET Core
/// abstraction.  The <see cref="ICurrentUserContext"/> interface remains in the
/// Application layer so the domain and service layers stay framework-agnostic.
/// </remarks>
public sealed class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    /// <inheritdoc />
    public string UserId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;

            // Prefer claims identity from a real auth middleware (future-proofs JWT upgrade).
            var claimsName = context?.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(claimsName))
                return claimsName;

            // Fall back to the explicit header used in the current demo/dev setup.
            var header = context?.Request.Headers["X-Employee-Id"].ToString();
            if (!string.IsNullOrWhiteSpace(header))
                return header;

            throw new InvalidOperationException(
                "No authenticated user identity is available on this request. " +
                "Provide an X-Employee-Id header or configure an authentication middleware.");
        }
    }
}
