using Xunit;
using TimeReportingMcp.AutoTracking;

namespace TimeReportingMcp.Tests.AutoTracking;

public class ContextPersistenceTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly ContextPersistence _persistence;

    public ContextPersistenceTests()
    {
        // Use temp file for testing
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test-context-{Guid.NewGuid()}.json");
        _persistence = new ContextPersistence(_testFilePath);
    }

    public void Dispose()
    {
        // Cleanup test file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task SaveContextAsync_CreatesFile()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        await _persistence.SaveContextAsync(context);

        // Assert
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public async Task SaveContextAsync_WritesValidJson()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        await _persistence.SaveContextAsync(context);

        // Assert
        var json = await File.ReadAllTextAsync(_testFilePath);
        Assert.Contains("INTERNAL", json);
        Assert.Contains("Development", json);
    }

    [Fact]
    public async Task LoadContextAsync_RestoresContext()
    {
        // Arrange
        var originalContext = CreateTestContext();
        await _persistence.SaveContextAsync(originalContext);

        // Act
        var loadedContext = await _persistence.LoadContextAsync();

        // Assert
        Assert.NotNull(loadedContext);
        Assert.Equal("INTERNAL", loadedContext.LastProjectCode);
        Assert.Equal("Development", loadedContext.LastTask);
    }

    [Fact]
    public async Task LoadContextAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        // Act
        var context = await _persistence.LoadContextAsync();

        // Assert
        Assert.Null(context);
    }

    [Fact]
    public async Task LoadContextAsync_ReturnsNull_WhenContextIsStale()
    {
        // Arrange
        var persistence = new ContextPersistence(_testFilePath, maxStaleMinutes: 5);
        var context = CreateTestContext();
        context.LastActivityAt = DateTime.UtcNow.AddMinutes(-10); // 10 min ago
        await persistence.SaveContextAsync(context);

        // Wait to ensure saved timestamp is old
        await Task.Delay(100);

        // Act
        var loadedContext = await persistence.LoadContextAsync();

        // Assert
        Assert.Null(loadedContext); // Should be rejected as stale
    }

    [Fact]
    public async Task LoadContextAsync_HandlesCorruptFile()
    {
        // Arrange
        await File.WriteAllTextAsync(_testFilePath, "{ invalid json");

        // Act
        var context = await _persistence.LoadContextAsync();

        // Assert
        Assert.Null(context); // Should return null, not throw
    }

    [Fact]
    public async Task LoadContextAsync_ResetsSessionCounters()
    {
        // Arrange
        var originalContext = CreateTestContext();
        originalContext.ToolCallCount = 50;
        originalContext.SuggestionShownForCurrentSession = true;
        await _persistence.SaveContextAsync(originalContext);

        // Act
        var loadedContext = await _persistence.LoadContextAsync();

        // Assert
        Assert.NotNull(loadedContext);
        Assert.Equal(0, loadedContext.ToolCallCount); // Reset
        Assert.False(loadedContext.SuggestionShownForCurrentSession); // Reset
    }

    [Fact]
    public void ClearContext_DeletesFile()
    {
        // Arrange
        File.WriteAllText(_testFilePath, "test");

        // Act
        _persistence.ClearContext();

        // Assert
        Assert.False(File.Exists(_testFilePath));
    }

    [Fact]
    public void ClearContext_DoesNotThrow_WhenFileDoesNotExist()
    {
        // Act & Assert (should not throw)
        _persistence.ClearContext();
    }

    [Fact]
    public async Task SaveAndLoad_PreservesAllFields()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var context = new SessionContext
        {
            LastProjectCode = "CUSTOMER-XYZ",
            LastTask = "Bug Fixing",
            LastEntryCreatedAt = DateTime.UtcNow.AddHours(-1),
            LastActivityAt = DateTime.UtcNow.AddMinutes(-5),
            SessionStartedAt = DateTime.UtcNow.AddMinutes(-30),
            LastEntryId = entryId
        };

        // Act
        await _persistence.SaveContextAsync(context);
        var loaded = await _persistence.LoadContextAsync();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("CUSTOMER-XYZ", loaded.LastProjectCode);
        Assert.Equal("Bug Fixing", loaded.LastTask);
        Assert.Equal(entryId, loaded.LastEntryId);
        Assert.NotNull(loaded.LastEntryCreatedAt);
        Assert.NotNull(loaded.SessionStartedAt);
    }

    // Helper
    private SessionContext CreateTestContext()
    {
        return new SessionContext
        {
            LastProjectCode = "INTERNAL",
            LastTask = "Development",
            LastEntryCreatedAt = DateTime.UtcNow.AddMinutes(-20),
            LastActivityAt = DateTime.UtcNow,
            SessionStartedAt = DateTime.UtcNow.AddMinutes(-45),
            LastEntryId = Guid.NewGuid(),
            ToolCallCount = 10
        };
    }
}
