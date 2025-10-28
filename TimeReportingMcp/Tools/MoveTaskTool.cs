using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool to move a time entry to a different project and task.
/// Useful when an entry was logged to the wrong project.
/// </summary>
public class MoveTaskTool
{
    private readonly GraphQLClientWrapper _client;

    public MoveTaskTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse arguments
            var entryId = arguments.GetProperty("entryId").GetString()
                ?? throw new ArgumentException("entryId is required");
            var newProjectCode = arguments.GetProperty("newProjectCode").GetString()
                ?? throw new ArgumentException("newProjectCode is required");
            var newTask = arguments.GetProperty("newTask").GetString()
                ?? throw new ArgumentException("newTask is required");

            // 2. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation MoveTaskToProject($entryId: UUID!, $newProjectCode: String!, $newTask: String!) {
                        moveTaskToProject(entryId: $entryId, newProjectCode: $newProjectCode, newTask: $newTask) {
                            id
                            projectCode
                            task
                            standardHours
                            overtimeHours
                            startDate
                            completionDate
                            status
                            updatedAt
                        }
                    }",
                Variables = new
                {
                    entryId,
                    newProjectCode,
                    newTask
                }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<MoveTaskResponse>(mutation);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Format success response
            var entry = response.Data.MoveTaskToProject;
            return CreateSuccessResult(entry);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(TimeEntryData entry)
    {
        var message = $"✅ Time entry moved successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"New Project: {entry.ProjectCode} - {entry.Task}\n" +
                      $"Hours: {entry.StandardHours} standard, {entry.OvertimeHours} overtime\n" +
                      $"Period: {entry.StartDate} to {entry.CompletionDate}\n" +
                      $"Status: {entry.Status}";

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
        var errorMessage = "❌ Failed to move time entry:\n\n";
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

/// <summary>
/// Response wrapper for moveTaskToProject mutation
/// </summary>
public class MoveTaskResponse
{
    public TimeEntryData MoveTaskToProject { get; set; } = null!;
}

/// <summary>
/// Time entry data returned by the mutation
/// </summary>
public class TimeEntryData
{
    public string Id { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public decimal StandardHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string CompletionDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
