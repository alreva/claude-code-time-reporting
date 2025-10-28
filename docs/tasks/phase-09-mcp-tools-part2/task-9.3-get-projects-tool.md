# Task 9.3: Implement get_available_projects Tool

**Phase:** 9 - MCP Server Tools Part 2
**Estimated Time:** 1 hour
**Prerequisites:** Task 9.2 complete (delete_time_entry tool)
**Status:** Pending

## Objective

Implement the `get_available_projects` MCP tool that retrieves the list of available projects with their tasks and tag configurations. This tool helps users discover what projects they can log time to and what metadata is required.

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/GetProjectsTool.cs` with tool handler
- [ ] Parse tool arguments (activeOnly)
- [ ] Call GraphQL `projects` query
- [ ] Fetch project details including tasks and tag configurations
- [ ] Format output in user-friendly structured format
- [ ] Handle errors gracefully
- [ ] Return structured MCP tool result
- [ ] Write unit tests for the tool handler
- [ ] All tests pass

## GraphQL Query

The tool should execute this query:

```graphql
query GetProjects($activeOnly: Boolean) {
  projects(activeOnly: $activeOnly) {
    code
    name
    isActive
    tasks {
      name
      isActive
    }
    tags {
      name
      isActive
      allowedValues {
        value
      }
    }
  }
}
```

## Input Schema

```json
{
  "activeOnly": "boolean (optional, default: true)"
}
```

## Implementation Steps

### 1. Create Tool Handler Class

Create `TimeReportingMcp/Tools/GetProjectsTool.cs`:

```csharp
using System.Text.Json;
using System.Text;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

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
                    query GetProjects($activeOnly: Boolean) {
                        projects(activeOnly: $activeOnly) {
                            code
                            name
                            isActive
                            tasks {
                                name
                                isActive
                            }
                            tags {
                                name
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

            // 4. Format response
            var output = FormatProjects(response.Projects);
            return ToolResult.Success(output);
        }
        catch (Exception ex)
        {
            return ErrorHandler.HandleToolError(ex, "get_available_projects");
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
                sb.AppendLine($"   Tasks: {string.Join(", ", activeTasks.Select(t => t.Name))}");
            }

            // Tags
            var activeTags = project.Tags.Where(t => t.IsActive).ToList();
            if (activeTags.Any())
            {
                sb.AppendLine("   Tags:");
                foreach (var tag in activeTags)
                {
                    var values = string.Join(", ", tag.AllowedValues.Select(v => v.Value));
                    sb.AppendLine($"     • {tag.Name}: {values}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

public class GetProjectsResponse
{
    public List<ProjectData> Projects { get; set; } = new();
}

public class ProjectData
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<TaskData> Tasks { get; set; } = new();
    public List<TagData> Tags { get; set; } = new();
}

public class TaskData
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class TagData
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<TagValueData> AllowedValues { get; set; } = new();
}

public class TagValueData
{
    public string Value { get; set; } = string.Empty;
}
```

### 2. Register Tool in McpServer

Update `TimeReportingMcp/McpServer.cs` to include the new tool:

```csharp
// In tools/list handler, add:
new ToolDefinition
{
    Name = "get_available_projects",
    Description = "Get list of available projects with their tasks and tag configurations",
    InputSchema = new
    {
        type = "object",
        properties = new
        {
            activeOnly = new
            {
                type = "boolean",
                description = "Only return active projects (default: true)",
                @default = true
            }
        }
    }
},

// In tools/call handler, add case:
case "get_available_projects":
    var getProjectsTool = new GetProjectsTool(_graphqlClient);
    return await getProjectsTool.ExecuteAsync(args);
```

### 3. Write Unit Tests

Create `TimeReportingMcp.Tests/Tools/GetProjectsToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class GetProjectsToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithActiveOnly_ReturnsActiveProjects()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithAllProjects_ReturnsAllProjects()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_FormatsProjectsCorrectly()
    {
        // Test implementation - verify output format
    }

    [Fact]
    public async Task ExecuteAsync_WithNoProjects_ReturnsEmptyMessage()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_IncludesTasksAndTags()
    {
        // Test implementation - verify tasks and tags are included
    }
}
```

## Testing

### Unit Tests
```bash
/test-mcp
```

### Manual Testing (via Claude Code)

Example conversation:
```
User: "What projects can I log time to?"

Expected output:
```
Available Projects (3):

📊 INTERNAL - Internal Development
   Status: Active
   Tasks: Development, Code Review, Testing, Documentation
   Tags:
     • Environment: Production, Staging, Development
     • Billable: Yes, No

📊 CLIENT-A - Client A Project
   Status: Active
   Tasks: Feature Development, Bug Fixing, Maintenance, Support
   Tags:
     • Priority: High, Medium, Low
     • Sprint: Sprint-1, Sprint-2, Sprint-3

📊 RESEARCH - Research & Development
   Status: Active
   Tasks: Research, Prototyping, Evaluation
   Tags:
     • Technology: AI, Cloud, Security, DevOps
```
```

## Error Handling

The tool must handle these error cases:

1. **No Projects:** Return friendly message "No projects found."
2. **API Errors:** Return formatted error message
3. **Network Errors:** Return connection error message

## Related Files

**Created:**
- `TimeReportingMcp/Tools/GetProjectsTool.cs`
- `TimeReportingMcp.Tests/Tools/GetProjectsToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add tool registration

## Next Steps

After completing this task:
- Proceed to Task 9.4: Implement `submit_time_entry` tool
- Ensure all tests pass before committing
