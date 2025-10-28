using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for GetProjectsTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class GetProjectsToolTests : IAsyncLifetime
{
    private GraphQLClientWrapper? _client;
    private GetProjectsTool? _tool;
    private bool _apiAvailable;

    public async Task InitializeAsync()
    {
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("GRAPHQL_API_URL",
            Environment.GetEnvironmentVariable("GRAPHQL_API_URL") ?? "http://localhost:5000/graphql");
        Environment.SetEnvironmentVariable("BEARER_TOKEN",
            Environment.GetEnvironmentVariable("BEARER_TOKEN") ?? "test-token-12345");

        // Check if API is available
        var config = new McpConfig();

        _client = new GraphQLClientWrapper(config);
        _tool = new GetProjectsTool(_client);

        // Test API connectivity
        try
        {
            var testQuery = new GraphQL.GraphQLRequest
            {
                Query = "query { __typename }"
            };
            await _client.SendQueryAsync<object>(testQuery);
            _apiAvailable = true;
        }
        catch
        {
            _apiAvailable = false;
            Console.WriteLine("⚠️  API not available - some tests will be skipped");
        }
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
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
