using Microsoft.Extensions.Configuration;
using TimeReportingApi.Data;

namespace TimeReportingApi.Tests.Fixtures;

/// <summary>
/// Provides database test infrastructure with lifecycle management.
/// Uses EF Core migrations to ensure test database schema matches production.
/// </summary>
public class DatabaseFixture : IDisposable
{
    public TimeReportingDbContext DbContext { get; private set; }
    public string ConnectionString { get; private set; }
    private static bool _migrationApplied = false;
    private static readonly object _migrationLock = new object();

    public DatabaseFixture()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json")
            .Build();

        ConnectionString = configuration.GetConnectionString("TimeReportingDb")!;

        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        DbContext = new TimeReportingDbContext(options);

        // Apply EF Core migrations to test database (once per test run)
        ApplyMigrations();
    }

    /// <summary>
    /// Applies EF Core migrations to the test database.
    /// This ensures test database has identical schema to production (database-agnostic).
    /// </summary>
    private void ApplyMigrations()
    {
        lock (_migrationLock)
        {
            if (_migrationApplied) return;

            // Apply all pending migrations
            DbContext.Database.Migrate();

            _migrationApplied = true;
        }
    }

    /// <summary>
    /// Creates a new DbContext instance for test isolation
    /// </summary>
    public TimeReportingDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new TimeReportingDbContext(options);
    }

    /// <summary>
    /// Cleans all data from tables for next test
    /// </summary>
    public void Cleanup()
    {
        // Delete in order respecting foreign key constraints
        // 1. TimeEntryTags (has FK to TimeEntry and TagValue)
        DbContext.TimeEntryTags.RemoveRange(DbContext.TimeEntryTags);
        // 2. TimeEntries (has FK to Project and ProjectTask)
        DbContext.TimeEntries.RemoveRange(DbContext.TimeEntries);
        // 3. TagValues (has FK to ProjectTag)
        DbContext.TagValues.RemoveRange(DbContext.TagValues);
        // 4. ProjectTasks (has FK to Project)
        DbContext.ProjectTasks.RemoveRange(DbContext.ProjectTasks);
        // 5. ProjectTags (has FK to Project)
        DbContext.ProjectTags.RemoveRange(DbContext.ProjectTags);
        // 6. Projects (no FK dependencies)
        DbContext.Projects.RemoveRange(DbContext.Projects);
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
