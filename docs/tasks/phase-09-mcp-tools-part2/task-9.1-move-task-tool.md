# Task 9.1: Implement move_task_to_project Tool

**Phase:** 9 - MCP Server Tools Part 2
**Estimated Time:** 1 hour
**Prerequisites:** Phase 8 complete (log_time, query_time_entries, update_time_entry tools)
**Status:** Pending

## Objective

Implement the `move_task_to_project` MCP tool that moves a time entry to a different project and task. This is useful when an entry was logged to the wrong project and needs to be reassigned with revalidation.

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/MoveTaskTool.cs` with tool handler
- [ ] Parse tool arguments (entryId, newProjectCode, newTask)
- [ ] Call GraphQL `moveTaskToProject` mutation
- [ ] Handle successful responses with user-friendly output
- [ ] Handle validation errors (invalid project, invalid task, status restrictions)
- [ ] Return structured MCP tool result
- [ ] Write unit tests for the tool handler
- [ ] All tests pass

## GraphQL Mutation

The tool should execute this mutation:

```graphql
mutation MoveTaskToProject($entryId: UUID!, $newProjectCode: String!, $newTask: String!) {
  moveTaskToProject(entryId: $entryId, newProjectCode: $newProjectCode, newTask: $newTask) {
    id
    projectCode
    task
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    updatedAt
  }
}
```

## Input Schema

```json
{
  "entryId": "uuid (required)",
  "newProjectCode": "string (required, max 10 chars)",
  "newTask": "string (required, max 100 chars)"
}
```

## Implementation Steps

### 1. Create Tool Handler Class

Create `TimeReportingMcp/Tools/MoveTaskTool.cs`:

```csharp
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

public class MoveTaskTool
{
    private readonly GraphQLClientWrapper _client;

    public MoveTaskTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse arguments
            var entryId = arguments.GetProperty("entryId").GetString()
                ?? throw new ArgumentException("entryId is required");
            var newProjectCode = arguments.GetProperty("newProjectCode").GetString()
                ?? throw new ArgumentException("newProjectCode is required");
            var newTask = arguments.GetProperty("newTask").GetString()
                ?? throw new ArgumentException("newTask is required");

            // 2. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation MoveTaskToProject($entryId: UUID!, $newProjectCode: String!, $newTask: String!) {
                        moveTaskToProject(entryId: $entryId, newProjectCode: $newProjectCode, newTask: $newTask) {
                            id
                            projectCode
                            task
                            standardHours
                            overtimeHours
                            startDate
                            completionDate
                            status
                            updatedAt
                        }
                    }",
                Variables = new
                {
                    entryId,
                    newProjectCode,
                    newTask
                }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<MoveTaskResponse>(mutation);

            // 4. Format success response
            var entry = response.MoveTaskToProject;
            return ToolResult.Success(
                $"Time entry {entry.Id} moved to {entry.ProjectCode} - {entry.Task}\n" +
                $"Hours: {entry.StandardHours} standard, {entry.OvertimeHours} overtime\n" +
                $"Date: {entry.StartDate} to {entry.CompletionDate}\n" +
                $"Status: {entry.Status}"
            );
        }
        catch (Exception ex)
        {
            return ErrorHandler.HandleToolError(ex, "move_task_to_project");
        }
    }
}

public class MoveTaskResponse
{
    public TimeEntryData MoveTaskToProject { get; set; } = null!;
}

public class TimeEntryData
{
    public string Id { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public decimal StandardHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string CompletionDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}
```

### 2. Register Tool in McpServer

Update `TimeReportingMcp/McpServer.cs` to include the new tool:

```csharp
// In tools/list handler, add:
new ToolDefinition
{
    Name = "move_task_to_project",
    Description = "Move a time entry to a different project and task",
    InputSchema = new
    {
        type = "object",
        properties = new
        {
            entryId = new
            {
                type = "string",
                description = "UUID of the time entry to move"
            },
            newProjectCode = new
            {
                type = "string",
                description = "Target project code",
                maxLength = 10
            },
            newTask = new
            {
                type = "string",
                description = "Task name in the target project",
                maxLength = 100
            }
        },
        required = new[] { "entryId", "newProjectCode", "newTask" }
    }
},

// In tools/call handler, add case:
case "move_task_to_project":
    var moveTaskTool = new MoveTaskTool(_graphqlClient);
    return await moveTaskTool.ExecuteAsync(args);
```

### 3. Write Unit Tests

Create `TimeReportingMcp.Tests/Tools/MoveTaskToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class MoveTaskToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidInput_MovesEntry()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidProject_ReturnsError()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidTask_ReturnsError()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithApprovedEntry_ReturnsError()
    {
        // Test implementation (cannot move approved entries)
    }

    [Fact]
    public async Task ExecuteAsync_MissingEntryId_ThrowsArgumentException()
    {
        // Test implementation
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
User: "Move today's development entry to CLIENT-A project under Feature Development"

Expected behavior:
1. Tool parses entryId, newProjectCode="CLIENT-A", newTask="Feature Development"
2. Calls GraphQL mutation
3. Returns success message with new project/task details
```

## Error Handling

The tool must handle these error cases:

1. **Invalid Project:** "Project 'INVALID' not found"
2. **Invalid Task:** "Task 'InvalidTask' not available for project 'CLIENT-A'"
3. **Entry Not Found:** "Time entry not found"
4. **Status Restriction:** "Cannot move entry with status APPROVED"
5. **Missing Arguments:** "entryId is required"

## Related Files

**Created:**
- `TimeReportingMcp/Tools/MoveTaskTool.cs`
- `TimeReportingMcp.Tests/Tools/MoveTaskToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add tool registration

## Next Steps

After completing this task:
- Proceed to Task 9.2: Implement `delete_time_entry` tool
- Ensure all tests pass before committing
