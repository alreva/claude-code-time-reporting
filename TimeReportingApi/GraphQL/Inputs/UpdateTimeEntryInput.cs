namespace TimeReportingApi.GraphQL.Inputs;

/// <summary>
/// Input type for updating an existing time entry.
/// All fields are optional - only provided fields will be updated.
/// Cannot update projectCode (use moveTaskToProject) or status (use workflow mutations).
/// </summary>
public class UpdateTimeEntryInput
{
    /// <summary>
    /// Task name (must be in project's available tasks)
    /// </summary>
    public string? Task { get; set; }

    /// <summary>
    /// Issue/ticket identifier
    /// </summary>
    public string? IssueId { get; set; }

    /// <summary>
    /// Standard working hours (must be >= 0)
    /// </summary>
    public decimal? StandardHours { get; set; }

    /// <summary>
    /// Overtime hours (must be >= 0)
    /// </summary>
    public decimal? OvertimeHours { get; set; }

    /// <summary>
    /// Work description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Start date of the work (must be <= CompletionDate)
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Completion date of the work (must be >= StartDate)
    /// </summary>
    public DateOnly? CompletionDate { get; set; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public List<TagInput>? Tags { get; set; }
}
