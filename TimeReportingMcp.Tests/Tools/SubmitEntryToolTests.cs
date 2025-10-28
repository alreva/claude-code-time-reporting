using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

/// <summary>
/// Tests for SubmitEntryTool
/// Note: These are integration tests that require the API to be running
/// </summary>
public class SubmitEntryToolTests : IAsyncLifetime
{
    private GraphQLClientWrapper? _client;
    private SubmitEntryTool? _tool;
    private LogTimeTool? _logTimeTool;
    private DeleteEntryTool? _deleteTool;
    private bool _apiAvailable;
    private List<string> _testEntryIds = new();

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
        _tool = new SubmitEntryTool(_client);
        _logTimeTool = new LogTimeTool(_client);
        _deleteTool = new DeleteEntryTool(_client);

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

    public async Task DisposeAsync()
    {
        // Clean up test entries
        if (_apiAvailable && _client != null && _deleteTool != null)
        {
            foreach (var entryId in _testEntryIds)
            {
                try
                {
                    var deleteArgs = JsonSerializer.SerializeToElement(new { id = entryId });
                    await _deleteTool.ExecuteAsync(deleteArgs);
                }
                catch
                {
                    // Ignore cleanup errors (entry might be submitted and can't be deleted)
                }
            }
        }

        _client?.Dispose();
    }

    private async Task<string> CreateTestEntry()
    {
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
        var entryId = text.Substring(idStart, idEnd - idStart);

        _testEntryIds.Add(entryId);
        return entryId;
    }

    [Fact]
    public async Task ExecuteAsync_WithValidId_SubmitsEntry()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var testEntryId = await CreateTestEntry();
        var args = JsonSerializer.SerializeToElement(new { id = testEntryId });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("submitted");
        result.Content[0].Text.Should().Contain("SUBMITTED");
        result.Content[0].Text.Should().Contain(testEntryId);
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
    public async Task ExecuteAsync_WithAlreadySubmittedEntry_ReturnsError()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange - Create and submit an entry
        var testEntryId = await CreateTestEntry();
        var submitArgs = JsonSerializer.SerializeToElement(new { id = testEntryId });

        // Submit once (should succeed)
        await _tool!.ExecuteAsync(submitArgs);

        // Act - Try to submit again (should fail)
        var result = await _tool.ExecuteAsync(submitArgs);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().MatchRegex("already|SUBMITTED");
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

    [Fact]
    public async Task ExecuteAsync_ChangesStatusToSubmitted()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var testEntryId = await CreateTestEntry();
        var args = JsonSerializer.SerializeToElement(new { id = testEntryId });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("Status: SUBMITTED");
    }

    [Fact]
    public async Task ExecuteAsync_IncludesProjectAndTaskInfo()
    {
        if (!_apiAvailable)
        {
            Console.WriteLine("⏭️  Skipping test - API not available");
            return;
        }

        // Arrange
        var testEntryId = await CreateTestEntry();
        var args = JsonSerializer.SerializeToElement(new { id = testEntryId });

        // Act
        var result = await _tool!.ExecuteAsync(args);

        // Assert
        Assert.False(result.IsError.GetValueOrDefault());
        result.Content[0].Text.Should().Contain("INTERNAL");
        result.Content[0].Text.Should().Contain("Development");
        result.Content[0].Text.Should().Contain("Hours:");
    }
}
