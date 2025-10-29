# Task 7.4: Implement MCP Server Core

**Phase:** 7 - MCP Server Setup
**Estimated Time:** 1 hour
**Prerequisites:** Task 7.3 complete (JSON-RPC models defined)
**Status:** ✅ Complete

---

## Objective

Implement the core MCP server that reads JSON-RPC requests from stdin, routes to tool handlers, and writes responses to stdout.

---

## Acceptance Criteria

- [ ] McpServer class created with stdio handling
- [ ] Request/response loop implemented
- [ ] tools/list method implemented (returns available tools)
- [ ] tools/call method routing implemented (placeholder handlers)
- [ ] Error handling for invalid requests
- [ ] Server can be started and stopped gracefully
- [ ] Integration test passes (echo test)
- [ ] Project builds and runs successfully

---

## Implementation Steps

### 1. Create MCP Server Class

Create `McpServer.cs`:

```csharp
using System;
using System.Text.Json;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

/// <summary>
/// MCP server that handles JSON-RPC communication via stdio
/// </summary>
public class McpServer
{
    private readonly GraphQLClientWrapper _graphqlClient;
    private readonly List<ToolDefinition> _availableTools;

    public McpServer(GraphQLClientWrapper graphqlClient)
    {
        _graphqlClient = graphqlClient;
        _availableTools = InitializeToolDefinitions();
    }

    /// <summary>
    /// Start the MCP server and process requests from stdin
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Console.Error.WriteLine("MCP Server ready - listening on stdin");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Read one line from stdin (JSON-RPC request)
                var line = await Console.In.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                {
                    Console.Error.WriteLine("Received empty line, continuing...");
                    continue;
                }

                // Process request and write response to stdout
                await HandleRequestAsync(line);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error in MCP server: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Handle a single JSON-RPC request
    /// </summary>
    private async Task HandleRequestAsync(string requestJson)
    {
        JsonRpcResponse response;
        int? requestId = null;

        try
        {
            Console.Error.WriteLine($"Received request: {requestJson.Substring(0, Math.Min(100, requestJson.Length))}...");

            // Deserialize request
            var request = JsonHelper.DeserializeRequest(requestJson);
            if (request == null)
            {
                response = JsonHelper.ErrorResponse(
                    null,
                    JsonRpcError.InvalidRequest("Failed to parse JSON-RPC request")
                );
            }
            else
            {
                requestId = request.Id;
                response = await ProcessRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error handling request: {ex.Message}");
            response = JsonHelper.ErrorResponse(
                requestId,
                JsonRpcError.InternalError(ex.Message)
            );
        }

        // Write response to stdout (one line)
        var responseJson = JsonHelper.SerializeResponse(response);
        Console.WriteLine(responseJson);
        await Console.Out.FlushAsync();
    }

    /// <summary>
    /// Process a JSON-RPC request and return response
    /// </summary>
    private async Task<JsonRpcResponse> ProcessRequestAsync(JsonRpcRequest request)
    {
        return request.Method switch
        {
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolCallAsync(request),
            _ => JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.MethodNotFound(request.Method)
            )
        };
    }

    /// <summary>
    /// Handle tools/list - return list of available tools
    /// </summary>
    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        Console.Error.WriteLine("Handling tools/list");

        var result = new ToolsListResult
        {
            Tools = _availableTools
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// Handle tools/call - route to specific tool handler
    /// </summary>
    private async Task<JsonRpcResponse> HandleToolCallAsync(JsonRpcRequest request)
    {
        var toolParams = JsonHelper.ParseToolCallParams(request.Params);

        if (toolParams == null || string.IsNullOrEmpty(toolParams.Name))
        {
            return JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.InvalidParams("Missing tool name")
            );
        }

        Console.Error.WriteLine($"Handling tool call: {toolParams.Name}");

        try
        {
            var result = await ExecuteToolAsync(toolParams);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Tool execution failed: {ex.Message}");
            return JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.InternalError($"Tool execution failed: {ex.Message}")
            );
        }
    }

    /// <summary>
    /// Execute a specific tool by name
    /// </summary>
    private async Task<ToolResult> ExecuteToolAsync(ToolCallParams toolParams)
    {
        // Tool handlers will be implemented in Phase 8
        // For now, return placeholder responses
        return toolParams.Name switch
        {
            "log_time" => PlaceholderToolResult("log_time"),
            "query_time_entries" => PlaceholderToolResult("query_time_entries"),
            "update_time_entry" => PlaceholderToolResult("update_time_entry"),
            "move_task_to_project" => PlaceholderToolResult("move_task_to_project"),
            "delete_time_entry" => PlaceholderToolResult("delete_time_entry"),
            "get_available_projects" => PlaceholderToolResult("get_available_projects"),
            "submit_time_entry" => PlaceholderToolResult("submit_time_entry"),
            _ => throw new InvalidOperationException($"Unknown tool: {toolParams.Name}")
        };
    }

    /// <summary>
    /// Placeholder tool result (will be replaced in Phase 8)
    /// </summary>
    private ToolResult PlaceholderToolResult(string toolName)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.Text($"Tool '{toolName}' not yet implemented (placeholder)")
            }
        };
    }

    /// <summary>
    /// Initialize tool definitions for tools/list
    /// </summary>
    private List<ToolDefinition> InitializeToolDefinitions()
    {
        return new List<ToolDefinition>
        {
            new()
            {
                Name = "log_time",
                Description = "Create a new time entry for tracking work hours",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectCode = new { type = "string", description = "Project code (e.g., INTERNAL)" },
                        task = new { type = "string", description = "Task name (e.g., Development)" },
                        standardHours = new { type = "number", description = "Standard hours worked" },
                        overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                        startDate = new { type = "string", description = "Start date (YYYY-MM-DD)" },
                        completionDate = new { type = "string", description = "Completion date (YYYY-MM-DD)" },
                        description = new { type = "string", description = "Work description (optional)" },
                        issueId = new { type = "string", description = "Issue/ticket ID (optional)" },
                        tags = new { type = "object", description = "Metadata tags (optional)" }
                    },
                    required = new[] { "projectCode", "task", "standardHours", "startDate", "completionDate" }
                }
            },
            new()
            {
                Name = "query_time_entries",
                Description = "Query time entries with filters",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        projectCode = new { type = "string", description = "Filter by project code (optional)" },
                        status = new { type = "string", description = "Filter by status (optional)" },
                        startDate = new { type = "string", description = "Filter from date (optional)" },
                        endDate = new { type = "string", description = "Filter to date (optional)" }
                    }
                }
            },
            new()
            {
                Name = "update_time_entry",
                Description = "Update an existing time entry",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Time entry ID (UUID)" },
                        standardHours = new { type = "number", description = "Standard hours (optional)" },
                        overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                        description = new { type = "string", description = "Description (optional)" }
                    },
                    required = new[] { "id" }
                }
            },
            new()
            {
                Name = "move_task_to_project",
                Description = "Move a time entry to a different project",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        entryId = new { type = "string", description = "Time entry ID (UUID)" },
                        newProjectCode = new { type = "string", description = "New project code" },
                        newTask = new { type = "string", description = "New task name" }
                    },
                    required = new[] { "entryId", "newProjectCode", "newTask" }
                }
            },
            new()
            {
                Name = "delete_time_entry",
                Description = "Delete a time entry (only if not submitted)",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Time entry ID (UUID)" }
                    },
                    required = new[] { "id" }
                }
            },
            new()
            {
                Name = "get_available_projects",
                Description = "List all available projects with their tasks and tags",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        activeOnly = new { type = "boolean", description = "Only return active projects (default: true)" }
                    }
                }
            },
            new()
            {
                Name = "submit_time_entry",
                Description = "Submit a time entry for approval",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Time entry ID (UUID)" }
                    },
                    required = new[] { "id" }
                }
            }
        };
    }
}
```

### 2. Update Program.cs

Update `Program.cs` to use the MCP server:

```csharp
using System;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.Error.WriteLine("TimeReporting MCP Server starting...");

            // Load configuration
            var config = new McpConfig();
            config.Validate();

            // Initialize GraphQL client
            using var graphqlClient = new GraphQLClientWrapper(config);

            // Create and start MCP server
            var server = new McpServer(graphqlClient);

            Console.Error.WriteLine("MCP Server initialized successfully");
            Console.Error.WriteLine("Waiting for requests...");

            // Run server (blocks until Ctrl+C)
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.Error.WriteLine("\nShutting down MCP server...");
                cts.Cancel();
                e.Cancel = true;
            };

            await server.RunAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
```

### 3. Build Project

```bash
cd TimeReportingMcp
dotnet build
```

---

## Testing

### Test 1: tools/list Request

Start the server:

```bash
export GRAPHQL_API_URL="http://localhost:5001/graphql"
export BEARER_TOKEN="test-token-1234567890"

dotnet run
```

In another terminal, send a tools/list request:

```bash
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | dotnet run
```

**Expected output (stdout):**
```json
{"jsonrpc":"2.0","id":1,"result":{"tools":[{"name":"log_time","description":"Create a new time entry for tracking work hours","inputSchema":{...}}, ...]}}
```

### Test 2: tools/call Request (Placeholder)

```bash
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"log_time","arguments":{"projectCode":"INTERNAL","task":"Development","standardHours":8.0,"startDate":"2025-10-28","completionDate":"2025-10-28"}}}' | dotnet run
```

**Expected output (stdout):**
```json
{"jsonrpc":"2.0","id":2,"result":{"content":[{"type":"text","text":"Tool 'log_time' not yet implemented (placeholder)"}]}}
```

### Test 3: Invalid Method

```bash
echo '{"jsonrpc":"2.0","id":3,"method":"unknown_method"}' | dotnet run
```

**Expected output (stdout):**
```json
{"jsonrpc":"2.0","id":3,"error":{"code":-32601,"message":"Method not found: unknown_method"}}
```

### Test 4: Invalid JSON

```bash
echo 'not valid json' | dotnet run
```

**Expected output (stdout):**
```json
{"jsonrpc":"2.0","error":{"code":-32600,"message":"Failed to parse JSON-RPC request"}}
```

---

## Interactive Testing Script

Create `test-mcp.sh` for manual testing:

```bash
#!/bin/bash

# Test MCP Server interactively

echo "Starting MCP Server test..."
export GRAPHQL_API_URL="http://localhost:5001/graphql"
export BEARER_TOKEN="test-token-12345678"

# Start server in background
dotnet run &
MCP_PID=$!

sleep 2

echo ""
echo "Test 1: tools/list"
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' | nc localhost 9999

echo ""
echo "Test 2: tools/call (log_time placeholder)"
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"log_time","arguments":{}}}' | nc localhost 9999

echo ""
echo "Test 3: Invalid method"
echo '{"jsonrpc":"2.0","id":3,"method":"invalid"}' | nc localhost 9999

# Cleanup
kill $MCP_PID
echo "Tests complete"
```

Make executable:
```bash
chmod +x test-mcp.sh
```

---

## Verification Checklist

- [ ] Project builds with no warnings
- [ ] Server starts successfully with valid config
- [ ] Server shows error with missing config
- [ ] tools/list returns 7 tool definitions
- [ ] tools/call returns placeholder responses
- [ ] Invalid method returns error code -32601
- [ ] Invalid JSON returns error code -32600
- [ ] Logging output goes to stderr
- [ ] JSON responses go to stdout
- [ ] Server shuts down gracefully with Ctrl+C

---

## Related Files

**Created:**
- `McpServer.cs` - Core MCP server implementation

**Modified:**
- `Program.cs` - Server initialization and startup

**Test Files:**
- `test-mcp.sh` - Interactive test script (optional)

---

## Next Steps

**Phase 7 Complete!** 🎉

Proceed to **Phase 8: MCP Server - Tools Part 1** to implement the actual tool handlers that call the GraphQL API:

- [Task 8.1: Implement log_time, query_time_entries, update_time_entry](../phase-08-mcp-tools-1/task-8.1-core-tools.md)

---

## Notes

### Stdio Communication

- **stdin:** Read JSON-RPC requests (one per line)
- **stdout:** Write JSON-RPC responses (one per line)
- **stderr:** Logging and debug output

**NEVER write logs to stdout!** It will corrupt the MCP protocol.

### Error Handling

The server handles errors at multiple levels:

1. **JSON parsing errors** → InvalidRequest (-32600)
2. **Unknown methods** → MethodNotFound (-32601)
3. **Invalid parameters** → InvalidParams (-32602)
4. **Tool execution errors** → InternalError (-32603)

### Tool Placeholders

Phase 7 provides placeholder responses for all 7 tools. Phase 8 will replace these with actual GraphQL API calls.

### MCP Protocol Reference

- JSON-RPC 2.0: https://www.jsonrpc.org/specification
- MCP Specification: See Claude Code documentation
- Tool schema: JSON Schema format for input validation

### Cancellation Token

The `CancellationToken` allows graceful shutdown when Ctrl+C is pressed. This ensures:
- In-flight requests complete
- Resources are disposed properly
- Clean exit without hanging

### Testing Strategy

**Phase 7:** Test protocol handling (request/response format)
**Phase 8:** Test actual tool functionality (GraphQL integration)
**Phase 11:** End-to-end testing with Claude Code
