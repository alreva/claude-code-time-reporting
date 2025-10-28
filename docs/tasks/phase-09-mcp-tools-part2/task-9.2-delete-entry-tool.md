# Task 9.2: Implement delete_time_entry Tool

**Phase:** 9 - MCP Server Tools Part 2
**Estimated Time:** 30 minutes
**Prerequisites:** Task 9.1 complete (move_task_to_project tool)
**Status:** Pending

## Objective

Implement the `delete_time_entry` MCP tool that deletes a time entry. This operation is only allowed for entries with NOT_REPORTED status to prevent accidental deletion of submitted/approved time.

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/DeleteEntryTool.cs` with tool handler
- [ ] Parse tool arguments (id)
- [ ] Call GraphQL `deleteTimeEntry` mutation
- [ ] Handle successful deletion with confirmation message
- [ ] Handle validation errors (entry not found, invalid status)
- [ ] Return structured MCP tool result
- [ ] Write unit tests for the tool handler
- [ ] All tests pass

## GraphQL Mutation

The tool should execute this mutation:

```graphql
mutation DeleteTimeEntry($id: UUID!) {
  deleteTimeEntry(id: $id) {
    success
    message
  }
}
```

## Input Schema

```json
{
  "id": "uuid (required)"
}
```

## Implementation Steps

### 1. Create Tool Handler Class

Create `TimeReportingMcp/Tools/DeleteEntryTool.cs`:

```csharp
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

public class DeleteEntryTool
{
    private readonly GraphQLClientWrapper _client;

    public DeleteEntryTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse arguments
            var id = arguments.GetProperty("id").GetString()
                ?? throw new ArgumentException("id is required");

            // 2. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation DeleteTimeEntry($id: UUID!) {
                        deleteTimeEntry(id: $id) {
                            success
                            message
                        }
                    }",
                Variables = new { id }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<DeleteTimeEntryResponse>(mutation);

            // 4. Format success response
            var result = response.DeleteTimeEntry;
            return ToolResult.Success(result.Message);
        }
        catch (Exception ex)
        {
            return ErrorHandler.HandleToolError(ex, "delete_time_entry");
        }
    }
}

public class DeleteTimeEntryResponse
{
    public DeleteResult DeleteTimeEntry { get; set; } = null!;
}

public class DeleteResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

### 2. Register Tool in McpServer

Update `TimeReportingMcp/McpServer.cs` to include the new tool:

```csharp
// In tools/list handler, add:
new ToolDefinition
{
    Name = "delete_time_entry",
    Description = "Delete a time entry (only allowed for NOT_REPORTED entries)",
    InputSchema = new
    {
        type = "object",
        properties = new
        {
            id = new
            {
                type = "string",
                description = "UUID of the time entry to delete"
            }
        },
        required = new[] { "id" }
    }
},

// In tools/call handler, add case:
case "delete_time_entry":
    var deleteEntryTool = new DeleteEntryTool(_graphqlClient);
    return await deleteEntryTool.ExecuteAsync(args);
```

### 3. Write Unit Tests

Create `TimeReportingMcp.Tests/Tools/DeleteEntryToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class DeleteEntryToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidId_DeletesEntry()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsError()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithSubmittedEntry_ReturnsError()
    {
        // Test implementation (cannot delete SUBMITTED entries)
    }

    [Fact]
    public async Task ExecuteAsync_WithApprovedEntry_ReturnsError()
    {
        // Test implementation (cannot delete APPROVED entries)
    }

    [Fact]
    public async Task ExecuteAsync_MissingId_ThrowsArgumentException()
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
User: "Delete that last entry I just created"

Expected behavior:
1. Tool parses id from context or user specification
2. Calls GraphQL mutation
3. Returns "Time entry deleted successfully" or error if not allowed
```

## Error Handling

The tool must handle these error cases:

1. **Entry Not Found:** "Time entry not found"
2. **Invalid Status:** "Cannot delete entry with status SUBMITTED/APPROVED/DECLINED"
3. **Missing Arguments:** "id is required"

## Related Files

**Created:**
- `TimeReportingMcp/Tools/DeleteEntryTool.cs`
- `TimeReportingMcp.Tests/Tools/DeleteEntryToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add tool registration

## Next Steps

After completing this task:
- Proceed to Task 9.3: Implement `get_available_projects` tool
- Ensure all tests pass before committing
