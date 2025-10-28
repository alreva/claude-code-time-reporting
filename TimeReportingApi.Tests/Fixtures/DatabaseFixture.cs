using Microsoft.Extensions.Configuration;
using Npgsql;
using TimeReportingApi.Data;

namespace TimeReportingApi.Tests.Fixtures;

/// <summary>
/// Provides database test infrastructure with lifecycle management
/// </summary>
public class DatabaseFixture : IDisposable
{
    public TimeReportingDbContext DbContext { get; private set; }
    public string ConnectionString { get; private set; }
    private bool _schemaApplied = false;

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

        // Ensure database exists and schema is applied with constraints
        EnsureDatabaseAndSchemaAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureDatabaseAndSchemaAsync()
    {
        if (_schemaApplied) return;

        using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Check if tables exist
        var checkTablesCmd = new NpgsqlCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'",
            conn);

        var tableCount = Convert.ToInt32(await checkTablesCmd.ExecuteScalarAsync());

        if (tableCount == 0)
        {
            // Apply SQL schema from db/schema/schema.sql
            var projectRoot = GetProjectRoot();
            var schemaPath = Path.Combine(projectRoot, "db/schema/schema.sql");

            if (File.Exists(schemaPath))
            {
                var schemaSql = await File.ReadAllTextAsync(schemaPath);

                // Execute schema SQL
                using var cmd = new NpgsqlCommand(schemaSql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                // Fallback to EF migration if schema file not found
                await DbContext.Database.EnsureCreatedAsync();
            }
        }

        _schemaApplied = true;
    }

    private static string GetProjectRoot()
    {
        var directory = Directory.GetCurrentDirectory();

        while (directory != null && !Directory.GetFiles(directory, "*.sln").Any())
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        if (directory == null)
        {
            throw new InvalidOperationException("Could not find project root directory (no .sln file found)");
        }

        return directory;
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
