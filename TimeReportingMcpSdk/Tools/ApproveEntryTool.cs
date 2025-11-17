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
            var overtimeInfo = entry.OvertimeHours > 0 ? $" + {entry.OvertimeHours} overtime" : "";
            return $"""
                     ✅ Time entry approved successfully!

                     ID: {entry.Id}
                     Project: {entry.Project.Code} - {entry.Project.Name}
                     Task: {entry.ProjectTask.TaskName}
                     Hours: {entry.StandardHours}{overtimeInfo}
                     New Status: {entry.Status}
                     Updated At: {entry.UpdatedAt}
                     """;
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
