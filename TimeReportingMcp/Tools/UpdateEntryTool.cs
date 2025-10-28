using System.Text;
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for updating existing time entries
/// </summary>
public class UpdateEntryTool
{
    private readonly GraphQLClientWrapper _client;

    public UpdateEntryTool(GraphQLClientWrapper client)
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

            var id = idElement.GetString();
            if (string.IsNullOrEmpty(id))
            {
                return CreateValidationError("Entry ID cannot be empty");
            }

            // 2. Parse update fields (all optional)
            var input = ParseUpdateFields(arguments);

            if (input == null || ((IDictionary<string, object>)input).Count == 0)
            {
                return CreateValidationError("At least one field must be provided to update");
            }

            // 3. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation UpdateTimeEntry($id: UUID!, $input: UpdateTimeEntryInput!) {
                        updateTimeEntry(id: $id, input: $input) {
                            id
                            projectCode
                            task
                            issueId
                            standardHours
                            overtimeHours
                            description
                            startDate
                            completionDate
                            status
                            updatedAt
                        }
                    }",
                Variables = new { id, input }
            };

            // 4. Execute mutation
            var response = await _client.SendMutationAsync<UpdateTimeEntryResponse>(mutation);

            // 5. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 6. Return success result with changes highlighted
            return CreateSuccessResult(response.Data.UpdateTimeEntry, input);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private object? ParseUpdateFields(JsonElement arguments)
    {
        var updates = new Dictionary<string, object>();

        if (arguments.TryGetProperty("task", out var task))
        {
            updates["task"] = task.GetString()!;
        }

        if (arguments.TryGetProperty("issueId", out var issueId))
        {
            updates["issueId"] = issueId.GetString()!;
        }

        if (arguments.TryGetProperty("standardHours", out var standardHours))
        {
            updates["standardHours"] = standardHours.GetDouble();
        }

        if (arguments.TryGetProperty("overtimeHours", out var overtimeHours))
        {
            updates["overtimeHours"] = overtimeHours.GetDouble();
        }

        if (arguments.TryGetProperty("description", out var description))
        {
            updates["description"] = description.GetString()!;
        }

        if (arguments.TryGetProperty("startDate", out var startDate))
        {
            updates["startDate"] = startDate.GetString()!;
        }

        if (arguments.TryGetProperty("completionDate", out var completionDate))
        {
            updates["completionDate"] = completionDate.GetString()!;
        }

        if (arguments.TryGetProperty("tags", out var tags))
        {
            updates["tags"] = JsonSerializer.Deserialize<List<TagInput>>(tags.GetRawText())!;
        }

        return updates.Count > 0 ? updates : null;
    }

    private ToolResult CreateSuccessResult(TimeEntry entry, object updates)
    {
        var message = new StringBuilder();
        message.AppendLine("✅ Time entry updated successfully!\n");
        message.AppendLine($"ID: {entry.Id}");
        message.AppendLine($"Project: {entry.ProjectCode} - {entry.Task}");
        message.AppendLine($"Hours: {entry.StandardHours} standard");

        if (entry.OvertimeHours > 0)
        {
            message.Append($", {entry.OvertimeHours} overtime");
        }

        message.AppendLine($"\nPeriod: {entry.StartDate} to {entry.CompletionDate}");
        message.AppendLine($"Status: {entry.Status}");

        if (!string.IsNullOrEmpty(entry.Description))
        {
            message.AppendLine($"Description: {entry.Description}");
        }

        if (!string.IsNullOrEmpty(entry.IssueId))
        {
            message.AppendLine($"Issue: {entry.IssueId}");
        }

        message.AppendLine($"\nUpdated: {entry.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

        // Highlight what changed
        var updateDict = updates as IDictionary<string, object>;
        if (updateDict != null && updateDict.Count > 0)
        {
            message.AppendLine($"\n**Changes applied:**");
            foreach (var kvp in updateDict)
            {
                message.AppendLine($"  • {kvp.Key}: {kvp.Value}");
            }
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message.ToString())
            }
        };
    }

    private ToolResult CreateValidationError(string errorMessage)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"❌ Validation error: {errorMessage}")
            },
            IsError = true
        };
    }

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to update time entry:\n\n";

        foreach (var error in errors)
        {
            errorMessage += $"- {error.Message}\n";

            // Special handling for common errors
            if (error.Message.Contains("Cannot update"))
            {
                errorMessage += "\n💡 Tip: Only entries with status NOT_REPORTED or DECLINED can be updated.\n";
                errorMessage += "   Submitted or approved entries are read-only.\n";
            }
            else if (error.Message.Contains("not found"))
            {
                errorMessage += "\n💡 Tip: Double-check the entry ID. You can use query_time_entries to find entries.\n";
            }
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

// Response type
public class UpdateTimeEntryResponse
{
    public TimeEntry UpdateTimeEntry { get; set; } = null!;
}
