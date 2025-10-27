---
description: Run all tests (API + MCP Server)
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Run all tests for the entire solution.

### Execution

```bash
.claude/hooks/guard.sh "dotnet test --nologo" "slash"
```

### Expected Output

- ✅ All tests passed (X total)
- ❌ Tests failed - Shows failed test details

### Notes

- Runs all test projects in the solution
- Use /test-api or /test-mcp for targeted testing
