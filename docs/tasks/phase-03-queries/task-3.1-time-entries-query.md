# Task 3.1: TimeEntries Query with HotChocolate Filtering

**Phase:** 3 - GraphQL API Queries
**Estimated Time:** 30-45 minutes
**Prerequisites:** Phase 2 complete (Entity models, EF Core, DbContext configured)
**Status:** Pending

---

## Objective

Implement the `timeEntries` query using HotChocolate's built-in filtering, sorting, and pagination features. Instead of writing custom resolver logic, we'll leverage HotChocolate's conventions to automatically expose DbContext queries with powerful filtering capabilities.

---

## Acceptance Criteria

- [ ] HotChocolate filtering, sorting, and paging packages installed
- [ ] `timeEntries` query uses `[UseDbContext]`, `[UseProjection]`, `[UseFiltering]`, `[UseSorting]`, `[UsePaging]`
- [ ] Query configured in `Program.cs` with filtering conventions
- [ ] GraphQL supports filtering by any field (projectCode, status, dates, etc.)
- [ ] GraphQL supports sorting by any field
- [ ] GraphQL supports cursor-based pagination (default 50 items)
- [ ] Navigation properties (Project, Tags) are automatically loaded
- [ ] Integration tests verify filtering, sorting, and pagination work
- [ ] All tests pass with `/test-api`

---

## TDD Approach

1. **RED:** Write integration test for basic query
2. **RED:** Write tests for filtering by various fields
3. **GREEN:** Add HotChocolate packages and configure query
4. **GREEN:** Implement simple query method with attributes
5. **REFACTOR:** Fine-tune filtering rules if needed

---

## Implementation Steps

### Step 1: Install HotChocolate Packages

```bash
cd TimeReportingApi
dotnet add package HotChocolate.Data --version 13.9.0
dotnet add package HotChocolate.Data.EntityFramework --version 13.9.0
```

### Step 2: Update Program.cs to Enable Filtering

Update `TimeReportingApi/Program.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
});

// Add DbContext
builder.Services.AddDbContext<TimeReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add GraphQL with filtering, sorting, and pagination
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()        // ✅ Enable field selection optimization
    .AddFiltering()          // ✅ Enable filtering
    .AddSorting()            // ✅ Enable sorting
    .RegisterDbContext<TimeReportingDbContext>(DbContextKind.Pooled); // ✅ Register DbContext

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapHealthChecks("/health");
app.MapGraphQL();

app.Run();

// Make Program class accessible to test project
public partial class Program { }
```

### Step 3: Implement Query with HotChocolate Attributes

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

    /// <summary>
    /// Get time entries with filtering, sorting, and pagination.
    /// HotChocolate automatically generates filtering and sorting capabilities.
    /// </summary>
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
}
```

**That's it!** HotChocolate handles all the filtering, sorting, and pagination logic automatically.

### Step 4: Write Integration Tests (RED Phase)

Create `TimeReportingApi.Tests/Integration/TimeEntriesQueryTests.cs`:

```csharp
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingApi.Data;
using TimeReportingApi.Models;
using Xunit;

namespace TimeReportingApi.Tests.Integration;

public class TimeEntriesQueryTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly TimeReportingDbContext _context;

    public TimeEntriesQueryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TimeReportingDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TimeReportingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<TimeReportingDbContext>();

        SeedTestData();
    }

    private void SeedTestData()
    {
        var project = new Project { Code = "TEST", Name = "Test Project", IsActive = true };
        _context.Projects.Add(project);

        var entries = new[]
        {
            new TimeEntry
            {
                Id = Guid.NewGuid(),
                ProjectCode = "TEST",
                Task = "Development",
                StandardHours = 8.0m,
                StartDate = new DateOnly(2025, 10, 20),
                CompletionDate = new DateOnly(2025, 10, 20),
                Status = TimeEntryStatus.NotReported,
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TimeEntry
            {
                Id = Guid.NewGuid(),
                ProjectCode = "TEST",
                Task = "Testing",
                StandardHours = 6.0m,
                StartDate = new DateOnly(2025, 10, 21),
                CompletionDate = new DateOnly(2025, 10, 21),
                Status = TimeEntryStatus.Submitted,
                UserId = "user2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.TimeEntries.AddRange(entries);
        _context.SaveChanges();
    }

    private async Task<JsonDocument> ExecuteGraphQL(string query)
    {
        var request = new { query };
        var response = await _client.PostAsJsonAsync("/graphql", request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    [Fact]
    public async Task TimeEntries_WithNoFilters_ReturnsAllEntries()
    {
        // Arrange
        var query = @"
            query {
                timeEntries {
                    nodes {
                        id
                        projectCode
                        task
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task TimeEntries_FilterByStatus_Works()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(where: { status: { eq: SUBMITTED } }) {
                    nodes {
                        status
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(1);
        nodes[0].GetProperty("status").GetString().Should().Be("SUBMITTED");
    }

    [Fact]
    public async Task TimeEntries_FilterByProjectCode_Works()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(where: { projectCode: { eq: ""TEST"" } }) {
                    nodes {
                        projectCode
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task TimeEntries_SortByStartDate_Works()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(order: { startDate: DESC }) {
                    nodes {
                        startDate
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");

        var firstDate = DateOnly.Parse(nodes[0].GetProperty("startDate").GetString()!);
        var secondDate = DateOnly.Parse(nodes[1].GetProperty("startDate").GetString()!);
        firstDate.Should().BeAfter(secondDate);
    }

    [Fact]
    public async Task TimeEntries_WithPagination_ReturnsFirstPage()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(first: 1) {
                    nodes {
                        id
                    }
                    pageInfo {
                        hasNextPage
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var data = result.RootElement.GetProperty("data").GetProperty("timeEntries");
        var nodes = data.GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(1);

        var hasNextPage = data.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
        hasNextPage.Should().BeTrue();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _scope.Dispose();
        _client.Dispose();
    }
}
```

### Step 5: Run Tests (Should PASS - GREEN Phase)

```bash
/test-api
```

**Expected:** All tests pass ✅

### Step 6: Test in GraphQL Playground

Start the API:

```bash
/run-api
```

Open http://localhost:5001/graphql and explore the auto-generated capabilities:

```graphql
# Simple query
query {
  timeEntries {
    nodes {
      id
      projectCode
      task
      standardHours
    }
  }
}

# Filter by status
query {
  timeEntries(where: { status: { eq: SUBMITTED } }) {
    nodes {
      id
      status
    }
  }
}

# Filter by date range
query {
  timeEntries(where: {
    startDate: { gte: "2025-10-01" }
    completionDate: { lte: "2025-10-31" }
  }) {
    nodes {
      startDate
      completionDate
    }
  }
}

# Complex filter (AND/OR)
query {
  timeEntries(where: {
    and: [
      { status: { eq: NOT_REPORTED } }
      { standardHours: { gte: 4 } }
    ]
  }) {
    nodes {
      status
      standardHours
    }
  }
}

# Sort by multiple fields
query {
  timeEntries(order: [
    { startDate: DESC }
    { projectCode: ASC }
  ]) {
    nodes {
      startDate
      projectCode
    }
  }
}

# Pagination (cursor-based)
query {
  timeEntries(first: 10, after: "<cursor>") {
    nodes {
      id
    }
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
  }
}

# Load navigation properties (automatic via projection)
query {
  timeEntries {
    nodes {
      id
      project {
        name
        availableTasks {
          taskName
        }
      }
    }
  }
}
```

---

## What HotChocolate Provides Automatically

With just `[UseFiltering]`, `[UseSorting]`, and `[UsePaging]`, HotChocolate generates:

### Filtering Operators

For each field type:
- **Strings:** `eq`, `neq`, `contains`, `startsWith`, `endsWith`, `in`, `nin`
- **Numbers:** `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `in`, `nin`
- **Dates:** `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `in`, `nin`
- **Enums:** `eq`, `neq`, `in`, `nin`
- **Boolean:** `eq`, `neq`
- **Complex:** `and`, `or`, `not`

### Sorting

- Sort by any field: `ASC` or `DESC`
- Multi-field sorting support

### Pagination

- **Cursor-based:** `first`, `after`, `last`, `before`
- **pageInfo:** `hasNextPage`, `hasPreviousPage`, `startCursor`, `endCursor`
- **totalCount:** Available if explicitly requested

### Projection

- Only loads fields requested in the query
- Automatically includes navigation properties when queried
- Optimizes SQL queries (no over-fetching)

---

## Benefits of HotChocolate Approach

✅ **Less code** - No manual filtering logic
✅ **More powerful** - Clients can filter by any combination
✅ **Type-safe** - Filtering respects entity types
✅ **Performance** - Filters translate to SQL WHERE clauses
✅ **Standardized** - Follows GraphQL Cursor Connections spec
✅ **Maintainable** - No custom resolver code to maintain

---

## Project Structure After Completion

```
TimeReportingApi/
├── GraphQL/
│   └── Query.cs                              ✅ Simple IQueryable method
├── Program.cs                                ✅ Configured with filtering
TimeReportingApi.Tests/
└── Integration/
    └── TimeEntriesQueryTests.cs              ✅ Integration tests
```

---

## Testing Checklist

- [ ] HotChocolate.Data packages installed
- [ ] Program.cs configured with filtering, sorting, projection
- [ ] Query method returns `IQueryable<TimeEntry>`
- [ ] Integration tests verify filtering works
- [ ] Integration tests verify sorting works
- [ ] Integration tests verify pagination works
- [ ] GraphQL Playground shows auto-generated filter schema
- [ ] `/test-api` shows all tests passing ✅

---

## Common Issues & Troubleshooting

### Issue: "Filtering not working in GraphQL"

**Solution:** Ensure `.AddFiltering()` is called in `Program.cs` and `[UseFiltering]` is on the method.

### Issue: "Navigation properties not loading"

**Solution:** Add `[UseProjection]` attribute and ensure `.AddProjections()` is in `Program.cs`.

### Issue: "Pagination returns wrong format"

**Solution:** HotChocolate uses cursor-based pagination by default. Use:
```graphql
{ nodes { ... } pageInfo { ... } }
```

---

## Related Documentation

- **HotChocolate Filtering:** https://chillicream.com/docs/hotchocolate/v13/fetching-data/filtering
- **HotChocolate Sorting:** https://chillicream.com/docs/hotchocolate/v13/fetching-data/sorting
- **HotChocolate Pagination:** https://chillicream.com/docs/hotchocolate/v13/fetching-data/pagination
- **HotChocolate Projections:** https://chillicream.com/docs/hotchocolate/v13/fetching-data/projections

---

## Next Steps

After completing this task:

1. ✅ Update TASK-INDEX.md to mark Task 3.1 as completed
2. ✅ Commit changes: "Complete Task 3.1: TimeEntries Query with HotChocolate filtering - All tests passing"
3. ➡️ Proceed to **Task 3.2** - TimeEntry Query (single lookup)

---

## Notes

- HotChocolate's filtering is **much more powerful** than custom resolvers
- Clients can create complex filters without API changes
- All filtering translates to efficient SQL queries (no in-memory filtering)
- Cursor-based pagination is better for large datasets than offset pagination
- `[UseProjection]` prevents N+1 queries automatically
- This approach follows GraphQL best practices and conventions
