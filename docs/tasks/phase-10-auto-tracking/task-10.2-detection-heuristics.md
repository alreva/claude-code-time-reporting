# Task 10.2: Detection Heuristics

**Phase:** 10 - MCP Server Auto-Tracking
**Estimated Time:** 2 hours
**Prerequisites:** Task 10.1 complete (SessionContext working)
**Status:** Pending

---

## Objective

Implement smart detection logic that analyzes session context and user activity patterns to determine when to suggest logging time. This is the "intelligence" of the auto-tracking system.

---

## Background

The detection heuristics need to balance being helpful (catching unlogged work) without being annoying (suggesting too frequently). We'll use multiple signals:
- Time elapsed since last entry
- Number of tool calls (activity level)
- Session duration
- Whether a suggestion was already shown

---

## Acceptance Criteria

- [ ] `DetectionHeuristics.cs` class created with detection logic
- [ ] Method to check if suggestion should be triggered
- [ ] Configurable thresholds for detection
- [ ] Clear detection reasons for debugging
- [ ] Unit tests covering all detection scenarios (minimum 12 tests)
- [ ] All tests pass (`/test-mcp`)

---

## Implementation

### 1. Create DetectionHeuristics Class

**File:** `TimeReportingMcp/AutoTracking/DetectionHeuristics.cs`

```csharp
namespace TimeReportingMcp.AutoTracking;

/// <summary>
/// Heuristics engine for detecting when to suggest time entry creation.
/// Analyzes session context and activity patterns to make intelligent suggestions.
/// </summary>
public class DetectionHeuristics
{
    // Configuration thresholds
    private readonly int _minMinutesForSuggestion;
    private readonly int _minToolCallsForSuggestion;
    private readonly int _minMinutesSinceLastEntry;
    private readonly int _idleThresholdMinutes;

    public DetectionHeuristics(
        int minMinutesForSuggestion = 30,
        int minToolCallsForSuggestion = 5,
        int minMinutesSinceLastEntry = 30,
        int idleThresholdMinutes = 10)
    {
        _minMinutesForSuggestion = minMinutesForSuggestion;
        _minToolCallsForSuggestion = minToolCallsForSuggestion;
        _minMinutesSinceLastEntry = minMinutesSinceLastEntry;
        _idleThresholdMinutes = idleThresholdMinutes;
    }

    /// <summary>
    /// Check if we should suggest creating a time entry based on current context
    /// </summary>
    /// <returns>Tuple of (shouldSuggest, reason)</returns>
    public (bool ShouldSuggest, string Reason) ShouldSuggestTimeEntry(SessionContext context)
    {
        // Rule 1: Don't suggest if we already showed suggestion for this session
        if (context.SuggestionShownForCurrentSession)
        {
            return (false, "Suggestion already shown for current session");
        }

        // Rule 2: Don't suggest if user is idle (no recent activity)
        if (context.GetIdleMinutes() > _idleThresholdMinutes)
        {
            return (false, $"User idle for {context.GetIdleMinutes():F1} minutes");
        }

        // Rule 3: Must have context (previous project/task to suggest)
        if (!context.HasSuggestionContext())
        {
            return (false, "No previous project/task context available");
        }

        // Rule 4: Must have been working for minimum duration
        var sessionMinutes = context.GetSessionMinutes();
        if (sessionMinutes < _minMinutesForSuggestion)
        {
            return (false, $"Session too short ({sessionMinutes:F1} min < {_minMinutesForSuggestion} min)");
        }

        // Rule 5: Must have sufficient activity (tool calls)
        if (context.ToolCallCount < _minToolCallsForSuggestion)
        {
            return (false, $"Insufficient activity ({context.ToolCallCount} calls < {_minToolCallsForSuggestion} calls)");
        }

        // Rule 6: Must be sufficient time since last logged entry
        var minutesSinceLastEntry = context.GetMinutesSinceLastEntry();
        if (minutesSinceLastEntry < _minMinutesSinceLastEntry)
        {
            return (false, $"Recent entry logged ({minutesSinceLastEntry:F1} min ago)");
        }

        // All conditions met - suggest!
        return (true, $"Active session: {sessionMinutes:F1} min, {context.ToolCallCount} tool calls");
    }

    /// <summary>
    /// Analyze context and provide detailed detection info (for debugging/logging)
    /// </summary>
    public DetectionInfo AnalyzeContext(SessionContext context)
    {
        var (shouldSuggest, reason) = ShouldSuggestTimeEntry(context);

        return new DetectionInfo
        {
            ShouldSuggest = shouldSuggest,
            Reason = reason,
            SessionMinutes = context.GetSessionMinutes(),
            IdleMinutes = context.GetIdleMinutes(),
            ToolCallCount = context.ToolCallCount,
            MinutesSinceLastEntry = context.GetMinutesSinceLastEntry(),
            HasContext = context.HasSuggestionContext(),
            SuggestionAlreadyShown = context.SuggestionShownForCurrentSession,
            SuggestedHours = context.GetSuggestedHours(),
            LastProjectCode = context.LastProjectCode,
            LastTask = context.LastTask
        };
    }

    /// <summary>
    /// Detect work type based on recent activity patterns
    /// This is a simple heuristic that could be expanded in the future
    /// </summary>
    public string DetectLikelyTaskType(SessionContext context, string defaultTask = "Development")
    {
        // For v1, return the last used task as the most likely
        // Future: Analyze tool usage patterns, time of day, etc.
        return context.LastTask ?? defaultTask;
    }
}

/// <summary>
/// Detailed information about detection analysis
/// </summary>
public class DetectionInfo
{
    public bool ShouldSuggest { get; set; }
    public string Reason { get; set; } = string.Empty;
    public double SessionMinutes { get; set; }
    public double IdleMinutes { get; set; }
    public int ToolCallCount { get; set; }
    public double MinutesSinceLastEntry { get; set; }
    public bool HasContext { get; set; }
    public bool SuggestionAlreadyShown { get; set; }
    public decimal SuggestedHours { get; set; }
    public string? LastProjectCode { get; set; }
    public string? LastTask { get; set; }

    public override string ToString()
    {
        return $"Detection: {(ShouldSuggest ? "SUGGEST" : "NO")}, Reason: {Reason}, " +
               $"Session: {SessionMinutes:F1}m, Idle: {IdleMinutes:F1}m, Calls: {ToolCallCount}";
    }
}
```

### 2. Integrate with McpServer

Update `TimeReportingMcp/McpServer.cs`:

```csharp
public class McpServer
{
    private readonly SessionContext _sessionContext = new SessionContext();
    private readonly DetectionHeuristics _heuristics = new DetectionHeuristics();

    private async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
    {
        _sessionContext.RecordActivity();

        // ... existing tool routing ...

        // After handling the tool, check if we should suggest time entry
        var (shouldSuggest, reason) = _heuristics.ShouldSuggestTimeEntry(_sessionContext);

        if (shouldSuggest)
        {
            // Mark as shown to avoid repeated suggestions
            _sessionContext.SuggestionShownForCurrentSession = true;

            // TODO (Task 10.3): Format and include suggestion in response
            Console.Error.WriteLine($"[Auto-tracking] {reason}");
        }

        return response;
    }
}
```

---

## Testing

### Unit Tests

**File:** `TimeReportingMcp.Tests/AutoTracking/DetectionHeuristicsTests.cs`

```csharp
using Xunit;
using TimeReportingMcp.AutoTracking;

namespace TimeReportingMcp.Tests.AutoTracking;

public class DetectionHeuristicsTests
{
    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsFalse_WhenSuggestionAlreadyShown()
    {
        // Arrange
        var heuristics = new DetectionHeuristics();
        var context = CreateActiveContext();
        context.SuggestionShownForCurrentSession = true;

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.False(shouldSuggest);
        Assert.Contains("already shown", reason);
    }

    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsFalse_WhenUserIsIdle()
    {
        // Arrange
        var heuristics = new DetectionHeuristics(idleThresholdMinutes: 10);
        var context = CreateActiveContext();
        context.LastActivityAt = DateTime.UtcNow.AddMinutes(-15); // Idle for 15 min

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.False(shouldSuggest);
        Assert.Contains("idle", reason.ToLower());
    }

    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsFalse_WhenNoContext()
    {
        // Arrange
        var heuristics = new DetectionHeuristics();
        var context = new SessionContext();
        context.SessionStartedAt = DateTime.UtcNow.AddMinutes(-40);
        context.ToolCallCount = 10;

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.False(shouldSuggest);
        Assert.Contains("context", reason.ToLower());
    }

    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsFalse_WhenSessionTooShort()
    {
        // Arrange
        var heuristics = new DetectionHeuristics(minMinutesForSuggestion: 30);
        var context = CreateActiveContext();
        context.SessionStartedAt = DateTime.UtcNow.AddMinutes(-15); // Only 15 min

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.False(shouldSuggest);
        Assert.Contains("too short", reason.ToLower());
    }

    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsFalse_WhenInsufficientActivity()
    {
        // Arrange
        var heuristics = new DetectionHeuristics(minToolCallsForSuggestion: 5);
        var context = CreateActiveContext();
        context.ToolCallCount = 3; // Only 3 calls

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.False(shouldSuggest);
        Assert.Contains("insufficient activity", reason.ToLower());
    }

    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsFalse_WhenRecentEntryLogged()
    {
        // Arrange
        var heuristics = new DetectionHeuristics(minMinutesSinceLastEntry: 30);
        var context = CreateActiveContext();
        context.LastEntryCreatedAt = DateTime.UtcNow.AddMinutes(-10); // Logged 10 min ago

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.False(shouldSuggest);
        Assert.Contains("recent entry", reason.ToLower());
    }

    [Fact]
    public void ShouldSuggestTimeEntry_ReturnsTrue_WhenAllConditionsMet()
    {
        // Arrange
        var heuristics = new DetectionHeuristics(
            minMinutesForSuggestion: 30,
            minToolCallsForSuggestion: 5,
            minMinutesSinceLastEntry: 30,
            idleThresholdMinutes: 10
        );
        var context = CreateActiveContext();

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.True(shouldSuggest);
        Assert.Contains("active session", reason.ToLower());
    }

    [Fact]
    public void ShouldSuggestTimeEntry_UsesCustomThresholds()
    {
        // Arrange - lower thresholds
        var heuristics = new DetectionHeuristics(
            minMinutesForSuggestion: 10,  // Lower threshold
            minToolCallsForSuggestion: 2   // Lower threshold
        );
        var context = CreateActiveContext();
        context.SessionStartedAt = DateTime.UtcNow.AddMinutes(-15);
        context.ToolCallCount = 3;

        // Act
        var (shouldSuggest, reason) = heuristics.ShouldSuggestTimeEntry(context);

        // Assert
        Assert.True(shouldSuggest); // Should suggest with lower thresholds
    }

    [Fact]
    public void AnalyzeContext_ReturnsDetailedInfo()
    {
        // Arrange
        var heuristics = new DetectionHeuristics();
        var context = CreateActiveContext();

        // Act
        var info = heuristics.AnalyzeContext(context);

        // Assert
        Assert.NotNull(info);
        Assert.Equal("INTERNAL", info.LastProjectCode);
        Assert.Equal("Development", info.LastTask);
        Assert.True(info.SessionMinutes > 0);
        Assert.True(info.ToolCallCount > 0);
        Assert.False(string.IsNullOrEmpty(info.Reason));
    }

    [Fact]
    public void AnalyzeContext_IncludesSuggestedHours()
    {
        // Arrange
        var heuristics = new DetectionHeuristics();
        var context = CreateActiveContext();
        context.SessionStartedAt = DateTime.UtcNow.AddMinutes(-45);

        // Act
        var info = heuristics.AnalyzeContext(context);

        // Assert
        Assert.True(info.SuggestedHours > 0);
        Assert.Equal(0.75m, info.SuggestedHours); // 45 min ≈ 0.75 hours
    }

    [Fact]
    public void DetectLikelyTaskType_ReturnsLastTask()
    {
        // Arrange
        var heuristics = new DetectionHeuristics();
        var context = CreateActiveContext();

        // Act
        var taskType = heuristics.DetectLikelyTaskType(context);

        // Assert
        Assert.Equal("Development", taskType);
    }

    [Fact]
    public void DetectLikelyTaskType_ReturnsDefaultWhenNoContext()
    {
        // Arrange
        var heuristics = new DetectionHeuristics();
        var context = new SessionContext();

        // Act
        var taskType = heuristics.DetectLikelyTaskType(context, "Bug Fixing");

        // Assert
        Assert.Equal("Bug Fixing", taskType);
    }

    [Fact]
    public void DetectionInfo_ToStringProvidesSummary()
    {
        // Arrange
        var info = new DetectionInfo
        {
            ShouldSuggest = true,
            Reason = "Test reason",
            SessionMinutes = 45.5,
            IdleMinutes = 2.3,
            ToolCallCount = 8
        };

        // Act
        var summary = info.ToString();

        // Assert
        Assert.Contains("SUGGEST", summary);
        Assert.Contains("45.5", summary);
        Assert.Contains("8", summary);
    }

    // Helper method to create a context that meets all suggestion criteria
    private SessionContext CreateActiveContext()
    {
        var context = new SessionContext
        {
            LastProjectCode = "INTERNAL",
            LastTask = "Development",
            LastEntryCreatedAt = DateTime.UtcNow.AddHours(-2), // 2 hours ago
            LastActivityAt = DateTime.UtcNow.AddMinutes(-2), // Active 2 min ago
            SessionStartedAt = DateTime.UtcNow.AddMinutes(-45), // 45 min session
            ToolCallCount = 10,
            SuggestionShownForCurrentSession = false
        };

        return context;
    }
}
```

### Test Execution

```bash
# Run all MCP tests
/test-mcp

# Or run specific test file
dotnet test TimeReportingMcp.Tests --filter "FullyQualifiedName~DetectionHeuristicsTests"
```

**Expected:** 13 tests pass ✅

---

## Configuration

Default thresholds (can be adjusted based on usage patterns):

| Parameter | Default | Description |
|-----------|---------|-------------|
| `minMinutesForSuggestion` | 30 | Minimum session duration before suggesting |
| `minToolCallsForSuggestion` | 5 | Minimum activity level (tool calls) |
| `minMinutesSinceLastEntry` | 30 | Minimum time since last logged entry |
| `idleThresholdMinutes` | 10 | Maximum idle time before considering user inactive |

---

## Decision Logic Flow

```
Check if should suggest:
  ├─ Already shown suggestion? → NO
  ├─ User idle (>10 min)? → NO
  ├─ No previous context? → NO
  ├─ Session too short (<30 min)? → NO
  ├─ Low activity (<5 calls)? → NO
  ├─ Recent entry (<30 min ago)? → NO
  └─ All conditions pass? → YES, SUGGEST!
```

---

## Related Files

**Created:**
- `TimeReportingMcp/AutoTracking/DetectionHeuristics.cs`
- `TimeReportingMcp.Tests/AutoTracking/DetectionHeuristicsTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add DetectionHeuristics integration

---

## Validation

After implementation:

1. ✅ All 13 unit tests pass
2. ✅ Detection correctly identifies when to suggest
3. ✅ All heuristic rules work as expected
4. ✅ Custom thresholds can be configured
5. ✅ Detailed analysis info available for debugging

---

## Next Steps

After completing Task 10.2:
- **Task 10.3:** Implement Confirmation Prompts to format the suggestions in a user-friendly way
- **Task 10.4:** Add Context Persistence for cross-session state

---

## Notes

- Thresholds are configurable but have sensible defaults
- Heuristics can be enhanced in future versions (e.g., time-of-day patterns, keyword detection)
- Detection is conservative by default (better to miss a suggestion than be annoying)
- All time calculations use UTC for consistency
