# Task 10.3: Confirmation Prompts

**Phase:** 10 - MCP Server Auto-Tracking
**Estimated Time:** 1 hour
**Prerequisites:** Tasks 10.1 and 10.2 complete (SessionContext and DetectionHeuristics working)
**Status:** Pending

---

## Objective

Implement user-friendly suggestion formatting that presents auto-tracking suggestions to the user through Claude Code's natural language interface. Format suggestions clearly and make them easy to accept or modify.

---

## Background

When the detection heuristics determine it's time to suggest logging time, we need to format that suggestion in a way that:
- Is clear and non-intrusive
- Pre-fills intelligent defaults (project, task, hours)
- Makes it easy for the user to accept, modify, or decline
- Fits naturally into Claude Code's conversational flow

---

## Acceptance Criteria

- [ ] `SuggestionFormatter.cs` class created with formatting logic
- [ ] Method to format time entry suggestions
- [ ] Clear, friendly message format
- [ ] Includes suggested project, task, and hours
- [ ] Easy to understand and act upon
- [ ] Unit tests for all formatting scenarios (minimum 6 tests)
- [ ] All tests pass (`/test-mcp`)

---

## Implementation

### 1. Create SuggestionFormatter Class

**File:** `TimeReportingMcp/AutoTracking/SuggestionFormatter.cs`

```csharp
namespace TimeReportingMcp.AutoTracking;

/// <summary>
/// Formats auto-tracking suggestions for user-friendly display.
/// Creates clear, actionable prompts for time entry creation.
/// </summary>
public class SuggestionFormatter
{
    /// <summary>
    /// Format a time entry suggestion based on session context
    /// </summary>
    public string FormatSuggestion(SessionContext context)
    {
        if (!context.HasSuggestionContext())
        {
            return string.Empty;
        }

        var hours = context.GetSuggestedHours();
        var sessionMinutes = (int)Math.Round(context.GetSessionMinutes());

        var message = $@"
🕐 Time Tracking Suggestion

I noticed you've been working for about {FormatDuration(sessionMinutes)}. Would you like to log this time?

Suggested entry:
  • Project: {context.LastProjectCode}
  • Task: {context.LastTask}
  • Hours: {hours}

To log this time, just say:
  ""Log {hours} hours on {context.LastProjectCode}, {context.LastTask}""

Or modify as needed:
  ""Log 1.5 hours on {context.LastProjectCode}, Bug Fixing""
  ""Log 2 hours on CUSTOMER-XYZ, Development""

Or say ""skip"" to dismiss this suggestion.
";

        return message.Trim();
    }

    /// <summary>
    /// Format a minimal suggestion (shorter version)
    /// </summary>
    public string FormatMinimalSuggestion(SessionContext context)
    {
        if (!context.HasSuggestionContext())
        {
            return string.Empty;
        }

        var hours = context.GetSuggestedHours();

        return $"🕐 Log time? ({hours}h on {context.LastProjectCode}/{context.LastTask})";
    }

    /// <summary>
    /// Format a suggestion with custom message
    /// </summary>
    public string FormatCustomSuggestion(
        SessionContext context,
        string customMessage)
    {
        if (!context.HasSuggestionContext())
        {
            return string.Empty;
        }

        var hours = context.GetSuggestedHours();

        return $@"
🕐 {customMessage}

Suggested: {hours}h on {context.LastProjectCode} / {context.LastTask}

Say: ""Log {hours} hours on {context.LastProjectCode}, {context.LastTask}""
Or modify as needed.
".Trim();
    }

    /// <summary>
    /// Format duration in a human-friendly way
    /// </summary>
    private string FormatDuration(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} minutes";
        }

        var hours = minutes / 60;
        var remainingMinutes = minutes % 60;

        if (remainingMinutes == 0)
        {
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        return hours == 1
            ? $"1 hour and {remainingMinutes} minutes"
            : $"{hours} hours and {remainingMinutes} minutes";
    }

    /// <summary>
    /// Create a suggestion result that can be included in MCP response
    /// </summary>
    public SuggestionResult CreateSuggestion(SessionContext context)
    {
        return new SuggestionResult
        {
            Message = FormatSuggestion(context),
            ProjectCode = context.LastProjectCode ?? "",
            Task = context.LastTask ?? "",
            SuggestedHours = context.GetSuggestedHours(),
            SessionMinutes = context.GetSessionMinutes()
        };
    }
}

/// <summary>
/// Structured suggestion data
/// </summary>
public class SuggestionResult
{
    public string Message { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public decimal SuggestedHours { get; set; }
    public double SessionMinutes { get; set; }
}
```

### 2. Integrate with McpServer

Update `TimeReportingMcp/McpServer.cs` to include suggestions in responses:

```csharp
public class McpServer
{
    private readonly SessionContext _sessionContext = new SessionContext();
    private readonly DetectionHeuristics _heuristics = new DetectionHeuristics();
    private readonly SuggestionFormatter _formatter = new SuggestionFormatter();

    private async Task<JsonRpcResponse> HandleToolCall(JsonRpcRequest request)
    {
        _sessionContext.RecordActivity();

        // ... existing tool routing ...
        var response = await ExecuteTool(request);

        // After handling the tool, check if we should suggest time entry
        var (shouldSuggest, reason) = _heuristics.ShouldSuggestTimeEntry(_sessionContext);

        if (shouldSuggest)
        {
            // Mark as shown to avoid repeated suggestions
            _sessionContext.SuggestionShownForCurrentSession = true;

            // Format the suggestion
            var suggestion = _formatter.FormatSuggestion(_sessionContext);

            // Append suggestion to the tool's response
            // Note: This adds the suggestion as additional content
            var existingContent = response.Result?.content ?? new List<object>();
            var updatedContent = new List<object>(existingContent)
            {
                new { type = "text", text = suggestion }
            };

            response.Result = new { content = updatedContent };

            Console.Error.WriteLine($"[Auto-tracking] Suggestion shown: {reason}");
        }

        return response;
    }
}
```

---

## Testing

### Unit Tests

**File:** `TimeReportingMcp.Tests/AutoTracking/SuggestionFormatterTests.cs`

```csharp
using Xunit;
using TimeReportingMcp.AutoTracking;

namespace TimeReportingMcp.Tests.AutoTracking;

public class SuggestionFormatterTests
{
    [Fact]
    public void FormatSuggestion_ReturnsFormattedMessage()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 45);

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.NotEmpty(message);
        Assert.Contains("INTERNAL", message);
        Assert.Contains("Development", message);
        Assert.Contains("0.75", message); // 45 min = 0.75 hours
        Assert.Contains("🕐", message); // Clock emoji
    }

    [Fact]
    public void FormatSuggestion_ReturnsEmpty_WhenNoContext()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = new SessionContext();

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.Empty(message);
    }

    [Fact]
    public void FormatSuggestion_IncludesExampleCommands()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 60);

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.Contains("Log 1 hours on INTERNAL, Development", message);
        Assert.Contains("Or modify as needed:", message);
    }

    [Fact]
    public void FormatMinimalSuggestion_ReturnsShortFormat()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 45);

        // Act
        var message = formatter.FormatMinimalSuggestion(context);

        // Assert
        Assert.NotEmpty(message);
        Assert.Contains("🕐", message);
        Assert.Contains("0.75h", message);
        Assert.Contains("INTERNAL/Development", message);
        Assert.DoesNotContain("Would you like", message); // Shorter format
    }

    [Fact]
    public void FormatCustomSuggestion_UsesCustomMessage()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 45);
        var customMessage = "Don't forget to log your work!";

        // Act
        var message = formatter.FormatCustomSuggestion(context, customMessage);

        // Assert
        Assert.Contains(customMessage, message);
        Assert.Contains("0.75h", message);
        Assert.Contains("INTERNAL", message);
    }

    [Fact]
    public void FormatDuration_HandlesMinutes()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 25);

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.Contains("25 minutes", message);
    }

    [Fact]
    public void FormatDuration_HandlesExactHours()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 120); // 2 hours

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.Contains("2 hours", message);
        Assert.DoesNotContain("minutes", message);
    }

    [Fact]
    public void FormatDuration_HandlesHoursAndMinutes()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 95); // 1h 35m

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.Contains("1 hour and 35 minutes", message);
    }

    [Fact]
    public void CreateSuggestion_ReturnsStructuredData()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("INTERNAL", "Development", 45);

        // Act
        var result = formatter.CreateSuggestion(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("INTERNAL", result.ProjectCode);
        Assert.Equal("Development", result.Task);
        Assert.Equal(0.75m, result.SuggestedHours);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public void FormatSuggestion_HandlesLongProjectNames()
    {
        // Arrange
        var formatter = new SuggestionFormatter();
        var context = CreateContext("CUSTOMER-VERY-LONG-PROJECT-NAME", "Bug Fixing", 60);

        // Act
        var message = formatter.FormatSuggestion(context);

        // Assert
        Assert.Contains("CUSTOMER-VERY-LONG-PROJECT-NAME", message);
        Assert.Contains("Bug Fixing", message);
    }

    // Helper method
    private SessionContext CreateContext(string projectCode, string task, int sessionMinutes)
    {
        var context = new SessionContext
        {
            LastProjectCode = projectCode,
            LastTask = task,
            SessionStartedAt = DateTime.UtcNow.AddMinutes(-sessionMinutes),
            LastActivityAt = DateTime.UtcNow,
            ToolCallCount = 10
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
dotnet test TimeReportingMcp.Tests --filter "FullyQualifiedName~SuggestionFormatterTests"
```

**Expected:** 10 tests pass ✅

---

## Example Outputs

### Full Suggestion
```
🕐 Time Tracking Suggestion

I noticed you've been working for about 45 minutes. Would you like to log this time?

Suggested entry:
  • Project: INTERNAL
  • Task: Development
  • Hours: 0.75

To log this time, just say:
  "Log 0.75 hours on INTERNAL, Development"

Or modify as needed:
  "Log 1.5 hours on INTERNAL, Bug Fixing"
  "Log 2 hours on CUSTOMER-XYZ, Development"

Or say "skip" to dismiss this suggestion.
```

### Minimal Suggestion
```
🕐 Log time? (0.75h on INTERNAL/Development)
```

### Custom Message Suggestion
```
🕐 Great progress! Time to log your work?

Suggested: 1h on INTERNAL / Development

Say: "Log 1 hours on INTERNAL, Development"
Or modify as needed.
```

---

## User Experience Flow

```
User works for 45 minutes → Uses Claude Code tools →
Detection triggers → Suggestion formatted →
Displayed to user → User responds:
  ├─ "Log 0.75 hours on INTERNAL, Development" → Entry created
  ├─ "Log 1 hour on INTERNAL, Bug Fixing" → Modified and created
  └─ "Skip" or ignores → Suggestion dismissed
```

---

## Related Files

**Created:**
- `TimeReportingMcp/AutoTracking/SuggestionFormatter.cs`
- `TimeReportingMcp.Tests/AutoTracking/SuggestionFormatterTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add SuggestionFormatter integration

---

## Validation

After implementation:

1. ✅ All 10 unit tests pass
2. ✅ Suggestions are clear and friendly
3. ✅ Formatting handles edge cases (long names, various durations)
4. ✅ Easy for users to accept or modify suggestions
5. ✅ Minimal version available for less intrusive prompts

---

## Next Steps

After completing Task 10.3:
- **Task 10.4:** Add Context Persistence to maintain state across MCP server restarts

---

## Notes

- Suggestions use the 🕐 emoji for visual recognition
- Messages are formatted for readability in CLI/chat interfaces
- Users can accept, modify, or ignore suggestions naturally
- Minimal version can be used for frequent users who prefer brevity
- All formatting is tested for various input scenarios
