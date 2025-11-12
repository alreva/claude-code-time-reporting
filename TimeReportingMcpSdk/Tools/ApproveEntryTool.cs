using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class ApproveEntryTool
{
    private readonly ITimeReportingClient _client;

    public ApproveEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool, Description("Approve a submitted time entry")]
    public async Task<string> ApproveTimeEntry(
        [Description("Entry ID to approve")] [Required] string id)
    {
        try
        {
            var entryId = Guid.Parse(id);
            var result = await _client.ApproveTimeEntry.ExecuteAsync(entryId);

            if (result.Errors is { Count: > 0 })
            {
                return "❌ Failed to approve time entry:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entry = result.Data!.ApproveTimeEntry;
            return $"✅ Time entry approved successfully!\n\n" +
                   $"ID: {entry.Id}\n" +
                   $"Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                   $"Task: {entry.ProjectTask.TaskName}\n" +
                   $"Hours: {entry.StandardHours}" +
                   (entry.OvertimeHours > 0 ? $" + {entry.OvertimeHours} overtime" : "") + "\n" +
                   $"New Status: {entry.Status}\n" +
                   $"Updated At: {entry.UpdatedAt}";
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
