# Task 10.4: Context Persistence

**Phase:** 10 - MCP Server Auto-Tracking
**Estimated Time:** 1 hour
**Prerequisites:** Tasks 10.1, 10.2, 10.3 complete (SessionContext, DetectionHeuristics, SuggestionFormatter working)
**Status:** Pending

---

## Objective

Add persistence for session context so that auto-tracking state survives across MCP server restarts. This allows the system to maintain context even if Claude Code or the MCP server is restarted during a work session.

---

## Background

Currently, `SessionContext` is stored in-memory only. When the MCP server restarts (e.g., user restarts Claude Code, server crashes, or configuration changes), all context is lost.

For better user experience, we should persist:
- Last project/task used
- Last entry created timestamp
- Session start time (if recently active)

This allows "continuing" a work session across restarts.

---

## Acceptance Criteria

- [ ] `ContextPersistence.cs` class created for save/load operations
- [ ] Context saved to JSON file in user's config directory
- [ ] Context loaded on MCP server startup
- [ ] Stale context (>1 hour old) is ignored on load
- [ ] Graceful handling of missing/corrupt files
- [ ] Unit tests for persistence logic (minimum 8 tests)
- [ ] All tests pass (`/test-mcp`)

---

## Implementation

### 1. Create ContextPersistence Class

**File:** `TimeReportingMcp/AutoTracking/ContextPersistence.cs`

```csharp
using System.Text.Json;

namespace TimeReportingMcp.AutoTracking;

/// <summary>
/// Handles persistence of session context to survive MCP server restarts.
/// Saves context to JSON file in user's config directory.
/// </summary>
public class ContextPersistence
{
    private readonly string _persistenceFilePath;
    private readonly int _maxStaleMinutes;

    public ContextPersistence(
        string? customPath = null,
        int maxStaleMinutes = 60)
    {
        _maxStaleMinutes = maxStaleMinutes;

        // Default to user's home directory/.time-reporting/context.json
        _persistenceFilePath = customPath ?? GetDefaultPersistencePath();

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_persistenceFilePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Save session context to persistent storage
    /// </summary>
    public async Task SaveContextAsync(SessionContext context)
    {
        try
        {
            var persistedData = new PersistedContext
            {
                LastProjectCode = context.LastProjectCode,
                LastTask = context.LastTask,
                LastEntryCreatedAt = context.LastEntryCreatedAt,
                LastActivityAt = context.LastActivityAt,
                SessionStartedAt = context.SessionStartedAt,
                LastEntryId = context.LastEntryId,
                SavedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(persistedData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_persistenceFilePath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to save context: {ex.Message}");
            // Don't throw - persistence failures shouldn't break the app
        }
    }

    /// <summary>
    /// Load session context from persistent storage
    /// Returns null if no valid context found or context is stale
    /// </summary>
    public async Task<SessionContext?> LoadContextAsync()
    {
        try
        {
            if (!File.Exists(_persistenceFilePath))
            {
                return null; // No saved context
            }

            var json = await File.ReadAllTextAsync(_persistenceFilePath);
            var persistedData = JsonSerializer.Deserialize<PersistedContext>(json);

            if (persistedData == null)
            {
                return null; // Invalid JSON
            }

            // Check if context is stale (too old to be useful)
            var ageMinutes = (DateTime.UtcNow - persistedData.SavedAt).TotalMinutes;
            if (ageMinutes > _maxStaleMinutes)
            {
                Console.Error.WriteLine($"[Persistence] Context is stale ({ageMinutes:F0} min old), ignoring");
                return null;
            }

            // Restore context
            var context = new SessionContext
            {
                LastProjectCode = persistedData.LastProjectCode,
                LastTask = persistedData.LastTask,
                LastEntryCreatedAt = persistedData.LastEntryCreatedAt,
                LastActivityAt = persistedData.LastActivityAt,
                SessionStartedAt = persistedData.SessionStartedAt,
                LastEntryId = persistedData.LastEntryId,
                ToolCallCount = 0, // Reset - new session
                SuggestionShownForCurrentSession = false // Reset
            };

            Console.Error.WriteLine($"[Persistence] Loaded context: {persistedData.LastProjectCode}/{persistedData.LastTask}");
            return context;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to load context: {ex.Message}");
            return null; // Don't throw - just start fresh
        }
    }

    /// <summary>
    /// Clear persisted context (delete file)
    /// </summary>
    public void ClearContext()
    {
        try
        {
            if (File.Exists(_persistenceFilePath))
            {
                File.Delete(_persistenceFilePath);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Persistence] Failed to clear context: {ex.Message}");
        }
    }

    /// <summary>
    /// Get default persistence file path
    /// </summary>
    private static string GetDefaultPersistencePath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".time-reporting");
        return Path.Combine(configDir, "mcp-context.json");
    }
}

/// <summary>
/// Serializable context data for persistence
/// </summary>
internal class PersistedContext
{
    public string? LastProjectCode { get; set; }
    public string? LastTask { get; set; }
    public DateTime? LastEntryCreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? SessionStartedAt { get; set; }
    public Guid? LastEntryId { get; set; }
    public DateTime SavedAt { get; set; }
}
```

### 2. Integrate with McpServer

Update `TimeReportingMcp/McpServer.cs`:

```csharp
public class McpServer
{
    private SessionContext _sessionContext;
    private readonly DetectionHeuristics _heuristics;
    private readonly SuggestionFormatter _formatter;
    private readonly ContextPersistence _persistence;

    public McpServer()
    {
        _heuristics = new DetectionHeuristics();
        _formatter = new SuggestionFormatter();
        _persistence = new ContextPersistence();

        // Load persisted context on startup
        _sessionContext = LoadPersistedContext().Result ?? new SessionContext();
    }

    private async Task<SessionContext?> LoadPersistedContext()
    {
        try
        {
            var context = await _persistence.LoadContextAsync();
            if (context != null)
            {
                Console.Error.WriteLine("[MCP] Loaded persisted session context");
            }
            return context;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] Failed to load context: {ex.Message}");
            return null;
        }
    }

    private async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
    {
        _sessionContext.RecordActivity();

        // ... existing tool routing ...

        // After handling the tool
        var response = await ExecuteTool(request);

        // Save context periodically (after time entry creation or every N calls)
        if (ShouldSaveContext())
        {
            await _persistence.SaveContextAsync(_sessionContext);
        }

        // Check for suggestions...
        // ... existing suggestion logic ...

        return response;
    }

    private bool ShouldSaveContext()
    {
        // Save after logging time, or every 10 tool calls, or after 5 minutes
        return _sessionContext.ToolCallCount % 10 == 0 ||
               _sessionContext.LastEntryCreatedAt != null ||
               _sessionContext.GetSessionMinutes() >= 5;
    }

    // Optional: Save on shutdown (if MCP protocol supports graceful shutdown)
    public async Task ShutdownAsync()
    {
        await _persistence.SaveContextAsync(_sessionContext);
        Console.Error.WriteLine("[MCP] Context saved on shutdown");
    }
}
```

### 3. Update Program.cs for Graceful Shutdown

**File:** `TimeReportingMcp/Program.cs`

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        var server = new McpServer();

        // Register shutdown handler
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            await server.ShutdownAsync();
            Environment.Exit(0);
        };

        await server.RunAsync();
    }
}
```

---

## Testing

### Unit Tests

**File:** `TimeReportingMcp.Tests/AutoTracking/ContextPersistenceTests.cs`

```csharp
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
```

### Test Execution

```bash
# Run all MCP tests
/test-mcp

# Or run specific test file
dotnet test TimeReportingMcp.Tests --filter "FullyQualifiedName~ContextPersistenceTests"
```

**Expected:** 10 tests pass ✅

---

## Persistence File Location

**Default path:** `~/.time-reporting/mcp-context.json`

**Example file content:**
```json
{
  "LastProjectCode": "INTERNAL",
  "LastTask": "Development",
  "LastEntryCreatedAt": "2025-10-29T14:30:00Z",
  "LastActivityAt": "2025-10-29T15:15:00Z",
  "SessionStartedAt": "2025-10-29T14:45:00Z",
  "LastEntryId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "SavedAt": "2025-10-29T15:15:05Z"
}
```

---

## Stale Context Handling

Context is considered "stale" and ignored if:
- More than 60 minutes (default) have passed since `SavedAt`
- This prevents using very old context from days ago

**Configurable threshold:**
```csharp
// Custom staleness threshold (30 minutes)
var persistence = new ContextPersistence(maxStaleMinutes: 30);
```

---

## Save Strategy

Context is saved:
1. **After logging time** - Always save after time entry creation
2. **Every 10 tool calls** - Periodic saves to catch recent activity
3. **After 5+ minutes of session** - Ensure long sessions are persisted
4. **On shutdown** - If MCP server exits gracefully

This balances performance (not saving too frequently) with data safety (not losing too much context).

---

## Error Handling

All persistence operations are wrapped in try-catch:
- **Save failures:** Log error, continue operation (don't crash)
- **Load failures:** Log error, start with fresh context
- **Corrupt files:** Ignore, start fresh
- **Missing directory:** Create automatically

**Philosophy:** Persistence is a "nice-to-have" - failures shouldn't break the app.

---

## Related Files

**Created:**
- `TimeReportingMcp/AutoTracking/ContextPersistence.cs`
- `TimeReportingMcp.Tests/AutoTracking/ContextPersistenceTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add persistence integration
- `TimeReportingMcp/Program.cs` - Add graceful shutdown handler

---

## Validation

After implementation:

1. ✅ All 10 unit tests pass
2. ✅ Context saves to JSON file correctly
3. ✅ Context loads on MCP server restart
4. ✅ Stale context is rejected
5. ✅ Errors are handled gracefully
6. ✅ File permissions work correctly

---

## Manual Testing

### Test Persistence Works

```bash
# 1. Start MCP server and log time
echo '{"method":"log_time","params":{"project":"INTERNAL","task":"Development","hours":1}}' | dotnet run

# 2. Check persistence file was created
cat ~/.time-reporting/mcp-context.json

# 3. Restart MCP server
# Stop and start again

# 4. Verify context was loaded
# Check stderr output for "[MCP] Loaded persisted session context"
```

### Test Stale Context Rejection

```bash
# 1. Create old context file
echo '{"SavedAt":"2025-01-01T00:00:00Z","LastProjectCode":"OLD"}' > ~/.time-reporting/mcp-context.json

# 2. Start MCP server
# Should see "[Persistence] Context is stale, ignoring"

# 3. Verify fresh context is used
```

---

## Next Steps

After completing Task 10.4:
- ✅ **Phase 10 Complete!** All auto-tracking features implemented
- 🎯 **Move to Phase 11:** Integration & Testing
- 📝 **Update TASK-INDEX.md:** Mark Phase 10 tasks as complete

---

## Notes

- Persistence file is JSON for easy debugging and manual editing
- Context is user-specific (stored in home directory)
- Stale threshold prevents using outdated context
- All I/O operations are async for better performance
- Graceful shutdown ensures context is saved properly
- File paths are cross-platform (Windows/Mac/Linux)
