---
description: Stop the running MCP Server
allowed-tools: Bash(pkill:*)
---

Stop the MCP Server process.

### Execution

```bash
pkill -9 -f "TimeReportingMcp"
```

### Expected Output

- Kills all TimeReportingMcp processes
- Frees up MCP server

### Notes

- Use this to clean up hanging MCP processes
- Will exit with status 1 if no matching processes found
