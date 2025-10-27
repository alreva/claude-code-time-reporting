namespace TimeReportingApi.Models;

/// <summary>
/// Defines available projects that users can log time against.
/// </summary>
public class Project
{
    [Key]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List<ProjectTask> AvailableTasks { get; set; } = new();
    public List<TagConfiguration> TagConfigurations { get; set; } = new();
    public List<TimeEntry> TimeEntries { get; set; } = new();
}
