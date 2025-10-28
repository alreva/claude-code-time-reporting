# Task 9.4: Implement submit_time_entry Tool

**Phase:** 9 - MCP Server Tools Part 2
**Estimated Time:** 30 minutes
**Prerequisites:** Task 9.3 complete (get_available_projects tool)
**Status:** Pending

## Objective

Implement the `submit_time_entry` MCP tool that submits a time entry for approval. This changes the entry's status from NOT_REPORTED to SUBMITTED, initiating the approval workflow.

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/SubmitEntryTool.cs` with tool handler
- [ ] Parse tool arguments (id)
- [ ] Call GraphQL `submitTimeEntry` mutation
- [ ] Handle successful submission with confirmation message
- [ ] Handle validation errors (entry not found, invalid status)
- [ ] Return structured MCP tool result
- [ ] Write unit tests for the tool handler
- [ ] All tests pass

## GraphQL Mutation

The tool should execute this mutation:

```graphql
mutation SubmitTimeEntry($id: UUID!) {
  submitTimeEntry(id: $id) {
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
  "id": "uuid (required)"
}
```

## Implementation Steps

### 1. Create Tool Handler Class

Create `TimeReportingMcp/Tools/SubmitEntryTool.cs`:

```csharp
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

public class SubmitEntryTool
{
    private readonly GraphQLClientWrapper _client;

    public SubmitEntryTool(GraphQLClientWrapper client)
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
                    mutation SubmitTimeEntry($id: UUID!) {
                        submitTimeEntry(id: $id) {
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
                Variables = new { id }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<SubmitEntryResponse>(mutation);

            // 4. Format success response
            var entry = response.SubmitTimeEntry;
            return ToolResult.Success(
                $"Time entry {entry.Id} submitted for approval\n" +
                $"Project: {entry.ProjectCode} - {entry.Task}\n" +
                $"Hours: {entry.StandardHours} standard, {entry.OvertimeHours} overtime\n" +
                $"Date: {entry.StartDate} to {entry.CompletionDate}\n" +
                $"Status: {entry.Status}"
            );
        }
        catch (Exception ex)
        {
            return ErrorHandler.HandleToolError(ex, "submit_time_entry");
        }
    }
}

public class SubmitEntryResponse
{
    public SubmittedTimeEntry SubmitTimeEntry { get; set; } = null!;
}

public class SubmittedTimeEntry
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
    Name = "submit_time_entry",
    Description = "Submit a time entry for approval (changes status to SUBMITTED)",
    InputSchema = new
    {
        type = "object",
        properties = new
        {
            id = new
            {
                type = "string",
                description = "UUID of the time entry to submit"
            }
        },
        required = new[] { "id" }
    }
},

// In tools/call handler, add case:
case "submit_time_entry":
    var submitEntryTool = new SubmitEntryTool(_graphqlClient);
    return await submitEntryTool.ExecuteAsync(args);
```

### 3. Write Unit Tests

Create `TimeReportingMcp.Tests/Tools/SubmitEntryToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class SubmitEntryToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidId_SubmitsEntry()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentId_ReturnsError()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadySubmittedEntry_ReturnsError()
    {
        // Test implementation (cannot re-submit)
    }

    [Fact]
    public async Task ExecuteAsync_WithApprovedEntry_ReturnsError()
    {
        // Test implementation (cannot submit approved entries)
    }

    [Fact]
    public async Task ExecuteAsync_MissingId_ThrowsArgumentException()
    {
        // Test implementation
    }

    [Fact]
    public async Task ExecuteAsync_ChangesStatusToSubmitted()
    {
        // Test implementation - verify status change
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
User: "Submit all my time entries for this week for approval"

Expected behavior:
1. First, query entries for the week (using query_time_entries)
2. For each NOT_REPORTED entry, call submit_time_entry
3. Return summary: "Submitted 5 time entries for approval"
```

## Error Handling

The tool must handle these error cases:

1. **Entry Not Found:** "Time entry not found"
2. **Invalid Status:** "Cannot submit entry with status SUBMITTED/APPROVED" (only NOT_REPORTED or DECLINED can be submitted)
3. **Missing Arguments:** "id is required"

## Related Files

**Created:**
- `TimeReportingMcp/Tools/SubmitEntryTool.cs`
- `TimeReportingMcp.Tests/Tools/SubmitEntryToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Add tool registration

## Next Steps

After completing this task:
- Phase 9 complete! All 4 MCP tools implemented
- Run full test suite to verify all tools work correctly
- Update TASK-INDEX.md to mark Phase 9 as complete
- Commit all changes with comprehensive message
