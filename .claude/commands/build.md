---
description: Build the entire solution (API + MCP Server)
allowed-tools:
  - Bash(.claude/hooks/guard.sh)
---

# 🏗️ Build the API and MCP Server

Runs the `.claude/hooks/guard.sh` script, which builds both the **TimeReportingApi** and **TimeReportingMcp** projects.  
All warnings are treated as errors.

---

## 🧩 Execution

<toolcall>

```Bash
./.claude/hooks/guard.sh "dotnet build" "slash"
```

</toolcall>

---

## ✅ Expected Output

- ✅ **Build succeeded** – Both projects compiled successfully  
- ❌ **Build failed** – Shows compilation errors and warnings
