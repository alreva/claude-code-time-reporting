using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using TimeReportingMcp.Services;

namespace TimeReportingMcp.Handlers;

/// <summary>
/// HTTP message handler that automatically refreshes the authentication token on 401 Unauthorized responses.
/// Implements a retry mechanism with token refresh for handling expired tokens transparently.
/// </summary>
public class TokenRefreshHandler : DelegatingHandler
{
    private readonly TokenService _tokenService;
    private readonly ILogger<TokenRefreshHandler> _logger;
    private const int MaxRetries = 2;

    public TokenRefreshHandler(TokenService tokenService, ILogger<TokenRefreshHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // First attempt with the current token
        var response = await base.SendAsync(request, cancellationToken);

        // If unauthorized, retry with a fresh token
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized. Attempting to refresh token and retry...");

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                // Clear cached token and get a fresh one
                _tokenService.ClearCache();
                var newToken = await _tokenService.GetTokenAsync(cancellationToken);

                // Clone the request with the new token (requests can only be sent once)
                var newRequest = await CloneRequestAsync(request, cancellationToken);
                newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                _logger.LogInformation("Retry attempt {Attempt}/{MaxRetries} with refreshed token", attempt, MaxRetries);

                // Dispose the old response
                response.Dispose();

                // Send the request with the new token
                response = await base.SendAsync(newRequest, cancellationToken);

                // If successful, return
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                {
                    _logger.LogInformation("Request succeeded after token refresh");
                    return response;
                }

                _logger.LogWarning("Retry attempt {Attempt} still received 401 Unauthorized", attempt);
            }

            _logger.LogError("All retry attempts failed. Token refresh did not resolve the authentication issue.");
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(content);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
