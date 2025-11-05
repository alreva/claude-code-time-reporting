# Task 7.1: MCP Project Initialization

**Phase:** 7 - MCP Server Setup
**Estimated Time:** 1 hour
**Prerequisites:** Phase 6 complete (GraphQL API running in Docker)
**Status:** ✅ Complete

---

## Objective

Initialize a C# .NET Console Application project for the MCP server with proper structure and dependencies.

---

## Acceptance Criteria

- [ ] .NET Console project created (`TimeReportingMcp.csproj`)
- [ ] Project targets .NET 10.0
- [ ] Basic project structure in place
- [ ] Project builds successfully with `dotnet build`
- [ ] Solution file updated to include MCP project
- [ ] README created explaining MCP server purpose

---

## Implementation Steps

### 1. Create Console Application Project

```bash
# From repository root
cd /path/to/time-reporting-system

# Create new console app
dotnet new console -n TimeReportingMcp -f net8.0

# Verify project was created
ls TimeReportingMcp/
```

**Expected output:**
```
TimeReportingMcp/
├── Program.cs
├── TimeReportingMcp.csproj
└── obj/
```

### 2. Configure Project File

Update `TimeReportingMcp/TimeReportingMcp.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>TimeReportingMcp</RootNamespace>
  </PropertyGroup>

  <!-- NuGet packages will be added in Task 7.2 -->

</Project>
```

### 3. Create Project Structure

```bash
cd TimeReportingMcp

# Create directories
mkdir Models
mkdir Tools
mkdir Utils
```

**Final structure:**
```
TimeReportingMcp/
├── Models/           # JSON-RPC request/response models
├── Tools/            # 7 tool implementations (Phase 8)
├── Utils/            # Helper utilities
├── Program.cs        # Entry point
└── TimeReportingMcp.csproj
```

### 4. Create Basic Program.cs

Replace the default `Program.cs` with:

```csharp
using System;

namespace TimeReportingMcp;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Error.WriteLine("TimeReporting MCP Server starting...");

        // Placeholder - will implement McpServer in Task 7.2
        Console.Error.WriteLine("MCP Server initialized");

        // Keep process alive
        await Task.Delay(Timeout.Infinite);
    }
}
```

**Note:** Using `Console.Error` for logging so it doesn't interfere with stdio communication (which uses `Console.In` / `Console.Out`).

### 5. Add to Solution (Optional)

If you have a solution file:

```bash
# From repository root
dotnet sln add TimeReportingMcp/TimeReportingMcp.csproj
```

### 6. Verify Build

```bash
cd TimeReportingMcp
dotnet restore
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 7. Create MCP Server README

Create `TimeReportingMcp/README.md`:

```markdown
# TimeReporting MCP Server

A lightweight C# console application that bridges Claude Code to the TimeReporting GraphQL API.

## Overview

This MCP server implements the Model Context Protocol (MCP) by:
1. Reading JSON-RPC requests from `stdin`
2. Calling the GraphQL API via HTTP
3. Writing JSON-RPC responses to `stdout`

## Architecture

- **Protocol:** MCP (Model Context Protocol) over stdio
- **Language:** C# .NET 10.0
- **Client:** GraphQL.Client (NuGet)
- **Total Lines:** ~200 lines

## Tools Provided

1. `log_time` - Create time entry
2. `query_time_entries` - Query entries with filters
3. `update_time_entry` - Update existing entry
4. `move_task_to_project` - Move entry to different project
5. `delete_time_entry` - Delete entry
6. `get_available_projects` - List available projects
7. `submit_time_entry` - Submit for approval

## Configuration

Set these environment variables:

- `GRAPHQL_API_URL` - GraphQL endpoint (e.g., http://localhost:5001/graphql)
- `Azure AD via AzureCliCredential` - Authentication token

## Usage

```bash
# Run locally
dotnet run --project TimeReportingMcp.csproj

# Or via Claude Code (configured in claude_desktop_config.json)
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/TimeReportingMcp/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Azure AD via AzureCliCredential": "your-token-here"
      }
    }
  }
}
```

## Development

```bash
# Build
dotnet build

# Run
dotnet run

# Test (Phase 8)
dotnet test
```

## Related

- GraphQL API: `../TimeReportingApi/`
- Architecture: `../docs/prd/architecture.md`
- MCP Tools Spec: `../docs/prd/mcp-tools.md`
```

---

## Testing

### Manual Test

```bash
# Build project
cd TimeReportingMcp
dotnet build

# Verify output
ls bin/Debug/net8.0/
# Should see: TimeReportingMcp.dll, TimeReportingMcp.exe (on Windows), etc.
```

### Test Run

```bash
# Run the app
dotnet run

# Expected output on stderr:
# TimeReporting MCP Server starting...
# MCP Server initialized

# Press Ctrl+C to stop
```

---

## Related Files

**Created:**
- `TimeReportingMcp/TimeReportingMcp.csproj`
- `TimeReportingMcp/Program.cs`
- `TimeReportingMcp/README.md`
- `TimeReportingMcp/Models/` (empty directory)
- `TimeReportingMcp/Tools/` (empty directory)
- `TimeReportingMcp/Utils/` (empty directory)

**Modified:**
- `*.sln` (if solution file exists)

---

## Next Steps

Proceed to [Task 7.2: Install Dependencies](./task-7.2-dependencies.md) to add GraphQL client and JSON libraries.

---

## Notes

- **Simplicity is key:** The MCP server is intentionally minimal (~200 lines total)
- **No auto-tracking:** v1 focuses on explicit tool calls only
- **No session management:** Each tool call is stateless
- **Error handling:** Keep it simple - let exceptions bubble up to MCP protocol layer

**Remember:** MCP is just JSON-RPC over stdio. Don't overthink it!
