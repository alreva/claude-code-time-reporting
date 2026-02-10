using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TimeReportingMcpSdk.Services;

/// <summary>
/// Service for acquiring Azure Entra ID access tokens via multiple authentication methods:
/// - azure-cli: Reads tokens from Azure CLI's cache (default, requires 'az login')
/// - device-pairing: Gets tokens from device pairing backend (for headless devices like RPi)
/// </summary>
/// <remarks>
/// The authentication method is selected via AUTH_METHOD environment variable.
///
/// For azure-cli mode:
/// Uses AzureCliCredential which reads tokens from Azure CLI's own disk cache.
/// No application-level caching is performed since Azure CLI already caches tokens.
///
/// For device-pairing mode:
/// Calls the Azure Function backend to get tokens using device credentials.
/// Requires PAIRING_FUNCTION_URL, DEVICE_ID, and DEVICE_SECRET environment variables.
/// </remarks>
public class TokenService : IDisposable
{
    private readonly string[] _scopes;
    private readonly ILogger<TokenService> _logger;
    private readonly string _authMethod;
    private readonly DevicePairingConfig? _deviceConfig;
    private readonly HttpClient? _httpClient;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _logger = logger;
        _authMethod = Environment.GetEnvironmentVariable("AUTH_METHOD") ?? "azure-cli";

        var apiScope = configuration["AzureAd:ApiScope"];
        if (string.IsNullOrEmpty(apiScope))
        {
            throw new InvalidOperationException(
                "AzureAd:ApiScope not configured in appsettings.json. " +
                "Expected format: 'api://<app-id>/.default'");
        }

        _scopes = [apiScope];
        _logger.LogInformation("TokenService initialized with scope: {Scope}, auth method: {AuthMethod}",
            apiScope, _authMethod);

        if (_authMethod == "device-pairing")
        {
            var functionUrl = Environment.GetEnvironmentVariable("PAIRING_FUNCTION_URL");
            var deviceId = Environment.GetEnvironmentVariable("DEVICE_ID");
            var deviceSecret = Environment.GetEnvironmentVariable("DEVICE_SECRET");

            if (string.IsNullOrEmpty(functionUrl) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(deviceSecret))
            {
                throw new InvalidOperationException(
                    "Device pairing requires PAIRING_FUNCTION_URL, DEVICE_ID, and DEVICE_SECRET environment variables.");
            }

            _deviceConfig = new DevicePairingConfig
            {
                FunctionUrl = functionUrl,
                DeviceId = deviceId,
                DeviceSecret = deviceSecret
            };

            _httpClient = new HttpClient();
            _logger.LogInformation("Device pairing configured for device: {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Get an access token for the configured API scope.
    /// Uses the authentication method specified by AUTH_METHOD environment variable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token string</returns>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return _authMethod switch
        {
            "device-pairing" => await GetTokenFromDevicePairingAsync(cancellationToken),
            "azure-cli" or _ => await GetTokenFromAzureCliAsync(cancellationToken)
        };
    }

    private async Task<string> GetTokenFromAzureCliAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Acquiring token from Azure CLI for scope: {Scope}", _scopes[0]);

            // Create new AzureCliCredential instance for each request
            // This ensures we always get the current Azure CLI user's token
            var credential = new AzureCliCredential();
            var tokenRequest = new TokenRequestContext(_scopes);
            var token = await credential.GetTokenAsync(tokenRequest, cancellationToken);

            _logger.LogDebug("Token acquired successfully (expires: {Expiry})", token.ExpiresOn);

            // DEBUG: Decode token to verify which user's token we got
            LogTokenClaims(token.Token);

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

    private async Task<string> GetTokenFromDevicePairingAsync(CancellationToken cancellationToken)
    {
        if (_deviceConfig == null || _httpClient == null)
        {
            throw new InvalidOperationException("Device pairing not configured");
        }

        try
        {
            _logger.LogInformation("Acquiring token from device pairing backend for device: {DeviceId}",
                _deviceConfig.DeviceId);

            var request = new DeviceTokenRequest
            {
                DeviceId = _deviceConfig.DeviceId,
                DeviceSecret = _deviceConfig.DeviceSecret
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_deviceConfig.FunctionUrl}/api/device/token",
                request,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new InvalidOperationException(
                    $"Device '{_deviceConfig.DeviceId}' is registered but not linked to a user. " +
                    "Scan the QR code to link your account.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    "Device authentication failed. The refresh token may have expired. " +
                    "Scan the QR code to re-authenticate.");
            }

            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<DeviceTokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Failed to parse token response");

            _logger.LogInformation("Token acquired from device pairing (expires in: {ExpiresIn}s)",
                tokenResponse.ExpiresIn);

            // Log token claims for debugging
            LogTokenClaims(tokenResponse.AccessToken);

            return tokenResponse.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to contact device pairing backend at {Url}",
                _deviceConfig.FunctionUrl);
            throw new InvalidOperationException(
                $"Failed to contact device pairing backend. Ensure the function is running and accessible.",
                ex);
        }
    }

    private void LogTokenClaims(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var email = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == "email" || c.Type == "unique_name" || c.Type == "preferred_username")?.Value ?? "unknown";
            var oid = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value ?? "unknown";
            _logger.LogInformation("[TokenService] Retrieved token for user: {Email} (oid: {Oid})", email, oid);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to decode token claims");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class DevicePairingConfig
    {
        public string FunctionUrl { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceSecret { get; set; } = string.Empty;
    }

    private class DeviceTokenRequest
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [JsonPropertyName("deviceSecret")]
        public string DeviceSecret { get; set; } = string.Empty;
    }

    private class DeviceTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
    }
}
