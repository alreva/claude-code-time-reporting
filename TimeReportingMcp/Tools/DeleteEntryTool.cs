using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for deleting time entries
/// </summary>
public class DeleteEntryTool : IMcpTool
{
    private readonly ITimeReportingClient _client;

    public DeleteEntryTool(ITimeReportingClient client)
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

            // 2. Execute strongly-typed mutation
            var result = await _client.DeleteTimeEntry.ExecuteAsync(id);

            // 3. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 4. Return success
            var deleted = result.Data!.DeleteTimeEntry;
            if (deleted)
            {
                return new ToolResult
                {
                    Content = new List<ContentItem>
                    {
                        ContentItem.CreateText($"✅ Time entry {id} deleted successfully!")
                    }
                };
            }
            else
            {
                return CreateErrorResult(null);
            }
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
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
        var errorMessage = "❌ Failed to delete time entry:\n\n";
        if (errors != null)
        {
            errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));
        }
        else
        {
            errorMessage += "- Delete operation returned false";
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
