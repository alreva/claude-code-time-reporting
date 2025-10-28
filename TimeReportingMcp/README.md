# TimeReporting MCP Server

A lightweight C# console application that bridges Claude Code to the TimeReporting GraphQL API.

## Overview

This MCP server implements the Model Context Protocol (MCP) by:
1. Reading JSON-RPC requests from `stdin`
2. Calling the GraphQL API via HTTP
3. Writing JSON-RPC responses to `stdout`

## Architecture

- **Protocol:** MCP (Model Context Protocol) over stdio
- **Language:** C# .NET 8.0
- **Client:** GraphQL.Client (NuGet)
- **Total Lines:** ~200 lines

## Tools Provided

1. `log_time` - Create time entry
2. `query_time_entries` - Query entries with filters
3. `update_time_entry` - Update existing entry
4. `move_task_to_project` - Move entry to different project
5. `delete_time_entry` - Delete entry
6. `get_available_projects` - List available projects
7. `submit_time_entry` - Submit for approval

## Configuration

Set these environment variables:

- `GRAPHQL_API_URL` - GraphQL endpoint (e.g., http://localhost:5000/graphql)
- `BEARER_TOKEN` - Authentication token

## Usage

```bash
# Run locally
dotnet run --project TimeReportingMcp.csproj

# Or via Claude Code (configured in claude_desktop_config.json)
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

## Development

```bash
# Build
dotnet build

# Run
dotnet run

# Test (Phase 8)
dotnet test
```

## Related

- GraphQL API: `../TimeReportingApi/`
- Architecture: `../docs/prd/architecture.md`
- MCP Tools Spec: `../docs/prd/mcp-tools.md`
