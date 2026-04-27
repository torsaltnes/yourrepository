using Microsoft.AspNetCore.Http;

namespace GreenfieldArchitecture.Api.Filters;

/// <summary>
/// Endpoint filter that enforces a caller identity check before the handler executes.
/// Returns <c>401 Unauthorized</c> when the request carries neither an authenticated
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> name nor an
/// <c>X-Employee-Id</c> header.
/// This provides a lightweight authorization gate that is compatible with both the
/// current demo header-based identity scheme and any future JWT/cookie auth layer.
/// </summary>
public sealed class RequireUserIdentityFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var http = context.HttpContext;

        var hasClaimsIdentity = !string.IsNullOrWhiteSpace(http.User?.Identity?.Name);
        var hasHeader = !string.IsNullOrWhiteSpace(
            http.Request.Headers["X-Employee-Id"].ToString());

        if (!hasClaimsIdentity && !hasHeader)
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "A valid user identity is required. " +
                        "Provide an X-Employee-Id header or authenticate via the configured auth scheme.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
