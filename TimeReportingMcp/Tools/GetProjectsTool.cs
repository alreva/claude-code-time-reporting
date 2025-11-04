using System.Text;
using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool to get list of available projects with their tasks and tag configurations
/// </summary>
public class GetProjectsTool
{
    private readonly ITimeReportingClient _client;

    public GetProjectsTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse arguments
            var activeOnly = true; // default
            if (arguments.TryGetProperty("activeOnly", out var activeOnlyElement))
            {
                activeOnly = activeOnlyElement.GetBoolean();
            }

            // 2. Execute strongly-typed query (choose based on activeOnly)
            List<IGetAvailableProjects_Projects> projects;

            if (activeOnly)
            {
                var result = await _client.GetActiveProjects.ExecuteAsync();

                if (result.Errors is { Count: > 0 })
                {
                    return CreateErrorResult(result.Errors);
                }

                projects = result.Data!.Projects.Cast<IGetAvailableProjects_Projects>().ToList();
            }
            else
            {
                var result = await _client.GetAvailableProjects.ExecuteAsync();

                if (result.Errors is { Count: > 0 })
                {
                    return CreateErrorResult(result.Errors);
                }

                projects = result.Data!.Projects.ToList();
            }

            // 3. Format and return response
            var output = FormatProjects(projects);
            return CreateSuccessResult(output);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
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

    private ToolResult CreateSuccessResult(string output)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(output)
            }
        };
    }

    private ToolResult CreateErrorResult(global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.IClientError>? errors)
    {
        var errorMessage = "❌ Failed to get projects:\n\n";
        if (errors != null)
        {
            errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(errorMessage)
            },
            IsError = true
        };
    }

    private ToolResult CreateExceptionResult(Exception ex)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"❌ Error: {ex.Message}")
            },
            IsError = true
        };
    }
}
