namespace TimeReportingApi.GraphQL.Inputs;

/// <summary>
/// Input type for tag metadata (name-value pair).
/// </summary>
public class TagInput
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}
