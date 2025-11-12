using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TimeReportingMcpSdk.Handlers;

/// <summary>
/// HTTP message handler that logs all outgoing requests and responses for debugging.
/// </summary>
public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Log request
        _logger.LogInformation("→ {Method} {Uri}", request.Method, request.RequestUri);

        // Log auth header with token details
        var authHeader = request.Headers.Authorization?.ToString() ?? "";
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);
            LogTokenClaims(token);
        }

        // Log body if present
        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            var preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
            _logger.LogInformation("  Body: {Body}", preview);
        }

        // Send request
        var response = await base.SendAsync(request, cancellationToken);

        // Log response
        var responseContent = response.Content != null
            ? await response.Content.ReadAsStringAsync(cancellationToken)
            : "";
        var responsePreview = responseContent.Length > 500
            ? responseContent.Substring(0, 500) + "..."
            : responseContent;

        _logger.LogInformation("← {StatusCode} {ReasonPhrase}: {Body}",
            (int)response.StatusCode, response.ReasonPhrase, responsePreview);

        return response;
    }

    private void LogTokenClaims(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "unique_name")?.Value ?? "unknown";
            var oid = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value ?? "unknown";
            var aclClaims = jwtToken.Claims.Where(c => c.Type.Contains("TimeReportingACL")).Select(c => c.Value).ToList();

            var aclString = aclClaims.Any() ? string.Join("; ", aclClaims) : "none";

            _logger.LogInformation("  Token: {Email} (oid: {Oid}), ACL: [{Acl}], exp: {Expiration}",
                email, oid, aclString, jwtToken.ValidTo.ToString("yyyy-MM-dd HH:mm"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to decode token: {Error}", ex.Message);
        }
    }

    private static string MaskToken(string authHeader)
    {
        if (string.IsNullOrEmpty(authHeader))
            return "";

        var parts = authHeader.Split(' ');
        if (parts.Length == 2 && parts[0] == "Bearer")
        {
            var token = parts[1];
            return $"Bearer {token.Substring(0, Math.Min(20, token.Length))}...{token.Substring(Math.Max(0, token.Length - 10))}";
        }

        return authHeader;
    }
}
