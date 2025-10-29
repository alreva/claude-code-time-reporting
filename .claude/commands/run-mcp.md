---
description: Run the MCP Server
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Run the MCP Server for testing (normally started by Claude Code automatically).

### Execution

```bash
./run-mcp.sh
```

### Expected Output

- MCP Server starts and listens on stdio
- Waits for JSON-RPC requests
- Shows configuration loaded message

### Notes

- This is mainly for manual testing
- Claude Code will automatically start the MCP server via `.mcp.json` configuration
- Requires API to be running and accessible
- Requires environment variables to be set: `source env.sh` first
- Environment variables needed: `GRAPHQL_API_URL`, `Authentication__BearerToken`
