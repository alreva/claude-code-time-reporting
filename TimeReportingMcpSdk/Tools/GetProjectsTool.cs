using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

/// <summary>
/// Tool to get list of available projects with their tasks and tag configurations
/// </summary>
[McpServerToolType]
public class GetProjectsTool
{
    private readonly ITimeReportingClient _client;

    public GetProjectsTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("""
                 Get list of available projects with their tasks and tag configurations

                 CRITICAL: Always call this tool FIRST before creating or updating time entries. It provides the authoritative list of valid values.

                 Returns complete project catalog including:
                 - Project codes and names (for log_time projectCode parameter)
                 - Available tasks per project (for log_time task parameter)
                 - Tag configurations per project (for log_time tags parameter)
                 - Tag allowed values (dropdown/select tags only)

                 Use Cases:
                 1. Before logging time: Get valid project codes and task names
                 2. Before adding tags: See which tags are available for a project
                 3. Exploring available work: See all projects you can log time to
                 4. Validation: Verify a project/task combination exists

                 Example Workflow:
                 1. Call get_available_projects
                 2. Find project (e.g., 'INTERNAL - Internal Projects')
                 3. Choose task from project's task list (e.g., 'Development')
                 4. Optional: Add tags from project's tag configuration
                 5. Call log_time with selected values

                 Returns:
                 - Project list with:
                   - Code and name
                   - Available tasks
                   - Tag configurations (name, type, required, allowed values)
                 - Active projects only (archived projects excluded)

                 Note: Project/task names are case-sensitive. Use exact values from this response.
                 """)]
    public async Task<string> GetAvailableProjects()
    {
        try
        {
            // 1. Execute query to get all projects
            var result = await _client.GetAvailableProjects.ExecuteAsync();

            // 2. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                var errorMessage = "❌ Failed to get projects:\n\n";
                errorMessage += string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return errorMessage;
            }

            // 3. Format and return response
            var projects = result.Data!.Projects.ToList();
            return FormatProjects(projects);
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }

    private string FormatProjects(List<IGetAvailableProjects_Projects> projects)
    {
        if (projects.Count == 0)
        {
            return "No projects found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Available Projects ({projects.Count}):\n");

        foreach (var project in projects)
        {
            sb.AppendLine($"📊 {project.Code} - {project.Name}");
            sb.AppendLine($"   Status: {(project.IsActive ? "Active" : "Inactive")}");

            // Tasks
            var activeTasks = project.AvailableTasks.Where(t => t.IsActive).ToList();
            if (activeTasks.Any())
            {
                sb.AppendLine($"   Tasks: {string.Join(", ", activeTasks.Select(t => t.TaskName))}");
            }
            else
            {
                sb.AppendLine("   Tasks: None");
            }

            // Tags
            var activeTags = project.Tags.Where(t => t.IsActive).ToList();
            if (activeTags.Any())
            {
                sb.AppendLine("   Tags:");
                foreach (var tag in activeTags)
                {
                    var values = string.Join(", ", tag.AllowedValues.Select(v => v.Value));
                    sb.AppendLine($"     • {tag.TagName}: {values}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
