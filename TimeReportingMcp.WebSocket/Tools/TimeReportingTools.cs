using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;
using TimeReportingMcp.WebSocket.Generated;

namespace TimeReportingMcp.WebSocket.Tools;

/// <summary>
/// MCP tools for time reporting operations.
/// Uses MCP SDK attributes for tool registration and discovery.
/// </summary>
[McpServerToolType]
public class TimeReportingTools
{
    private readonly ITimeReportingClient _client;
    private readonly ILogger<TimeReportingTools> _logger;

    public TimeReportingTools(
        ITimeReportingClient client,
        ILogger<TimeReportingTools> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Log time spent on a project task.
    /// </summary>
    [McpServerTool]
    [Description("Log time spent on a project task")]
    public async Task<string> LogTime(
        [Description("Project code (e.g., INTERNAL)")] string projectCode,
        [Description("Task name (e.g., Development)")] string task,
        [Description("Regular hours worked")] decimal standardHours,
        [Description("Start date (YYYY-MM-DD)")] string startDate,
        [Description("Completion date (YYYY-MM-DD)")] string completionDate,
        [Description("Overtime hours (optional)")] decimal? overtimeHours = null,
        [Description("Work description (optional)")] string? description = null,
        [Description("Issue/ticket ID (optional)")] string? issueId = null,
        [Description("Tags as JSON array (optional)")] string? tagsJson = null)
    {
        try
        {
            _logger.LogInformation("LogTime: Project={Project}, Task={Task}, Hours={Hours}",
                projectCode, task, standardHours);

            // Parse dates
            var start = DateOnly.Parse(startDate);
            var completion = DateOnly.Parse(completionDate);

            // Parse tags if provided
            List<TagInput>? tags = null;
            if (!string.IsNullOrEmpty(tagsJson))
            {
                tags = JsonSerializer.Deserialize<List<TagInput>>(tagsJson);
            }

            var input = new LogTimeInput
            {
                ProjectCode = projectCode,
                Task = task,
                StandardHours = standardHours,
                OvertimeHours = overtimeHours,
                StartDate = start,
                CompletionDate = completion,
                Description = description,
                IssueId = issueId,
                Tags = tags
            };

            // Execute strongly-typed mutation
            var result = await _client.LogTime.ExecuteAsync(input);

            // Handle errors
            if (result.Errors is { Count: > 0 })
            {
                var errorMessage = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"❌ GraphQL error:\n{errorMessage}";
            }

            // Format success response
            var entry = result.Data!.LogTime;
            var message = $"✅ Time entry logged successfully!\n\n" +
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
            _logger.LogError(ex, "Error logging time");
            return $"❌ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Get list of available projects and their tasks.
    /// </summary>
    [McpServerTool]
    [Description("Get list of available projects and their tasks")]
    public async Task<string> GetAvailableProjects(
        [Description("Filter to active projects only (default: true)")] bool activeOnly = true)
    {
        try
        {
            _logger.LogInformation("GetAvailableProjects: ActiveOnly={ActiveOnly}", activeOnly);

            // Execute strongly-typed query
            var result = await _client.GetAvailableProjects.ExecuteAsync(activeOnly);

            // Handle errors
            if (result.Errors is { Count: > 0 })
            {
                var errorMessage = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"❌ GraphQL error:\n{errorMessage}";
            }

            // Format projects
            var projects = result.Data!.Projects.ToList();
            if (projects.Count == 0)
            {
                return "No projects found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Available Projects ({projects.Count}):\n");

            foreach (var project in projects)
            {
                sb.AppendLine($"📊 {project.Code} - {project.Name}");
                sb.AppendLine($"   Status: {(project.IsActive ? "Active" : "Inactive")}");

                // Tasks
                var activeTasks = project.AvailableTasks.Where(t => t.IsActive).ToList();
                if (activeTasks.Any())
                {
                    sb.AppendLine($"   Tasks: {string.Join(", ", activeTasks.Select(t => t.TaskName))}");
                }
                else
                {
                    sb.AppendLine("   Tasks: None");
                }

                // Tags
                var activeTags = project.Tags.Where(t => t.IsActive).ToList();
                if (activeTags.Any())
                {
                    sb.AppendLine("   Tags:");
                    foreach (var tag in activeTags)
                    {
                        var values = string.Join(", ", tag.AllowedValues.Select(v => v.Value));
                        sb.AppendLine($"     • {tag.TagName}: {values}");
                    }
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available projects");
            return $"❌ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Query time entries with filters.
    /// </summary>
    [McpServerTool]
    [Description("Query time entries with filters")]
    public async Task<string> QueryTimeEntries(
        [Description("Project code filter (optional)")] string? projectCode = null,
        [Description("User email filter (optional)")] string? userEmail = null,
        [Description("Status filter (optional): NOT_REPORTED, SUBMITTED, APPROVED, DECLINED")] string? status = null,
        [Description("Start date filter (YYYY-MM-DD, optional)")] string? fromDate = null,
        [Description("End date filter (YYYY-MM-DD, optional)")] string? toDate = null)
    {
        try
        {
            _logger.LogInformation("QueryTimeEntries: ProjectCode={ProjectCode}, UserEmail={UserEmail}, Status={Status}",
                projectCode, userEmail, status);

            // Build filter using HotChocolate filter structure
            var filter = new TimeEntryFilterInput();

            if (!string.IsNullOrEmpty(projectCode))
            {
                filter.Project = new ProjectFilterInput
                {
                    Code = new StringOperationFilterInput
                    {
                        Eq = projectCode
                    }
                };
            }

            if (!string.IsNullOrEmpty(userEmail))
            {
                filter.UserId = new StringOperationFilterInput
                {
                    Eq = userEmail
                };
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TimeEntryStatus>(status, true, out var statusEnum))
            {
                filter.Status = new TimeEntryStatusOperationFilterInput
                {
                    Eq = statusEnum
                };
            }

            if (!string.IsNullOrEmpty(fromDate))
            {
                var from = DateOnly.Parse(fromDate);
                filter.StartDate = new LocalDateOperationFilterInput
                {
                    Gte = from
                };
            }

            if (!string.IsNullOrEmpty(toDate))
            {
                var to = DateOnly.Parse(toDate);
                if (filter.CompletionDate == null)
                {
                    filter.CompletionDate = new LocalDateOperationFilterInput();
                }
                filter.CompletionDate.Lte = to;
            }

            // Execute strongly-typed query
            var result = await _client.QueryTimeEntries.ExecuteAsync(filter);

            // Handle errors
            if (result.Errors is { Count: > 0 })
            {
                var errorMessage = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"❌ GraphQL error:\n{errorMessage}";
            }

            // Format results
            var entries = result.Data?.TimeEntries?.Nodes?.ToList() ?? new List<IQueryTimeEntries_TimeEntries_Nodes>();

            if (entries.Count == 0)
            {
                return "No time entries found matching the criteria.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Time Entries ({entries.Count}):\n");

            foreach (var entry in entries)
            {
                sb.AppendLine($"📝 ID: {entry.Id}");
                sb.AppendLine($"   Project: {entry.Project.Code} - {entry.Project.Name}");
                sb.AppendLine($"   Task: {entry.ProjectTask.TaskName}");
                sb.AppendLine($"   Hours: {entry.StandardHours} standard");

                if (entry.OvertimeHours > 0)
                {
                    sb.Append($", {entry.OvertimeHours} overtime");
                }

                sb.AppendLine();
                sb.AppendLine($"   Period: {entry.StartDate} to {entry.CompletionDate}");
                sb.AppendLine($"   Status: {entry.Status}");

                if (!string.IsNullOrEmpty(entry.Description))
                {
                    sb.AppendLine($"   Description: {entry.Description}");
                }

                if (!string.IsNullOrEmpty(entry.IssueId))
                {
                    sb.AppendLine($"   Issue: {entry.IssueId}");
                }

                if (entry.Tags.Any())
                {
                    var tags = string.Join(", ", entry.Tags.Select(t => $"{t.TagValue.ProjectTag.TagName}={t.TagValue.Value}"));
                    sb.AppendLine($"   Tags: {tags}");
                }

                sb.AppendLine($"   Created: {entry.CreatedAt:yyyy-MM-dd HH:mm}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying time entries");
            return $"❌ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Update an existing time entry.
    /// </summary>
    [McpServerTool]
    [Description("Update an existing time entry")]
    public Task<string> UpdateTimeEntry(
        [Description("Time entry ID (GUID)")] string entryId,
        [Description("Updated standard hours (optional)")] decimal? standardHours = null,
        [Description("Updated overtime hours (optional)")] decimal? overtimeHours = null,
        [Description("Updated description (optional)")] string? description = null)
    {
        // TODO: Implement when StrawberryShake mutation is added
        return Task.FromResult("❌ update_time_entry tool not yet implemented");
    }

    /// <summary>
    /// Submit a time entry for approval.
    /// </summary>
    [McpServerTool]
    [Description("Submit a time entry for approval")]
    public Task<string> SubmitTimeEntry(
        [Description("Time entry ID (GUID)")] string entryId)
    {
        // TODO: Implement when StrawberryShake mutation is added
        return Task.FromResult("❌ submit_time_entry tool not yet implemented");
    }

    /// <summary>
    /// Move a time entry to a different project and task.
    /// </summary>
    [McpServerTool]
    [Description("Move a time entry to a different project and task")]
    public Task<string> MoveTaskToProject(
        [Description("Time entry ID (GUID)")] string entryId,
        [Description("New project code")] string newProjectCode,
        [Description("New task name")] string newTask)
    {
        // TODO: Implement when StrawberryShake mutation is added
        return Task.FromResult("❌ move_task_to_project tool not yet implemented");
    }

    /// <summary>
    /// Delete a time entry (only if status is NOT_REPORTED).
    /// </summary>
    [McpServerTool]
    [Description("Delete a time entry (only if status is NOT_REPORTED)")]
    public Task<string> DeleteTimeEntry(
        [Description("Time entry ID (GUID)")] string entryId)
    {
        // TODO: Implement when StrawberryShake mutation is added
        return Task.FromResult("❌ delete_time_entry tool not yet implemented");
    }
}
