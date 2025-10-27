---
description: Run API tests only
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Run tests for the GraphQL API project only.

### Execution

```bash
.claude/hooks/guard.sh "dotnet test TimeReportingApi.Tests/TimeReportingApi.Tests.csproj --nologo" "slash"
```

### Expected Output

- ✅ All API tests passed
- ❌ Tests failed - Shows failed test details

### Notes

- Only runs API-related tests
- Faster feedback for API changes
