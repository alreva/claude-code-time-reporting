namespace TimeReportingApi.GraphQL.Inputs;

/// <summary>
/// Input type for the logTime mutation.
/// All required fields must be provided; optional fields have default values.
/// </summary>
public class LogTimeInput
{
    public required string ProjectCode { get; set; }
    public required string Task { get; set; }
    public string? IssueId { get; set; }
    public required decimal StandardHours { get; set; }

    [GraphQLType(typeof(DecimalType))]
    [DefaultValue(0.0)]
    public decimal? OvertimeHours { get; set; }

    public string? Description { get; set; }
    public required DateOnly StartDate { get; set; }
    public required DateOnly CompletionDate { get; set; }
    public List<TagInput>? Tags { get; set; }
}
