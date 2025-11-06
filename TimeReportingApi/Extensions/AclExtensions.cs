using System.Security.Claims;

namespace TimeReportingApi.Extensions;

/// <summary>
/// Extension methods for parsing and validating ACL claims from ClaimsPrincipal.
/// Implements hierarchical permission checking with parent-to-child fallback.
/// </summary>
public static class AclExtensions
{
    /// <summary>
    /// Name of the extension attribute claim in the JWT token.
    /// Azure AD automatically shortens long extension claim names to "extn.{PropertyName}".
    /// Extension property: extension_8b3f87d7bc23493288b5f24056999600_TimeReportingACL
    /// Claim name in token: extn.TimeReportingACL (shortened by Azure AD)
    /// </summary>
    private const string AclClaimType = "extn.TimeReportingACL";

    /// <summary>
    /// Parse all ACL entries from the user's JWT token claims.
    /// </summary>
    /// <param name="user">ClaimsPrincipal from authenticated request</param>
    /// <returns>Enumerable of parsed ACL entries</returns>
    /// <example>
    /// Token contains: ["Project/INTERNAL=V,A", "Project/CLIENT-A=V,E,T"]
    /// Returns: [
    ///   AclEntry("Project/INTERNAL", ["V", "A"]),
    ///   AclEntry("Project/CLIENT-A", ["V", "E", "T"])
    /// ]
    /// </example>
    public static IEnumerable<AclEntry> ParseAclClaims(this ClaimsPrincipal user)
    {
        var claimValues = user.FindAll(AclClaimType).Select(c => c.Value);

        foreach (var value in claimValues)
        {
            // Expected format: "Path=Perm1,Perm2"
            var parts = value.Split('=', 2);
            if (parts.Length != 2)
                continue; // Skip malformed entries

            var path = parts[0].Trim();
            var permissions = parts[1]
                .Split(',')
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToArray();

            yield return new AclEntry(path, permissions);
        }
    }

    /// <summary>
    /// Check if the user has a specific permission for a resource path.
    /// Implements hierarchical fallback: if the exact path is not found,
    /// checks parent paths up to the root.
    /// </summary>
    /// <param name="user">ClaimsPrincipal from authenticated request</param>
    /// <param name="resourcePath">Hierarchical resource path (e.g., "Project/INTERNAL/Task/17")</param>
    /// <param name="permission">Permission abbreviation (e.g., "A" for Approve)</param>
    /// <returns>True if user has permission, false otherwise</returns>
    /// <example>
    /// User has ACL: "Project/INTERNAL=A"
    ///
    /// HasPermission("Project/INTERNAL", "A") → true (exact match)
    /// HasPermission("Project/INTERNAL/Task/17", "A") → true (inherited from parent)
    /// HasPermission("Project/CLIENT-A", "A") → false (no match)
    /// </example>
    public static bool HasPermission(this ClaimsPrincipal user, string resourcePath, string permission)
    {
        var entries = user.ParseAclClaims().ToList();
        var pathSegments = resourcePath.Split('/');

        // Check from most specific to least specific (child → parent)
        for (var i = pathSegments.Length; i > 0; i--)
        {
            var candidatePath = string.Join('/', pathSegments.Take(i));

            var matchingEntry = entries.FirstOrDefault(e =>
                e.Path.Equals(candidatePath, StringComparison.OrdinalIgnoreCase));

            if (matchingEntry != null &&
                matchingEntry.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all ACL entries for the current user (for debugging/logging).
    /// </summary>
    /// <param name="user">ClaimsPrincipal from authenticated request</param>
    /// <returns>List of all ACL entries</returns>
    public static List<AclEntry> GetUserAcl(this ClaimsPrincipal user)
    {
        return user.ParseAclClaims().ToList();
    }

    /// <summary>
    /// Check if user has ANY of the specified permissions for a resource.
    /// Useful for OR logic (e.g., "user needs View OR Edit").
    /// </summary>
    /// <param name="user">ClaimsPrincipal from authenticated request</param>
    /// <param name="resourcePath">Hierarchical resource path</param>
    /// <param name="permissions">Array of permission abbreviations</param>
    /// <returns>True if user has at least one of the permissions</returns>
    public static bool HasAnyPermission(this ClaimsPrincipal user, string resourcePath, params string[] permissions)
    {
        return permissions.Any(p => user.HasPermission(resourcePath, p));
    }

    /// <summary>
    /// Check if user has ALL of the specified permissions for a resource.
    /// Useful for AND logic (e.g., "user needs View AND Approve").
    /// </summary>
    /// <param name="user">ClaimsPrincipal from authenticated request</param>
    /// <param name="resourcePath">Hierarchical resource path</param>
    /// <param name="permissions">Array of permission abbreviations</param>
    /// <returns>True if user has all of the permissions</returns>
    public static bool HasAllPermissions(this ClaimsPrincipal user, string resourcePath, params string[] permissions)
    {
        return permissions.All(p => user.HasPermission(resourcePath, p));
    }
}
