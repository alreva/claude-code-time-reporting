using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for declining time entries with a comment
/// </summary>
public class DeclineEntryTool
{
    private readonly ITimeReportingClient _client;

    public DeclineEntryTool(ITimeReportingClient client)
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

            // 2. Parse decline comment (required)
            if (!arguments.TryGetProperty("comment", out var commentElement))
            {
                return CreateValidationError("Decline comment is required");
            }

            var comment = commentElement.GetString();
            if (string.IsNullOrWhiteSpace(comment))
            {
                return CreateValidationError("Decline comment cannot be empty");
            }

            // 3. Execute strongly-typed mutation
            var result = await _client.DeclineTimeEntry.ExecuteAsync(id, comment);

            // 4. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 5. Return success
            return CreateSuccessResult(result.Data!.DeclineTimeEntry);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(IDeclineTimeEntry_DeclineTimeEntry entry)
    {
        var message = $"✅ Time entry declined successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                      $"Task: {entry.ProjectTask.TaskName}\n" +
                      $"Hours: {entry.StandardHours}";

        if (entry.OvertimeHours > 0)
        {
            message += $" + {entry.OvertimeHours} overtime";
        }

        message += $"\nPeriod: {entry.StartDate} to {entry.CompletionDate}\n" +
                   $"New Status: {entry.Status}\n" +
                   $"Decline Reason: {entry.DeclineComment}\n" +
                   $"Updated At: {entry.UpdatedAt}";

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
        var errorMessage = "❌ Failed to decline time entry:\n\n";
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
