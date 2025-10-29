---
description: Run the GraphQL API with hot reload
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Run the GraphQL API application with hot reload enabled.

### Execution

```bash
.claude/hooks/guard.sh "dotnet watch --project TimeReportingApi run" "slash"
```

### Expected Output

- Server starts on http://localhost:5001
- GraphQL Playground available at http://localhost:5001/graphql
- Hot reload enabled - file changes trigger automatic recompilation

### Notes

- Use /stop-api to stop the server
- Requires PostgreSQL to be running (/db-start)
- Access GraphQL Playground in your browser for testing
