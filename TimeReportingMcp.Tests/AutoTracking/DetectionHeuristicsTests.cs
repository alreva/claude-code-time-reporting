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
