# ADR 0002: C# Mono-Stack Architecture

## Status

**Accepted** (Implemented as v2.0 architecture)

## Context

The original project plan called for a polyglot architecture:
- **GraphQL API**: C# (ASP.NET Core + HotChocolate + Entity Framework)
- **MCP Server**: TypeScript/Node.js with session management, auto-tracking, and context persistence

This multi-language approach added complexity:
- Developers needed proficiency in both C# and TypeScript
- Two separate runtime environments (JVM/.NET + Node.js)
- No code sharing between API and MCP server
- Complex MCP server with 4 phases (15 hours) for session management and auto-tracking

The MCP protocol research revealed a critical insight: **MCP is just JSON-RPC over stdio** - far simpler than originally anticipated.

## Decision

**Use C# for BOTH the GraphQL API and MCP Server. Build the MCP server as a simple console application (~200 lines).**

## Rationale

### Why C# Console App for MCP Server?

1. **MCP Protocol is Simple**: Just read JSON from stdin, call GraphQL API, write JSON to stdout
2. **No Complex State Management Needed**: Each tool invocation is stateless
3. **Code Reusability**: Share models, validation logic, and utilities between API and MCP
4. **Single Runtime**: No Node.js installation required
5. **Team Efficiency**: One language to maintain and learn

### What We Discovered About MCP

**What we thought:**
- Complex TypeScript project with session management
- Auto-tracking engine with heuristics
- Context persistence across invocations
- AI logic for smart suggestions

**What it actually is:**
```csharp
// The entire MCP server pattern:
private async Task<JsonRpcResponse> LogTime(Dictionary<string, JsonElement> args)
{
    var mutation = new GraphQLRequest
    {
        Query = @"mutation LogTime($input: LogTimeInput!) {
            logTime(input: $input) { id status }
        }",
        Variables = new { input = args }
    };

    var response = await _graphqlClient.SendMutationAsync<LogTimeResponse>(mutation);

    return new JsonRpcResponse
    {
        Result = new { content = new[] {
            new { type = "text", text = $"Created entry {response.Data.LogTime.Id}" }
        }}
    };
}
```

Multiply this by 7 tools = complete MCP server!

## Consequences

### Benefits

✅ **Dramatic Complexity Reduction**
- Tasks: 60 → 42 tasks (-30%)
- Hours: 52-65 → 40-51 hours (-23%)
- MCP phases: 4 phases (15 hrs) → 3 phases (5 hrs) (-67%)

✅ **Single Language Stack**
- One language to learn and maintain (C#)
- Shared models between API and MCP
- Consistent code patterns across entire system
- Easier onboarding for new developers

✅ **Simplified MCP Server**
- ~200 lines total (vs. 500+ in TypeScript with session management)
- 7 simple tool handlers (10-20 lines each)
- Stateless design (no session management complexity)
- Just calls GraphQL API (thin wrapper)

✅ **Faster Development**
- Full-time: 6.5-8 days → 5-6 days (-23%)
- Part-time: 13-16 days → 10-13 days (-23%)
- Side project: 26-32 days → 20-25 days (-23%)

✅ **Easier Deployment**
- Single runtime environment (.NET)
- MCP server is just an executable
- No Node.js dependencies to manage

### Costs

⚠️ **Features Moved to v2**
- Auto-tracking heuristics (not essential for v1)
- Session context persistence (not required by MCP protocol)
- Duration estimation (can be added later)
- Smart suggestions (nice-to-have, not core)

⚠️ **Different Ecosystem**
- C# GraphQL client instead of mature Node.js ecosystem
- Smaller community for C# MCP servers (we're early adopters)

⚠️ **Console App Constraints**
- No web UI for MCP server debugging (vs. Node.js tooling)
- Limited to stdio communication (but that's all MCP needs)

### Trade-off Assessment

**Decision: Simplicity over feature-richness for v1.**

The removed features (auto-tracking, session persistence) are not required by the MCP protocol or Claude Code integration. They can be added in v2 if user feedback shows they're valuable. Getting to a working v1 faster is more important.

## Implementation

### Project Structure

```
time-reporting-system/
├── TimeReportingApi/              # C# GraphQL API
│   ├── Models/
│   ├── GraphQL/
│   ├── Services/
│   └── Data/
│
├── TimeReportingMcp/              # C# MCP Server (NEW!)
│   ├── Program.cs                 # Main entry point
│   ├── McpServer.cs               # JSON-RPC stdio handler
│   ├── Tools/                     # 7 tool handlers
│   │   ├── LogTimeHandler.cs
│   │   ├── QueryTimeEntriesHandler.cs
│   │   ├── UpdateTimeEntryHandler.cs
│   │   ├── MoveTaskToProjectHandler.cs
│   │   ├── DeleteTimeEntryHandler.cs
│   │   ├── GetAvailableProjectsHandler.cs
│   │   └── SubmitTimeEntryHandler.cs
│   └── Models/                    # JSON-RPC models
│       ├── JsonRpcRequest.cs
│       ├── JsonRpcResponse.cs
│       └── GraphQLModels.cs
```

### MCP Server Architecture

```
Claude Code (Natural Language)
    ↓ stdio (JSON-RPC)
C# MCP Server (Console App ~200 lines)
    ↓ HTTP/GraphQL
C# GraphQL API (ASP.NET Core + HotChocolate)
    ↓ Entity Framework
PostgreSQL Database
```

### Tool Handler Pattern

Each of the 7 tools follows this pattern:

1. **Parse JSON-RPC request** from stdin
2. **Map to GraphQL query/mutation**
3. **Call GraphQL API** via HttpClient
4. **Format response** as JSON-RPC
5. **Write to stdout**

**Lines of code per tool**: 10-20 lines
**Total MCP server**: ~200 lines

### Shared Code Between API and MCP

```csharp
// Shared models (can reference TimeReportingApi project)
public class LogTimeInput
{
    public string ProjectCode { get; set; }
    public string Task { get; set; }
    public decimal StandardHours { get; set; }
    // ... same model used by both API and MCP
}
```

## Alternatives Considered

### Alternative 1: TypeScript/Node.js MCP Server (Original Plan)

**Approach**: Build MCP server in TypeScript with full session management and auto-tracking.

**Why rejected:**
- Over-engineered for MCP protocol requirements
- Added 15 hours of complexity that isn't essential
- Required polyglot architecture (C# + TypeScript)
- MCP protocol is simpler than we thought - doesn't need complex state management

### Alternative 2: Python MCP Server

**Approach**: Use Python for MCP server (another common choice for MCP servers).

**Why rejected:**
- Still polyglot architecture (C# + Python)
- Python not as strong for typed GraphQL clients
- Team already proficient in C#
- No significant advantage over C# console app

### Alternative 3: Build MCP Server Inside GraphQL API

**Approach**: Add MCP stdio endpoint to the ASP.NET Core API itself.

**Why rejected:**
- Mixing concerns (HTTP API + stdio MCP in same process)
- Deployment complexity (API runs in Docker, MCP needs to run on host)
- Can't restart API without restarting MCP server
- Harder to debug and test separately

## References

- Original document: `docs/IMPLEMENTATION-SUMMARY.md`
- MCP Protocol Specification: [Model Context Protocol](https://modelcontextprotocol.io)
- Detailed architecture: `docs/prd/architecture.md` Section 2.2
