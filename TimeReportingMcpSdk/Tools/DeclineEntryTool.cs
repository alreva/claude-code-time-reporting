using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class DeclineEntryTool
{
    private readonly ITimeReportingClient _client;

    public DeclineEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool, Description("Decline a submitted time entry with a comment")]
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
                return "❌ Failed to decline time entry:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entry = result.Data!.DeclineTimeEntry;
            return $"✅ Time entry declined successfully!\n\n" +
                   $"ID: {entry.Id}\n" +
                   $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                   $"Task: {entry.ProjectTask.TaskName}\n" +
                   $"Hours: {entry.StandardHours}" +
                   (entry.OvertimeHours > 0 ? $" + {entry.OvertimeHours} overtime" : "") + "\n" +
                   $"New Status: {entry.Status}\n" +
                   $"Decline Reason: {entry.DeclineComment}\n" +
                   $"Updated At: {entry.UpdatedAt}";
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
