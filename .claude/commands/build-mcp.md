---
description: Build the MCP Server project only
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Build only the TimeReportingMcp project.

### Execution

```bash
.claude/hooks/guard.sh "dotnet build TimeReportingMcp/TimeReportingMcp.csproj /p:TreatWarningsAsErrors=true" "slash"
```

### Expected Output

- ✅ Build succeeded - MCP Server compiled successfully
- ❌ Build failed - Shows compilation errors and warnings

### Notes

- Treats warnings as errors
- Only builds the MCP Server project (faster for MCP-only changes)
