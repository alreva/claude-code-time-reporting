using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class MoveTaskTool
{
    private readonly ITimeReportingClient _client;

    public MoveTaskTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = false,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("Move a time entry to a different project and task")]
    public async Task<string> MoveTaskToProject(
        [Description("Entry ID to move")] [Required] string entryId,
        [Description("New project code")] [Required] string newProjectCode,
        [Description("New task name")] [Required] string newTask)
    {
        try
        {
            var id = Guid.Parse(entryId);
            var result = await _client.MoveTaskToProject.ExecuteAsync(id, newProjectCode, newTask);

            if (result.Errors is { Count: > 0 })
            {
                var errors = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"""
                         ❌ Failed to move time entry:

                         {errors}
                         """;
            }

            var entry = result.Data!.MoveTaskToProject;
            var overtimeInfo = entry.OvertimeHours > 0 ? $", {entry.OvertimeHours} overtime" : "";
            return $"""
                     ✅ Time entry moved successfully!

                     ID: {entry.Id}
                     New Project: {entry.Project.Code} - {entry.Project.Name}
                     New Task: {entry.ProjectTask.TaskName}
                     Hours: {entry.StandardHours} standard{overtimeInfo}
                     Status: {entry.Status}
                     """;
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
