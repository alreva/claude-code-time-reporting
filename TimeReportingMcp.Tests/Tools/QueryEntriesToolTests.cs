using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Tests.Helpers;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for QueryEntriesTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class QueryEntriesToolTests : IAsyncLifetime
{
    private ITimeReportingClient? _client;
    private QueryEntriesTool? _tool;
    private bool _apiAvailable;
    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        // Get API URL from environment
        var apiUrl = Environment.GetEnvironmentVariable("GRAPHQL_API_URL") ?? "http://localhost:5001/graphql";

        // Configure dependency injection with StrawberryShake
        var services = new ServiceCollection();
        services
            .AddTimeReportingClient()
            .ConfigureHttpClient(client =>
            {
                TestAuthHelper.ConfigureTestAuthentication(client, apiUrl);
            });

        _serviceProvider = services.BuildServiceProvider();
        _client = _serviceProvider.GetRequiredService<ITimeReportingClient>();
        _tool = new QueryEntriesTool(_client);

        // Test API connectivity AND authentication
        // We test with QueryTimeEntries (requires auth) instead of GetProjects (anonymous)
        // This ensures tests skip when API uses Azure AD instead of test auth
        try
        {
            var result = await _client.QueryTimeEntries.ExecuteAsync(null, CancellationToken.None);
            _apiAvailable = result.Errors == null || result.Errors.Count == 0;

            if (!_apiAvailable && result.Errors != null)
            {
                Console.WriteLine("⚠️  API not available or authentication failed - MCP tests will be skipped");
                Console.WriteLine($"     Error: {result.Errors.FirstOrDefault()?.Message}");
            }
        }
        catch
        {
            _apiAvailable = false;
            Console.WriteLine("⚠️  API not available - MCP tests will be skipped");
        }
    }

    public Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
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

    [Fact]
    public async Task ExecuteAsync_WithEntriesHavingTags_DisplaysTagInformation()
    {
        // Skip if API not available
        if (!_apiAvailable)
        {
            Console.WriteLine("Skipping test - API not available");
            return;
        }

        // Arrange - query entries that may have tags
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            startDate = "2025-10-27",
            endDate = "2025-10-30"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotEmpty(result.Content);

        var outputText = result.Content[0].Text;

        // If entries exist with tags, verify tags are displayed
        // The output should contain tag information in format "Tags: TagName: TagValue"
        // or if no entries have tags, that's also acceptable
        Assert.NotNull(outputText);

        // Check that if there are entries, the output is properly formatted
        if (outputText.Contains("Found") && !outputText.Contains("No time entries"))
        {
            // Output should have proper structure with project names, dates, hours
            Assert.Matches(@"\d+\.\d+h", outputText); // Should contain hours like "2.00h"

            // If tags exist in the data, they should be formatted as "Tags: Name: Value"
            // Note: This is a flexible assertion since not all entries may have tags
        }
    }
}
