---
description: Display comprehensive project status report
allowed-tools: Bash(.claude/hooks/project-status.sh:*)
---

Display a comprehensive project status report that fits in a terminal screen.

### Execution

```bash
.claude/hooks/project-status.sh
```

### Report Sections

- **Git Status** - Current branch, last commit, working tree changes
- **Build Status** - API and test project build status with timestamps
- **Database Status** - PostgreSQL container status and table count
- **Task Progress** - Completed vs total tasks with progress bar
- **Project Statistics** - C# files, lines of code, test files
- **Configuration** - SDK version, package management, framework

### Expected Output

A formatted, color-coded status report showing:
- ✅ Green indicators for healthy/complete items
- ⚠️ Yellow indicators for warnings or pending items
- ❌ Red indicators for errors or missing items

### Notes

- Report is designed to fit on a standard Mac terminal screen
- Uses Unicode box-drawing characters for clean formatting
- Automatically clears screen before displaying
- Works with both Docker and Podman environments
