namespace TimeReportingMcpSdk.AutoTracking;

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

        var message = $"""

                       🕐 Time Tracking Suggestion

                       I noticed you've been working for about {FormatDuration(sessionMinutes)}. Would you like to log this time?

                       Suggested entry:
                         • Project: {context.LastProjectCode}
                         • Task: {context.LastTask}
                         • Hours: {hours}

                       To log this time, just say:
                         "Log {hours} hours on {context.LastProjectCode}, {context.LastTask}"

                       Or modify as needed:
                         "Log 1.5 hours on {context.LastProjectCode}, Bug Fixing"
                         "Log 2 hours on CUSTOMER-XYZ, Development"

                       Or say "skip" to dismiss this suggestion.

                       """;

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

        return $"""

                🕐 {customMessage}

                Suggested: {hours}h on {context.LastProjectCode} / {context.LastTask}

                Say: "Log {hours} hours on {context.LastProjectCode}, {context.LastTask}"
                Or modify as needed.

                """.Trim();
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
