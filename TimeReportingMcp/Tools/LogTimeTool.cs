using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for creating new time entries
/// </summary>
public class LogTimeTool
{
    private readonly ITimeReportingClient _client;

    public LogTimeTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse arguments into strongly-typed input
            var input = ParseArguments(arguments);

            // 2. Execute strongly-typed mutation
            var result = await _client.LogTime.ExecuteAsync(input);

            // 3. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 4. Return success result
            return CreateSuccessResult(result.Data!.LogTime);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private LogTimeInput ParseArguments(JsonElement arguments)
    {
        // Extract required fields
        var projectCode = arguments.GetProperty("projectCode").GetString()!;
        var task = arguments.GetProperty("task").GetString()!;
        var standardHours = (decimal)arguments.GetProperty("standardHours").GetDouble();
        var startDate = DateOnly.Parse(arguments.GetProperty("startDate").GetString()!);
        var completionDate = DateOnly.Parse(arguments.GetProperty("completionDate").GetString()!);

        // Extract optional fields
        decimal? overtimeHours = arguments.TryGetProperty("overtimeHours", out var ot)
            ? (decimal)ot.GetDouble()
            : null;
        var description = arguments.TryGetProperty("description", out var desc) ? desc.GetString() : null;
        var issueId = arguments.TryGetProperty("issueId", out var issue) ? issue.GetString() : null;

        // Parse tags if provided
        List<TagInput>? tags = null;
        if (arguments.TryGetProperty("tags", out var tagsElement))
        {
            tags = JsonSerializer.Deserialize<List<TagInput>>(tagsElement.GetRawText());
        }

        return new LogTimeInput
        {
            ProjectCode = projectCode,
            Task = task,
            StandardHours = standardHours,
            OvertimeHours = overtimeHours,
            StartDate = startDate,
            CompletionDate = completionDate,
            Description = description,
            IssueId = issueId,
            Tags = tags
        };
    }

    private ToolResult CreateSuccessResult(ILogTime_LogTime entry)
    {
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

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message)
            }
        };
    }

    private ToolResult CreateErrorResult(global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.IClientError>? errors)
    {
        var errorMessage = "❌ Failed to create time entry:\n\n";
        if (errors != null)
        {
            errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(errorMessage)
            },
            IsError = true
        };
    }

    private ToolResult CreateExceptionResult(Exception ex)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"❌ Error: {ex.Message}")
            },
            IsError = true
        };
    }
}
