using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for moving time entries between projects
/// </summary>
public class MoveTaskTool : IMcpTool
{
    private readonly ITimeReportingClient _client;

    public MoveTaskTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse required arguments
            if (!arguments.TryGetProperty("entryId", out var entryIdElement))
            {
                return CreateValidationError("Entry ID is required");
            }

            if (!arguments.TryGetProperty("newProjectCode", out var projElement))
            {
                return CreateValidationError("New project code is required");
            }

            if (!arguments.TryGetProperty("newTask", out var taskElement))
            {
                return CreateValidationError("New task is required");
            }

            var entryId = Guid.Parse(entryIdElement.GetString()!);
            var newProjectCode = projElement.GetString()!;
            var newTask = taskElement.GetString()!;

            // 2. Execute strongly-typed mutation
            var result = await _client.MoveTaskToProject.ExecuteAsync(entryId, newProjectCode, newTask);

            // 3. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 4. Return success
            return CreateSuccessResult(result.Data!.MoveTaskToProject);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(IMoveTaskToProject_MoveTaskToProject entry)
    {
        var message = $"✅ Time entry moved successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"New Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                      $"New Task: {entry.ProjectTask.TaskName}\n" +
                      $"Hours: {entry.StandardHours} standard";

        if (entry.OvertimeHours > 0)
        {
            message += $", {entry.OvertimeHours} overtime";
        }

        message += $"\nPeriod: {entry.StartDate} to {entry.CompletionDate}\n" +
                   $"Status: {entry.Status}";

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message)
            }
        };
    }

    private ToolResult CreateValidationError(string message)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"❌ Validation Error: {message}")
            },
            IsError = true
        };
    }

    private ToolResult CreateErrorResult(global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.IClientError>? errors)
    {
        var errorMessage = "❌ Failed to move time entry:\n\n";
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
