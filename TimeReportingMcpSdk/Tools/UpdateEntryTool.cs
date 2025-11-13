using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class UpdateEntryTool
{
    private readonly ITimeReportingClient _client;

    public UpdateEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = false,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description(@"Update an existing time entry

All fields are optional - only provided fields will be updated.
Only entries in NOT_REPORTED or DECLINED status can be updated.

Tags Format:
- JSON array of objects with name/value properties (case-insensitive)
- Example: '[{""name"": ""Type"", ""value"": ""Feature""}, {""name"": ""Environment"", ""value"": ""Development""}]'
- Use get_available_projects to see valid tag names and values for the project
- Replaces all existing tags (not additive)

Returns:
- Success: Updated entry details
- Error: Validation messages with suggestions")]
    public async Task<string> UpdateTimeEntry(
        [Description("Entry ID to update")] [Required] string id,
        [Description("New task name (optional)")] string? task = null,
        [Description("New standard hours (optional)")] decimal? standardHours = null,
        [Description("New overtime hours (optional)")] decimal? overtimeHours = null,
        [Description("New start date YYYY-MM-DD (optional)")] string? startDate = null,
        [Description("New completion date YYYY-MM-DD (optional)")] string? completionDate = null,
        [Description("New description (optional)")] string? description = null,
        [Description("New issue ID (optional)")] string? issueId = null,
        [Description(@"Tags in JSON array format (optional)
Example: '[{""name"": ""Type"", ""value"": ""Feature""}]'
Property names are case-insensitive")] string? tags = null)
    {
        try
        {
            var entryId = Guid.Parse(id);

            List<TagInput>? tagList = null;
            if (!string.IsNullOrEmpty(tags))
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                tagList = System.Text.Json.JsonSerializer.Deserialize<List<TagInput>>(tags, options);
            }

            var input = new UpdateTimeEntryInput
            {
                Task = task,
                StandardHours = standardHours,
                OvertimeHours = overtimeHours,
                StartDate = !string.IsNullOrEmpty(startDate) ? DateOnly.Parse(startDate) : null,
                CompletionDate = !string.IsNullOrEmpty(completionDate) ? DateOnly.Parse(completionDate) : null,
                Description = description,
                IssueId = issueId,
                Tags = tagList
            };

            var result = await _client.UpdateTimeEntry.ExecuteAsync(entryId, input);

            if (result.Errors is { Count: > 0 })
            {
                return "❌ Failed to update time entry:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entry = result.Data!.UpdateTimeEntry;
            return $"✅ Time entry updated successfully!\n\n" +
                   $"ID: {entry.Id}\n" +
                   $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                   $"Task: {entry.ProjectTask.TaskName}\n" +
                   $"Hours: {entry.StandardHours} standard" +
                   (entry.OvertimeHours > 0 ? $", {entry.OvertimeHours} overtime" : "") + "\n" +
                   $"Period: {entry.StartDate} to {entry.CompletionDate}\n" +
                   $"Status: {entry.Status}";
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
