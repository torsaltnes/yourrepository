using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace GreenfieldArchitecture.Api.Tests.Infrastructure;

/// <summary>
/// Bootstraps the API for integration testing.
/// </summary>
public sealed class GreenfieldArchitectureApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with the
    /// <c>X-Employee-Id</c> identity header so that endpoints protected by
    /// <c>RequireUserIdentityFilter</c> accept the request.
    /// </summary>
    /// <param name="employeeId">
    /// The employee identity to impersonate. Defaults to <c>employee-001</c>.
    /// </param>
    public HttpClient CreateAuthenticatedClient(string employeeId = "employee-001")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Employee-Id", employeeId);
        return client;
    }
}
