using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for updating existing time entries
/// </summary>
public class UpdateEntryTool
{
    private readonly ITimeReportingClient _client;

    public UpdateEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse entry ID (required)
            if (!arguments.TryGetProperty("id", out var idElement))
            {
                return CreateValidationError("Entry ID is required");
            }

            var id = Guid.Parse(idElement.GetString()!);

            // 2. Parse update fields (all optional)
            var input = ParseUpdateFields(arguments);

            // 3. Execute strongly-typed mutation
            var result = await _client.UpdateTimeEntry.ExecuteAsync(id, input);

            // 4. Handle errors
            if (result.IsErrorResult())
            {
                return CreateErrorResult(result.Errors);
            }

            // 5. Return success
            return CreateSuccessResult(result.Data!.UpdateTimeEntry);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private UpdateTimeEntryInput ParseUpdateFields(JsonElement arguments)
    {
        var input = new UpdateTimeEntryInput();

        if (arguments.TryGetProperty("task", out var task))
        {
            input.Task = task.GetString();
        }

        if (arguments.TryGetProperty("standardHours", out var sh))
        {
            input.StandardHours = (decimal)sh.GetDouble();
        }

        if (arguments.TryGetProperty("overtimeHours", out var oh))
        {
            input.OvertimeHours = (decimal)oh.GetDouble();
        }

        if (arguments.TryGetProperty("startDate", out var sd))
        {
            input.StartDate = DateOnly.Parse(sd.GetString()!);
        }

        if (arguments.TryGetProperty("completionDate", out var cd))
        {
            input.CompletionDate = DateOnly.Parse(cd.GetString()!);
        }

        if (arguments.TryGetProperty("description", out var desc))
        {
            input.Description = desc.GetString();
        }

        if (arguments.TryGetProperty("issueId", out var issue))
        {
            input.IssueId = issue.GetString();
        }

        if (arguments.TryGetProperty("tags", out var tags))
        {
            input.Tags = JsonSerializer.Deserialize<List<TagInput>>(tags.GetRawText());
        }

        return input;
    }

    private ToolResult CreateSuccessResult(IUpdateTimeEntry_UpdateTimeEntry entry)
    {
        var message = $"✅ Time entry updated successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                      $"Task: {entry.ProjectTask.TaskName}\n" +
                      $"Hours: {entry.StandardHours} standard";

        if (entry.OvertimeHours > 0)
        {
            message += $", {entry.OvertimeHours} overtime";
        }

        message += $"\nPeriod: {entry.StartDate} to {entry.CompletionDate}\n" +
                   $"Status: {entry.Status}\n" +
                   $"Last Updated: {entry.UpdatedAt}";

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

    private ToolResult CreateErrorResult(global::StrawberryShake.IClientError[]? errors)
    {
        var errorMessage = "❌ Failed to update time entry:\n\n";
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
