---
description: Stop the running GraphQL API
allowed-tools: Bash(pkill:*)
---

Stop the GraphQL API application running on port 5000.

### Execution

```bash
pkill -9 -f "TimeReportingApi"
```

### Expected Output

- Kills all TimeReportingApi and dotnet watch processes
- Frees up port 5000

### Notes

- Use this to clean up and resolve port conflicts
- Will exit with status 1 if no matching processes found
