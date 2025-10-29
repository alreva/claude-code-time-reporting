using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool to delete a time entry (only allowed for NOT_REPORTED entries)
/// </summary>
public class DeleteEntryTool
{
    private readonly GraphQLClientWrapper _client;

    public DeleteEntryTool(GraphQLClientWrapper client)
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
                    mutation DeleteTimeEntry($id: UUID!) {
                        deleteTimeEntry(id: $id)
                    }",
                Variables = new { id }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<DeleteTimeEntryResponse>(mutation);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Format success response
            var result = response.Data.DeleteTimeEntry;
            return CreateSuccessResult(result, id);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(bool success, string id)
    {
        var message = success
            ? $"✅ Time entry {id} deleted successfully"
            : $"❌ Failed to delete time entry {id}";

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
        var errorMessage = "❌ Failed to delete time entry:\n\n";
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
/// Response wrapper for deleteTimeEntry mutation
/// </summary>
public class DeleteTimeEntryResponse
{
    public bool DeleteTimeEntry { get; set; }
}
