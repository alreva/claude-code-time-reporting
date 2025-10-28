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
