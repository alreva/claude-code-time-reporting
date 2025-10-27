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

### Notes

- This is mainly for manual testing
- Claude Code will automatically start the MCP server via configuration
- Requires API to be running and accessible
- Set environment variables: GRAPHQL_API_URL, BEARER_TOKEN
