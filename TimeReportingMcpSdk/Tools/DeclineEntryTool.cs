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
            var overtimeInfo = entry.OvertimeHours > 0 ? $" + {entry.OvertimeHours} overtime" : "";
            return $"""
                     ✅ Time entry declined successfully!

                     ID: {entry.Id}
                     Project: {entry.Project.Code} - {entry.Project.Name}
                     Task: {entry.ProjectTask.TaskName}
                     Hours: {entry.StandardHours}{overtimeInfo}
                     New Status: {entry.Status}
                     Decline Reason: {entry.DeclineComment}
                     Updated At: {entry.UpdatedAt}
                     """;
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
