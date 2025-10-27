# Task 2.2: Entity Framework Core Config

**Phase:** 2 - GraphQL API Core Setup
**Estimated Time:** 1 hour
**Prerequisites:** Task 2.1 complete (ASP.NET + HotChocolate Setup)
**Status:** ☑ Completed

---

## Objective

Configure Entity Framework Core with PostgreSQL provider and establish database connection for the Time Reporting API.

---

## Acceptance Criteria

- [x] EF Core NuGet packages installed (Npgsql.EntityFrameworkCore.PostgreSQL)
- [x] TimeReportingDbContext class created
- [x] DbContext registered in Program.cs with PostgreSQL provider
- [x] Connection string configured in appsettings.json
- [x] Tests verify DbContext configuration
- [x] Project builds successfully with `/build`
- [x] All tests pass with `/test-api`

---

## TDD Approach

This task followed strict Test-Driven Development:

1. **RED:** Write tests for DbContext configuration - tests fail ❌
2. **GREEN:** Implement EF Core configuration - tests pass ✅
3. **REFACTOR:** Clean up and ensure build succeeds

---

## Implementation Summary

### Step 1: Write Tests First (RED Phase)

Created `TimeReportingApi.Tests/Data/DbContextConfigurationTests.cs`:
- Test: DbContext should be configured with Npgsql
- Test: DbContext should connect to PostgreSQL
- Test: DbContext should have correct connection string
- Test: DbContext should be registered as scoped

**Result:** All tests failed as expected ❌

### Step 2: Install EF Core Packages

Uncommented packages in `Directory.Packages.props`:
```xml
<PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10" />
```

Added references to both projects:
- `TimeReportingApi/TimeReportingApi.csproj`
- `TimeReportingApi.Tests/TimeReportingApi.Tests.csproj`

### Step 3: Create DbContext Class

Created `TimeReportingApi/Data/TimeReportingDbContext.cs`:
```csharp
public class TimeReportingDbContext : DbContext
{
    public TimeReportingDbContext(DbContextOptions<TimeReportingDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Entity configurations will be added in Task 2.3
    }
}
```

### Step 4: Configure Connection String

Updated `TimeReportingApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "TimeReportingDb": "Host=localhost;Port=5432;Database=time_reporting;Username=postgres;Password=postgres"
  }
}
```

### Step 5: Register DbContext in Program.cs

Added EF Core configuration:
```csharp
using Microsoft.EntityFrameworkCore;
using TimeReportingApi.Data;

builder.Services.AddDbContext<TimeReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TimeReportingDb")));
```

### Step 6: Run Tests (GREEN Phase)

**Result:** All 7 tests passed ✅
- 3 API setup tests (from Task 2.1)
- 4 DbContext configuration tests (new)

### Step 7: Build Verification

**Result:** Build succeeded with 0 warnings, 0 errors ✅

---

## Project Structure After Completion

```
TimeReportingApi/
├── Data/
│   └── TimeReportingDbContext.cs     ✅ Created
├── appsettings.json                  ✅ Updated (connection string)
├── Program.cs                        ✅ Updated (EF Core registration)
└── TimeReportingApi.csproj           ✅ Updated (EF Core packages)

TimeReportingApi.Tests/
├── Data/
│   └── DbContextConfigurationTests.cs  ✅ Created
└── TimeReportingApi.Tests.csproj       ✅ Updated (EF Core packages)

Directory.Packages.props                ✅ Updated (uncommented EF Core)
```

---

## Testing Results

### Test Output

```
Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7
```

### Build Output

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Key Implementation Details

### DbContext Configuration

- **Provider:** Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10
- **Connection String:** Stored in appsettings.json
- **Lifetime:** Scoped (default for DbContext)
- **Database:** PostgreSQL 16 running in container

### Connection String Format

```
Host=localhost;Port=5432;Database=time_reporting;Username=postgres;Password=postgres
```

### Central Package Management

This project uses Central Package Management:
- Package versions defined in `Directory.Packages.props`
- Project files only reference package names (no versions)
- Ensures consistent versions across all projects

---

## Common Issues & Troubleshooting

### Issue: Tests fail with "Cannot connect to database"

**Solution:** Ensure PostgreSQL is running:
```bash
/db-start
```

### Issue: EF Core packages not found

**Solution:** Restore packages:
```bash
dotnet restore
```

### Issue: Different connection string for tests

**Solution:** Tests use environment variable `TEST_DB_CONNECTION` if set, otherwise defaults to localhost connection string.

---

## Related Documentation

- **EF Core with PostgreSQL:** https://learn.microsoft.com/en-us/ef/core/providers/npgsql/
- **DbContext Configuration:** https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
- **Central Package Management:** https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management

---

## Next Steps

After completing this task:

1. ✅ Update TASK-INDEX.md to mark Task 2.2 as completed
2. ✅ Commit changes: "Complete Task 2.2: Entity Framework Core Config - All tests passing"
3. ➡️ Proceed to **Task 2.3** - Data Models (implement entity models)

---

## Notes

- DbContext is currently minimal - entity DbSets will be added in Task 2.3
- OnModelCreating is placeholder - entity configurations added in Task 2.3
- Connection string uses default postgres credentials from .env file
- Tests verify database connectivity, which requires PostgreSQL to be running
- EF Core 8.0.10 is compatible with .NET 10.0 (current target framework)
- All tests follow TDD Red-Green-Refactor cycle
