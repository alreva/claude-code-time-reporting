# Task 8.3: Implement update_time_entry Tool

**Phase:** 8 - MCP Server Tools Part 1
**Estimated Time:** 1 hour
**Prerequisites:** Task 8.2 complete
**Status:** Pending

## Objective

Implement the `update_time_entry` MCP tool that updates existing time entries. This tool allows users to modify time entries through Claude Code (only for NOT_REPORTED and DECLINED entries).

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/UpdateEntryTool.cs` with tool handler
- [ ] Parse entry ID and update fields
- [ ] Call GraphQL `updateTimeEntry` mutation
- [ ] Handle status validation errors (cannot update SUBMITTED/APPROVED)
- [ ] Format success message showing what changed
- [ ] Handle GraphQL errors
- [ ] Write unit tests
- [ ] All tests pass

## GraphQL Mutation

```graphql
mutation UpdateTimeEntry($id: UUID!, $input: UpdateTimeEntryInput!) {
  updateTimeEntry(id: $id, input: $input) {
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
    updatedAt
  }
}
```

## Input Schema

```json
{
  "id": "string UUID (required)",
  "task": "string (optional)",
  "issueId": "string (optional)",
  "standardHours": "number (optional)",
  "overtimeHours": "number (optional)",
  "description": "string (optional)",
  "startDate": "string YYYY-MM-DD (optional)",
  "completionDate": "string YYYY-MM-DD (optional)",
  "tags": "array of {name, value} (optional)"
}
```

## Implementation

### 1. Create Tool Handler

Create `TimeReportingMcp/Tools/UpdateEntryTool.cs`:

```csharp
using System.Text;
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

public class UpdateEntryTool
{
    private readonly GraphQLClientWrapper _client;

    public UpdateEntryTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse entry ID (required)
            if (!arguments.TryGetProperty("id", out var idElement))
            {
                return CreateValidationError("Entry ID is required");
            }

            var id = idElement.GetString();
            if (string.IsNullOrEmpty(id))
            {
                return CreateValidationError("Entry ID cannot be empty");
            }

            // 2. Parse update fields (all optional)
            var input = ParseUpdateFields(arguments);

            if (input == null || ((IDictionary<string, object>)input).Count == 0)
            {
                return CreateValidationError("At least one field must be provided to update");
            }

            // 3. Build GraphQL mutation
            var mutation = new GraphQLRequest
            {
                Query = @"
                    mutation UpdateTimeEntry($id: UUID!, $input: UpdateTimeEntryInput!) {
                        updateTimeEntry(id: $id, input: $input) {
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
                            updatedAt
                        }
                    }",
                Variables = new { id, input }
            };

            // 4. Execute mutation
            var response = await _client.SendMutationAsync<UpdateTimeEntryResponse>(mutation);

            // 5. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 6. Return success result with changes highlighted
            return CreateSuccessResult(response.Data.UpdateTimeEntry, input);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private object? ParseUpdateFields(JsonElement arguments)
    {
        var updates = new Dictionary<string, object>();

        if (arguments.TryGetProperty("task", out var task))
        {
            updates["task"] = task.GetString()!;
        }

        if (arguments.TryGetProperty("issueId", out var issueId))
        {
            updates["issueId"] = issueId.GetString()!;
        }

        if (arguments.TryGetProperty("standardHours", out var standardHours))
        {
            updates["standardHours"] = standardHours.GetDouble();
        }

        if (arguments.TryGetProperty("overtimeHours", out var overtimeHours))
        {
            updates["overtimeHours"] = overtimeHours.GetDouble();
        }

        if (arguments.TryGetProperty("description", out var description))
        {
            updates["description"] = description.GetString()!;
        }

        if (arguments.TryGetProperty("startDate", out var startDate))
        {
            updates["startDate"] = startDate.GetString()!;
        }

        if (arguments.TryGetProperty("completionDate", out var completionDate))
        {
            updates["completionDate"] = completionDate.GetString()!;
        }

        if (arguments.TryGetProperty("tags", out var tags))
        {
            updates["tags"] = JsonSerializer.Deserialize<List<TagInput>>(tags.GetRawText())!;
        }

        return updates.Count > 0 ? updates : null;
    }

    private ToolResult CreateSuccessResult(TimeEntry entry, object updates)
    {
        var message = new StringBuilder();
        message.AppendLine("✅ Time entry updated successfully!\n");
        message.AppendLine($"ID: {entry.Id}");
        message.AppendLine($"Project: {entry.ProjectCode} - {entry.Task}");
        message.AppendLine($"Hours: {entry.StandardHours} standard");

        if (entry.OvertimeHours > 0)
        {
            message.Append($", {entry.OvertimeHours} overtime");
        }

        message.AppendLine($"\nPeriod: {entry.StartDate} to {entry.CompletionDate}");
        message.AppendLine($"Status: {entry.Status}");

        if (!string.IsNullOrEmpty(entry.Description))
        {
            message.AppendLine($"Description: {entry.Description}");
        }

        if (!string.IsNullOrEmpty(entry.IssueId))
        {
            message.AppendLine($"Issue: {entry.IssueId}");
        }

        message.AppendLine($"\nUpdated: {entry.UpdatedAt:yyyy-MM-dd HH:mm:ss}");

        // Highlight what changed
        var updateDict = updates as IDictionary<string, object>;
        if (updateDict != null && updateDict.Count > 0)
        {
            message.AppendLine($"\n**Changes applied:**");
            foreach (var kvp in updateDict)
            {
                message.AppendLine($"  • {kvp.Key}: {kvp.Value}");
            }
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message.ToString())
            }
        };
    }

    private ToolResult CreateValidationError(string errorMessage)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"❌ Validation error: {errorMessage}")
            },
            IsError = true
        };
    }

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to update time entry:\n\n";

        foreach (var error in errors)
        {
            errorMessage += $"- {error.Message}\n";

            // Special handling for common errors
            if (error.Message.Contains("Cannot update"))
            {
                errorMessage += "\n💡 Tip: Only entries with status NOT_REPORTED or DECLINED can be updated.\n";
                errorMessage += "   Submitted or approved entries are read-only.\n";
            }
            else if (error.Message.Contains("not found"))
            {
                errorMessage += "\n💡 Tip: Double-check the entry ID. You can use query_time_entries to find entries.\n";
            }
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

// Response type
public class UpdateTimeEntryResponse
{
    public TimeEntry UpdateTimeEntry { get; set; } = null!;
}
```

### 2. Update McpServer.cs

Add to the tool switch:

```csharp
"update_time_entry" => await new UpdateEntryTool(_graphqlClient).ExecuteAsync(toolParams.Arguments),
```

### 3. Create Unit Tests

Create `TimeReportingMcp.Tests/Tools/UpdateEntryToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class UpdateEntryToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidUpdate_UpdatesEntry()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5000/graphql",
            BearerToken = "test-token"
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new UpdateEntryTool(client);

        // First create an entry, then update it
        var args = JsonSerializer.SerializeToElement(new
        {
            id = "existing-entry-id",
            standardHours = 7.5
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("updated successfully", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingId_ReturnsValidationError()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5000/graphql",
            BearerToken = "test-token"
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new UpdateEntryTool(client);

        var args = JsonSerializer.SerializeToElement(new
        {
            standardHours = 7.5
            // id is missing
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("required", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoUpdateFields_ReturnsValidationError()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5000/graphql",
            BearerToken = "test-token"
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new UpdateEntryTool(client);

        var args = JsonSerializer.SerializeToElement(new
        {
            id = "some-id"
            // No update fields provided
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("at least one field", result.Content[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatingSubmittedEntry_ReturnsError()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5000/graphql",
            BearerToken = "test-token"
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new UpdateEntryTool(client);

        // Create and submit an entry first
        var args = JsonSerializer.SerializeToElement(new
        {
            id = "submitted-entry-id",
            standardHours = 7.5
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        // Should fail because entry is submitted
        Assert.True(result.IsError);
    }
}
```

## Testing

### Manual Testing

1. **Create an entry to update:**
   ```bash
   # Use log_time tool to create an entry
   # Note the returned ID
   ```

2. **Test updating hours:**
   ```bash
   echo '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"update_time_entry","arguments":{"id":"<entry-id>","standardHours":7.5}}}' | dotnet run --project TimeReportingMcp
   ```

3. **Test updating description:**
   ```bash
   echo '{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"update_time_entry","arguments":{"id":"<entry-id>","description":"Updated work description"}}}' | dotnet run --project TimeReportingMcp
   ```

4. **Test updating submitted entry (should fail):**
   ```bash
   # First submit an entry
   # Then try to update it - should fail with validation error
   ```

### Test Scenarios

1. ✅ **Update single field:** Change only hours
2. ✅ **Update multiple fields:** Change hours and description
3. ❌ **Missing ID:** Returns validation error
4. ❌ **No update fields:** Returns validation error
5. ❌ **Update submitted entry:** Returns "cannot update" error
6. ❌ **Invalid entry ID:** Returns "not found" error
7. ✅ **Update declined entry:** Should succeed (declined can be edited)

## Related Files

**Created:**
- `TimeReportingMcp/Tools/UpdateEntryTool.cs`
- `TimeReportingMcp.Tests/Tools/UpdateEntryToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs`

## Next Steps

1. Run `/test-mcp` to verify tests pass
2. Commit changes
3. Proceed to Task 8.4: Add comprehensive error handling

## Reference

- PRD: `docs/prd/mcp-tools.md` (Section 2.3)
- API Spec: `docs/prd/api-specification.md` (updateTimeEntry mutation)
