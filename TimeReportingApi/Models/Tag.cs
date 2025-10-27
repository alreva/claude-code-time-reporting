namespace TimeReportingApi.Models;

/// <summary>
/// Represents a metadata tag with name-value pair.
/// Tags are stored as JSONB in the database.
/// </summary>
public class Tag
{
    [Required]
    [MaxLength(20)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = string.Empty;
}
