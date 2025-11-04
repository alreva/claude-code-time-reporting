using System.Security.Claims;

namespace TimeReportingApi.Extensions;

/// <summary>
/// Extension methods for extracting user information from ClaimsPrincipal.
/// Used to populate user tracking fields (UserId, UserEmail, UserName) in TimeEntry entities.
/// </summary>
/// <remarks>
/// These extensions extract claims from Azure Entra ID JWT tokens that have been
/// validated by Microsoft.Identity.Web authentication middleware.
///
/// Claims are extracted from the authenticated user's token and used to track
/// which user created or modified time entries.
///
/// See ADR 0010: WebSocket MCP Transport with Azure Entra ID Authentication
/// See Phase 14 - User tracking for time entries
/// </remarks>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extract the user's unique identifier from the token.
    /// </summary>
    /// <param name="user">The authenticated user's ClaimsPrincipal</param>
    /// <returns>User's object ID (oid) or subject identifier (sub), or null if not found</returns>
    /// <remarks>
    /// <para><strong>Claim Priority:</strong></para>
    /// <list type="number">
    /// <item><description>"oid" - Azure AD Object ID (preferred, unique per user in tenant)</description></item>
    /// <item><description>"sub" - Subject identifier (fallback, JWT standard claim)</description></item>
    /// </list>
    /// <para><strong>Example Values:</strong></para>
    /// <list type="bullet">
    /// <item><description>"a1b2c3d4-e5f6-7890-abcd-ef1234567890" (Azure AD Object ID)</description></item>
    /// </list>
    /// <para><strong>Use Case:</strong> Primary key for filtering time entries by user, audit trails</para>
    /// </remarks>
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        // Try Azure AD Object ID first (oid claim)
        var oid = user.FindFirst("oid")?.Value;
        if (!string.IsNullOrEmpty(oid))
        {
            return oid;
        }

        // Fallback to standard subject claim (sub)
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return sub;
    }

    /// <summary>
    /// Extract the user's email address from the token.
    /// </summary>
    /// <param name="user">The authenticated user's ClaimsPrincipal</param>
    /// <returns>User's email address, or null if not found</returns>
    /// <remarks>
    /// <para><strong>Claim Source:</strong> "email" optional claim configured in Entra ID token configuration</para>
    /// <para><strong>Example Values:</strong></para>
    /// <list type="bullet">
    /// <item><description>"john.doe@company.com"</description></item>
    /// <item><description>"jane.smith@company.com"</description></item>
    /// </list>
    /// <para><strong>Use Case:</strong> Display in reports, UI, email notifications</para>
    /// <para><strong>Note:</strong> Requires "email" optional claim to be configured in Azure AD app registration</para>
    /// </remarks>
    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        // Try email claim first
        var email = user.FindFirst("email")?.Value;
        if (!string.IsNullOrEmpty(email))
        {
            return email;
        }

        // Fallback to standard email claim type
        var emailClaim = user.FindFirst(ClaimTypes.Email)?.Value;
        return emailClaim;
    }

    /// <summary>
    /// Extract the user's display name from the token.
    /// </summary>
    /// <param name="user">The authenticated user's ClaimsPrincipal</param>
    /// <returns>User's display name, or null if not found</returns>
    /// <remarks>
    /// <para><strong>Claim Priority:</strong></para>
    /// <list type="number">
    /// <item><description>"name" - Full display name (preferred)</description></item>
    /// <item><description>"preferred_username" - UPN or email (fallback)</description></item>
    /// <item><description>Computed from "given_name" + "family_name" (fallback)</description></item>
    /// </list>
    /// <para><strong>Example Values:</strong></para>
    /// <list type="bullet">
    /// <item><description>"John Doe" (from name claim)</description></item>
    /// <item><description>"john.doe@company.com" (from preferred_username)</description></item>
    /// </list>
    /// <para><strong>Use Case:</strong> Display in UI, reports, audit logs</para>
    /// <para><strong>Note:</strong> Requires optional claims (name, preferred_username, given_name, family_name)
    /// to be configured in Azure AD app registration</para>
    /// </remarks>
    public static string? GetUserName(this ClaimsPrincipal user)
    {
        // Try name claim first (full display name)
        var name = user.FindFirst("name")?.Value;
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        // Try preferred_username (UPN)
        var preferredUsername = user.FindFirst("preferred_username")?.Value;
        if (!string.IsNullOrEmpty(preferredUsername))
        {
            return preferredUsername;
        }

        // Try to construct name from given_name and family_name
        var givenName = user.FindFirst("given_name")?.Value;
        var familyName = user.FindFirst("family_name")?.Value;

        if (!string.IsNullOrEmpty(givenName) && !string.IsNullOrEmpty(familyName))
        {
            return $"{givenName} {familyName}";
        }

        if (!string.IsNullOrEmpty(givenName))
        {
            return givenName;
        }

        if (!string.IsNullOrEmpty(familyName))
        {
            return familyName;
        }

        // Fallback to standard name claim type
        var nameClaim = user.FindFirst(ClaimTypes.Name)?.Value;
        return nameClaim;
    }

    /// <summary>
    /// Extract all user information at once for convenience.
    /// </summary>
    /// <param name="user">The authenticated user's ClaimsPrincipal</param>
    /// <returns>Tuple containing (userId, userEmail, userName)</returns>
    /// <remarks>
    /// <para><strong>Use Case:</strong> Convenient method for extracting all user fields when creating/updating TimeEntry</para>
    /// <para><strong>Example:</strong></para>
    /// <code>
    /// var (userId, userEmail, userName) = user.GetUserInfo();
    /// var entry = new TimeEntry
    /// {
    ///     // ... other properties
    ///     UserId = userId,
    ///     UserEmail = userEmail,
    ///     UserName = userName
    /// };
    /// </code>
    /// </remarks>
    public static (string? UserId, string? UserEmail, string? UserName) GetUserInfo(this ClaimsPrincipal user)
    {
        return (
            user.GetUserId(),
            user.GetUserEmail(),
            user.GetUserName()
        );
    }
}
