# ⚠️  DEPRECATED: TimeReporting MCP Server (stdio)

**This stdio-based MCP server is deprecated and should not be used.**

**Issue:** The stdio-based MCP server does not work properly with Azure Entra ID token acquisition. Token refresh fails due to stdio transport limitations.

**Replacement:** Use `TimeReportingMcp.WebSocket` instead, which:
- Runs as a containerized service on port 5002
- Properly handles Azure Entra ID token acquisition
- Uses WebSocket transport with StreamJsonRpc
- Integrates with the full Docker stack (postgres + api + mcp)

**How to use the new WebSocket MCP:**
1. Start the stack: `podman compose up -d`
2. MCP server available at: `ws://localhost:5002/mcp`
3. Claude Code connects automatically via `.mcp.json` configuration

---

# TimeReporting MCP Server (stdio) - LEGACY

A lightweight C# console application that bridges Claude Code to the TimeReporting GraphQL API.

## Overview

This MCP server implements the Model Context Protocol (MCP) by:
1. Reading JSON-RPC requests from `stdin`
2. Calling the GraphQL API via HTTP
3. Writing JSON-RPC responses to `stdout`

## Architecture

- **Protocol:** MCP (Model Context Protocol) over stdio
- **Language:** C# .NET 10.0
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

The MCP server uses .NET Configuration system to read from environment variables:

- `GRAPHQL_API_URL` - GraphQL endpoint (e.g., http://localhost:5001/graphql)
- `Authentication__BearerToken` - Authentication token (maps to `Authentication:BearerToken` config key)

**Note:** The double-underscore (`__`) in `Authentication__BearerToken` is .NET's convention for representing nested configuration (maps to `Authentication:BearerToken`).

## Usage

### Via Shell (Recommended)

```bash
# 1. Generate token and load environment
./setup.sh
source env.sh

# 2. Run MCP server (wrapper script loads environment)
./run-mcp.sh
```

### Via Claude Code

The repository includes `.mcp.json` which calls the `run-mcp.sh` wrapper:

```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "./run-mcp.sh"
    }
  }
}
```

The wrapper script validates that environment variables are set before starting the server.

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
