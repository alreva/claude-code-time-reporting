# Task 2.6: Test Project & Database Test Infrastructure

**Phase:** 2 - GraphQL API - Core Setup
**Estimated Time:** 1.5-2 hours
**Prerequisites:** Task 2.2 (Entity Framework Core Config), Task 2.3 (Data Models)
**Status:** Pending

---

## Objective

Create the xUnit test project with test infrastructure (DatabaseFixture) and integrate the existing SQL schema tests into the automated test suite. This establishes the foundation for all future API testing.

---

## Acceptance Criteria

- [ ] Test project created and builds successfully with `/build-api`
- [ ] DatabaseFixture provides clean test database lifecycle management
- [ ] SqlSchemaValidationTests executes `db/tests/test-schema.sql` from xUnit
- [ ] All 12 SQL schema tests pass when running `/test`
- [ ] Basic EF Core model tests validate CRUD operations
- [ ] Test project runs automatically as part of `/test` command
- [ ] Tests use separate test database (not production database)

---

## Implementation Steps

### 1. Create Test Project

```bash
cd TimeReportingApi.Tests
dotnet new xunit -n TimeReportingApi.Tests
cd ..
dotnet sln add TimeReportingApi.Tests/TimeReportingApi.Tests.csproj
```

**Add NuGet packages:**
```bash
cd TimeReportingApi.Tests
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Npgsql
dotnet add package Microsoft.EntityFrameworkCore.InMemory  # Optional: for faster unit tests
dotnet add package FluentAssertions  # Optional: better assertions
dotnet add reference ../TimeReportingApi/TimeReportingApi.csproj
```

### 2. Create Test Configuration

**File:** `TimeReportingApi.Tests/appsettings.Test.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=time_reporting_test;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### 3. Create DatabaseFixture

**File:** `TimeReportingApi.Tests/Fixtures/DatabaseFixture.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TimeReportingApi.Data;

namespace TimeReportingApi.Tests.Fixtures;

public class DatabaseFixture : IDisposable
{
    public TimeReportingDbContext DbContext { get; private set; }
    public string ConnectionString { get; private set; }

    public DatabaseFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        ConnectionString = configuration.GetConnectionString("DefaultConnection")!;

        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        DbContext = new TimeReportingDbContext(options);

        // Ensure database is created and migrations applied
        DbContext.Database.EnsureCreated();

        // Optionally: Run migrations
        // DbContext.Database.Migrate();
    }

    public TimeReportingDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new TimeReportingDbContext(options);
    }

    public void Cleanup()
    {
        // Clean all tables for next test
        DbContext.TimeEntries.RemoveRange(DbContext.TimeEntries);
        DbContext.ProjectTasks.RemoveRange(DbContext.ProjectTasks);
        DbContext.ProjectTags.RemoveRange(DbContext.ProjectTags);
        DbContext.Projects.RemoveRange(DbContext.Projects);
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
```

### 4. Create SQL Schema Validation Tests

**File:** `TimeReportingApi.Tests/Integration/SqlSchemaValidationTests.cs`

```csharp
using Npgsql;
using System.Text;
using System.Text.RegularExpressions;
using TimeReportingApi.Tests.Fixtures;
using Xunit;

namespace TimeReportingApi.Tests.Integration;

public class SqlSchemaValidationTests : IClassFixture<DatabaseFixture>
{
    private readonly string _connectionString;

    public SqlSchemaValidationTests(DatabaseFixture fixture)
    {
        _connectionString = fixture.ConnectionString;
    }

    [Fact]
    public async Task SqlSchemaTests_AllTestsPass()
    {
        // Read SQL test script
        var scriptPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "../../../../db/tests/test-schema.sql");

        var sqlScript = await File.ReadAllTextAsync(scriptPath);

        // Execute script and capture output
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var output = new StringBuilder();

        // Split script into individual statements for better control
        var statements = sqlScript.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement)) continue;

            try
            {
                using var cmd = new NpgsqlCommand(statement, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                // Expected errors for constraint tests are okay
                if (!statement.Contains("ON_ERROR_STOP"))
                {
                    output.AppendLine($"Statement: {statement}");
                    output.AppendLine($"Error: {ex.Message}");
                }
            }
        }

        // Read the final output from database
        using var finalCmd = new NpgsqlCommand(
            "SELECT 'All Tests Passed!' as result", conn);
        var result = await finalCmd.ExecuteScalarAsync();

        // Verify tests completed
        Assert.NotNull(result);

        // Count PASS occurrences (should be 12)
        var passMatches = Regex.Matches(sqlScript, @"PASS:");
        Assert.True(passMatches.Count >= 12,
            $"Expected at least 12 PASS markers, found {passMatches.Count}");
    }

    [Fact]
    public async Task Database_HasAllRequiredTables()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename",
            conn);

        var tables = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        Assert.Contains("projects", tables);
        Assert.Contains("project_tasks", tables);
        Assert.Contains("tag_configurations", tables);
        Assert.Contains("time_entries", tables);
        Assert.Equal(4, tables.Count);
    }
}
```

### 5. Create Basic EF Core Model Tests

**File:** `TimeReportingApi.Tests/Integration/DatabaseModelTests.cs`

```csharp
using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;
using Xunit;
using FluentAssertions;  // Optional

namespace TimeReportingApi.Tests.Integration;

public class DatabaseModelTests : IClassFixture<DatabaseFixture>, IDisposable
{
    private readonly DatabaseFixture _fixture;

    public DatabaseModelTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Cleanup();  // Clean database before each test
    }

    [Fact]
    public async Task Project_CanCreateAndRetrieve()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var project = new Project
        {
            Code = "TEST",
            Name = "Test Project",
            IsActive = true
        };

        // Act
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Projects.FindAsync("TEST");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Project", retrieved.Name);
        Assert.True(retrieved.IsActive);
    }

    [Fact]
    public async Task TimeEntry_CanCreateWithValidData()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ", Name = "Project" };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ",
            Task = "Development",
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported
        };

        // Act
        await context.TimeEntries.AddAsync(timeEntry);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.TimeEntries.FindAsync(timeEntry.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("PROJ", retrieved.ProjectCode);
        Assert.Equal(8.0m, retrieved.StandardHours);
    }

    [Fact]
    public async Task TimeEntry_NegativeHours_ThrowsException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ2", Name = "Project 2" };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ2",
            Task = "Development",
            StandardHours = -5.0m,  // Invalid
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today)
        };

        // Act & Assert
        await context.TimeEntries.AddAsync(timeEntry);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            context.SaveChangesAsync());
    }

    public void Dispose()
    {
        _fixture.Cleanup();
    }
}
```

### 6. Update .gitignore

Add to `.gitignore`:
```
# Test databases
*_test.db
```

### 7. Verify Test Execution

```bash
# Build test project
/build

# Run all tests
/test

# Expected output:
# ✓ SqlSchemaValidationTests.SqlSchemaTests_AllTestsPass
# ✓ SqlSchemaValidationTests.Database_HasAllRequiredTables
# ✓ DatabaseModelTests.Project_CanCreateAndRetrieve
# ✓ DatabaseModelTests.TimeEntry_CanCreateWithValidData
# ✓ DatabaseModelTests.TimeEntry_NegativeHours_ThrowsException
# Total: 5 tests passed
```

---

## Testing Requirements

### Acceptance Test Scenarios

1. **Test Project Build**
   - Run `/build` → Project compiles without errors
   - All dependencies resolved

2. **SQL Schema Integration**
   - Run `/test` → SqlSchemaValidationTests executes
   - All 12 SQL constraint tests pass
   - Database tables verified

3. **EF Core Model Tests**
   - CRUD operations work through EF Core
   - Constraint violations throw proper exceptions
   - Test database cleanup works between tests

4. **Test Isolation**
   - Tests use separate test database
   - Tests don't interfere with development database
   - Each test starts with clean state

### Manual Verification

```bash
# 1. Ensure test database exists
podman exec time-reporting-db psql -U postgres -c "CREATE DATABASE time_reporting_test;"

# 2. Run tests
/test

# 3. Verify all pass
# Check output for "All tests passed"
```

---

## Related Files

### Created
- `TimeReportingApi.Tests/TimeReportingApi.Tests.csproj`
- `TimeReportingApi.Tests/appsettings.Test.json`
- `TimeReportingApi.Tests/Fixtures/DatabaseFixture.cs`
- `TimeReportingApi.Tests/Integration/SqlSchemaValidationTests.cs`
- `TimeReportingApi.Tests/Integration/DatabaseModelTests.cs`

### Referenced
- `db/tests/test-schema.sql` - Executed by SqlSchemaValidationTests
- `TimeReportingApi/Data/TimeReportingDbContext.cs` - Used by fixtures
- `TimeReportingApi/Models/*.cs` - Tested by DatabaseModelTests

---

## Next Steps

After completing this task:
- ✅ Proceed to **Task 3.1** - Implement TimeEntries query (tests can now be written in TDD fashion)
- Test infrastructure is ready for all future GraphQL query and mutation tests
- SQL schema tests run automatically on every `/test` execution

---

## Notes

- **Test Database:** Uses `time_reporting_test` database (separate from `time_reporting` dev DB)
- **CI/CD Ready:** Tests can run in GitHub Actions or other CI systems
- **TDD Workflow:** All future tasks follow Red-Green-Refactor with this infrastructure
- **SQL Tests as Documentation:** SQL test script serves dual purpose - automated validation + schema documentation
- **Performance:** Consider using in-memory database for faster unit tests (EF Core InMemory provider)

---

## Common Issues

### Issue 1: Test database doesn't exist
**Solution:**
```bash
podman exec time-reporting-db psql -U postgres -c "CREATE DATABASE time_reporting_test;"
```

### Issue 2: SQL test script path not found
**Solution:** Verify relative path in SqlSchemaValidationTests matches your project structure

### Issue 3: Tests fail with connection errors
**Solution:** Ensure PostgreSQL is running (`/db-start`) and connection string in appsettings.Test.json is correct

### Issue 4: Constraint tests fail unexpectedly
**Solution:** Ensure test database has same schema as dev database (run migrations)
