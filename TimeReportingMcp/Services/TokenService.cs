using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TimeReportingMcp.Services;

/// <summary>
/// Service for acquiring Azure Entra ID access tokens via Azure CLI authentication.
/// Implements token pass-through pattern where developers authenticate via 'az login'
/// and this service reads tokens from the Azure CLI cache.
/// </summary>
/// <remarks>
/// Uses AzureCliCredential which reads tokens from Azure CLI's own disk cache.
/// No application-level caching is performed since Azure CLI already caches tokens
/// efficiently and handles token refresh automatically.
///
/// This approach provides immediate user switch detection when 'az login' is called
/// with a different account, as each call to GetTokenAsync() gets the current user's
/// token from Azure CLI's cache.
///
/// For production deployments, switch to ManagedIdentityCredential or use
/// ChainedTokenCredential for automatic fallback.
/// </remarks>
public class TokenService
{
    private static readonly AzureCliCredential _credential = new();
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
    /// Returns the current Azure CLI user's token from Azure CLI's cache.
    /// Azure CLI handles token refresh automatically when tokens expire.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token string for the current Azure CLI user</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Azure CLI authentication is not available.
    /// User must run 'az login' first.
    /// </exception>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Acquiring token from Azure CLI for scope: {Scope}", _scopes[0]);

            // Get token from Azure CLI cache (no application-level caching)
            // This ensures we always get the current Azure CLI user's token
            var tokenRequest = new TokenRequestContext(_scopes);
            var token = await _credential.GetTokenAsync(tokenRequest, cancellationToken);

            _logger.LogDebug("Token acquired successfully (expires: {Expiry})", token.ExpiresOn);

            return token.Token;
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Azure CLI authentication failed. User must run 'az login' first.");
            throw new InvalidOperationException(
                "Azure CLI authentication required. Please run 'az login' and authenticate with your Azure account.",
                ex);
        }
    }
}
