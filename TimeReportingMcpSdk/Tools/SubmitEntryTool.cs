using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class SubmitEntryTool
{
    private readonly ITimeReportingClient _client;

    public SubmitEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool, Description("Submit a time entry for approval")]
    public async Task<string> SubmitTimeEntry(
        [Description("Entry ID to submit")] [Required] string id)
    {
        try
        {
            var entryId = Guid.Parse(id);
            var result = await _client.SubmitTimeEntry.ExecuteAsync(entryId);

            if (result.Errors is { Count: > 0 })
            {
                return "❌ Failed to submit time entry:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entry = result.Data!.SubmitTimeEntry;
            return $"✅ Time entry submitted successfully!\n\n" +
                   $"ID: {entry.Id}\n" +
                   $"New Status: {entry.Status}\n" +
                   $"Updated At: {entry.UpdatedAt}";
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
