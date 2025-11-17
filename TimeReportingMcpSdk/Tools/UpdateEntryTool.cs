using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Utils;

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
    [Description("""
                 Update an existing time entry

                 All fields are optional - only provided fields will be updated.
                 Only entries in NOT_REPORTED or DECLINED status can be updated.

                 Tags Format:
                 - Supports two formats (both case-insensitive):
                   1. Dictionary: '{"Type": "Feature", "Environment": "Development"}' (recommended - simpler)
                   2. Array: '[{"name": "Type", "value": "Feature"}]'
                 - Use get_available_projects to see valid tag names and values for the project
                 - Replaces all existing tags (not additive)

                 Returns:
                 - Success: Updated entry details
                 - Error: Validation messages with suggestions
                 """)]
    public async Task<string> UpdateTimeEntry(
        [Description("Entry ID to update")] [Required] string id,
        [Description("New task name (optional)")] string? task = null,
        [Description("New standard hours (optional)")] decimal? standardHours = null,
        [Description("New overtime hours (optional)")] decimal? overtimeHours = null,
        [Description("New start date YYYY-MM-DD (optional)")] string? startDate = null,
        [Description("New completion date YYYY-MM-DD (optional)")] string? completionDate = null,
        [Description("New description (optional)")] string? description = null,
        [Description("New issue ID (optional)")] string? issueId = null,
        [Description("""
                     Tags in JSON format (optional)
                     Dictionary format: '{"Type": "Feature", "Environment": "Development"}'
                     Array format: '[{"name": "Type", "value": "Feature"}]'
                     Property names are case-insensitive
                     """)] string? tags = null)
    {
        try
        {
            var entryId = Guid.Parse(id);

            List<TagInput>? tagList = null;
            if (!string.IsNullOrEmpty(tags))
            {
                tagList = TagHelper.ParseTags(tags);
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
                var errors = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"""
                         ❌ Failed to update time entry:

                         {errors}
                         """;
            }

            var entry = result.Data!.UpdateTimeEntry;
            var overtimeInfo = entry.OvertimeHours > 0 ? $", {entry.OvertimeHours} overtime" : "";
            return $"""
                     ✅ Time entry updated successfully!

                     ID: {entry.Id}
                     Project: {entry.Project.Code} - {entry.Project.Name}
                     Task: {entry.ProjectTask.TaskName}
                     Hours: {entry.StandardHours} standard{overtimeInfo}
                     Period: {entry.StartDate} to {entry.CompletionDate}
                     Status: {entry.Status}
                     """;
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
