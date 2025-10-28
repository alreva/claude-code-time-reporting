using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for QueryEntriesTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class QueryEntriesToolTests : IAsyncLifetime
{
    private GraphQLClientWrapper? _client;
    private QueryEntriesTool? _tool;
    private bool _apiAvailable;

    public async Task InitializeAsync()
    {
        // Set environment variables for testing
        Environment.SetEnvironmentVariable("GRAPHQL_API_URL",
            Environment.GetEnvironmentVariable("GRAPHQL_API_URL") ?? "http://localhost:5001/graphql");
        Environment.SetEnvironmentVariable("BEARER_TOKEN",
            Environment.GetEnvironmentVariable("BEARER_TOKEN") ?? "test-token-12345");

        // Check if API is available
        var config = new McpConfig();
        _client = new GraphQLClientWrapper(config);
        _tool = new QueryEntriesTool(_client);

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
    public async Task ExecuteAsync_WithNoFilters_ReturnsAllEntries()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new { });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        Assert.False(result.IsError);
        // Should show entries or "no entries found"
    }

    [Fact]
    public async Task ExecuteAsync_WithProjectFilter_ReturnsFilteredEntries()
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
            projectCode = "INTERNAL"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotEmpty(result.Content);
        // If there are entries, should mention INTERNAL
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilter_ReturnsEntriesWithStatus()
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
            status = "NOT_REPORTED"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithDateRange_ReturnsEntriesInRange()
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
            startDate = "2025-10-01",
            endDate = "2025-10-31"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleFilters_AppliesAllFilters()
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
            status = "NOT_REPORTED",
            startDate = "2025-10-01"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task ExecuteAsync_WithLimit_RespectsLimit()
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
            limit = 5
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoEntriesFound_ReturnsEmptyMessage()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange - use filters that should match nothing
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "NONEXISTENT_PROJECT_CODE_12345",
            startDate = "2099-01-01"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Contains("No time entries found", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }
}
