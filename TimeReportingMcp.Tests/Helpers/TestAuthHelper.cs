using System.Net.Http.Headers;

namespace TimeReportingMcp.Tests.Helpers;

/// <summary>
/// Helper methods for configuring test authentication in MCP tests.
/// </summary>
public static class TestAuthHelper
{
    /// <summary>
    /// Configures HTTP client with test bearer token for API authentication.
    /// NOTE: MCP integration tests require the API to use test authentication (TestAuthHandler),
    /// not Azure AD. These tests will skip if the API is configured with Azure AD.
    /// </summary>
    public static void ConfigureTestAuthentication(HttpClient client, string apiUrl)
    {
        client.BaseAddress = new Uri(apiUrl);
        // Use simple bearer token that TestAuthHandler accepts
        // If API is using Azure AD, tests will skip gracefully
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-bearer-token-12345");
    }
}
