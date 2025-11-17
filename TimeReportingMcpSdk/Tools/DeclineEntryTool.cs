using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class DeclineEntryTool
{
    private readonly ITimeReportingClient _client;

    public DeclineEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = false,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("Decline a submitted time entry with a comment")]
    public async Task<string> DeclineTimeEntry(
        [Description("Entry ID to decline")] [Required] string id,
        [Description("Reason for declining")] [Required] string comment)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return "❌ Decline comment cannot be empty";
            }

            var entryId = Guid.Parse(id);
            var result = await _client.DeclineTimeEntry.ExecuteAsync(entryId, comment);

            if (result.Errors is { Count: > 0 })
            {
                var errors = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"""
                         ❌ Failed to decline time entry:

                         {errors}
                         """;
            }

            var entry = result.Data!.DeclineTimeEntry;
            return TimeEntryFormatter.FormatAsJson(entry);
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
