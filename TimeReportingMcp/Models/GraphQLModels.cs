namespace TimeReportingMcp.Models;

/// <summary>
/// Time entry response model matching GraphQL schema
/// </summary>
public class TimeEntryData
{
    public Guid Id { get; set; }
    public ProjectInfo Project { get; set; } = null!;
    public ProjectTaskInfo ProjectTask { get; set; } = null!;
    public string? IssueId { get; set; }
    public decimal StandardHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? Description { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string CompletionDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? DeclineComment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<TimeEntryTagData> Tags { get; set; } = new();
}

/// <summary>
/// Project info (code and name only)
/// </summary>
public class ProjectInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Project task info (task name only)
/// </summary>
public class ProjectTaskInfo
{
    public string TaskName { get; set; } = string.Empty;
}

/// <summary>
/// Tag input for mutations
/// </summary>
public class TagInput
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Time entry tag data from GraphQL response
/// </summary>
public class TimeEntryTagData
{
    public TagValueData TagValue { get; set; } = null!;
}

/// <summary>
/// Tag value info with project tag details
/// </summary>
public class TagValueData
{
    public string Value { get; set; } = string.Empty;
    public ProjectTagData ProjectTag { get; set; } = null!;
}

/// <summary>
/// Project tag info (tag name)
/// </summary>
public class ProjectTagData
{
    public string TagName { get; set; } = string.Empty;
}
