# Task 3.4: Project Single Lookup Query

**Phase:** 3 - GraphQL API Queries
**Estimated Time:** 15 minutes
**Prerequisites:** Task 3.3 complete
**Status:** Pending

---

## Objective

Add a `project(code)` query to fetch a single project by its code with all related tasks and tag configurations.

---

## Acceptance Criteria

- [ ] `GetProject(string code)` method added to `Query.cs`
- [ ] Method returns `Project?` (nullable)
- [ ] Uses `[UseProjection]` for automatic nested loading
- [ ] Returns null when project code doesn't exist
- [ ] Integration test verifies lookup and navigation loading
- [ ] All tests pass with `/test-api`

---

## Implementation Steps

### Step 1: Add Method to Query.cs

Update `TimeReportingApi/GraphQL/Query.cs`:

```csharp
/// <summary>
/// Get a single project by code
/// </summary>
[UseDbContext(typeof(TimeReportingDbContext))]
[UseProjection]
public async Task<Project?> GetProject(
    string code,
    [ScopedService] TimeReportingDbContext context)
{
    return await context.Projects
        .FirstOrDefaultAsync(p => p.Code == code);
}
```

### Step 2: Write Integration Test

Add to test file:

```csharp
[Fact]
public async Task Project_WithValidCode_ReturnsProjectWithDetails()
{
    // Arrange
    var query = @"
        query {
            project(code: ""TEST"") {
                code
                name
                isActive
                availableTasks {
                    taskName
                }
                tags {
                    tagName
                    allowedValues {
                        value
                    }
                }
            }
        }";

    // Act
    var result = await ExecuteGraphQL(query);

    // Assert
    var data = result.RootElement.GetProperty("data").GetProperty("project");
    data.GetProperty("code").GetString().Should().Be("TEST");
    data.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
}

[Fact]
public async Task Project_WithInvalidCode_ReturnsNull()
{
    // Arrange
    var query = @"
        query {
            project(code: ""NONEXISTENT"") {
                code
            }
        }";

    // Act
    var result = await ExecuteGraphQL(query);

    // Assert
    var data = result.RootElement.GetProperty("data").GetProperty("project");
    data.ValueKind.Should().Be(JsonValueKind.Null);
}
```

### Step 3: Test in GraphQL Playground

```graphql
# Get project with all details
query {
  project(code: "INTERNAL") {
    code
    name
    isActive
    availableTasks {
      id
      taskName
      isActive
    }
    tags {
      id
      tagName
      isActive
      allowedValues {
        id
        value
      }
    }
  }
}

# Non-existent project returns null
query {
  project(code: "NONEXISTENT") {
    code
  }
}
```

---

## Testing Checklist

- [ ] Method added to Query.cs
- [ ] Integration tests pass (2 tests)
- [ ] Returns project by code
- [ ] Returns null for invalid code
- [ ] Navigation properties load (tasks, tags, values)
- [ ] `/test-api` shows all green ✅

---

## Next Steps

1. ✅ Update TASK-INDEX.md to mark Task 3.4 as completed
2. ✅ Commit: "Complete Task 3.4: Project single lookup - All tests passing"
3. ➡️ Proceed to **Task 3.5** - Comprehensive query integration tests

---

## Summary of Phase 3 Queries

At this point, Query.cs should have:

```csharp
public class Query
{
    public string Hello() => "Hello, GraphQL!";

    // Task 3.1
    public IQueryable<TimeEntry> GetTimeEntries(...)

    // Task 3.2
    public Task<TimeEntry?> GetTimeEntry(Guid id, ...)

    // Task 3.3
    public IQueryable<Project> GetProjects(...)

    // Task 3.4
    public Task<Project?> GetProject(string code, ...)
}
```

All using HotChocolate's built-in features - minimal custom code!
