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
        // Configure Testcontainers to use Podman on macOS
        ConfigurePodmanForTestcontainers();

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("time_reporting_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    private static void ConfigurePodmanForTestcontainers()
    {
        // Skip if DOCKER_HOST is already set
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST")))
        {
            return;
        }

        // Try to detect Podman machine socket on macOS
        var podmanSocketPath = GetPodmanSocketPath();
        if (!string.IsNullOrEmpty(podmanSocketPath) && File.Exists(podmanSocketPath))
        {
            Environment.SetEnvironmentVariable("DOCKER_HOST", $"unix://{podmanSocketPath}");

            // Disable Ryuk for Podman compatibility (Ryuk fails on macOS Podman)
            Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");

            // Override socket for Testcontainers
            Environment.SetEnvironmentVariable("TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE", "/var/run/docker.sock");
        }
    }

    private static string? GetPodmanSocketPath()
    {
        try
        {
            // Try to get Podman machine socket path on macOS
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "podman",
                    Arguments = "machine inspect --format {{.ConnectionInfo.PodmanSocket.Path}}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return process.ExitCode == 0 ? output : null;
        }
        catch
        {
            // Fall back to common Docker socket locations
            var fallbackPaths = new[]
            {
                "/var/run/docker.sock",
                "/run/user/501/podman/podman.sock"
            };

            return fallbackPaths.FirstOrDefault(File.Exists);
        }
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
