namespace TimeReportingApi.Models;

/// <summary>
/// Defines available metadata tags per project.
/// Consistent naming: ProjectTask defines tasks, ProjectTag defines tags.
/// </summary>
public class ProjectTag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string TagName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Project Project { get; set; } = null!;
    public List<TagValue> AllowedValues { get; set; } = new();
}
