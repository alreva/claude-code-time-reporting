using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for GetProjectsTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class GetProjectsToolTests : IAsyncLifetime
{
    private ITimeReportingClient? _client;
    private GetProjectsTool? _tool;
    private bool _apiAvailable;
    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("GRAPHQL_API_URL",
            Environment.GetEnvironmentVariable("GRAPHQL_API_URL") ?? "http://localhost:5001/graphql");
        Environment.SetEnvironmentVariable("BEARER_TOKEN",
            Environment.GetEnvironmentVariable("BEARER_TOKEN") ?? "test-token-12345");

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
        _tool = new GetProjectsTool(_client);

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
    public async Task ExecuteAsync_WithActiveOnly_ReturnsActiveProjects()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            activeOnly = true
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("Available Projects");
        result.Content[0].Text.Should().Contain("INTERNAL");
    }

    [Fact]
    public async Task ExecuteAsync_WithAllProjects_ReturnsAllProjects()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            activeOnly = false
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("Available Projects");
    }

    [Fact]
    public async Task ExecuteAsync_DefaultActiveOnly_ReturnsActiveProjects()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange - No arguments, should default to activeOnly=true
        var args = JsonSerializer.SerializeToElement(new { });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("Available Projects");
    }

    [Fact]
    public async Task ExecuteAsync_FormatsProjectsCorrectly()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            activeOnly = true
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        var text = result.Content[0].Text ?? "";

        // Should contain project code and name
        text.Should().Contain("INTERNAL");
        text.Should().Contain("Internal Development");

        // Should contain tasks section
        text.Should().Contain("Tasks:");
        text.Should().Contain("Development");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesTasksAndTags()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            activeOnly = true
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        var text = result.Content[0].Text ?? "";

        // Should include tasks
        text.Should().Contain("Tasks:");

        // Should include tags (if any exist in seed data)
        // Note: This assumes tags exist in the seed data
        text.Should().MatchRegex("Tags:|No tags"); // Either has tags or mentions none
    }
}
