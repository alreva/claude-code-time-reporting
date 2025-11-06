using System.Security.Claims;
using TimeReportingApi.Extensions;

namespace TimeReportingApi.Tests.Extensions;

public class AclExtensionsTests
{
    private ClaimsPrincipal CreateUserWithAcl(params string[] aclEntries)
    {
        var claims = aclEntries.Select(acl =>
            new Claim("extension_8b3f87d7bc23493288b5f24056999600_TimeReportingACL", acl)).ToList();

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
