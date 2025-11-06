using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for submitting time entries for approval
/// </summary>
public class SubmitEntryTool : IMcpTool
{
    private readonly ITimeReportingClient _client;

    public SubmitEntryTool(ITimeReportingClient client)
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
            var result = await _client.SubmitTimeEntry.ExecuteAsync(id);

            // 3. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 4. Return success
            return CreateSuccessResult(result.Data!.SubmitTimeEntry);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(ISubmitTimeEntry_SubmitTimeEntry entry)
    {
        var message = $"✅ Time entry submitted successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"New Status: {entry.Status}\n" +
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
        var errorMessage = "❌ Failed to submit time entry:\n\n";
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
