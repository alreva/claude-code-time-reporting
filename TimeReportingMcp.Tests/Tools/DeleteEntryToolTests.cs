using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for DeleteEntryTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class DeleteEntryToolTests : IAsyncLifetime
{
    private ITimeReportingClient? _client;
    private DeleteEntryTool? _tool;
    private LogTimeTool? _logTimeTool;
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
        _tool = new DeleteEntryTool(_client);
        _logTimeTool = new LogTimeTool(_client);

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
    public async Task ExecuteAsync_WithValidId_DeletesEntry()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange - Create a test entry first
        var createArgs = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Development",
            standardHours = 1.0,
            startDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            completionDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
        });

        var createResult = await _logTimeTool!.ExecuteAsync(createArgs);
        var text = createResult.Content[0].Text ?? "";
        var idStart = text.IndexOf("ID: ") + 4;
        var idEnd = text.IndexOf("\n", idStart);
        var testEntryId = text.Substring(idStart, idEnd - idStart);

        var deleteArgs = JsonSerializer.SerializeToElement(new
        {
            id = testEntryId
        });

        // Act
        var result = await _tool!.ExecuteAsync(deleteArgs);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("deleted");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsError()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            id = Guid.NewGuid().ToString()
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithSubmittedEntry_ReturnsError()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange - Create and submit an entry
        var createArgs = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Development",
            standardHours = 1.0,
            startDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            completionDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
        });

        var createResult = await _logTimeTool!.ExecuteAsync(createArgs);
        var text = createResult.Content[0].Text ?? "";
        var idStart = text.IndexOf("ID: ") + 4;
        var idEnd = text.IndexOf("\n", idStart);
        var testEntryId = text.Substring(idStart, idEnd - idStart);

        // Submit the entry
        var submitTool = new SubmitEntryTool(_client!);
        var submitArgs = JsonSerializer.SerializeToElement(new { id = testEntryId });
        await submitTool.ExecuteAsync(submitArgs);

        var deleteArgs = JsonSerializer.SerializeToElement(new
        {
            id = testEntryId
        });

        // Act
        var result = await _tool!.ExecuteAsync(deleteArgs);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("SUBMITTED");
    }

    [Fact]
    public async Task ExecuteAsync_MissingId_ReturnsError()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new { });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("id");
    }
}
