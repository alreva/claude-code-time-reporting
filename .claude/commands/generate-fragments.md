---
description: Auto-generate GraphQL fragments from schema
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Auto-generate GraphQL fragments from the API schema using schema introspection.

### Execution

```bash
.claude/hooks/guard.sh "dotnet run --project TimeReportingMcpSdk.Tools -- generate-fragments --schema TimeReportingMcpSdk/schema.graphql --output TimeReportingMcpSdk/GraphQL/Fragments.graphql" "slash"
```

### Expected Output

- ✅ Generated fragments in TimeReportingMcpSdk/GraphQL/Fragments.graphql
- ❌ Error with schema parsing details

### Notes

- Automatically includes ALL fields from TimeEntry, Project, ProjectTask, and related types
- Max depth of 2 levels to prevent circular references
- Overwrites existing Fragments.graphql file
- Run this after updating schema.graphql
