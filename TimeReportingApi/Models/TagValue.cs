namespace TimeReportingApi.Models;

/// <summary>
/// Represents an allowed value for a project tag.
/// Consistent naming: ProjectTask → TaskName, ProjectTag → TagValue.
/// </summary>
public class TagValue
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The allowed value (e.g., "High", "Medium", "Low" for Priority tag).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the project tag.
    /// </summary>
    public ProjectTag ProjectTag { get; set; } = null!;
}
