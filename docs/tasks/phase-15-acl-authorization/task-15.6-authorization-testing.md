# Task 15.6: Authorization Testing

**Phase:** 15 - ACL-Based Authorization
**Estimated Time:** 90 minutes
**Prerequisites:** Tasks 15.1-15.5 complete
**Status:** Pending

---

## Objective

Perform comprehensive testing of the ACL authorization system, including unit tests, integration tests, and manual end-to-end testing with real Azure Entra ID tokens. Verify hierarchical permission inheritance, error handling, and security boundaries.

---

## Background

Authorization testing requires verifying:
1. **Positive Cases**: Users with proper permissions can perform operations
2. **Negative Cases**: Users without permissions are denied
3. **Hierarchical Inheritance**: Child resources inherit parent permissions
4. **Edge Cases**: Empty ACL, malformed entries, missing claims
5. **Security**: Cannot bypass authorization checks

---

## Acceptance Criteria

- [ ] Unit tests for `AclExtensions` cover all scenarios (minimum 13 tests)
- [ ] Integration tests for approve/decline mutations (minimum 6 tests)
- [ ] Manual end-to-end test with real tokens
- [ ] Test scenarios document all test cases
- [ ] Edge cases tested (empty ACL, no permission, etc.)
- [ ] Security validation performed
- [ ] All tests pass (`/test-api`)
- [ ] Test coverage report generated

---

## Implementation

### Phase 1: Unit Tests for AclExtensions

**These tests were already created in Task 15.2.** Verify they exist:

**File:** `TimeReportingApi.Tests/Extensions/AclExtensionsTests.cs`

Run the tests:
```bash
dotnet test TimeReportingApi.Tests --filter "FullyQualifiedName~AclExtensionsTests" --logger "console;verbosity=detailed"
```

**Expected:** 13 tests pass ✅

### Phase 2: Integration Tests for Mutations

**These tests were started in Task 15.4.** Complete the test suite:

**File:** `TimeReportingApi.Tests/GraphQL/AuthorizationTests.cs` (NEW FILE)

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TimeReportingApi.Data;
using TimeReportingApi.Models;

namespace TimeReportingApi.Tests.GraphQL;

public class AuthorizationTests : IAsyncLifetime
{
    private TestWebApplicationFactory _factory = null!;
    private TimeReportingDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        _context = _factory.CreateDbContext();
        await SeedTestData();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    private async Task SeedTestData()
    {
        // Seed projects
        _context.Projects.Add(new Project
        {
            Code = "INTERNAL",
            Name = "Internal Development",
            IsActive = true,
            Tasks = new List<ProjectTask>
            {
                new ProjectTask { Name = "Development", SortOrder = 1 }
            }
        });

        _context.Projects.Add(new Project
        {
            Code = "CLIENT-A",
            Name = "Client A Project",
            IsActive = true,
            Tasks = new List<ProjectTask>
            {
                new ProjectTask { Name = "Development", SortOrder = 1 }
            }
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task ApproveTimeEntry_WithValidPermission_Succeeds()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/INTERNAL=A");

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.Null(result.Errors);
        Assert.Equal("APPROVED", result.Data?["approveTimeEntry"]?["status"]?.ToString());
    }

    [Fact]
    public async Task ApproveTimeEntry_WithoutPermission_Fails()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/CLIENT-A=A");  // Wrong project

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.NotNull(result.Errors);
        Assert.Contains("not authorized", result.Errors[0].Message);
        Assert.Equal("AUTH_FORBIDDEN", result.Errors[0].Extensions?["code"]);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithInheritedPermission_Succeeds()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project=A");  // Parent level permission

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.Null(result.Errors);
        Assert.Equal("APPROVED", result.Data?["approveTimeEntry"]?["status"]?.ToString());
    }

    [Fact]
    public async Task DeclineTimeEntry_WithValidPermission_Succeeds()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/INTERNAL=A");

        // Act
        var result = await ExecuteDecline(user, entry.Id, "Not enough detail");

        // Assert
        Assert.Null(result.Errors);
        Assert.Equal("DECLINED", result.Data?["declineTimeEntry"]?["status"]?.ToString());
        Assert.Equal("Not enough detail", result.Data?["declineTimeEntry"]?["declineReason"]?.ToString());
    }

    [Fact]
    public async Task DeclineTimeEntry_WithoutPermission_Fails()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/CLIENT-A=A");  // Wrong project

        // Act
        var result = await ExecuteDecline(user, entry.Id, "Not enough detail");

        // Assert
        Assert.NotNull(result.Errors);
        Assert.Contains("not authorized", result.Errors[0].Message);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithEmptyAcl_Fails()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser();  // No ACL entries

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.NotNull(result.Errors);
        Assert.Contains("not authorized", result.Errors[0].Message);
    }

    [Fact]
    public async Task ApproveTimeEntry_CaseInsensitive_Succeeds()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("project/internal=a");  // Lowercase

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithMultipleProjects_UsesCorrectOne()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/CLIENT-A=V;Project/INTERNAL=A");

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.Null(result.Errors);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithViewPermissionOnly_Fails()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.Submitted);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/INTERNAL=V");  // Only View, not Approve

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.NotNull(result.Errors);
        Assert.Contains("not authorized", result.Errors[0].Message);
    }

    [Fact]
    public async Task ApproveTimeEntry_NotSubmittedStatus_FailsBusinessLogic()
    {
        // Arrange
        var entry = CreateTimeEntry("INTERNAL", "Development", TimeEntryStatus.NotReported);
        await _context.SaveChangesAsync();

        var user = CreateUser("Project/INTERNAL=A");

        // Act
        var result = await ExecuteApprove(user, entry.Id);

        // Assert
        Assert.NotNull(result.Errors);
        Assert.Contains("NOT_REPORTED", result.Errors[0].Message);
    }

    // Helper Methods

    private TimeEntry CreateTimeEntry(string projectCode, string taskName, TimeEntryStatus status)
    {
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 2.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = status,
            UserId = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TimeEntries.Add(entry);
        _context.Entry(entry).Property("ProjectCode").CurrentValue = projectCode;
        _context.Entry(entry).Property("TaskName").CurrentValue = taskName;

        return entry;
    }

    private ClaimsPrincipal CreateUser(params string[] aclEntries)
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

    private async Task<GraphQLResult> ExecuteApprove(ClaimsPrincipal user, Guid entryId)
    {
        var query = $@"
            mutation {{
                approveTimeEntry(id: ""{entryId}"") {{
                    id
                    status
                }}
            }}";

        return await ExecuteGraphQL(query, user);
    }

    private async Task<GraphQLResult> ExecuteDecline(ClaimsPrincipal user, Guid entryId, string comment)
    {
        var query = $@"
            mutation {{
                declineTimeEntry(id: ""{entryId}"", comment: ""{comment}"") {{
                    id
                    status
                    declineReason
                }}
            }}";

        return await ExecuteGraphQL(query, user);
    }

    private async Task<GraphQLResult> ExecuteGraphQL(string query, ClaimsPrincipal user)
    {
        // Implementation depends on your test infrastructure
        // This is a placeholder - adapt to your TestWebApplicationFactory setup
        throw new NotImplementedException("Implement GraphQL execution with custom user");
    }
}

public class GraphQLResult
{
    public Dictionary<string, object>? Data { get; set; }
    public List<GraphQLError>? Errors { get; set; }
}

public class GraphQLError
{
    public string Message { get; set; } = "";
    public Dictionary<string, object>? Extensions { get; set; }
}
```

Run the tests:
```bash
/test-api
```

**Expected:** 10 new authorization tests pass ✅

### Phase 3: Manual End-to-End Testing

Create a test scenario document:

**File:** `docs/tasks/phase-15-acl-authorization/TEST-SCENARIOS.md`

```markdown
# Phase 15 Authorization Test Scenarios

## Scenario 1: Admin Can Approve Own Project

**Setup:**
```bash
/user-add-acl --user your.email@example.com --entries "Project/INTERNAL=V,A,M"
```

**Wait:** 5-10 minutes for token propagation

**Test:**
1. Get new token:
   ```bash
   TOKEN=$(az account get-access-token --resource api://8b3f87d7-bc23-4932-88b5-f24056999600 --query accessToken -o tsv)
   ```

2. Create time entry:
   ```graphql
   mutation {
     logTime(input: {
       projectCode: "INTERNAL"
       task: "Development"
       standardHours: 2.0
       startDate: "2025-01-06"
       completionDate: "2025-01-06"
     }) {
       id
       status
     }
   }
   ```

3. Submit entry (get ID from step 2):
   ```graphql
   mutation {
     submitTimeEntry(id: "ENTRY_ID") {
       id
       status
     }
   }
   ```

4. Approve entry:
   ```graphql
   mutation {
     approveTimeEntry(id: "ENTRY_ID") {
       id
       status
     }
   }
   ```

**Expected:** ✅ Entry approved successfully, status = APPROVED

---

## Scenario 2: Non-Admin Cannot Approve

**Setup:**
```bash
/user-remove-acl --user your.email@example.com --entries "Project/INTERNAL=V,A,M"
/user-add-acl --user your.email@example.com --entries "Project/INTERNAL=V"
```

**Wait:** 5-10 minutes

**Test:**
1. Get new token (refresh from step 1 in Scenario 1)
2. Try to approve existing INTERNAL entry:
   ```graphql
   mutation {
     approveTimeEntry(id: "ENTRY_ID") {
       id
       status
     }
   }
   ```

**Expected:** ❌ Error response:
```json
{
  "errors": [{
    "message": "You are not authorized to approve time entries for project 'INTERNAL'",
    "extensions": {
      "code": "AUTH_FORBIDDEN",
      "projectCode": "INTERNAL",
      "requiredPermission": "Approve"
    }
  }],
  "data": {
    "approveTimeEntry": null
  }
}
```

---

## Scenario 3: Hierarchical Permission Inheritance

**Setup:**
```bash
/user-add-acl --user your.email@example.com --entries "Project=A"
```

**Wait:** 5-10 minutes

**Test:**
1. Get new token
2. Try to approve entries from INTERNAL, CLIENT-A, and MAINT projects

**Expected:** ✅ Can approve entries from ALL projects (inherited from parent "Project")

---

## Scenario 4: Case-Insensitive Matching

**Setup:**
```bash
/user-add-acl --user your.email@example.com --entries "project/internal=a"
```

**Test:**
1. Get new token
2. Approve entry for "INTERNAL" project (uppercase)

**Expected:** ✅ Permission check succeeds (case-insensitive)

---

## Scenario 5: Multiple Projects

**Setup:**
```bash
/user-add-acl --user your.email@example.com --entries "Project/INTERNAL=V,A;Project/CLIENT-A=V"
```

**Test:**
1. Get new token
2. Approve INTERNAL entry ✅
3. Try to approve CLIENT-A entry ❌ (only has View, not Approve)

**Expected:** INTERNAL succeeds, CLIENT-A fails with AUTH_FORBIDDEN

---

## Scenario 6: No ACL Entries

**Setup:**
```bash
/user-remove-acl --user your.email@example.com --entries "Project=A;Project/INTERNAL=V,A;Project/CLIENT-A=V"
/user-list-acl --user your.email@example.com  # Should show empty
```

**Test:**
1. Get new token
2. Try to approve any entry

**Expected:** ❌ AUTH_FORBIDDEN for all projects
```

### Phase 4: Security Validation

**Security Checklist:**

- [ ] **Cannot bypass with query parameters**: Trying to pass different project code doesn't work
- [ ] **Cannot bypass with GraphQL variables**: Authorization uses actual DB data, not user input
- [ ] **Token signature validated**: Tampered tokens are rejected
- [ ] **Expired tokens rejected**: Old tokens (>5 min) don't work
- [ ] **No database lookups**: Authorization is 100% token-based (check logs)
- [ ] **Error messages safe**: Don't leak sensitive information about other projects

**Test:** Try to approve entry with manipulated request:
```graphql
mutation {
  approveTimeEntry(id: "ENTRY_ID_FOR_CLIENT_A") {
    id
    status
  }
}
# User only has permission for INTERNAL, not CLIENT-A
```

**Expected:** ❌ Denied (uses project code from DB, not request)

---

## Test Execution

### Run All Tests

```bash
# Unit tests (AclExtensions)
dotnet test TimeReportingApi.Tests --filter "FullyQualifiedName~AclExtensionsTests"

# Integration tests (Authorization)
dotnet test TimeReportingApi.Tests --filter "FullyQualifiedName~AuthorizationTests"

# All API tests
/test-api
```

### Generate Coverage Report

```bash
dotnet test TimeReportingApi.Tests --collect:"XPlat Code Coverage"

# Install report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Open report
open coverage-report/index.html  # macOS
```

**Target Coverage:** >90% for authorization code

---

## Related Files

**Created:**
- `TimeReportingApi.Tests/GraphQL/AuthorizationTests.cs`
- `docs/tasks/phase-15-acl-authorization/TEST-SCENARIOS.md`

**Existing:**
- `TimeReportingApi.Tests/Extensions/AclExtensionsTests.cs` (from Task 15.2)

---

## Validation

After completing this task:

1. ✅ All 23+ tests pass (13 unit + 10 integration)
2. ✅ Manual scenarios executed successfully
3. ✅ Security validation passed
4. ✅ Code coverage >90% for authorization logic
5. ✅ Error messages follow HotChocolate format
6. ✅ No authorization bypasses found

---

## Next Steps

After completing Task 15.6:
- **Task 15.7:** Create comprehensive documentation and ADR

---

## Notes

- **Token Refresh**: Remember to get a new token after ACL changes (5-10 min wait)
- **Test Isolation**: Each test should create its own data to avoid interference
- **Async Testing**: Use `IAsyncLifetime` for proper setup/teardown
- **Error Format**: Verify errors match HotChocolate v15 format
- **Performance**: Authorization checks should be <5ms (in-memory)

---

## Reference

- [xUnit Best Practices](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Code Coverage with Coverlet](https://github.com/coverlet-coverage/coverlet)
- [HotChocolate Testing](https://chillicream.com/docs/hotchocolate/v15/testing)
