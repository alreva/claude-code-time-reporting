---
description: Run the MCP Server
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Run the MCP Server for testing (normally started by Claude Code automatically).

### Execution

```bash
.claude/hooks/guard.sh "dotnet run --project TimeReportingMcp" "slash"
```

### Expected Output

- MCP Server starts and listens on stdio
- Waits for JSON-RPC requests
- Shows configuration loaded message
- Acquires Azure AD token from Azure CLI

### Notes

- This is mainly for manual testing
- Claude Code will automatically start the MCP server via `.mcp.json` configuration
- Requires API to be running and accessible
- Requires environment variables to be set: `source env.sh` first
- Environment variable needed: `GRAPHQL_API_URL`
- **Authentication**: Requires `az login` - MCP server uses Azure CLI credentials
