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
    /// Foreign key to the time entry.
    /// </summary>
    public Guid TimeEntryId { get; set; }

    /// <summary>
    /// Foreign key to the allowed tag value (includes tag name via TagConfiguration).
    /// </summary>
    public int TagAllowedValueId { get; set; }

    /// <summary>
    /// Navigation property to the time entry.
    /// </summary>
    public TimeEntry TimeEntry { get; set; } = null!;

    /// <summary>
    /// Navigation property to the tag value (navigate to TagConfiguration for tag name).
    /// </summary>
    public TagAllowedValue TagAllowedValue { get; set; } = null!;
}
