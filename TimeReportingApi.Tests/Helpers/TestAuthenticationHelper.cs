using Microsoft.AspNetCore.Authentication;
using TimeReportingApi.Tests.Handlers;

namespace TimeReportingApi.Tests.Helpers;

/// <summary>
/// Helper methods for configuring test authentication in integration tests.
/// </summary>
public static class TestAuthenticationHelper
{
    /// <summary>
    /// Adds test authentication scheme that bypasses Azure AD authentication.
    /// Call this in ConfigureServices within WithWebHostBuilder.
    /// </summary>
    public static IServiceCollection AddTestAuthentication(this IServiceCollection services)
    {
        // Add test authentication scheme that accepts any bearer token
        services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

        // Override default authentication scheme
        services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultAuthenticateScheme = "Test";
            options.DefaultChallengeScheme = "Test";
        });

        return services;
    }
}
