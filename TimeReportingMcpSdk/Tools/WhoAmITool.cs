using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Services;

namespace TimeReportingMcpSdk.Tools;

/// <summary>
/// Tool to display current user token information and ACL permissions
/// </summary>
[McpServerToolType]
public class WhoAmITool
{
    private readonly TokenService _tokenService;

    public WhoAmITool(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [McpServerTool(
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("""
                 Display current user identity and permissions from JWT token

                 Shows:
                 - User ID (oid) - Azure AD Object ID
                 - Email address
                 - Display name
                 - Token expiration
                 - ACL permissions (extn.TimeReportingACLv2)
                 - All token claims for debugging

                 Use Cases:
                 - Verify which user identity the MCP server is using
                 - Check ACL permissions for project access
                 - Debug authentication issues
                 - Confirm token expiration time

                 Returns formatted text with:
                 1. User identity information
                 2. Token metadata (issued, expires)
                 3. ACL permission entries (Project/CODE=V,E,T,A,M)
                 4. All JWT claims

                 Note: Requires Azure CLI authentication (az login)
                 """)]
    public async Task<string> WhoAmI()
    {
        try
        {
            // Get access token
            var token = await _tokenService.GetTokenAsync();

            // Parse JWT token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Build formatted output
            var sb = new StringBuilder();
            sb.AppendLine("=== Current User Identity ===\n");

            // Extract user identity claims
            var oid = jwtToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value ?? "N/A";
            var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == "unique_name" || c.Type == "preferred_username")?.Value ?? "N/A";
            var name = jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "N/A";

            sb.AppendLine($"User ID (oid):  {oid}");
            sb.AppendLine($"Email:          {email}");
            sb.AppendLine($"Name:           {name}");
            sb.AppendLine();

            // Token metadata
            sb.AppendLine("=== Token Metadata ===\n");
            var issuedAt = jwtToken.IssuedAt.ToLocalTime();
            var expiresAt = jwtToken.ValidTo.ToLocalTime();
            var timeRemaining = expiresAt - DateTime.Now;

            sb.AppendLine($"Issued:         {issuedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Expires:        {expiresAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Time Remaining: {timeRemaining.Hours}h {timeRemaining.Minutes}m");
            sb.AppendLine($"Audience:       {jwtToken.Audiences.FirstOrDefault() ?? "N/A"}");
            sb.AppendLine();

            // ACL permissions
            var aclClaims = jwtToken.Claims.Where(c => c.Type == "extn.TimeReportingACLv2").ToList();
            sb.AppendLine("=== ACL Permissions ===\n");

            if (aclClaims.Any())
            {
                foreach (var claim in aclClaims)
                {
                    sb.AppendLine($"  • {claim.Value}");
                }
            }
            else
            {
                sb.AppendLine("  No ACL permissions found");
            }
            sb.AppendLine();

            // All claims (for debugging)
            sb.AppendLine("=== All Token Claims ===\n");
            var claimsByType = jwtToken.Claims
                .GroupBy(c => c.Type)
                .OrderBy(g => g.Key);

            foreach (var group in claimsByType)
            {
                var values = group.Select(c => c.Value).ToList();
                if (values.Count == 1)
                {
                    sb.AppendLine($"{group.Key,-30} {values[0]}");
                }
                else
                {
                    sb.AppendLine($"{group.Key,-30} [{string.Join(", ", values)}]");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}\n\nMake sure you're authenticated with Azure CLI (az login)";
        }
    }
}
