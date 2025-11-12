using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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

    [McpServerTool, Description("Log time spent on a task")]
    public async Task<string> LogTime(
        [Description("Project code (e.g., INTERNAL, CLIENT-A)")] [Required] string projectCode,
        [Description("Task name")] [Required] string task,
        [Description("Standard hours worked")] [Required] decimal standardHours,
        [Description("Start date (YYYY-MM-DD)")] [Required] string startDate,
        [Description("Completion date (YYYY-MM-DD)")] [Required] string completionDate,
        [Description("Overtime hours (optional)")] decimal? overtimeHours = null,
        [Description("Description of work done (optional)")] string? description = null,
        [Description("Issue/ticket ID (optional)")] string? issueId = null,
        [Description("Tags in JSON array format (optional)")] string? tags = null)
    {
        try
        {
            // Parse tags if provided
            List<TagInput>? tagList = null;
            if (!string.IsNullOrEmpty(tags))
            {
                tagList = System.Text.Json.JsonSerializer.Deserialize<List<TagInput>>(tags);
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
}
