using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class DeleteEntryTool
{
    private readonly ITimeReportingClient _client;

    public DeleteEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool, Description("Delete a time entry")]
    public async Task<string> DeleteTimeEntry(
        [Description("Entry ID to delete")] [Required] string id)
    {
        try
        {
            var entryId = Guid.Parse(id);
            var result = await _client.DeleteTimeEntry.ExecuteAsync(entryId);

            if (result.Errors is { Count: > 0 })
            {
                return "❌ Failed to delete time entry:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var deleted = result.Data!.DeleteTimeEntry;
            if (deleted)
            {
                return $"✅ Time entry {id} deleted successfully!";
            }
            else
            {
                return $"❌ Failed to delete time entry {id}";
            }
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
