using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for MoveTaskTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class MoveTaskToolTests : IAsyncLifetime
{
    private ITimeReportingClient? _client;
    private MoveTaskTool? _tool;
    private LogTimeTool? _logTimeTool;
    private bool _apiAvailable;
    private string? _testEntryId;
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
        _tool = new MoveTaskTool(_client);
        _logTimeTool = new LogTimeTool(_client);

        // Test API connectivity
        try
        {
            var result = await _client.GetAvailableProjects.ExecuteAsync(true);
            _apiAvailable = result.Errors == null || result.Errors.Count == 0;

            // Create a test entry to use for move operations
            var logArgs = JsonSerializer.SerializeToElement(new
            {
                projectCode = "INTERNAL",
                task = "Development",
                standardHours = 8.0,
                startDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                completionDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            });

            var logResult = await _logTimeTool.ExecuteAsync(logArgs);
            if (!logResult.IsError.GetValueOrDefault())
            {
                // Extract ID from result text (format: "ID: {guid}")
                var text = logResult.Content[0].Text ?? "";
                var idStart = text.IndexOf("ID: ") + 4;
                var idEnd = text.IndexOf("\n", idStart);
                _testEntryId = text.Substring(idStart, idEnd - idStart);
            }
        }
        catch
        {
            _apiAvailable = false;
            Console.WriteLine("⚠️  API not available - some tests will be skipped");
        }
    }

    public async Task DisposeAsync()
    {
        // Clean up test entry if created
        if (_apiAvailable && !string.IsNullOrEmpty(_testEntryId) && _client != null)
        {
            try
            {
                var deleteArgs = JsonSerializer.SerializeToElement(new
                {
                    id = _testEntryId
                });

                var deleteTool = new DeleteEntryTool(_client);
                await deleteTool.ExecuteAsync(deleteArgs);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_WithValidInput_MovesEntry()
    {
        if (!_apiAvailable || string.IsNullOrEmpty(_testEntryId))
        {
            Console.WriteLine("⏭️  Skipping test - API not available or test entry not created");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            entryId = _testEntryId,
            newProjectCode = "CLIENT-A",
            newTask = "Feature Development"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("CLIENT-A");
        result.Content[0].Text.Should().Contain("Feature Development");
        result.Content[0].Text.Should().Contain(_testEntryId);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidProject_ReturnsError()
    {
        if (!_apiAvailable || string.IsNullOrEmpty(_testEntryId))
        {
            Console.WriteLine("⏭️  Skipping test - API not available or test entry not created");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            entryId = _testEntryId,
            newProjectCode = "INVALID",
            newTask = "Development"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("INVALID");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTask_ReturnsError()
    {
        if (!_apiAvailable || string.IsNullOrEmpty(_testEntryId))
        {
            Console.WriteLine("⏭️  Skipping test - API not available or test entry not created");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            entryId = _testEntryId,
            newProjectCode = "INTERNAL",
            newTask = "InvalidTask"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("InvalidTask");
    }

    [Fact]
    public async Task ExecuteAsync_MissingEntryId_ReturnsError()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            newProjectCode = "CLIENT-A",
            newTask = "Development"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("entryId");
    }

    [Fact]
    public async Task ExecuteAsync_MissingNewProjectCode_ReturnsError()
    {
        if (!_apiAvailable || string.IsNullOrEmpty(_testEntryId))
        {
            Console.WriteLine("⏭️  Skipping test - API not available or test entry not created");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            entryId = _testEntryId,
            newTask = "Development"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("newProjectCode");
    }

    [Fact]
    public async Task ExecuteAsync_MissingNewTask_ReturnsError()
    {
        if (!_apiAvailable || string.IsNullOrEmpty(_testEntryId))
        {
            Console.WriteLine("⏭️  Skipping test - API not available or test entry not created");
            return;
        }

        // Arrange
        var args = JsonSerializer.SerializeToElement(new
        {
            entryId = _testEntryId,
            newProjectCode = "CLIENT-A"
        });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("newTask");
    }
}
