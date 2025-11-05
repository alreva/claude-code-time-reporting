# Task 8.1: Implement log_time Tool

**Phase:** 8 - MCP Server Tools Part 1
**Estimated Time:** 1-2 hours
**Prerequisites:** Phase 7 complete (MCP server core setup)
**Status:** Pending

## Objective

Implement the `log_time` MCP tool that creates new time entries via the GraphQL API. This tool allows users to log work hours through natural language commands in Claude Code.

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/LogTimeTool.cs` with tool handler
- [ ] Parse tool arguments and validate required fields
- [ ] Call GraphQL `logTime` mutation with proper input mapping
- [ ] Handle successful responses and format user-friendly output
- [ ] Handle validation errors from GraphQL API
- [ ] Return structured MCP tool result
- [ ] Write unit tests for the tool handler
- [ ] All tests pass

## GraphQL Mutation

The tool should execute this mutation:

```graphql
mutation LogTime($input: LogTimeInput!) {
  logTime(input: $input) {
    id
    projectCode
    task
    issueId
    standardHours
    overtimeHours
    description
    startDate
    completionDate
    status
    createdAt
    updatedAt
  }
}
```

## Input Schema

```json
{
  "projectCode": "string (required)",
  "task": "string (required)",
  "standardHours": "number (required)",
  "overtimeHours": "number (optional, default: 0)",
  "startDate": "string YYYY-MM-DD (required)",
  "completionDate": "string YYYY-MM-DD (required)",
  "description": "string (optional)",
  "issueId": "string (optional)",
  "tags": "array of {name, value} (optional)"
}
```

## Implementation Steps

### 1. Create Tool Handler Class

Create `TimeReportingMcp/Tools/LogTimeTool.cs`:

```csharp
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

public class LogTimeTool
{
    private readonly GraphQLClientWrapper _client;

    public LogTimeTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse and validate arguments
            var input = ParseArguments(arguments);

            // 2. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation LogTime($input: LogTimeInput!) {
                        logTime(input: $input) {
                            id
                            projectCode
                            task
                            issueId
                            standardHours
                            overtimeHours
                            description
                            startDate
                            completionDate
                            status
                            createdAt
                            updatedAt
                        }
                    }",
                Variables = new { input }
            };

            // 3. Execute mutation
            var response = await _client.SendMutationAsync<LogTimeResponse>(mutation);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Return success result
            return CreateSuccessResult(response.Data.LogTime);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private object ParseArguments(JsonElement arguments)
    {
        // Extract required fields
        var projectCode = arguments.GetProperty("projectCode").GetString();
        var task = arguments.GetProperty("task").GetString();
        var standardHours = arguments.GetProperty("standardHours").GetDouble();
        var startDate = arguments.GetProperty("startDate").GetString();
        var completionDate = arguments.GetProperty("completionDate").GetString();

        // Extract optional fields
        var overtimeHours = arguments.TryGetProperty("overtimeHours", out var ot) ? ot.GetDouble() : 0.0;
        var description = arguments.TryGetProperty("description", out var desc) ? desc.GetString() : null;
        var issueId = arguments.TryGetProperty("issueId", out var issue) ? issue.GetString() : null;

        // Parse tags if provided
        List<TagInput>? tags = null;
        if (arguments.TryGetProperty("tags", out var tagsElement))
        {
            tags = JsonSerializer.Deserialize<List<TagInput>>(tagsElement.GetRawText());
        }

        return new
        {
            projectCode,
            task,
            standardHours,
            overtimeHours,
            startDate,
            completionDate,
            description,
            issueId,
            tags
        };
    }

    private ToolResult CreateSuccessResult(TimeEntry entry)
    {
        var message = $"✅ Time entry created successfully!\n\n" +
                      $"ID: {entry.Id}\n" +
                      $"Project: {entry.ProjectCode} - {entry.Task}\n" +
                      $"Hours: {entry.StandardHours} standard";

        if (entry.OvertimeHours > 0)
        {
            message += $", {entry.OvertimeHours} overtime";
        }

        message += $"\nPeriod: {entry.StartDate} to {entry.CompletionDate}\n" +
                   $"Status: {entry.Status}";

        if (!string.IsNullOrEmpty(entry.Description))
        {
            message += $"\nDescription: {entry.Description}";
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message)
            }
        };
    }

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to create time entry:\n\n";
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

// Response type
public class LogTimeResponse
{
    public TimeEntry LogTime { get; set; } = null!;
}

// Input types
public class TagInput
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class TimeEntry
{
    public Guid Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string? IssueId { get; set; }
    public decimal StandardHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? Description { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string CompletionDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### 2. Update McpServer.cs

Replace the placeholder in `ExecuteToolAsync`:

```csharp
private async Task<ToolResult> ExecuteToolAsync(ToolCallParams toolParams)
{
    return toolParams.Name switch
    {
        "log_time" => await new LogTimeTool(_graphqlClient).ExecuteAsync(toolParams.Arguments),
        // ... other tools remain placeholders for now
        _ => throw new InvalidOperationException($"Unknown tool: {toolParams.Name}")
    };
}
```

### 3. Create Unit Tests

Create `TimeReportingMcp.Tests/Tools/LogTimeToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class LogTimeToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidInput_CreatesTimeEntry()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5001/graphql",
            // Authentication via AzureCliCredential (see TokenService.cs)
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new LogTimeTool(client);

        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL",
            task = "Development",
            standardHours = 8.0,
            startDate = "2025-10-29",
            completionDate = "2025-10-29"
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
        // Note: This requires API to be running
    }

    [Fact]
    public void ParseArguments_WithRequiredFields_ReturnsValidInput()
    {
        // Test argument parsing logic in isolation
        // Implementation depends on extracted parsing method
    }
}
```

## Testing

### Manual Testing

1. **Start the API:**
   ```bash
   /run-api
   ```

2. **Build MCP Server:**
   ```bash
   /build-mcp
   ```

3. **Test via stdin:**
   ```bash
   echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"log_time","arguments":{"projectCode":"INTERNAL","task":"Development","standardHours":8.0,"startDate":"2025-10-29","completionDate":"2025-10-29"}}}' | dotnet run --project TimeReportingMcp
   ```

4. **Verify response contains:**
   - Success message with entry ID
   - Project code and task
   - Hours logged
   - Status: NOT_REPORTED

### Test Scenarios

1. ✅ **Valid entry:** All required fields provided
2. ❌ **Missing project:** Should return validation error
3. ❌ **Invalid task:** Should return "task not available" error
4. ❌ **Invalid date:** Should return date validation error
5. ✅ **With optional fields:** Description, issue ID, overtime hours
6. ✅ **With tags:** Include metadata tags

## Related Files

**Created:**
- `TimeReportingMcp/Tools/LogTimeTool.cs`
- `TimeReportingMcp.Tests/Tools/LogTimeToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs` - Wire up tool handler

## Next Steps

After completing this task:
1. Run `/test-mcp` to verify all tests pass
2. Commit changes
3. Proceed to Task 8.2: Implement query_time_entries tool

## Reference

- PRD: `docs/prd/mcp-tools.md` (Section 2.1)
- API Spec: `docs/prd/api-specification.md` (logTime mutation)
