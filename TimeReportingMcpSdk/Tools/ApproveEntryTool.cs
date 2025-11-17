using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class ApproveEntryTool
{
    private readonly ITimeReportingClient _client;

    public ApproveEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = false,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("Approve a submitted time entry")]
    public async Task<string> ApproveTimeEntry(
        [Description("Entry ID to approve")] [Required] string id)
    {
        try
        {
            var entryId = Guid.Parse(id);
            var result = await _client.ApproveTimeEntry.ExecuteAsync(entryId);

            if (result.Errors is { Count: > 0 })
            {
                var errors = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"""
                         ❌ Failed to approve time entry:

                         {errors}
                         """;
            }

            var entry = result.Data!.ApproveTimeEntry;
            return TimeEntryFormatter.FormatAsJson(entry);
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
