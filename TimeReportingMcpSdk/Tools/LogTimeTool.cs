using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

/// <summary>
/// Tool for creating new time entries
/// </summary>
[McpServerToolType]
public class LogTimeTool
{
    private readonly ITimeReportingClient _client;

    public LogTimeTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = false,
        Idempotent = false,
        Destructive = false,
        OpenWorld = true
    )]
    [Description(@"Log time spent on a task

Creates a new time entry with NOT_REPORTED status that can be edited until submitted.

Prerequisites:
- Use get_available_projects first to see valid project codes, tasks, and tags
- Ensure you have authenticated with Azure CLI (az login)

Input Requirements:
- projectCode: Valid project code (e.g., 'INTERNAL', 'CLIENT-A')
- task: Valid task name for the project (case-sensitive)
- standardHours: Hours worked (must be >= 0, decimal allowed)
- startDate/completionDate: YYYY-MM-DD format, startDate <= completionDate
- overtimeHours: Optional overtime hours (must be >= 0)
- description: Optional work description
- issueId: Optional ticket/issue ID
- tags: Optional JSON array with name/value properties (case-insensitive)

Tags Format:
- Supports two formats (both case-insensitive):
  1. Dictionary: '{""Type"": ""Feature"", ""Billable"": ""Yes""}' (recommended - simpler)
  2. Array: '[{""name"": ""Type"", ""value"": ""Feature""}]'
- Use get_available_projects to see valid tag names and values for the project

Example Usage:
  projectCode: 'INTERNAL'
  task: 'Development'
  standardHours: 8.5
  startDate: '2025-01-13'
  completionDate: '2025-01-13'
  description: 'Implemented user authentication'
  issueId: 'JIRA-123'
  tags: '{""Type"": ""Feature""}'

Returns:
- Success: Entry ID, project details, hours, status
- Error: Validation messages with suggestions for correction")]
    public async Task<string> LogTime(
        [Description("Project code (e.g., INTERNAL, CLIENT-A)")] [Required] string projectCode,
        [Description("Task name")] [Required] string task,
        [Description("Standard hours worked")] [Required] decimal standardHours,
        [Description("Start date (YYYY-MM-DD)")] [Required] string startDate,
        [Description("Completion date (YYYY-MM-DD)")] [Required] string completionDate,
        [Description("Overtime hours (optional)")] decimal? overtimeHours = null,
        [Description("Description of work done (optional)")] string? description = null,
        [Description("Issue/ticket ID (optional)")] string? issueId = null,
        [Description(@"Tags in JSON format (optional)
Dictionary format: '{""Type"": ""Feature"", ""Billable"": ""Yes""}'
Array format: '[{""name"": ""Type"", ""value"": ""Feature""}]'
Property names are case-insensitive")] string? tags = null)
    {
        try
        {
            // Parse tags if provided
            List<TagInput>? tagList = null;
            if (!string.IsNullOrEmpty(tags))
            {
                tagList = ParseTags(tags);
            }

            var input = new LogTimeInput
            {
                ProjectCode = projectCode,
                Task = task,
                StandardHours = standardHours,
                OvertimeHours = overtimeHours,
                StartDate = DateOnly.Parse(startDate),
                CompletionDate = DateOnly.Parse(completionDate),
                Description = description,
                IssueId = issueId,
                Tags = tagList
            };

            var result = await _client.LogTime.ExecuteAsync(input);

            if (result.Errors is { Count: > 0 })
            {
                var errorMessage = "❌ Failed to create time entry:\n\n";
                errorMessage += string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return errorMessage;
            }

            var entry = result.Data!.LogTime;
            var message = $"✅ Time entry created successfully!\n\n" +
                          $"ID: {entry.Id}\n" +
                          $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                          $"Task: {entry.ProjectTask.TaskName}\n" +
                          $"Hours: {entry.StandardHours} standard";

            if (entry.OvertimeHours > 0)
            {
                message += $", {entry.OvertimeHours} overtime";
            }

            message += $"\nPeriod: {entry.StartDate} to {entry.CompletionDate}\n" +
                       $"Status: {entry.Status}";

            if (!string.IsNullOrEmpty(entry.Description))
            {
                message += $"\nDescription: {entry.Description}";
            }

            if (!string.IsNullOrEmpty(entry.IssueId))
            {
                message += $"\nIssue: {entry.IssueId}";
            }

            return message;
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }

    private static List<TagInput> ParseTags(string tagsJson)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Try parsing as array format first: [{"name": "Type", "value": "Feature"}]
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<TagInput>>(tagsJson, options)
                   ?? new List<TagInput>();
        }
        catch
        {
            // If that fails, try parsing as dictionary format: {"Type": "Feature", "Environment": "Production"}
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, options);
            if (dict == null) return new List<TagInput>();

            return dict.Select(kvp => new TagInput
            {
                Name = kvp.Key,
                Value = kvp.Value
            }).ToList();
        }
    }
}
