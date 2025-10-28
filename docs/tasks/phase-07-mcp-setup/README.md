# Phase 7: MCP Server - Setup

**Status:** Pending
**Estimated Time:** 3 hours
**Dependencies:** Phase 6 complete (GraphQL API running in Docker)

---

## Overview

Phase 7 sets up the MCP (Model Context Protocol) server - a lightweight C# console application that bridges Claude Code to your GraphQL API. This is **THE SIMPLE PART** of the project!

### What You're Building

A ~200 line C# console app that:
1. Reads JSON-RPC requests from `stdin`
2. Routes to tool handlers
3. Calls your GraphQL API
4. Writes JSON-RPC responses to `stdout`

**That's it!** No complex session management, no auto-tracking, just simple stdio communication.

---

## Architecture

```
Claude Code (AI Assistant)
    ↓ stdio (JSON-RPC)
McpServer.cs (Console App)
    ↓ GraphQL.Client
GraphQL API (Your ASP.NET Core API)
    ↓ Entity Framework
PostgreSQL Database
```

---

## Tasks

### Task 7.1: MCP Project Initialization (1 hour)
**Goal:** Create .NET Console Application project

**What you'll do:**
- Run `dotnet new console -n TimeReportingMcp`
- Set up directory structure (`Models/`, `Tools/`, `Utils/`)
- Create basic `Program.cs` entry point
- Add to solution file

**Key files created:**
- `TimeReportingMcp/TimeReportingMcp.csproj`
- `TimeReportingMcp/Program.cs`
- `TimeReportingMcp/README.md`

[Start Task 7.1 →](./task-7.1-mcp-project-init.md)

---

### Task 7.2: Dependencies and GraphQL Client (30 min)
**Goal:** Install NuGet packages and configure GraphQL client

**What you'll do:**
- Install `GraphQL.Client` and `GraphQL.Client.Serializer.SystemTextJson`
- Create `McpConfig.cs` (reads env vars: GRAPHQL_API_URL, BEARER_TOKEN)
- Create `GraphQLClientWrapper.cs` (wraps GraphQL client with auth)
- Test configuration loading

**Key files created:**
- `Utils/McpConfig.cs` - Configuration management
- `Utils/GraphQLClientWrapper.cs` - GraphQL client with Bearer token auth

[Start Task 7.2 →](./task-7.2-dependencies.md)

---

### Task 7.3: JSON-RPC Models (30 min)
**Goal:** Define models for MCP protocol communication

**What you'll do:**
- Create `JsonRpcRequest` and `JsonRpcResponse` models
- Create `ToolCallParams` and `ToolResult` models
- Create `ContentItem` and `ToolDefinition` models
- Create `JsonHelper` utilities for serialization
- Test serialization/deserialization

**Key files created:**
- `Models/JsonRpcRequest.cs` - Request structure
- `Models/JsonRpcResponse.cs` - Response and error structures
- `Models/McpContent.cs` - Content items
- `Models/ToolDefinition.cs` - Tool metadata
- `Utils/JsonHelper.cs` - JSON utilities

[Start Task 7.3 →](./task-7.3-json-rpc-models.md)

---

### Task 7.4: MCP Server Core (1 hour)
**Goal:** Implement stdio handler and request routing

**What you'll do:**
- Create `McpServer.cs` with stdio loop
- Implement `tools/list` handler (returns tool definitions)
- Implement `tools/call` router (placeholder handlers)
- Add error handling for invalid requests
- Test with manual JSON-RPC requests

**Key files created:**
- `McpServer.cs` - Main server with stdio handling

**Result:** Working MCP server that can:
- ✅ List available tools
- ✅ Route tool calls (returns placeholders)
- ✅ Handle errors gracefully
- ✅ Communicate via stdio

[Start Task 7.4 →](./task-7.4-mcp-server.md)

---

## Project Structure After Phase 7

```
TimeReportingMcp/
├── Models/
│   ├── JsonRpcRequest.cs         # JSON-RPC request models
│   ├── JsonRpcResponse.cs        # JSON-RPC response models
│   ├── McpContent.cs             # Content item models
│   └── ToolDefinition.cs         # Tool metadata models
├── Tools/                        # (empty - filled in Phase 8)
├── Utils/
│   ├── McpConfig.cs              # Configuration management
│   ├── GraphQLClientWrapper.cs   # GraphQL client wrapper
│   └── JsonHelper.cs             # JSON serialization helpers
├── McpServer.cs                  # Core server (stdio handler)
├── Program.cs                    # Entry point
├── README.md                     # Project documentation
└── TimeReportingMcp.csproj       # Project file
```

---

## Key Concepts

### MCP Protocol

**MCP = Model Context Protocol**
- JSON-RPC 2.0 over stdio
- One request per line (stdin)
- One response per line (stdout)
- Logging goes to stderr

**Two methods:**
1. `tools/list` - Returns available tools
2. `tools/call` - Executes a specific tool

### JSON-RPC Example

**Request (stdin):**
```json
{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"log_time","arguments":{"projectCode":"INTERNAL","task":"Development","standardHours":8.0,"startDate":"2025-10-28","completionDate":"2025-10-28"}}}
```

**Response (stdout):**
```json
{"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"Time entry created successfully"}]}}
```

### Stdio Communication

- **stdin** (`Console.In`) - Read JSON-RPC requests
- **stdout** (`Console.Out`) - Write JSON-RPC responses
- **stderr** (`Console.Error`) - Logging and debug output

**CRITICAL:** Never write logs to stdout! It corrupts the protocol.

---

## Tools Provided (7 total)

Phase 7 provides **placeholder** implementations. Phase 8 adds real GraphQL calls.

1. **log_time** - Create time entry
2. **query_time_entries** - Query entries with filters
3. **update_time_entry** - Update existing entry
4. **move_task_to_project** - Move entry to different project
5. **delete_time_entry** - Delete entry
6. **get_available_projects** - List projects
7. **submit_time_entry** - Submit for approval

---

## Testing Strategy

### Phase 7 Testing
✅ Test protocol handling (request/response format)
✅ Test tools/list returns 7 tool definitions
✅ Test tools/call routes to placeholder handlers
✅ Test error handling (invalid JSON, unknown methods)

### Phase 8 Testing (Next)
✅ Test actual GraphQL API calls
✅ Test data validation
✅ Test error responses from API

### Phase 11 Testing (Later)
✅ End-to-end testing with Claude Code
✅ Integration testing with full stack

---

## Configuration

Set these environment variables before running:

```bash
export GRAPHQL_API_URL="http://localhost:5000/graphql"
export BEARER_TOKEN="your-token-from-.env-file"
```

**For Claude Code integration (later):**
```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/TimeReportingMcp/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5000/graphql",
        "BEARER_TOKEN": "your-token-here"
      }
    }
  }
}
```

---

## Common Pitfalls to Avoid

### ❌ Don't Overthink It
MCP is just JSON-RPC over stdio. No need for complex frameworks or session management.

### ❌ Don't Log to Stdout
Use `Console.Error.WriteLine()` for logs, not `Console.WriteLine()`.

### ❌ Don't Add Auto-Tracking Yet
v1 is explicit tool calls only. Auto-tracking is a v2 feature.

### ❌ Don't Use Async Main Wrong
```csharp
// ✅ Correct
static async Task Main(string[] args)
{
    await server.RunAsync();
}

// ❌ Wrong
static void Main(string[] args)
{
    server.RunAsync().Wait();  // Can deadlock
}
```

---

## Success Criteria

By the end of Phase 7, you should have:

- [ ] .NET Console project compiles successfully
- [ ] GraphQL client connects to API
- [ ] MCP server starts and listens on stdin
- [ ] `tools/list` returns 7 tool definitions
- [ ] `tools/call` routes to placeholder handlers
- [ ] Error handling works (invalid JSON, unknown methods)
- [ ] Server shuts down gracefully with Ctrl+C
- [ ] All logging goes to stderr, responses to stdout

---

## Next Phase

**Phase 8: MCP Server - Tools Part 1**

Replace placeholder handlers with actual GraphQL API calls for:
- log_time
- query_time_entries
- update_time_entry
- move_task_to_project
- delete_time_entry
- get_available_projects
- submit_time_entry

Each tool is 10-20 lines of code. Total: ~140 lines.

---

## Resources

### Documentation
- [Architecture Overview](../../prd/architecture.md) - See Section 2.2 for MCP server details
- [MCP Tools Specification](../../prd/mcp-tools.md) - Tool schemas and examples
- [Implementation Summary](../../IMPLEMENTATION-SUMMARY.md) - Why C# instead of TypeScript

### Task Files
- [Task 7.1: MCP Project Init](./task-7.1-mcp-project-init.md)
- [Task 7.2: Dependencies](./task-7.2-dependencies.md)
- [Task 7.3: JSON-RPC Models](./task-7.3-json-rpc-models.md)
- [Task 7.4: MCP Server Core](./task-7.4-mcp-server.md)

### External References
- JSON-RPC 2.0: https://www.jsonrpc.org/specification
- GraphQL.Client: https://github.com/graphql-dotnet/graphql-client
- MCP Protocol: See Claude Code documentation

---

## Questions?

If you encounter issues:
1. Check task file troubleshooting sections
2. Review architecture.md for code examples
3. Verify GraphQL API is running (`/run-api`)
4. Check environment variables are set
5. Look at stderr logs for error details

---

**Ready to start? Begin with [Task 7.1: MCP Project Init](./task-7.1-mcp-project-init.md)!**
