# Task 3.5: Comprehensive Query Integration Tests

**Phase:** 3 - GraphQL API Queries
**Estimated Time:** 30-45 minutes
**Prerequisites:** Tasks 3.1-3.4 complete (All query methods implemented)
**Status:** Pending

---

## Objective

Consolidate and expand integration tests for all query resolvers. While individual tasks included basic tests, this task ensures comprehensive coverage of all query scenarios including edge cases, complex filters, and navigation property loading.

---

## Acceptance Criteria

- [ ] All queries have integration tests covering happy path
- [ ] Tests verify HotChocolate filtering works correctly
- [ ] Tests verify sorting works correctly
- [ ] Tests verify pagination works correctly
- [ ] Tests verify navigation properties load correctly
- [ ] Tests verify null cases (not found scenarios)
- [ ] All tests pass with `/test-api`
- [ ] Test coverage report shows >80% coverage for Query.cs

---

## Implementation Steps

### Step 1: Expand Integration Test Suite

Update `TimeReportingApi.Tests/Integration/TimeEntriesQueryTests.cs` to include comprehensive coverage:

```csharp
using System;
using System.Linq;
using System.Net.Http;
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

public class ComprehensiveQueryTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly TimeReportingDbContext _context;
    private readonly Guid _testEntryId;
    private readonly string _testProjectCode = "TESTPROJ";

    public ComprehensiveQueryTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("ComprehensiveTests_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<TimeReportingDbContext>();

        _testEntryId = SeedComprehensiveTestData();
    }

    private Guid SeedComprehensiveTestData()
    {
        // Create projects
        var projects = new[]
        {
            new Project
            {
                Code = _testProjectCode,
                Name = "Test Project",
                IsActive = true
            },
            new Project
            {
                Code = "INACTIVE",
                Name = "Inactive Project",
                IsActive = false
            }
        };
        _context.Projects.AddRange(projects);

        // Create tasks
        var tasks = new[]
        {
            new ProjectTask { ProjectCode = _testProjectCode, TaskName = "Development", IsActive = true },
            new ProjectTask { ProjectCode = _testProjectCode, TaskName = "Testing", IsActive = true },
            new ProjectTask { ProjectCode = _testProjectCode, TaskName = "Documentation", IsActive = false }
        };
        _context.ProjectTasks.AddRange(tasks);

        // Create tags
        var tag = new ProjectTag
        {
            ProjectCode = _testProjectCode,
            TagName = "Environment",
            IsActive = true
        };
        _context.ProjectTags.Add(tag);
        _context.SaveChanges();

        var tagValues = new[]
        {
            new TagValue { ProjectTagId = tag.Id, Value = "Production" },
            new TagValue { ProjectTagId = tag.Id, Value = "Staging" }
        };
        _context.TagValues.AddRange(tagValues);

        // Create time entries with variety
        var entryId = Guid.NewGuid();
        var entries = new[]
        {
            new TimeEntry
            {
                Id = entryId,
                ProjectCode = _testProjectCode,
                Task = "Development",
                IssueId = "TEST-123",
                StandardHours = 8.0m,
                OvertimeHours = 0m,
                Description = "Comprehensive test entry",
                StartDate = new DateOnly(2025, 10, 24),
                CompletionDate = new DateOnly(2025, 10, 24),
                Status = TimeEntryStatus.NotReported,
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TimeEntry
            {
                Id = Guid.NewGuid(),
                ProjectCode = _testProjectCode,
                Task = "Testing",
                StandardHours = 6.0m,
                OvertimeHours = 2.0m,
                StartDate = new DateOnly(2025, 10, 23),
                CompletionDate = new DateOnly(2025, 10, 23),
                Status = TimeEntryStatus.Submitted,
                UserId = "user1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new TimeEntry
            {
                Id = Guid.NewGuid(),
                ProjectCode = _testProjectCode,
                Task = "Development",
                StandardHours = 4.0m,
                OvertimeHours = 0m,
                StartDate = new DateOnly(2025, 10, 22),
                CompletionDate = new DateOnly(2025, 10, 22),
                Status = TimeEntryStatus.Approved,
                UserId = "user2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        _context.TimeEntries.AddRange(entries);
        _context.SaveChanges();

        return entryId;
    }

    private async Task<JsonDocument> ExecuteGraphQL(string query)
    {
        var request = new { query };
        var response = await _client.PostAsJsonAsync("/graphql", request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    #region TimeEntries Query Tests

    [Fact]
    public async Task TimeEntries_NoFilters_ReturnsAll()
    {
        var query = @"
            query {
                timeEntries {
                    nodes {
                        id
                        projectCode
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var nodes = result.RootElement.GetProperty("data").GetProperty("timeEntries").GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task TimeEntries_FilterByStatus_ReturnsFiltered()
    {
        var query = @"
            query {
                timeEntries(where: { status: { eq: SUBMITTED } }) {
                    nodes {
                        status
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var nodes = result.RootElement.GetProperty("data").GetProperty("timeEntries").GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(1);
        nodes[0].GetProperty("status").GetString().Should().Be("SUBMITTED");
    }

    [Fact]
    public async Task TimeEntries_ComplexFilter_Works()
    {
        var query = @"
            query {
                timeEntries(where: {
                    and: [
                        { status: { in: [NOT_REPORTED, SUBMITTED] } }
                        { standardHours: { gte: 6 } }
                    ]
                }) {
                    nodes {
                        standardHours
                        status
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var nodes = result.RootElement.GetProperty("data").GetProperty("timeEntries").GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task TimeEntries_SortByMultipleFields_Works()
    {
        var query = @"
            query {
                timeEntries(order: [
                    { startDate: DESC }
                    { standardHours: ASC }
                ]) {
                    nodes {
                        startDate
                        standardHours
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var nodes = result.RootElement.GetProperty("data").GetProperty("timeEntries").GetProperty("nodes");

        var firstDate = DateOnly.Parse(nodes[0].GetProperty("startDate").GetString()!);
        var secondDate = DateOnly.Parse(nodes[1].GetProperty("startDate").GetString()!);
        firstDate.Should().BeOnOrAfter(secondDate);
    }

    [Fact]
    public async Task TimeEntries_WithPagination_HasCorrectPageInfo()
    {
        var query = @"
            query {
                timeEntries(first: 2) {
                    nodes {
                        id
                    }
                    pageInfo {
                        hasNextPage
                        hasPreviousPage
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var data = result.RootElement.GetProperty("data").GetProperty("timeEntries");
        var nodes = data.GetProperty("nodes");
        var pageInfo = data.GetProperty("pageInfo");

        nodes.GetArrayLength().Should().Be(2);
        pageInfo.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task TimeEntries_LoadsNavigationProperties()
    {
        var query = @"
            query {
                timeEntries(first: 1) {
                    nodes {
                        id
                        project {
                            code
                            name
                            availableTasks {
                                taskName
                            }
                        }
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var node = result.RootElement.GetProperty("data").GetProperty("timeEntries").GetProperty("nodes")[0];
        var project = node.GetProperty("project");

        project.GetProperty("code").GetString().Should().Be(_testProjectCode);
        project.GetProperty("name").GetString().Should().Be("Test Project");
        project.GetProperty("availableTasks").GetArrayLength().Should().Be(3);
    }

    #endregion

    #region TimeEntry Query Tests

    [Fact]
    public async Task TimeEntry_WithValidId_Returns()
    {
        var query = $@"
            query {{
                timeEntry(id: ""{_testEntryId}"") {{
                    id
                    projectCode
                    task
                    issueId
                    description
                }}
            }}";

        var result = await ExecuteGraphQL(query);
        var data = result.RootElement.GetProperty("data").GetProperty("timeEntry");

        data.GetProperty("id").GetString().Should().Be(_testEntryId.ToString());
        data.GetProperty("projectCode").GetString().Should().Be(_testProjectCode);
        data.GetProperty("issueId").GetString().Should().Be("TEST-123");
    }

    [Fact]
    public async Task TimeEntry_WithInvalidId_ReturnsNull()
    {
        var query = $@"
            query {{
                timeEntry(id: ""{Guid.NewGuid()}"") {{
                    id
                }}
            }}";

        var result = await ExecuteGraphQL(query);
        var data = result.RootElement.GetProperty("data").GetProperty("timeEntry");
        data.ValueKind.Should().Be(JsonValueKind.Null);
    }

    #endregion

    #region Projects Query Tests

    [Fact]
    public async Task Projects_FilterByActive_ReturnsActive()
    {
        var query = @"
            query {
                projects(where: { isActive: { eq: true } }) {
                    code
                    isActive
                }
            }";

        var result = await ExecuteGraphQL(query);
        var nodes = result.RootElement.GetProperty("data").GetProperty("projects");

        nodes.GetArrayLength().Should().Be(1);
        nodes[0].GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Projects_NoFilter_ReturnsAll()
    {
        var query = @"
            query {
                projects {
                    code
                }
            }";

        var result = await ExecuteGraphQL(query);
        var nodes = result.RootElement.GetProperty("data").GetProperty("projects");
        nodes.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Projects_LoadsNavigationProperties()
    {
        var query = @"
            query {
                projects(where: { code: { eq: ""TESTPROJ"" } }) {
                    code
                    availableTasks {
                        taskName
                        isActive
                    }
                    tags {
                        tagName
                        allowedValues {
                            value
                        }
                    }
                }
            }";

        var result = await ExecuteGraphQL(query);
        var project = result.RootElement.GetProperty("data").GetProperty("projects")[0];

        var tasks = project.GetProperty("availableTasks");
        tasks.GetArrayLength().Should().Be(3);

        var tags = project.GetProperty("tags");
        tags.GetArrayLength().Should().Be(1);
        tags[0].GetProperty("allowedValues").GetArrayLength().Should().Be(2);
    }

    #endregion

    #region Project Query Tests

    [Fact]
    public async Task Project_WithValidCode_ReturnsProject()
    {
        var query = $@"
            query {{
                project(code: ""{_testProjectCode}"") {{
                    code
                    name
                    isActive
                }}
            }}";

        var result = await ExecuteGraphQL(query);
        var data = result.RootElement.GetProperty("data").GetProperty("project");

        data.GetProperty("code").GetString().Should().Be(_testProjectCode);
        data.GetProperty("name").GetString().Should().Be("Test Project");
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Project_WithInvalidCode_ReturnsNull()
    {
        var query = @"
            query {
                project(code: ""NONEXISTENT"") {
                    code
                }
            }";

        var result = await ExecuteGraphQL(query);
        var data = result.RootElement.GetProperty("data").GetProperty("project");
        data.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Project_LoadsAllNestedData()
    {
        var query = $@"
            query {{
                project(code: ""{_testProjectCode}"") {{
                    code
                    availableTasks {{
                        taskName
                        isActive
                    }}
                    tags {{
                        tagName
                        allowedValues {{
                            value
                        }}
                    }}
                }}
            }}";

        var result = await ExecuteGraphQL(query);
        var project = result.RootElement.GetProperty("data").GetProperty("project");

        var tasks = project.GetProperty("availableTasks");
        tasks.GetArrayLength().Should().Be(3);
        tasks[0].GetProperty("taskName").GetString().Should().NotBeNullOrEmpty();

        var tags = project.GetProperty("tags");
        tags.GetArrayLength().Should().Be(1);

        var allowedValues = tags[0].GetProperty("allowedValues");
        allowedValues.GetArrayLength().Should().Be(2);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _scope.Dispose();
        _client.Dispose();
    }
}
```

### Step 2: Run All Tests

```bash
/test-api
```

**Expected:** All tests pass ✅ (should be ~15 integration tests)

### Step 3: Verify Coverage

Optional - if you have coverage tools:

```bash
dotnet test TimeReportingApi.Tests/TimeReportingApi.Tests.csproj \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura
```

---

## Testing Checklist

- [ ] All 15+ integration tests pass
- [ ] TimeEntries query tested (6 tests)
- [ ] TimeEntry single lookup tested (2 tests)
- [ ] Projects query tested (3 tests)
- [ ] Project single lookup tested (3 tests)
- [ ] Navigation properties loading verified
- [ ] Filtering scenarios covered
- [ ] Sorting scenarios covered
- [ ] Pagination scenarios covered
- [ ] Null/not-found cases covered
- [ ] `/test` shows all green ✅

---

## Test Summary

| Query | Test Count | Coverage |
|-------|------------|----------|
| timeEntries | 6 | Filtering, sorting, pagination, navigation |
| timeEntry | 2 | Found, not found |
| projects | 3 | Filtering, navigation |
| project | 3 | Found, not found, nested navigation |
| **Total** | **14+** | **Comprehensive** |

---

## Next Steps

After completing this task:

1. ✅ Update TASK-INDEX.md to mark Task 3.5 as completed
2. ✅ Run `/test` one final time to ensure everything passes
3. ✅ Commit: "Complete Task 3.5: Comprehensive query integration tests - All tests passing"
4. 🎉 **Phase 3 Complete!** All queries implemented using HotChocolate's built-in features
5. ➡️ Proceed to **Phase 4** - Mutations Part 1 (logTime, updateTimeEntry, deleteTimeEntry, ValidationService)

---

## Phase 3 Summary

**What We Built:**
- ✅ 4 query methods in ~40 lines of code (vs 400+ with custom resolvers)
- ✅ Automatic filtering by any field
- ✅ Automatic sorting by any field
- ✅ Cursor-based pagination
- ✅ Automatic navigation property loading
- ✅ Comprehensive integration tests

**Benefits Achieved:**
- 90% less code than custom resolvers
- More powerful filtering capabilities
- Type-safe queries
- Optimal SQL generation
- Easy to extend and maintain

**Phase 3 is complete when:**
- All 14+ integration tests pass
- Query.cs has 4 simple methods
- Program.cs configured with filtering/sorting/projection
- HotChocolate.Data packages installed
- All tests green ✅
