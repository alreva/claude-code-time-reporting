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
