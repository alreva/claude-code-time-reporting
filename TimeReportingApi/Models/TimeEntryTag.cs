namespace TimeReportingApi.Models;

/// <summary>
/// Join table linking time entries to their tag values.
/// Enforces referential integrity - tags must come from project's allowed values.
/// </summary>
public class TimeEntryTag
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Navigation property to the time entry.
    /// </summary>
    public TimeEntry TimeEntry { get; set; } = null!;

    /// <summary>
    /// Navigation property to the tag value (navigate to ProjectTag for tag name).
    /// </summary>
    public TagValue TagValue { get; set; } = null!;
}
