namespace TimeReportingApi.Models;

/// <summary>
/// Represents a tag (metadata key-value pair) associated with a time entry.
/// Fully relational design for database-agnostic flexibility.
/// </summary>
public class TimeEntryTag
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the time entry.
    /// </summary>
    public Guid TimeEntryId { get; set; }

    /// <summary>
    /// Tag name (e.g., "Priority", "Component", "Sprint").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag value (e.g., "High", "Frontend", "Sprint-23").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the time entry.
    /// </summary>
    public TimeEntry TimeEntry { get; set; } = null!;
}
