# Task 15.4: Update Mutations Authorization

**Phase:** 15 - ACL-Based Authorization
**Estimated Time:** 45 minutes
**Prerequisites:** Task 15.1, 15.2, and 15.3 complete
**Status:** Pending

---

## Objective

Update the `ApproveTimeEntry` and `DeclineTimeEntry` mutations to enforce ACL-based authorization using hierarchical permission checks. Users must have the "Approve" permission for the project to approve or decline time entries.

---

## Background

Currently, the mutations use `[Authorize]` which only checks authentication. With ACL authorization, we need to:

1. **Load the time entry** to get the project code
2. **Check user permission** using `HasPermission(Project/{code}, "A")`
3. **Throw GraphQL exception** if unauthorized
4. **Return proper error format**: 200 OK with GraphQL errors array

**Authorization Flow:**
```
User → JWT Token → ClaimsPrincipal → AclExtensions.HasPermission()
                                              ↓
                        Check: Project/{ProjectCode}=A (Approve permission)
                                              ↓
                                    Allow / Deny operation
```

---

## Acceptance Criteria

- [ ] `ApproveTimeEntry` mutation checks ACL before approving
- [ ] `DeclineTimeEntry` mutation checks ACL before declining
- [ ] Time entry's project code is extracted and validated
- [ ] GraphQL exception thrown when user lacks permission
- [ ] Error response follows HotChocolate format (200 OK + errors array)
- [ ] Error message is descriptive and includes project code
- [ ] Existing business logic (status checks) preserved
- [ ] Unit tests updated to cover authorization scenarios
- [ ] All tests pass (`/test-api`)

---

## Implementation

### Step 1: Update ApproveTimeEntry Mutation

**File:** `TimeReportingApi/GraphQL/Mutation.cs`

**Find the ApproveTimeEntry method (around line 502-540) and replace with:**

```csharp
/// <summary>
/// Approve a submitted time entry.
/// Requires "Approve" permission for the time entry's project.
/// </summary>
[Authorize]
public async Task<TimeEntry> ApproveTimeEntry(
    Guid id,
    ClaimsPrincipal user,
    [Service] TimeReportingDbContext context)
{
    // Load entry with project information
    var entry = await context.TimeEntries
        .Include(e => e.TagValues)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (entry == null)
    {
        throw new GraphQLException($"Time entry {id} not found");
    }

    // Get project code from shadow property
    var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue;
    if (string.IsNullOrEmpty(projectCode))
    {
        throw new GraphQLException("Time entry has no associated project");
    }

    // Check ACL permission: User must have "Approve" permission for this project
    var resourcePath = $"Project/{projectCode}";
    if (!user.HasPermission(resourcePath, Permissions.Approve))
    {
        throw new GraphQLException(new ErrorBuilder()
            .SetMessage($"You are not authorized to approve time entries for project '{projectCode}'")
            .SetCode("AUTH_FORBIDDEN")
            .SetExtension("projectCode", projectCode)
            .SetExtension("requiredPermission", "Approve")
            .Build());
    }

    // Existing business logic validation
    if (entry.Status != TimeEntryStatus.Submitted)
    {
        throw new GraphQLException($"Cannot approve entry in status {entry.Status}. Only SUBMITTED entries can be approved.");
    }

    // Approve the entry
    entry.Status = TimeEntryStatus.Approved;
    entry.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return entry;
}
```

### Step 2: Update DeclineTimeEntry Mutation

**File:** `TimeReportingApi/GraphQL/Mutation.cs`

**Find the DeclineTimeEntry method (around line 548-594) and replace with:**

```csharp
/// <summary>
/// Decline a submitted time entry with a comment explaining why.
/// Requires "Approve" permission for the time entry's project.
/// </summary>
[Authorize]
public async Task<TimeEntry> DeclineTimeEntry(
    Guid id,
    string comment,
    ClaimsPrincipal user,
    [Service] TimeReportingDbContext context)
{
    // Validate comment
    if (string.IsNullOrWhiteSpace(comment))
    {
        throw new GraphQLException("A comment is required when declining a time entry");
    }

    // Load entry with project information
    var entry = await context.TimeEntries
        .Include(e => e.TagValues)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (entry == null)
    {
        throw new GraphQLException($"Time entry {id} not found");
    }

    // Get project code from shadow property
    var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue;
    if (string.IsNullOrEmpty(projectCode))
    {
        throw new GraphQLException("Time entry has no associated project");
    }

    // Check ACL permission: User must have "Approve" permission for this project
    var resourcePath = $"Project/{projectCode}";
    if (!user.HasPermission(resourcePath, Permissions.Approve))
    {
        throw new GraphQLException(new ErrorBuilder()
            .SetMessage($"You are not authorized to decline time entries for project '{projectCode}'")
            .SetCode("AUTH_FORBIDDEN")
            .SetExtension("projectCode", projectCode)
            .SetExtension("requiredPermission", "Approve")
            .Build());
    }

    // Existing business logic validation
    if (entry.Status != TimeEntryStatus.Submitted)
    {
        throw new GraphQLException($"Cannot decline entry in status {entry.Status}. Only SUBMITTED entries can be declined.");
    }

    // Decline the entry
    entry.Status = TimeEntryStatus.Declined;
    entry.DeclineReason = comment;
    entry.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return entry;
}
```

### Step 3: Add Using Statements

**At the top of `Mutation.cs`, ensure these using statements exist:**

```csharp
using System.Security.Claims;
using TimeReportingApi.Extensions;
using HotChocolate;
```

---

## Testing

### Update Integration Tests

**File:** `TimeReportingApi.Tests/GraphQL/WorkflowMutationTests.cs`

**Add new authorization test methods:**

```csharp
[Fact]
public async Task ApproveTimeEntry_WithoutApprovePermission_ThrowsAuthorizationError()
{
    // Arrange
    await using var factory = new TestWebApplicationFactory();
    var context = factory.CreateDbContext();

    // Create test entry
    var entry = CreateTestEntry(context, "INTERNAL", "Development");
    entry.Status = TimeEntryStatus.Submitted;
    await context.SaveChangesAsync();

    // Create user WITHOUT Approve permission for INTERNAL
    var user = CreateUserWithAcl("Project/CLIENT-A=A");  // Only approve CLIENT-A, not INTERNAL

    // Act
    var result = await ExecuteMutationAsUser(factory, user, $@"
        mutation {{
            approveTimeEntry(id: ""{entry.Id}"") {{
                id
                status
            }}
        }}
    ");

    // Assert
    Assert.NotNull(result.Errors);
    Assert.Contains(result.Errors, e =>
        e.Message.Contains("not authorized") &&
        e.Message.Contains("INTERNAL"));
    Assert.Equal("AUTH_FORBIDDEN", result.Errors[0].Extensions?["code"]);
}

[Fact]
public async Task ApproveTimeEntry_WithApprovePermission_Succeeds()
{
    // Arrange
    await using var factory = new TestWebApplicationFactory();
    var context = factory.CreateDbContext();

    // Create test entry
    var entry = CreateTestEntry(context, "INTERNAL", "Development");
    entry.Status = TimeEntryStatus.Submitted;
    await context.SaveChangesAsync();

    // Create user WITH Approve permission for INTERNAL
    var user = CreateUserWithAcl("Project/INTERNAL=A");

    // Act
    var result = await ExecuteMutationAsUser(factory, user, $@"
        mutation {{
            approveTimeEntry(id: ""{entry.Id}"") {{
                id
                status
            }}
        }}
    ");

    // Assert
    Assert.Null(result.Errors);
    Assert.Equal("APPROVED", result.Data["approveTimeEntry"]["status"]);
}

[Fact]
public async Task ApproveTimeEntry_WithInheritedPermission_Succeeds()
{
    // Arrange
    await using var factory = new TestWebApplicationFactory();
    var context = factory.CreateDbContext();

    // Create test entry for specific project
    var entry = CreateTestEntry(context, "INTERNAL", "Development");
    entry.Status = TimeEntryStatus.Submitted;
    await context.SaveChangesAsync();

    // Create user with permission at higher level (should inherit)
    // User has Approve for all projects under "Project"
    var user = CreateUserWithAcl("Project=A");

    // Act
    var result = await ExecuteMutationAsUser(factory, user, $@"
        mutation {{
            approveTimeEntry(id: ""{entry.Id}"") {{
                id
                status
            }}
        }}
    ");

    // Assert
    Assert.Null(result.Errors);
    Assert.Equal("APPROVED", result.Data["approveTimeEntry"]["status"]);
}

[Fact]
public async Task DeclineTimeEntry_WithoutApprovePermission_ThrowsAuthorizationError()
{
    // Arrange
    await using var factory = new TestWebApplicationFactory();
    var context = factory.CreateDbContext();

    // Create test entry
    var entry = CreateTestEntry(context, "INTERNAL", "Development");
    entry.Status = TimeEntryStatus.Submitted;
    await context.SaveChangesAsync();

    // Create user WITHOUT Approve permission
    var user = CreateUserWithAcl("Project/CLIENT-A=A");

    // Act
    var result = await ExecuteMutationAsUser(factory, user, $@"
        mutation {{
            declineTimeEntry(id: ""{entry.Id}"", comment: ""Not valid"") {{
                id
                status
            }}
        }}
    ");

    // Assert
    Assert.NotNull(result.Errors);
    Assert.Contains(result.Errors, e =>
        e.Message.Contains("not authorized") &&
        e.Message.Contains("INTERNAL"));
}

// Helper method to create user with ACL
private ClaimsPrincipal CreateUserWithAcl(params string[] aclEntries)
{
    var claims = new List<Claim>
    {
        new Claim("oid", Guid.NewGuid().ToString()),
        new Claim("email", "test@example.com")
    };

    foreach (var acl in aclEntries)
    {
        claims.Add(new Claim("extension_TimeReporting_acl", acl));
    }

    var identity = new ClaimsIdentity(claims, "TestAuth");
    return new ClaimsPrincipal(identity);
}
```

### Test Execution

```bash
# Run all API tests
/test-api

# Or run specific authorization tests
dotnet test TimeReportingApi.Tests --filter "FullyQualifiedName~WorkflowMutationTests"
```

**Expected:** All existing tests + 4 new authorization tests pass ✅

---

## Error Response Format

When authorization fails, the GraphQL response follows HotChocolate standard format:

**HTTP Status:** 200 OK

**Response Body:**
```json
{
  "errors": [
    {
      "message": "You are not authorized to approve time entries for project 'INTERNAL'",
      "locations": [
        {
          "line": 2,
          "column": 5
        }
      ],
      "path": [
        "approveTimeEntry"
      ],
      "extensions": {
        "code": "AUTH_FORBIDDEN",
        "projectCode": "INTERNAL",
        "requiredPermission": "Approve"
      }
    }
  ],
  "data": {
    "approveTimeEntry": null
  }
}
```

This matches the HotChocolate error pattern you specified in your requirements.

---

## Integration Points

The authorization logic in this task integrates with:
- **Task 15.2**: `AclExtensions.HasPermission()` for permission checks
- **Task 15.3**: `ClaimsPrincipal` from JWT token
- **Existing validation**: Status checks still enforced (only SUBMITTED can be approved/declined)

---

## Related Files

**Modified:**
- `TimeReportingApi/GraphQL/Mutation.cs` - Updated `ApproveTimeEntry` and `DeclineTimeEntry`
- `TimeReportingApi.Tests/GraphQL/WorkflowMutationTests.cs` - Added authorization tests

---

## Validation

After completing this task:

1. ✅ Users with "Approve" permission can approve/decline entries
2. ✅ Users without "Approve" permission get `AUTH_FORBIDDEN` error
3. ✅ Error message includes project code and required permission
4. ✅ Hierarchical permission inheritance works (parent → child)
5. ✅ Existing business logic still enforced (status checks)
6. ✅ All tests pass (including new authorization tests)

---

## Next Steps

After completing Task 15.4:
- **Task 15.5:** Create slash commands for managing ACL values via Azure CLI
- **Task 15.6:** Comprehensive authorization testing

---

## Notes

- **Token-Based Only**: No database lookups for authorization - all checks use JWT claims
- **Performance**: Permission checks are fast (in-memory string matching)
- **Hierarchical**: `Project=A` grants approval for ALL projects
- **Security**: Cannot bypass by manipulating request - JWT is cryptographically signed
- **Error Details**: Including `projectCode` in extensions helps clients provide better UX

---

## Reference

- [HotChocolate Error Handling](https://chillicream.com/docs/hotchocolate/v15/server/errors)
- [GraphQL Error Specification](https://spec.graphql.org/June2018/#sec-Errors)
- [ErrorBuilder API](https://chillicream.com/docs/hotchocolate/v15/server/errors#error-builder)
