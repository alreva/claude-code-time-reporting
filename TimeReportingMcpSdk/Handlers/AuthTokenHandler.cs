using System.Net.Http.Headers;
using TimeReportingMcpSdk.Services;

namespace TimeReportingMcpSdk.Handlers;

/// <summary>
/// HTTP message handler that dynamically adds authentication tokens to outgoing requests.
/// Fetches the current token from TokenService for each request, ensuring retries use refreshed tokens.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly TokenService _tokenService;

    public AuthTokenHandler(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get current token (may be cached or freshly acquired)
        var token = await _tokenService.GetTokenAsync(cancellationToken);

        // Add Bearer token to request
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
