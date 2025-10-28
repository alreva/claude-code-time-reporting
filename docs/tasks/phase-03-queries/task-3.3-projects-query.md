# Task 3.3: Projects Query

**Phase:** 3 - GraphQL API Queries
**Estimated Time:** 15 minutes
**Prerequisites:** Task 3.2 complete
**Status:** Pending

---

## Objective

Add a `projects` query that returns all projects with optional filtering by active status. Uses HotChocolate's filtering to allow flexible queries.

---

## Acceptance Criteria

- [ ] `GetProjects()` method added to `Query.cs`
- [ ] Returns `IQueryable<Project>` with filtering enabled
- [ ] Clients can filter by `isActive` and any other field
- [ ] Navigation properties (AvailableTasks, Tags) load automatically
- [ ] Integration test verifies filtering works
- [ ] All tests pass with `/test-api`

---

## Implementation Steps

### Step 1: Add Method to Query.cs

Update `TimeReportingApi/GraphQL/Query.cs`:

```csharp
/// <summary>
/// Get all projects with filtering capabilities
/// </summary>
[UseDbContext(typeof(TimeReportingDbContext))]
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Project> GetProjects(
    [ScopedService] TimeReportingDbContext context)
{
    return context.Projects;
}
```

### Step 2: Write Integration Test

Add to test file:

```csharp
[Fact]
public async Task Projects_FilterByActive_Works()
{
    // Arrange
    var query = @"
        query {
            projects(where: { isActive: { eq: true } }) {
                code
                name
                isActive
            }
        }";

    // Act
    var result = await ExecuteGraphQL(query);

    // Assert
    var nodes = result.RootElement.GetProperty("data").GetProperty("projects");
    nodes.GetArrayLength().Should().BeGreaterThan(0);
    foreach (var node in nodes.EnumerateArray())
    {
        node.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }
}
```

### Step 3: Test in GraphQL Playground

```graphql
# Get all active projects
query {
  projects(where: { isActive: { eq: true } }) {
    code
    name
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
}

# Get all projects (no filter)
query {
  projects {
    code
    name
    isActive
  }
}

# Sort by name
query {
  projects(order: { name: ASC }) {
    code
    name
  }
}
```

---

## Testing Checklist

- [ ] Method added to Query.cs
- [ ] Integration test passes
- [ ] Can filter by isActive
- [ ] Can sort projects
- [ ] Navigation properties load
- [ ] `/test-api` shows all green ✅

---

## Next Steps

1. ✅ Update TASK-INDEX.md to mark Task 3.3 as completed
2. ✅ Commit: "Complete Task 3.3: Projects query with filtering - All tests passing"
3. ➡️ Proceed to **Task 3.4** - Project single lookup
