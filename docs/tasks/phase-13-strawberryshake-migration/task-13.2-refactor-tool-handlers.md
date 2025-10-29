# Task 13.2: Refactor Tool Handlers to Use StrawberryShake

**Phase:** 13 - StrawberryShake Migration
**Estimated Time:** 2-3 hours
**Dependencies:** Task 13.1 (GraphQL Operations Created)
**Status:** Pending

---

## Objective

Refactor all 7 MCP tool handler classes to use the strongly-typed StrawberryShake client (`ITimeReportingClient`) instead of hardcoded GraphQL strings and `GraphQLClientWrapper`.

---

## Background

The MCP tools currently use `GraphQLClientWrapper` with hardcoded string queries and manual response parsing. After Task 13.1, all operations are defined in `.graphql` files and StrawberryShake has generated strongly-typed client code. This task migrates each tool to use the generated client.

---

## Acceptance Criteria

1. ✅ All 7 tool handlers inject and use `ITimeReportingClient`
2. ✅ No hardcoded GraphQL query strings remain in tool handlers
3. ✅ All tools use generated input types (e.g., `LogTimeInput`)
4. ✅ All tools use generated result types (e.g., `ILogTimeResult`)
5. ✅ Error handling uses StrawberryShake's `result.IsErrorResult()`
6. ✅ All existing tests pass with refactored code
7. ✅ Code is cleaner and more maintainable (less boilerplate)

---

## Implementation Steps

### Step 1: Refactor LogTimeTool

**Before (Hardcoded Strings):**
```csharp
public class LogTimeTool
{
    private readonly GraphQLClientWrapper _client;

    public LogTimeTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        var mutation = new GraphQLRequest
        {
            Query = @"
                mutation LogTime($input: LogTimeInput!) {
                    logTime(input: $input) {
                        id
                        project { code name }
                        // ... 50 lines of string query
                    }
                }",
            Variables = new { input }
        };

        var response = await _client.SendMutationAsync<LogTimeResponse>(mutation);
        // Manual error handling
    }
}
```

**After (StrawberryShake):**
```csharp
public class LogTimeTool
{
    private readonly ITimeReportingClient _client;

    public LogTimeTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        // Parse arguments into generated input type
        var input = new LogTimeInput
        {
            ProjectCode = arguments.GetProperty("projectCode").GetString()!,
            Task = arguments.GetProperty("task").GetString()!,
            StandardHours = arguments.GetProperty("standardHours").GetDecimal(),
            StartDate = arguments.GetProperty("startDate").GetString()!,
            CompletionDate = arguments.GetProperty("completionDate").GetString()!,
            OvertimeHours = arguments.TryGetProperty("overtimeHours", out var ot)
                ? ot.GetDecimal()
                : null,
            Description = arguments.TryGetProperty("description", out var desc)
                ? desc.GetString()
                : null,
            IssueId = arguments.TryGetProperty("issueId", out var issue)
                ? issue.GetString()
                : null
        };

        // Execute strongly-typed mutation
        var result = await _client.LogTime.ExecuteAsync(input);

        // Use generated error handling
        if (result.IsErrorResult())
        {
            return CreateErrorResult(result.Errors);
        }

        // Access strongly-typed data
        var entry = result.Data!.LogTime;
        return CreateSuccessResult(entry);
    }
}
```

**File:** `TimeReportingMcp/Tools/LogTimeTool.cs`

---

### Step 2: Refactor GetProjectsTool

**Before:**
```csharp
var query = new GraphQLRequest
{
    Query = @"
        query GetProjects($activeOnly: Boolean!) {
            projects(where: { isActive: { eq: $activeOnly } }) {
                code name isActive
                // ... hardcoded string
            }
        }",
    Variables = new { activeOnly }
};
```

**After:**
```csharp
var result = await _client.GetAvailableProjects.ExecuteAsync(activeOnly);

if (result.IsErrorResult())
{
    return CreateErrorResult(result.Errors);
}

var projects = result.Data!.Projects.Nodes;
return FormatProjectsResult(projects);
```

**File:** `TimeReportingMcp/Tools/GetProjectsTool.cs`

---

### Step 3: Refactor QueryEntriesTool

**Changes:**
- Replace `GraphQLClientWrapper` with `ITimeReportingClient`
- Use `_client.QueryTimeEntries.ExecuteAsync(projectCode, startDate, endDate, status)`
- Access `result.Data.TimeEntries.Nodes` for entries
- Use `result.Data.TimeEntries.TotalCount` for count

**File:** `TimeReportingMcp/Tools/QueryEntriesTool.cs`

---

### Step 4: Refactor UpdateEntryTool

**Changes:**
- Build `UpdateTimeEntryInput` from arguments
- Use `_client.UpdateTimeEntry.ExecuteAsync(id, input)`
- Access `result.Data.UpdateTimeEntry` for updated entry

**File:** `TimeReportingMcp/Tools/UpdateEntryTool.cs`

---

### Step 5: Refactor DeleteEntryTool

**Changes:**
- Use `_client.DeleteTimeEntry.ExecuteAsync(id)`
- Result is boolean: `result.Data.DeleteTimeEntry`
- Simplest refactor (no complex input types)

**File:** `TimeReportingMcp/Tools/DeleteEntryTool.cs`

---

### Step 6: Refactor MoveTaskTool

**Changes:**
- Use `_client.MoveTaskToProject.ExecuteAsync(entryId, newProjectCode, newTask)`
- Access `result.Data.MoveTaskToProject` for moved entry

**File:** `TimeReportingMcp/Tools/MoveTaskTool.cs`

---

### Step 7: Refactor SubmitEntryTool

**Changes:**
- Use `_client.SubmitTimeEntry.ExecuteAsync(id)`
- Access `result.Data.SubmitTimeEntry` for status change

**File:** `TimeReportingMcp/Tools/SubmitEntryTool.cs`

---

## Error Handling Pattern

**Standard Pattern for All Tools:**

```csharp
var result = await _client.SomeOperation.ExecuteAsync(args);

if (result.IsErrorResult())
{
    var errorMessage = "❌ Operation failed:\n\n";
    errorMessage += string.Join("\n", result.Errors!.Select(e => $"- {e.Message}"));

    return new ToolResult
    {
        Content = new List<ContentItem>
        {
            ContentItem.CreateText(errorMessage)
        },
        IsError = true
    };
}

// Safe to access result.Data! (null-forgiving operator justified by IsErrorResult check)
var data = result.Data!.SomeOperation;
return CreateSuccessResult(data);
```

---

## Dependency Injection Update

**File:** `TimeReportingMcp/McpServer.cs`

**Before:**
```csharp
public class McpServer
{
    private readonly GraphQLClientWrapper _graphqlClient;

    public McpServer(GraphQLClientWrapper graphqlClient)
    {
        _graphqlClient = graphqlClient;
    }

    private void InitializeTools()
    {
        _tools["log_time"] = new LogTimeTool(_graphqlClient);
        _tools["query_time_entries"] = new QueryEntriesTool(_graphqlClient);
        // ...
    }
}
```

**After:**
```csharp
public class McpServer
{
    private readonly ITimeReportingClient _graphqlClient;

    public McpServer(ITimeReportingClient graphqlClient)
    {
        _graphqlClient = graphqlClient;
    }

    private void InitializeTools()
    {
        _tools["log_time"] = new LogTimeTool(_graphqlClient);
        _tools["query_time_entries"] = new QueryEntriesTool(_graphqlClient);
        // ...
    }
}
```

**File:** `TimeReportingMcp/Program.cs`

**Add StrawberryShake DI:**
```csharp
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;

var services = new ServiceCollection();

// Configure StrawberryShake client
services
    .AddTimeReportingClient()  // Generated extension method
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(apiUrl);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);
    });

var serviceProvider = services.BuildServiceProvider();
var graphqlClient = serviceProvider.GetRequiredService<ITimeReportingClient>();

var server = new McpServer(graphqlClient);
await server.RunAsync();
```

---

## Testing

### Verify All Existing Tests Pass

```bash
/test-mcp
```

**Expected:**
- All 97 MCP tests pass
- No test changes required (tools have same interface)

### Manual Test: Execute Each Tool

```bash
/run-mcp
```

Test each tool via Claude Code:
1. `get_available_projects` - Verify projects listed
2. `log_time` - Create new entry
3. `query_time_entries` - Find created entry
4. `update_time_entry` - Modify entry
5. `move_task_to_project` - Move entry
6. `submit_time_entry` - Submit for approval
7. `delete_time_entry` - Delete entry

---

## Definition of Done

- [x] All 7 tool handlers refactored to use `ITimeReportingClient`
- [x] No `GraphQLClientWrapper` usage remains in tool handlers
- [x] No hardcoded query strings in tool handlers
- [x] All 97 existing MCP tests pass
- [x] Manual testing confirms all 7 tools work correctly
- [x] Code review shows cleaner, more maintainable code
- [x] Error handling is consistent across all tools

---

## Files Modified

```
TimeReportingMcp/
├── Program.cs                    # Add StrawberryShake DI
├── McpServer.cs                  # Change to ITimeReportingClient
└── Tools/
    ├── LogTimeTool.cs            # Refactored
    ├── QueryEntriesTool.cs       # Refactored
    ├── UpdateEntryTool.cs        # Refactored
    ├── DeleteEntryTool.cs        # Refactored
    ├── MoveTaskTool.cs           # Refactored
    ├── GetProjectsTool.cs        # Refactored
    └── SubmitEntryTool.cs        # Refactored
```

---

## Code Reduction Estimate

**Before:**
- ~1,200 lines of tool code with hardcoded queries

**After:**
- ~800 lines of tool code (33% reduction)
- **Eliminated:**
  - ~200 lines of hardcoded GraphQL strings
  - ~150 lines of manual type definitions
  - ~50 lines of manual response parsing

**Benefits:**
- ✅ Compile-time safety
- ✅ IntelliSense everywhere
- ✅ Less boilerplate
- ✅ Easier to maintain

---

## Notes

- **Gradual Migration**: Can refactor one tool at a time and test incrementally
- **Type Safety**: Generated types catch schema mismatches at build time
- **Null Safety**: Use null-forgiving operator (`!`) after `IsErrorResult()` check
- **Performance**: No performance difference (same HTTP/GraphQL calls)

---

## Related

- **Previous Task**: [Task 13.1 - Create GraphQL Operations](./task-13.1-create-graphql-operations.md)
- **Next Task**: [Task 13.3 - Remove Old GraphQL Client Code](./task-13.3-remove-old-client-code.md)
- **ADR**: [0009-strawberryshake-typed-graphql-client.md](../../adr/0009-strawberryshake-typed-graphql-client.md)
