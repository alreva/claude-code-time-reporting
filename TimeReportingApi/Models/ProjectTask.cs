namespace TimeReportingApi.Models;

/// <summary>
/// Represents available tasks within a project.
/// </summary>
public class ProjectTask
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TaskName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Navigation property
    public Project Project { get; set; } = null!;
}
