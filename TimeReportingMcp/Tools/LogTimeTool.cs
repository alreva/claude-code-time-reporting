using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for creating new time entries
/// </summary>
public class LogTimeTool
{
    private readonly GraphQLClientWrapper _client;

    public LogTimeTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse and validate arguments
            var input = ParseArguments(arguments);

            // 2. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation LogTime($input: LogTimeInput!) {
                        logTime(input: $input) {
                            id
                            project {
                                code
                                name
                            }
                            projectTask {
                                taskName
                            }
                            issueId
                            standardHours
                            overtimeHours
                            description
                            startDate
                            completionDate
                            status
                            createdAt
                            updatedAt
                        }
                    }",
                Variables = new { input }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<LogTimeResponse>(mutation);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Return success result
            return CreateSuccessResult(response.Data.LogTime);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private object ParseArguments(JsonElement arguments)
    {
        // Extract required fields
        var projectCode = arguments.GetProperty("projectCode").GetString();
        var task = arguments.GetProperty("task").GetString();
        var standardHours = arguments.GetProperty("standardHours").GetDouble();
        var startDate = arguments.GetProperty("startDate").GetString();
        var completionDate = arguments.GetProperty("completionDate").GetString();

        // Extract optional fields
        var overtimeHours = arguments.TryGetProperty("overtimeHours", out var ot) ? ot.GetDouble() : 0.0;
        var description = arguments.TryGetProperty("description", out var desc) ? desc.GetString() : null;
        var issueId = arguments.TryGetProperty("issueId", out var issue) ? issue.GetString() : null;

        // Parse tags if provided
        List<TagInput>? tags = null;
        if (arguments.TryGetProperty("tags", out var tagsElement))
        {
            tags = JsonSerializer.Deserialize<List<TagInput>>(tagsElement.GetRawText());
        }

        return new
        {
            projectCode,
            task,
            standardHours,
            overtimeHours,
            startDate,
            completionDate,
            description,
            issueId,
            tags
        };
    }

    private ToolResult CreateSuccessResult(TimeEntryData entry)
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

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to create time entry:\n\n";
        errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));

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
public class LogTimeResponse
{
    public TimeEntryData LogTime { get; set; } = null!;
}
