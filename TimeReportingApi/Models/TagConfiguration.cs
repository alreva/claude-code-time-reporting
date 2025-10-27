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

    public List<string> AllowedValues { get; set; } = new();

    public bool IsActive { get; set; } = true;

    // Navigation property
    public Project Project { get; set; } = null!;
}
