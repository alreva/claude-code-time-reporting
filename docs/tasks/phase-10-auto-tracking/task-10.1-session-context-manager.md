# Task 10.1: Session Context Manager

**Phase:** 10 - MCP Server Auto-Tracking
**Estimated Time:** 1-2 hours
**Prerequisites:** Phase 9 complete (all 7 MCP tools working)
**Status:** Pending

---

## Objective

Create a session context manager that tracks user activity, maintains state about recent work, and provides context for auto-tracking heuristics. This is the foundation for intelligent time logging suggestions.

---

## Background

The Session Context Manager is the "memory" of the auto-tracking system. It needs to:
- Track when the user starts/stops working
- Remember the last project and task used
- Calculate elapsed time since last activity
- Store context that helps make smart suggestions

---

## Acceptance Criteria

- [ ] `SessionContext.cs` class created with state properties
- [ ] Methods to update context when tools are called
- [ ] Method to calculate elapsed time since last activity
- [ ] Method to get suggested project/task based on recent activity
- [ ] In-memory persistence (survives across multiple tool calls in same session)
- [ ] Unit tests for all context management logic (minimum 8 tests)
- [ ] All tests pass (`/test-mcp`)

---

## Implementation

### 1. Create SessionContext Model

**File:** `TimeReportingMcp/AutoTracking/SessionContext.cs`

```csharp
namespace TimeReportingMcp.AutoTracking;

/// <summary>
/// Manages session state for auto-tracking features.
/// Tracks user activity, recent projects/tasks, and timing information.
/// </summary>
public class SessionContext
{
    /// <summary>
    /// The last project code the user logged time to
    /// </summary>
    public string? LastProjectCode { get; set; }

    /// <summary>
    /// The last task name the user logged time to
    /// </summary>
    public string? LastTask { get; set; }

    /// <summary>
    /// Timestamp of the last time entry created
    /// </summary>
    public DateTime? LastEntryCreatedAt { get; set; }

    /// <summary>
    /// Timestamp of the last user activity (any tool call)
    /// </summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>
    /// Timestamp when the current work session started
    /// </summary>
    public DateTime? SessionStartedAt { get; set; }

    /// <summary>
    /// Total number of tool calls in this session
    /// </summary>
    public int ToolCallCount { get; set; }

    /// <summary>
    /// ID of the last time entry created (for reference)
    /// </summary>
    public Guid? LastEntryId { get; set; }

    /// <summary>
    /// Whether a suggestion has already been shown for the current work session
    /// </summary>
    public bool SuggestionShownForCurrentSession { get; set; }

    public SessionContext()
    {
        LastActivityAt = DateTime.UtcNow;
        SessionStartedAt = DateTime.UtcNow;
        ToolCallCount = 0;
        SuggestionShownForCurrentSession = false;
    }

    /// <summary>
    /// Update context after a time entry is created
    /// </summary>
    public void RecordTimeEntry(string projectCode, string task, Guid entryId)
    {
        LastProjectCode = projectCode;
        LastTask = task;
        LastEntryCreatedAt = DateTime.UtcNow;
        LastEntryId = entryId;
        LastActivityAt = DateTime.UtcNow;
        ToolCallCount++;

        // Reset session after logging time
        SuggestionShownForCurrentSession = false;
        SessionStartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update context after any tool activity
    /// </summary>
    public void RecordActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        ToolCallCount++;

        // Start a new session if this is the first activity after a long break
        if (SessionStartedAt == null || GetIdleMinutes() > 30)
        {
            SessionStartedAt = DateTime.UtcNow;
            SuggestionShownForCurrentSession = false;
        }
    }

    /// <summary>
    /// Calculate minutes elapsed since last activity
    /// </summary>
    public double GetIdleMinutes()
    {
        return (DateTime.UtcNow - LastActivityAt).TotalMinutes;
    }

    /// <summary>
    /// Calculate minutes elapsed since current session started
    /// </summary>
    public double GetSessionMinutes()
    {
        if (SessionStartedAt == null)
            return 0;

        return (DateTime.UtcNow - SessionStartedAt.Value).TotalMinutes;
    }

    /// <summary>
    /// Calculate minutes elapsed since last time entry was created
    /// </summary>
    public double GetMinutesSinceLastEntry()
    {
        if (LastEntryCreatedAt == null)
            return double.MaxValue; // No entry yet

        return (DateTime.UtcNow - LastEntryCreatedAt.Value).TotalMinutes;
    }

    /// <summary>
    /// Get suggested hours based on session duration
    /// Rounds to nearest 0.25 hours (15 min increments)
    /// </summary>
    public decimal GetSuggestedHours()
    {
        var minutes = GetSessionMinutes();
        var hours = minutes / 60.0;

        // Round to nearest 0.25
        var rounded = Math.Round(hours * 4) / 4;

        // Minimum 0.25 hours, maximum 8 hours per suggestion
        return (decimal)Math.Max(0.25, Math.Min(8.0, rounded));
    }

    /// <summary>
    /// Check if we have context to make a suggestion
    /// </summary>
    public bool HasSuggestionContext()
    {
        return LastProjectCode != null && LastTask != null;
    }

    /// <summary>
    /// Reset suggestion flag to allow showing another suggestion
    /// </summary>
    public void ResetSuggestionFlag()
    {
        SuggestionShownForCurrentSession = false;
    }
}
```

### 2. Integrate with McpServer

Update `TimeReportingMcp/McpServer.cs` to maintain session context:

```csharp
private readonly SessionContext _sessionContext = new SessionContext();

// In each tool handler, call RecordActivity()
private async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
{
    _sessionContext.RecordActivity();

    // ... existing tool routing logic ...

    // After successful LogTime, update context:
    if (toolName == "log_time")
    {
        // Extract from response
        var projectCode = /* from result */;
        var task = /* from result */;
        var entryId = /* from result */;

        _sessionContext.RecordTimeEntry(projectCode, task, entryId);
    }
}
```

---

## Testing

### Unit Tests

**File:** `TimeReportingMcp.Tests/AutoTracking/SessionContextTests.cs`

```csharp
using Xunit;
using TimeReportingMcp.AutoTracking;

namespace TimeReportingMcp.Tests.AutoTracking;

public class SessionContextTests
{
    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        // Arrange & Act
        var context = new SessionContext();

        // Assert
        Assert.Equal(0, context.ToolCallCount);
        Assert.Null(context.LastProjectCode);
        Assert.Null(context.LastTask);
        Assert.False(context.SuggestionShownForCurrentSession);
        Assert.NotNull(context.SessionStartedAt);
    }

    [Fact]
    public void RecordTimeEntry_UpdatesAllRelevantFields()
    {
        // Arrange
        var context = new SessionContext();
        var entryId = Guid.NewGuid();

        // Act
        context.RecordTimeEntry("INTERNAL", "Development", entryId);

        // Assert
        Assert.Equal("INTERNAL", context.LastProjectCode);
        Assert.Equal("Development", context.LastTask);
        Assert.Equal(entryId, context.LastEntryId);
        Assert.NotNull(context.LastEntryCreatedAt);
        Assert.Equal(1, context.ToolCallCount);
        Assert.False(context.SuggestionShownForCurrentSession);
    }

    [Fact]
    public void RecordActivity_IncrementsToolCallCount()
    {
        // Arrange
        var context = new SessionContext();

        // Act
        context.RecordActivity();
        context.RecordActivity();

        // Assert
        Assert.Equal(2, context.ToolCallCount);
    }

    [Fact]
    public void GetIdleMinutes_ReturnsZeroImmediatelyAfterActivity()
    {
        // Arrange
        var context = new SessionContext();
        context.RecordActivity();

        // Act
        var idle = context.GetIdleMinutes();

        // Assert
        Assert.True(idle < 0.1); // Less than 6 seconds
    }

    [Fact]
    public void GetSessionMinutes_ReturnsElapsedTime()
    {
        // Arrange
        var context = new SessionContext();

        // Act
        System.Threading.Thread.Sleep(100); // Wait 100ms
        var elapsed = context.GetSessionMinutes();

        // Assert
        Assert.True(elapsed > 0);
        Assert.True(elapsed < 1); // Should be less than 1 minute
    }

    [Fact]
    public void GetSuggestedHours_RoundsToNearestQuarterHour()
    {
        // Arrange
        var context = new SessionContext();
        context.SessionStartedAt = DateTime.UtcNow.AddMinutes(-37); // 37 minutes ago

        // Act
        var suggested = context.GetSuggestedHours();

        // Assert
        Assert.Equal(0.75m, suggested); // 37 min ≈ 0.62 hrs → rounds to 0.75
    }

    [Fact]
    public void GetSuggestedHours_EnforcesMinimum()
    {
        // Arrange
        var context = new SessionContext();
        context.SessionStartedAt = DateTime.UtcNow.AddMinutes(-5); // 5 minutes

        // Act
        var suggested = context.GetSuggestedHours();

        // Assert
        Assert.Equal(0.25m, suggested); // Minimum 0.25 hours
    }

    [Fact]
    public void GetSuggestedHours_EnforcesMaximum()
    {
        // Arrange
        var context = new SessionContext();
        context.SessionStartedAt = DateTime.UtcNow.AddHours(-10); // 10 hours

        // Act
        var suggested = context.GetSuggestedHours();

        // Assert
        Assert.Equal(8.0m, suggested); // Maximum 8 hours
    }

    [Fact]
    public void HasSuggestionContext_ReturnsTrueWhenProjectAndTaskSet()
    {
        // Arrange
        var context = new SessionContext();

        // Act & Assert - before setting
        Assert.False(context.HasSuggestionContext());

        // Act - set context
        context.RecordTimeEntry("INTERNAL", "Development", Guid.NewGuid());

        // Assert - after setting
        Assert.True(context.HasSuggestionContext());
    }

    [Fact]
    public void GetMinutesSinceLastEntry_ReturnsMaxValueWhenNoEntry()
    {
        // Arrange
        var context = new SessionContext();

        // Act
        var minutes = context.GetMinutesSinceLastEntry();

        // Assert
        Assert.Equal(double.MaxValue, minutes);
    }
}
```

### Test Execution

```bash
# Run all MCP tests
/test-mcp

# Or run specific test file
dotnet test TimeReportingMcp.Tests --filter "FullyQualifiedName~SessionContextTests"
```

**Expected:** 10 tests pass ✅

---

## Integration Points

### McpServer Integration

The `SessionContext` will be used by:
1. **McpServer.cs** - Maintains instance, calls RecordActivity/RecordTimeEntry
2. **DetectionHeuristics.cs** (Task 10.2) - Reads context to detect work patterns
3. **SuggestionFormatter.cs** (Task 10.3) - Uses context to format suggestions

### Example Usage in McpServer

```csharp
public class McpServer
{
    private readonly SessionContext _sessionContext = new SessionContext();

    private async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
    {
        // Record activity for all tool calls
        _sessionContext.RecordActivity();

        var toolName = request.Params.Name;

        // ... route to tool handlers ...

        // After successful log_time call:
        if (toolName == "log_time" && response.Result != null)
        {
            var entry = /* extract from response */;
            _sessionContext.RecordTimeEntry(
                entry.ProjectCode,
                entry.Task,
                entry.Id
            );
        }

        return response;
    }
}
```

---

## Related Files

**Created:**
- `TimeReportingMcp/AutoTracking/SessionContext.cs`
- `TimeReportingMcp.Tests/AutoTracking/SessionContextTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add SessionContext instance and integration

---

## Validation

After implementation:

1. ✅ All 10 unit tests pass
2. ✅ SessionContext correctly tracks state across tool calls
3. ✅ Idle time calculation works correctly
4. ✅ Suggested hours rounding works correctly
5. ✅ Context persists within same MCP server session

---

## Next Steps

After completing Task 10.1:
- **Task 10.2:** Implement Detection Heuristics to analyze context and decide when to suggest logging time
- **Task 10.3:** Implement Confirmation Prompts to format user-friendly suggestions
- **Task 10.4:** Add Context Persistence for cross-session state management

---

## Notes

- Session context is **in-memory only** - state resets when MCP server restarts (addressed in Task 10.4)
- Time calculations use `DateTime.UtcNow` for consistency
- Hours are rounded to 0.25 increments (standard time tracking practice)
- Maximum 8 hours per suggestion prevents unrealistic values
