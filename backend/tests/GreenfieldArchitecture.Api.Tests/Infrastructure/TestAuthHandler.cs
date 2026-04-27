using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GreenfieldArchitecture.Api.Tests.Infrastructure;

/// <summary>
/// A fake authentication handler used in integration tests.
/// Every incoming request is unconditionally authenticated as a test user
/// so that mutation endpoints protected with <c>RequireAuthorization()</c>
/// can be exercised without a real JWT token.
/// <para>
/// The authenticated user ID defaults to <see cref="DefaultUserId"/> ("test-user-id").
/// To authenticate as a different user, set <see cref="AuthenticationSchemeOptions.ClaimsIssuer"/>
/// to the desired user ID when registering the scheme — see
/// <see cref="GreenfieldArchitectureApiFactory.CreateClientForUser"/>.
/// </para>
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string DefaultUserId = "test-user-id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // ClaimsIssuer is repurposed as the user-ID slot for cross-user tests.
        var userId = string.IsNullOrWhiteSpace(Options.ClaimsIssuer)
            ? DefaultUserId
            : Options.ClaimsIssuer;

        Claim[] claims =
        [
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Administrator"),
        ];

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
