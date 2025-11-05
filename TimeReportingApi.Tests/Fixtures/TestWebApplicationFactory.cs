using Microsoft.AspNetCore.Hosting;
using TimeReportingApi.Tests.Helpers;

namespace TimeReportingApi.Tests.Fixtures;

/// <summary>
/// Test web application factory that replaces Azure AD authentication with test authentication.
/// This allows integration tests to bypass Entra ID and use simple bearer tokens.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddTestAuthentication();
        });

        base.ConfigureWebHost(builder);
    }
}
