using TimeReportingMcpSdk.AutoTracking;

namespace TimeReportingMcpSdk.Tests.AutoTracking;

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
        Assert.Equal(0.5m, suggested); // 37 min ≈ 0.62 hrs → rounds to 0.5 (nearest quarter)
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
