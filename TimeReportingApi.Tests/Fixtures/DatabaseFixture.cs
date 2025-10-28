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
        DbContext.TimeEntries.RemoveRange(DbContext.TimeEntries);
        DbContext.ProjectTasks.RemoveRange(DbContext.ProjectTasks);
        DbContext.TagConfigurations.RemoveRange(DbContext.TagConfigurations);
        DbContext.Projects.RemoveRange(DbContext.Projects);
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
