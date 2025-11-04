using Azure.Core;
using Azure.Identity;

namespace TimeReportingMcp.WebSocket.Services;

/// <summary>
/// Service for acquiring Azure Entra ID access tokens via Azure CLI authentication.
/// Implements token pass-through pattern where developers authenticate via 'az login'
/// and this service reads tokens from the Azure CLI cache.
/// </summary>
/// <remarks>
/// Uses AzureCliCredential to read tokens that were acquired when the developer
/// ran 'az login'. Tokens are cached with a 5-minute expiry buffer to minimize
/// repeated token acquisition calls.
///
/// For production deployments, switch to ManagedIdentityCredential or use
/// ChainedTokenCredential for automatic fallback.
/// </remarks>
public class TokenService
{
    private static readonly AzureCliCredential _credential = new();
    private AccessToken? _cachedToken;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string[] _scopes;
    private readonly ILogger<TokenService> _logger;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _logger = logger;

        var apiScope = configuration["AzureAd:ApiScope"];
        if (string.IsNullOrEmpty(apiScope))
        {
            throw new InvalidOperationException(
                "AzureAd:ApiScope not configured in appsettings.json. " +
                "Expected format: 'api://<app-id>/.default'");
        }

        _scopes = new[] { apiScope };
        _logger.LogInformation("TokenService initialized with scope: {Scope}", apiScope);
    }

    /// <summary>
    /// Get an access token for the configured API scope.
    /// Tokens are cached and automatically refreshed when they expire.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token string</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Azure CLI authentication is not available.
    /// User must run 'az login' first.
    /// </exception>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // Check cache with 5-minute expiry buffer
        if (_cachedToken.HasValue &&
            _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            _logger.LogDebug("Returning cached token (expires: {Expiry})", _cachedToken.Value.ExpiresOn);
            return _cachedToken.Value.Token;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cachedToken.HasValue &&
                _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                _logger.LogDebug("Returning cached token after lock (expires: {Expiry})", _cachedToken.Value.ExpiresOn);
                return _cachedToken.Value.Token;
            }

            _logger.LogInformation("Acquiring new token from Azure CLI for scope: {Scope}", _scopes[0]);

            // Acquire new token from Azure CLI
            var tokenRequest = new TokenRequestContext(_scopes);
            _cachedToken = await _credential.GetTokenAsync(tokenRequest, cancellationToken);

            _logger.LogInformation("Token acquired successfully (expires: {Expiry})", _cachedToken.Value.ExpiresOn);

            return _cachedToken.Value.Token;
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Azure CLI authentication failed. User must run 'az login' first.");
            throw new InvalidOperationException(
                "Azure CLI authentication required. Please run 'az login' and authenticate with your Azure account.",
                ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Clear the cached token. Useful for testing or forcing token refresh.
    /// </summary>
    public void ClearCache()
    {
        _lock.Wait();
        try
        {
            _cachedToken = null;
            _logger.LogInformation("Token cache cleared");
        }
        finally
        {
            _lock.Release();
        }
    }
}
