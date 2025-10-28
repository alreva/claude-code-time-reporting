namespace TimeReportingApi.Models;

/// <summary>
/// Core entity representing a single time log entry.
/// </summary>
public class TimeEntry
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    public int ProjectTaskId { get; set; }

    [MaxLength(30)]
    public string? IssueId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal StandardHours { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OvertimeHours { get; set; }

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly CompletionDate { get; set; }

    [Required]
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.NotReported;

    public string? DeclineComment { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public ProjectTask ProjectTask { get; set; } = null!;
    public List<TimeEntryTag> Tags { get; set; } = new();
}
