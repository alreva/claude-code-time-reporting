# Task 3.2: TimeEntry Single Lookup Query

**Phase:** 3 - GraphQL API Queries
**Estimated Time:** 15 minutes
**Prerequisites:** Task 3.1 complete (HotChocolate filtering configured)
**Status:** Pending

---

## Objective

Add a simple `timeEntry(id)` query method to fetch a single time entry by ID. With HotChocolate's projection already configured, this is just a simple method that returns a single entity.

---

## Acceptance Criteria

- [ ] `GetTimeEntry(Guid id)` method added to `Query.cs`
- [ ] Method returns `TimeEntry?` (nullable)
- [ ] Uses `[UseProjection]` for automatic navigation loading
- [ ] Integration test verifies lookup works
- [ ] Returns null when ID not found
- [ ] All tests pass with `/test-api`

---

## Implementation Steps

### Step 1: Add Method to Query.cs

Update `TimeReportingApi/GraphQL/Query.cs`:

```csharp
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using TimeReportingApi.Data;
using TimeReportingApi.Models;

namespace TimeReportingApi.GraphQL;

public class Query
{
    public string Hello() => "Hello, GraphQL!";

    [UseDbContext(typeof(TimeReportingDbContext))]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200)]
    public IQueryable<TimeEntry> GetTimeEntries(
        [ScopedService] TimeReportingDbContext context)
    {
        return context.TimeEntries;
    }

    /// <summary>
    /// Get a single time entry by ID
    /// </summary>
    [UseDbContext(typeof(TimeReportingDbContext))]
    [UseProjection]
    public async Task<TimeEntry?> GetTimeEntry(
        Guid id,
        [ScopedService] TimeReportingDbContext context)
    {
        return await context.TimeEntries
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
```

### Step 2: Write Integration Test

Add to `TimeReportingApi.Tests/Integration/TimeEntriesQueryTests.cs`:

```csharp
[Fact]
public async Task TimeEntry_WithValidId_ReturnsEntry()
{
    // Arrange
    var existingEntry = _context.TimeEntries.First();
    var query = $@"
        query {{
            timeEntry(id: ""{existingEntry.Id}"") {{
                id
                projectCode
                task
                standardHours
                project {{
                    name
                }}
            }}
        }}";

    // Act
    var result = await ExecuteGraphQL(query);

    // Assert
    var data = result.RootElement.GetProperty("data").GetProperty("timeEntry");
    data.GetProperty("id").GetString().Should().Be(existingEntry.Id.ToString());
    data.GetProperty("projectCode").GetString().Should().Be(existingEntry.ProjectCode);
}

[Fact]
public async Task TimeEntry_WithInvalidId_ReturnsNull()
{
    // Arrange
    var query = $@"
        query {{
            timeEntry(id: ""{Guid.NewGuid()}"") {{
                id
            }}
        }}";

    // Act
    var result = await ExecuteGraphQL(query);

    // Assert
    var data = result.RootElement.GetProperty("data").GetProperty("timeEntry");
    data.ValueKind.Should().Be(JsonValueKind.Null);
}
```

### Step 3: Test in GraphQL Playground

```graphql
# Get single entry by ID
query {
  timeEntry(id: "123e4567-e89b-12d3-a456-426614174000") {
    id
    projectCode
    task
    standardHours
    description
    project {
      name
    }
  }
}

# Non-existent ID returns null
query {
  timeEntry(id: "00000000-0000-0000-0000-000000000000") {
    id
  }
}
```

---

## Testing Checklist

- [ ] Method added to Query.cs
- [ ] Integration tests pass (2 tests)
- [ ] GraphQL query returns entry by ID
- [ ] GraphQL query returns null for invalid ID
- [ ] Navigation properties load correctly
- [ ] `/test-api` shows all green ✅

---

## Next Steps

1. ✅ Update TASK-INDEX.md to mark Task 3.2 as completed
2. ✅ Commit: "Complete Task 3.2: TimeEntry single lookup - All tests passing"
3. ➡️ Proceed to **Task 3.3** - Projects Query
