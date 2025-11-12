namespace TimeReportingMcpSdk.AutoTracking;

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
