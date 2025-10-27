---
description: Build the entire solution (API + MCP Server)
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Build both the GraphQL API and MCP Server projects.

### Execution

```bash
.claude/hooks/guard.sh "dotnet build" "slash"
```

### Expected Output

- ✅ Build succeeded - Both projects compiled successfully
- ❌ Build failed - Shows compilation errors and warnings

### Notes

- Treats warnings as errors (zero-warning policy)
- Builds both TimeReportingApi and TimeReportingMcp projects
