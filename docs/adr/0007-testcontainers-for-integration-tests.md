# ADR 0007: Testcontainers for Integration Tests

## Status

**Accepted**

## Context

When implementing Phase 3 (GraphQL Queries), we encountered a critical limitation with the InMemory database provider approach:

**The Problem:**
- EF Core does not allow registering multiple database providers in the same service collection
- Integration tests need to validate GraphQL queries against a real PostgreSQL database
- Production code uses PostgreSQL with Npgsql provider
- Test code attempted to use InMemory provider for simplicity
- Error: `Services for database providers 'Npgsql.EntityFrameworkCore.PostgreSQL', 'Microsoft.EntityFrameworkCore.InMemory' have been registered in the service provider`

**Constraints:**
- Tests must validate against the actual database schema (migrations applied)
- Tests must verify PostgreSQL-specific behaviors (snake_case mapping, DateOnly, JSONB)
- Tests should be isolated (no shared state between test classes)
- Tests should be fast enough for TDD workflow

**Current Situation:**
- InMemory database cannot be used due to provider conflicts
- Need a testing approach that uses real PostgreSQL instances
- Want parallel test execution without interference

## Decision

Use Testcontainers to spin up isolated PostgreSQL containers for integration tests, with one container per test class (via IClassFixture).

**Approach:**
- Create `PostgresContainerFixture` that manages PostgreSQL container lifecycle
- Each test class using `IClassFixture<PostgresContainerFixture>` gets its own isolated database
- Container starts before tests run, applies migrations, and stops after tests complete
- Tests use `WebApplicationFactory` configured to point at the container's connection string

## Rationale

**Why Testcontainers over alternatives:**
1. **Real PostgreSQL**: Tests run against actual PostgreSQL, not a mock
2. **Isolation**: Each test class gets its own container (no shared state issues)
3. **Automatic Lifecycle**: Containers start/stop automatically with test class lifecycle
4. **Migration Validation**: Tests verify that migrations apply correctly
5. **Docker-based**: Uses Docker/Podman, which is already a project dependency
6. **Parallel Execution**: xUnit can run test classes in parallel, each with isolated containers

**Alignment with Project Goals:**
- Follows TDD workflow (run tests frequently)
- High test fidelity (tests validate real PostgreSQL behavior)
- CI/CD friendly (works in GitHub Actions with Docker)

## Consequences

### Benefits

✅ **True Integration Testing**
- Tests validate against the actual PostgreSQL database, not an in-memory approximation
- Catches PostgreSQL-specific issues (data types, constraints, indexing)

✅ **Test Isolation**
- Each test class gets its own database container
- No shared state between test classes
- Parallel test execution without conflicts

✅ **Migration Validation**
- Tests verify that EF Core migrations apply successfully
- Catches schema issues early

✅ **Realistic Test Environment**
- Tests run against the same database engine as production
- Validates snake_case column mappings, DateOnly conversions, JSONB handling

✅ **Developer Experience**
- No need to manually start/stop PostgreSQL for tests
- Tests "just work" if Docker is running
- Clear error messages if Docker is not available

### Costs

⚠️ **Docker Dependency**
- Requires Docker or Podman to be running on developer machines
- Adds ~2-5 seconds per test class for container startup
- CI/CD pipelines must have Docker available

⚠️ **Test Execution Speed**
- Slower than InMemory tests (~5-10 seconds per test class vs milliseconds)
- Still acceptable for TDD workflow (not multiple minutes)

⚠️ **Resource Usage**
- Each test class spins up a PostgreSQL container
- Uses more CPU/memory than InMemory tests
- Limited by Docker resource limits

⚠️ **Complexity**
- Adds Testcontainers NuGet packages
- Requires understanding of IClassFixture and IAsyncLifetime patterns
- More setup code (PostgresContainerFixture)

### Trade-off Assessment

**Decision: Test fidelity outweighs execution speed.**

The InMemory provider is not a viable option due to EF Core limitations. Between a shared PostgreSQL instance and isolated containers per test class, we choose **isolated containers** for test independence and parallel execution. The 5-10 second startup time per test class is acceptable for TDD workflow.

## Implementation

### Step 1: Add Testcontainers Packages

`Directory.Packages.props`:
```xml
<PackageVersion Include="Testcontainers" Version="4.8.1" />
<PackageVersion Include="Testcontainers.PostgreSql" Version="4.8.1" />
```

`TimeReportingApi.Tests.csproj`:
```xml
<PackageReference Include="Testcontainers" />
<PackageReference Include="Testcontainers.PostgreSql" />
```

### Step 2: Create PostgresContainerFixture

`TimeReportingApi.Tests/Fixtures/PostgresContainerFixture.cs`:
```csharp
using Testcontainers.PostgreSql;
using TimeReportingApi.Data;

namespace TimeReportingApi.Tests.Fixtures;

/// <summary>
/// Fixture that provides a PostgreSQL container for integration tests.
/// Each test class that uses this fixture gets its own isolated database container.
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresContainerFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("time_reporting_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    /// <summary>
    /// Connection string to the test database container
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the PostgreSQL container and applies migrations
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start the container
        await _container.StartAsync();

        // Apply migrations to create the schema
        var optionsBuilder = new DbContextOptionsBuilder<TimeReportingDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString);

        await using var context = new TimeReportingDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Stops and disposes the PostgreSQL container
    /// </summary>
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
```

### Step 3: Use Fixture in Integration Tests

`TimeReportingApi.Tests/Integration/TimeEntriesQueryTests.cs`:
```csharp
public class TimeEntriesQueryTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private TimeReportingDbContext _context = null!;

    public TimeEntriesQueryTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext configuration
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TimeReportingDbContext>));

                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    var dbContextServiceDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(TimeReportingDbContext));

                    if (dbContextServiceDescriptor != null)
                    {
                        services.Remove(dbContextServiceDescriptor);
                    }

                    // Add PostgreSQL DbContext pointing to the test container
                    services.AddDbContext<TimeReportingDbContext>(options =>
                    {
                        options.UseNpgsql(_fixture.ConnectionString);
                    });
                });

                // Use test bearer token
                builder.UseSetting("Authentication:BearerToken", "test-bearer-token-12345");
            });

        _client = _factory.CreateDefaultClient(new AuthenticationHandler("test-bearer-token-12345"));
    }

    public async Task InitializeAsync()
    {
        // Create a new DbContext for seeding
        var optionsBuilder = new DbContextOptionsBuilder<TimeReportingDbContext>();
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);
        _context = new TimeReportingDbContext(optionsBuilder.Options);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        _context.Projects.RemoveRange(_context.Projects);
        await _context.SaveChangesAsync();

        await _context.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    // ... test methods
}
```

### Pattern to Follow

**For all integration tests that need a real database:**
1. Add `IClassFixture<PostgresContainerFixture>` to test class
2. Inject `PostgresContainerFixture` via constructor
3. Configure `WebApplicationFactory` to use `_fixture.ConnectionString`
4. Implement `IAsyncLifetime` for test data seeding and cleanup

**For unit tests that don't need a database:**
- Continue using mocks (Moq) for isolated unit tests
- Only use Testcontainers for integration tests

## Alternatives Considered

### Alternative 1: InMemory Database Provider

**Approach**: Use `Microsoft.EntityFrameworkCore.InMemory` provider for test database.

**Why rejected:**
- EF Core does not allow multiple database providers in the same service collection
- Error: `Services for database providers 'Npgsql.EntityFrameworkCore.PostgreSQL', 'Microsoft.EntityFrameworkCore.InMemory' have been registered`
- InMemory does not validate PostgreSQL-specific features (DateOnly, snake_case, JSONB)
- Tests would pass against InMemory but fail against real PostgreSQL

### Alternative 2: Shared PostgreSQL Container

**Approach**: Spin up one PostgreSQL container shared across all test classes.

**Why rejected:**
- Shared state between test classes (test data pollution)
- Tests cannot run in parallel safely
- Harder to isolate test failures
- Need complex cleanup logic between tests

### Alternative 3: Local PostgreSQL Instance

**Approach**: Require developers to run PostgreSQL locally (via `docker-compose` or Podman).

**Why rejected:**
- Requires manual setup before running tests
- Shared state across test runs
- Different developers might have different PostgreSQL versions
- CI/CD requires additional setup scripts

### Alternative 4: SQLite with EF Core

**Approach**: Use SQLite for tests (faster, file-based).

**Why rejected:**
- SQLite does not support PostgreSQL-specific features
- Different SQL dialect (JSONB, DateOnly, etc.)
- Tests might pass on SQLite but fail on PostgreSQL
- Doesn't validate migrations for PostgreSQL

## References

- Testcontainers Documentation: https://dotnet.testcontainers.org/
- Package: `Testcontainers.PostgreSql` v4.8.1
- Related Task: Phase 3, Task 3.1 - TimeEntries Query
- EF Core Multi-Provider Issue: https://github.com/dotnet/efcore/issues/11101
- xUnit IClassFixture: https://xunit.net/docs/shared-context
