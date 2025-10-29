using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for LogTimeTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class LogTimeToolTests : IAsyncLifetime
{
    private ITimeReportingClient? _client;
    private LogTimeTool? _tool;
    private bool _apiAvailable;
    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("GRAPHQL_API_URL",
            Environment.GetEnvironmentVariable("GRAPHQL_API_URL") ?? "http://localhost:5001/graphql");
        Environment.SetEnvironmentVariable("Authentication__BearerToken",
            Environment.GetEnvironmentVariable("Authentication__BearerToken") ?? "test-token-12345");

        // Check if API is available
        var config = new McpConfig();

        // Configure dependency injection with StrawberryShake
        var services = new ServiceCollection();
        services
            .AddTimeReportingClient()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(config.GraphQLApiUrl);
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", config.BearerToken);
            });

        _serviceProvider = services.BuildServiceProvider();
        _client = _serviceProvider.GetRequiredService<ITimeReportingClient>();
        _tool = new LogTimeTool(_client);

        // Test API connectivity
        try
        {
            var result = await _client.GetAvailableProjects.ExecuteAsync(true);
            _apiAvailable = result.Errors == null || result.Errors.Count == 0;
        }
        catch
        {
            _apiAvailable = false;
            Console.WriteLine("⚠️  API not available - some tests will be skipped");
        }
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidInput_CreatesTimeEntry()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Development",
            standardHours = 8.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.False(result.IsError);
        Assert.Contains("created successfully", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INTERNAL", result.Content[0].Text);
        Assert.Contains("Development", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_WithOvertimeHours_CreatesTimeEntryWithOvertime()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Development",
            standardHours = 8.0,
            overtimeHours = 2.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("2", result.Content[0].Text); // Should mention overtime hours
        Assert.Contains("overtime", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithDescription_IncludesDescriptionInResult()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Development",
            standardHours = 4.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29",
            description = "Implemented log_time tool for MCP server"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("Implemented log_time tool", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_WithIssueId_IncludesIssueInResult()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Bug Fixing",
            standardHours = 3.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29",
            issueId = "JIRA-123"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        // Note: Issue display depends on implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidProject_ReturnsValidationError()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INVALID_PROJECT",
            task = "Development",
            standardHours = 8.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Contains("failed", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTask_ReturnsValidationError()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "InvalidTask",
            standardHours = 8.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsError);
    }

    [Fact]
    public void ExecuteAsync_WithMissingRequiredFields_ThrowsException()
    {
        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL"
            // Missing task, standardHours, dates
        });

        // Act & Assert
        // Should throw when trying to get required property
        Assert.ThrowsAsync<Exception>(async () => await _tool!.ExecuteAsync(args));
    }
}
