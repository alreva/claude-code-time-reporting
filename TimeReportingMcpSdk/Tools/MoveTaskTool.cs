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
                return "❌ Failed to move time entry:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entry = result.Data!.MoveTaskToProject;
            return $"✅ Time entry moved successfully!\n\n" +
                   $"ID: {entry.Id}\n" +
                   $"New Project: {entry.Project.Code} - {entry.Project.Name}\n" +
                   $"New Task: {entry.ProjectTask.TaskName}\n" +
                   $"Hours: {entry.StandardHours} standard" +
                   (entry.OvertimeHours > 0 ? $", {entry.OvertimeHours} overtime" : "") + "\n" +
                   $"Status: {entry.Status}";
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
