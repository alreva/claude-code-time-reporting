---
description: Build the GraphQL API project only
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Build only the TimeReportingApi project.

### Execution

```bash
.claude/hooks/guard.sh "dotnet build TimeReportingApi/TimeReportingApi.csproj /p:TreatWarningsAsErrors=true" "slash"
```

### Expected Output

- ✅ Build succeeded - API compiled successfully
- ❌ Build failed - Shows compilation errors and warnings

### Notes

- Treats warnings as errors
- Only builds the API project (faster for API-only changes)
