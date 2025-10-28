namespace TimeReportingApi.Models;

/// <summary>
/// Defines available metadata tags per project with allowed values.
/// </summary>
public class TagConfiguration
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string TagName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Project Project { get; set; } = null!;
    public List<TagAllowedValue> AllowedValues { get; set; } = new();
}
