using System.Text;
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool to get list of available projects with their tasks and tag configurations
/// </summary>
public class GetProjectsTool
{
    private readonly GraphQLClientWrapper _client;

    public GetProjectsTool(GraphQLClientWrapper client)
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

            // 2. Build GraphQL query
            var query = new GraphQLRequest
            {
                Query = @"
                    query GetProjects($activeOnly: Boolean!) {
                        projects(where: { isActive: { eq: $activeOnly } }) {
                            code
                            name
                            isActive
                            availableTasks {
                                taskName
                                isActive
                            }
                            tags {
                                tagName
                                isActive
                                allowedValues {
                                    value
                                }
                            }
                        }
                    }",
                Variables = new { activeOnly }
            };

            // 3. Execute query
            var response = await _client.SendQueryAsync<GetProjectsResponse>(query);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Format and return response
            var output = FormatProjects(response.Data.Projects);
            return CreateSuccessResult(output);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private string FormatProjects(List<ProjectData> projects)
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
            var activeTasks = project.Tasks.Where(t => t.IsActive).ToList();
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

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to get projects:\n\n";
        errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));

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

/// <summary>
/// Response wrapper for projects query
/// </summary>
public class GetProjectsResponse
{
    public List<ProjectData> Projects { get; set; } = new();
}

/// <summary>
/// Project data with tasks and tags
/// </summary>
public class ProjectData
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<TaskData> Tasks { get; set; } = new();
    public List<TagData> Tags { get; set; } = new();
}

/// <summary>
/// Task data within a project
/// </summary>
public class TaskData
{
    public string TaskName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Tag configuration data
/// </summary>
public class TagData
{
    public string TagName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<TagValueData> AllowedValues { get; set; } = new();
}

/// <summary>
/// Allowed tag value
/// </summary>
public class TagValueData
{
    public string Value { get; set; } = string.Empty;
}
