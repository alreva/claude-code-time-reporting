using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool to submit a time entry for approval (changes status to SUBMITTED)
/// </summary>
public class SubmitEntryTool
{
    private readonly GraphQLClientWrapper _client;

    public SubmitEntryTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse arguments
            var id = arguments.GetProperty("id").GetString()
                ?? throw new ArgumentException("id is required");

            // 2. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation SubmitTimeEntry($id: UUID!) {
                        submitTimeEntry(id: $id) {
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
                Variables = new { id }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<SubmitEntryResponse>(mutation);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Format success response
            var entry = response.Data.SubmitTimeEntry;
            return CreateSuccessResult(entry);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(SubmittedTimeEntry entry)
    {
        var message = $"✅ Time entry submitted for approval!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"Project: {entry.ProjectCode} - {entry.Task}\n" +
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
        var errorMessage = "❌ Failed to submit time entry:\n\n";
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
/// Response wrapper for submitTimeEntry mutation
/// </summary>
public class SubmitEntryResponse
{
    public SubmittedTimeEntry SubmitTimeEntry { get; set; } = null!;
}

/// <summary>
/// Submitted time entry data
/// </summary>
public class SubmittedTimeEntry
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
