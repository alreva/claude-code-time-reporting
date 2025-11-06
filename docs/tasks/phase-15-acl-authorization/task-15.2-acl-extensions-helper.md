# Task 15.2: ACL Extensions Helper

**Phase:** 15 - ACL-Based Authorization
**Estimated Time:** 60 minutes
**Prerequisites:** Task 15.1 complete (Azure Entra ID configured with extension attribute)
**Status:** Pending

---

## Objective

Create a ClaimsPrincipal extension library that parses ACL claims from JWT tokens and provides hierarchical permission checking. This is the core authorization logic that enables token-based, database-free permission evaluation.

---

## Background

The ACL system uses **hierarchical resource paths** with **parent-to-child permission inheritance**:

- `Project/INTERNAL=A` grants Approve permission to `Project/INTERNAL` and **all sub-resources**
- When checking `Project/INTERNAL/Task/123`, the system falls back to `Project/INTERNAL` if the specific task path isn't found
- This enables flexible, granular permissions without requiring explicit ACL entries for every resource

**Example Hierarchy:**
```
Project/INTERNAL=V,A,M
  ↓ (inherits all permissions)
Project/INTERNAL/Task/17
  ↓ (inherits all permissions)
Project/INTERNAL/Task/17/Comment/5
```

---

## Acceptance Criteria

- [ ] `AclExtensions.cs` created in `TimeReportingApi/Extensions/`
- [ ] `AclEntry` record created to represent parsed ACL entries
- [ ] `ParseAclClaims()` method extracts and parses extension attribute
- [ ] `HasPermission(path, perm)` method implements hierarchical fallback
- [ ] Permission constants defined (V, E, A, M, T)
- [ ] Case-insensitive path and permission matching
- [ ] Unit tests cover all scenarios (minimum 12 tests)
- [ ] All tests pass (`/test-api`)

---

## Implementation

### 1. Create ACL Entry Record

**File:** `TimeReportingApi/Extensions/AclEntry.cs`

```csharp
namespace TimeReportingApi.Extensions;

/// <summary>
/// Represents a single ACL (Access Control List) entry from the JWT token.
/// Format: "Path=Perm1,Perm2" (e.g., "Project/INTERNAL=V,A,M")
/// </summary>
/// <param name="Path">Hierarchical resource path (e.g., "Project/INTERNAL/Task/17")</param>
/// <param name="Permissions">Array of permission abbreviations (e.g., ["V", "A", "M"])</param>
public record AclEntry(string Path, string[] Permissions);
```

### 2. Create Permission Constants

**File:** `TimeReportingApi/Extensions/Permissions.cs`

```csharp
namespace TimeReportingApi.Extensions;

/// <summary>
/// Standard permission abbreviations used in ACL entries.
/// </summary>
public static class Permissions
{
    /// <summary>View permission - read access to resources</summary>
    public const string View = "V";

    /// <summary>Edit permission - modify time entries and resources</summary>
    public const string Edit = "E";

    /// <summary>Approve permission - approve or decline time entries</summary>
    public const string Approve = "A";

    /// <summary>Manage permission - administrative operations</summary>
    public const string Manage = "M";

    /// <summary>Track permission - log new time entries</summary>
    public const string Track = "T";
}
```

### 3. Create ACL Extensions Helper

**File:** `TimeReportingApi/Extensions/AclExtensions.cs`

```csharp
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
    /// This must match the schema extension created in Azure Entra ID.
    /// </summary>
    private const string AclClaimType = "extension_TimeReporting_acl";

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
```

---

## Testing

### Unit Tests

**File:** `TimeReportingApi.Tests/Extensions/AclExtensionsTests.cs`

```csharp
using System.Security.Claims;
using Xunit;
using TimeReportingApi.Extensions;

namespace TimeReportingApi.Tests.Extensions;

public class AclExtensionsTests
{
    private ClaimsPrincipal CreateUserWithAcl(params string[] aclEntries)
    {
        var claims = aclEntries.Select(acl =>
            new Claim("extension_TimeReporting_acl", acl)).ToList();

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void ParseAclClaims_WithValidEntries_ReturnsCorrectAclEntries()
    {
        // Arrange
        var user = CreateUserWithAcl(
            "Project/INTERNAL=V,A,M",
            "Project/CLIENT-A=V,E,T"
        );

        // Act
        var entries = user.ParseAclClaims().ToList();

        // Assert
        Assert.Equal(2, entries.Count);

        Assert.Equal("Project/INTERNAL", entries[0].Path);
        Assert.Equal(new[] { "V", "A", "M" }, entries[0].Permissions);

        Assert.Equal("Project/CLIENT-A", entries[1].Path);
        Assert.Equal(new[] { "V", "E", "T" }, entries[1].Permissions);
    }

    [Fact]
    public void ParseAclClaims_WithMalformedEntry_SkipsInvalidEntries()
    {
        // Arrange
        var user = CreateUserWithAcl(
            "Project/INTERNAL=V,A",
            "InvalidEntry",  // No equals sign
            "Project/CLIENT-A=E"
        );

        // Act
        var entries = user.ParseAclClaims().ToList();

        // Assert
        Assert.Equal(2, entries.Count); // Invalid entry skipped
        Assert.Equal("Project/INTERNAL", entries[0].Path);
        Assert.Equal("Project/CLIENT-A", entries[1].Path);
    }

    [Fact]
    public void HasPermission_WithExactPathMatch_ReturnsTrue()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=A");

        // Act & Assert
        Assert.True(user.HasPermission("Project/INTERNAL", "A"));
    }

    [Fact]
    public void HasPermission_WithParentInheritance_ReturnsTrue()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=A");

        // Act & Assert
        Assert.True(user.HasPermission("Project/INTERNAL/Task/17", "A"));
        Assert.True(user.HasPermission("Project/INTERNAL/Task/17/Comment/5", "A"));
    }

    [Fact]
    public void HasPermission_WithoutMatchingPath_ReturnsFalse()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=A");

        // Act & Assert
        Assert.False(user.HasPermission("Project/CLIENT-A", "A"));
    }

    [Fact]
    public void HasPermission_WithoutMatchingPermission_ReturnsFalse()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=V,E");

        // Act & Assert
        Assert.False(user.HasPermission("Project/INTERNAL", "A"));
    }

    [Fact]
    public void HasPermission_IsCaseInsensitive_ForPath()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=A");

        // Act & Assert
        Assert.True(user.HasPermission("project/internal", "A"));
        Assert.True(user.HasPermission("PROJECT/INTERNAL", "A"));
    }

    [Fact]
    public void HasPermission_IsCaseInsensitive_ForPermission()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=A");

        // Act & Assert
        Assert.True(user.HasPermission("Project/INTERNAL", "a"));
        Assert.True(user.HasPermission("Project/INTERNAL", "A"));
    }

    [Fact]
    public void HasPermission_WithMultiplePermissions_ChecksCorrectly()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=V,A,M");

        // Act & Assert
        Assert.True(user.HasPermission("Project/INTERNAL", "V"));
        Assert.True(user.HasPermission("Project/INTERNAL", "A"));
        Assert.True(user.HasPermission("Project/INTERNAL", "M"));
        Assert.False(user.HasPermission("Project/INTERNAL", "E"));
    }

    [Fact]
    public void HasPermission_WithNestedPaths_UsesClosestMatch()
    {
        // Arrange - More specific path overrides parent
        var user = CreateUserWithAcl(
            "Project/INTERNAL=V",  // Parent: View only
            "Project/INTERNAL/Task/17=V,E"  // Child: View and Edit
        );

        // Act & Assert
        Assert.False(user.HasPermission("Project/INTERNAL", "E")); // No Edit at parent
        Assert.True(user.HasPermission("Project/INTERNAL/Task/17", "E")); // Edit at child
    }

    [Fact]
    public void HasAnyPermission_WithOneMatching_ReturnsTrue()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=V");

        // Act & Assert
        Assert.True(user.HasAnyPermission("Project/INTERNAL", "V", "E", "A"));
    }

    [Fact]
    public void HasAllPermissions_WithAllMatching_ReturnsTrue()
    {
        // Arrange
        var user = CreateUserWithAcl("Project/INTERNAL=V,A,M");

        // Act & Assert
        Assert.True(user.HasAllPermissions("Project/INTERNAL", "V", "A"));
        Assert.False(user.HasAllPermissions("Project/INTERNAL", "V", "E"));
    }

    [Fact]
    public void GetUserAcl_ReturnsAllEntries()
    {
        // Arrange
        var user = CreateUserWithAcl(
            "Project/INTERNAL=V,A",
            "Project/CLIENT-A=V,E,T"
        );

        // Act
        var acl = user.GetUserAcl();

        // Assert
        Assert.Equal(2, acl.Count);
        Assert.Contains(acl, e => e.Path == "Project/INTERNAL");
        Assert.Contains(acl, e => e.Path == "Project/CLIENT-A");
    }
}
```

### Test Execution

```bash
# Run all API tests
/test-api

# Or run specific test file
dotnet test TimeReportingApi.Tests --filter "FullyQualifiedName~AclExtensionsTests"
```

**Expected:** 13 tests pass ✅

---

## Integration Points

The `AclExtensions` helper will be used by:
- **Task 15.4**: `ApproveTimeEntry` and `DeclineTimeEntry` mutations
- **Future tasks**: Any resolver that needs fine-grained authorization
- **Logging/Debugging**: `GetUserAcl()` for audit trails

---

## Related Files

**Created:**
- `TimeReportingApi/Extensions/AclEntry.cs`
- `TimeReportingApi/Extensions/Permissions.cs`
- `TimeReportingApi/Extensions/AclExtensions.cs`
- `TimeReportingApi.Tests/Extensions/AclExtensionsTests.cs`

**Modified:**
- None

---

## Validation

After completing this task:

1. ✅ All 13 unit tests pass
2. ✅ `ParseAclClaims()` correctly parses ACL format
3. ✅ `HasPermission()` implements hierarchical fallback
4. ✅ Case-insensitive matching works for paths and permissions
5. ✅ Malformed ACL entries are skipped gracefully

---

## Next Steps

After completing Task 15.2:
- **Task 15.3:** Configure JWT token mapping in `Program.cs`
- **Task 15.4:** Use `AclExtensions` in approve/decline mutations

---

## Notes

- **Performance**: Parsing is done on every permission check. For high-throughput scenarios, consider caching parsed ACL entries in request scope.
- **Validation**: Malformed ACL entries are silently skipped to prevent token parsing errors.
- **Hierarchical Fallback**: Always checks from most specific to least specific (child → parent).
- **Permission Abbreviations**: Single-letter codes keep tokens compact (important for 24 KB token size limit).
- **Thread Safety**: Extension methods are stateless and thread-safe.

---

## Reference

- [ClaimsPrincipal Extension Methods Best Practices](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimsprincipal)
- [JWT Claims in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims)
