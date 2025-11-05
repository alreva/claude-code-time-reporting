# Task 8.2: Implement query_time_entries Tool

**Phase:** 8 - MCP Server Tools Part 1
**Estimated Time:** 1-2 hours
**Prerequisites:** Task 8.1 complete
**Status:** Pending

## Objective

Implement the `query_time_entries` MCP tool that queries time entries with optional filters. This tool allows users to search and retrieve their time tracking data through Claude Code.

## Acceptance Criteria

- [ ] Create `TimeReportingMcp/Tools/QueryEntriesTo

.cs` with tool handler
- [ ] Parse optional filter arguments (projectCode, status, startDate, endDate, limit, offset)
- [ ] Call GraphQL `timeEntries` query with filters
- [ ] Format results in user-friendly table/list format
- [ ] Handle empty results gracefully
- [ ] Handle GraphQL errors
- [ ] Write unit tests
- [ ] All tests pass

## GraphQL Query

```graphql
query TimeEntries($filters: TimeEntryFiltersInput) {
  timeEntries(filters: $filters) {
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
    tags
    createdAt
  }
}
```

## Input Schema

```json
{
  "projectCode": "string (optional)",
  "status": "NOT_REPORTED | SUBMITTED | APPROVED | DECLINED (optional)",
  "startDate": "string YYYY-MM-DD (optional)",
  "endDate": "string YYYY-MM-DD (optional)",
  "limit": "integer (optional, default: 50)",
  "offset": "integer (optional, default: 0)"
}
```

## Implementation

### 1. Create Tool Handler

Create `TimeReportingMcp/Tools/QueryEntriesTool.cs`:

```csharp
using System.Text;
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

public class QueryEntriesTool
{
    private readonly GraphQLClientWrapper _client;

    public QueryEntriesTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse filters (all optional)
            var filters = ParseFilters(arguments);

            // 2. Build GraphQL query
            var query = new GraphQLRequest
            {
                Query = @"
                    query TimeEntries($filters: TimeEntryFiltersInput) {
                        timeEntries(filters: $filters) {
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
                            tags
                            createdAt
                        }
                    }",
                Variables = new { filters }
            };

            // 3. Execute query
            var response = await _client.SendQueryAsync<QueryTimeEntriesResponse>(query);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Return formatted results
            return CreateSuccessResult(response.Data.TimeEntries, filters);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private object? ParseFilters(JsonElement arguments)
    {
        // Return null if no arguments provided (query all)
        if (arguments.ValueKind == JsonValueKind.Undefined ||
            arguments.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var filters = new Dictionary<string, object>();

        if (arguments.TryGetProperty("projectCode", out var proj))
        {
            filters["projectCode"] = proj.GetString()!;
        }

        if (arguments.TryGetProperty("status", out var stat))
        {
            filters["status"] = stat.GetString()!;
        }

        if (arguments.TryGetProperty("startDate", out var start))
        {
            filters["startDate"] = start.GetString()!;
        }

        if (arguments.TryGetProperty("endDate", out var end))
        {
            filters["endDate"] = end.GetString()!;
        }

        if (arguments.TryGetProperty("limit", out var lim))
        {
            filters["limit"] = lim.GetInt32();
        }

        if (arguments.TryGetProperty("offset", out var off))
        {
            filters["offset"] = off.GetInt32();
        }

        return filters.Count > 0 ? filters : null;
    }

    private ToolResult CreateSuccessResult(List<TimeEntry> entries, object? filters)
    {
        if (entries.Count == 0)
        {
            return new ToolResult
            {
                Content = new List<ContentItem>
                {
                    ContentItem.CreateText("📋 No time entries found matching your criteria.")
                }
            };
        }

        var message = new StringBuilder();
        message.AppendLine($"📋 Found {entries.Count} time entries:\n");

        // Group by project for better readability
        var groupedEntries = entries.GroupBy(e => e.ProjectCode).OrderBy(g => g.Key);

        foreach (var group in groupedEntries)
        {
            message.AppendLine($"**{group.Key}**");
            foreach (var entry in group.OrderByDescending(e => e.StartDate))
            {
                message.Append($"  • {entry.StartDate}");

                if (entry.StartDate != entry.CompletionDate)
                {
                    message.Append($" to {entry.CompletionDate}");
                }

                message.Append($" - {entry.Task}");
                message.Append($" - {entry.StandardHours}h");

                if (entry.OvertimeHours > 0)
                {
                    message.Append($" (+{entry.OvertimeHours}h OT)");
                }

                message.Append($" [{entry.Status}]");

                if (!string.IsNullOrEmpty(entry.IssueId))
                {
                    message.Append($" - {entry.IssueId}");
                }

                message.AppendLine();

                if (!string.IsNullOrEmpty(entry.Description))
                {
                    var shortDesc = entry.Description.Length > 60
                        ? entry.Description.Substring(0, 57) + "..."
                        : entry.Description;
                    message.AppendLine($"    {shortDesc}");
                }

                message.AppendLine($"    ID: {entry.Id}");
            }
            message.AppendLine();
        }

        // Add summary
        var totalStandard = entries.Sum(e => e.StandardHours);
        var totalOvertime = entries.Sum(e => e.OvertimeHours);
        message.AppendLine($"**Summary:** {totalStandard}h standard");
        if (totalOvertime > 0)
        {
            message.Append($", {totalOvertime}h overtime");
        }
        message.Append($" | Total: {totalStandard + totalOvertime}h");

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message.ToString())
            }
        };
    }

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to query time entries:\n\n";
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
public class QueryTimeEntriesResponse
{
    public List<TimeEntry> TimeEntries { get; set; } = new();
}
```

### 2. Update McpServer.cs

Add to the tool switch:

```csharp
"query_time_entries" => await new QueryEntriesTool(_graphqlClient).ExecuteAsync(toolParams.Arguments),
```

### 3. Create Unit Tests

Create `TimeReportingMcp.Tests/Tools/QueryEntriesToolTests.cs`:

```csharp
using System.Text.Json;
using Xunit;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Tools;

public class QueryEntriesToolTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoFilters_ReturnsAllEntries()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5001/graphql",
            // Authentication via AzureCliCredential (see TokenService.cs)
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new QueryEntriesTool(client);
        var args = JsonSerializer.SerializeToElement(new { });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Content);
    }

    [Fact]
    public async Task ExecuteAsync_WithProjectFilter_ReturnsFilteredEntries()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5001/graphql",
            // Authentication via AzureCliCredential (see TokenService.cs)
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new QueryEntriesTool(client);
        var args = JsonSerializer.SerializeToElement(new
        {
            projectCode = "INTERNAL"
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("INTERNAL", result.Content[0].Text);
    }

    [Fact]
    public async Task ExecuteAsync_WithDateRange_ReturnsEntriesInRange()
    {
        // Arrange
        var config = new McpConfig
        {
            GraphQLApiUrl = "http://localhost:5001/graphql",
            // Authentication via AzureCliCredential (see TokenService.cs)
        };
        var client = new GraphQLClientWrapper(config);
        var tool = new QueryEntriesTool(client);
        var args = JsonSerializer.SerializeToElement(new
        {
            startDate = "2025-10-01",
            endDate = "2025-10-31"
        });

        // Act
        var result = await tool.ExecuteAsync(args);

        // Assert
        Assert.NotNull(result);
    }
}
```

## Testing

### Manual Testing

1. **Create some test entries first:**
   ```bash
   /run-api
   # Use log_time tool to create a few entries
   ```

2. **Test query with no filters:**
   ```bash
   echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"query_time_entries","arguments":{}}}' | dotnet run --project TimeReportingMcp
   ```

3. **Test with project filter:**
   ```bash
   echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"query_time_entries","arguments":{"projectCode":"INTERNAL"}}}' | dotnet run --project TimeReportingMcp
   ```

4. **Test with date range:**
   ```bash
   echo '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"query_time_entries","arguments":{"startDate":"2025-10-01","endDate":"2025-10-31"}}}' | dotnet run --project TimeReportingMcp
   ```

### Test Scenarios

1. ✅ **No filters:** Returns all entries
2. ✅ **Project filter:** Returns only entries for specified project
3. ✅ **Status filter:** Returns entries with specific status
4. ✅ **Date range:** Returns entries within date range
5. ✅ **Combined filters:** Multiple filters work together
6. ✅ **Empty results:** Handles no matches gracefully
7. ✅ **Pagination:** Limit and offset work correctly

## Related Files

**Created:**
- `TimeReportingMcp/Tools/QueryEntriesTool.cs`
- `TimeReportingMcp.Tests/Tools/QueryEntriesToolTests.cs`

**Modified:**
- `TimeReportingMcp/McpServer.cs`

## Next Steps

1. Run `/test-mcp` to verify tests pass
2. Commit changes
3. Proceed to Task 8.3: Implement update_time_entry tool

## Reference

- PRD: `docs/prd/mcp-tools.md` (Section 2.2)
- API Spec: `docs/prd/api-specification.md` (timeEntries query)
