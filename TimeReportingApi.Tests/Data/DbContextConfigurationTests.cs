using TimeReportingApi.Data;

namespace TimeReportingApi.Tests.Data;

public class DbContextConfigurationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public DbContextConfigurationTests()
    {
        // Arrange - Set up test service collection
        var services = new ServiceCollection();

        // Add DbContext with connection string from environment or default test value
        var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ??
            "Host=localhost;Port=5432;Database=time_reporting;Username=postgres;Password=postgres";

        services.AddDbContext<TimeReportingDbContext>(options =>
            options.UseNpgsql(connectionString));

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void DbContext_ShouldBeConfiguredWithNpgsql()
    {
        // Act
        var dbContext = _serviceProvider.GetRequiredService<TimeReportingDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        dbContext.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public async Task DbContext_ShouldConnectToPostgreSQL()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<TimeReportingDbContext>();

        // Act
        var canConnect = await dbContext.Database.CanConnectAsync();

        // Assert
        canConnect.Should().BeTrue("the DbContext should be able to connect to PostgreSQL");
    }

    [Fact]
    public void DbContext_ShouldHaveCorrectConnectionString()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<TimeReportingDbContext>();

        // Act
        var connectionString = dbContext.Database.GetConnectionString();

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("time_reporting", "database name should be specified");
        connectionString.Should().Contain("Host=localhost", "should connect to localhost");
    }

    [Fact]
    public void DbContext_ShouldBeRegisteredAsScoped()
    {
        // Arrange & Act - DbContext should be registered as scoped by default
        // This is verified by being able to get a new instance in each scope
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        var context1 = scope1.ServiceProvider.GetRequiredService<TimeReportingDbContext>();
        var context2 = scope2.ServiceProvider.GetRequiredService<TimeReportingDbContext>();

        // Assert
        context1.Should().NotBeNull("DbContext should be available in scope");
        context2.Should().NotBeNull("DbContext should be available in scope");
        context1.Should().NotBeSameAs(context2, "each scope should get a different DbContext instance");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
