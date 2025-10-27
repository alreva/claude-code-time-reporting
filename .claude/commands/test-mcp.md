---
description: Run MCP Server tests only
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Run tests for the MCP Server project only.

### Execution

```bash
.claude/hooks/guard.sh "dotnet test TimeReportingMcp.Tests/TimeReportingMcp.Tests.csproj --nologo" "slash"
```

### Expected Output

- ✅ All MCP tests passed
- ❌ Tests failed - Shows failed test details

### Notes

- Only runs MCP-related tests
- Faster feedback for MCP changes
