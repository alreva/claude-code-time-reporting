namespace TimeReportingApi.Models;

/// <summary>
/// Represents an allowed value for a tag in a project's tag configuration.
/// Fully relational design for database-agnostic flexibility.
/// </summary>
public class TagAllowedValue
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the tag configuration.
    /// </summary>
    public int TagConfigurationId { get; set; }

    /// <summary>
    /// The allowed value (e.g., "High", "Medium", "Low" for Priority tag).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the tag configuration.
    /// </summary>
    public TagConfiguration TagConfiguration { get; set; } = null!;
}
