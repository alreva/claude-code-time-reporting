# ADR 0010: stdio/JSON-RPC Transport for MCP Server

## Status

**Accepted**

## Context

The Time Reporting system includes an MCP (Model Context Protocol) server that enables Claude Code to interact with the GraphQL API through natural language commands. The MCP server acts as a bridge between Claude Code and the time tracking backend.

**Key Requirements:**
- Enable Claude Code to create, query, and manage time entries via natural language
- Integrate with existing GraphQL API (no duplicate business logic)
- Support Azure Entra ID authentication for user identity tracking
- Simple to develop, deploy, and maintain
- Works seamlessly with Claude Code CLI

**Transport Options Considered:**

1. **stdio (Standard Input/Output)**
   - Claude Code launches MCP server as subprocess
   - Communication via stdin/stdout using JSON-RPC 2.0
   - Process-based isolation

2. **WebSocket**
   - Long-lived bidirectional connection
   - MCP server runs as separate service (ASP.NET Core)
   - Network-based communication

3. **HTTP with SSE (Server-Sent Events)**
   - HTTP POST for requests, SSE for server-to-client notifications
   - Separate request/response channels

## Decision

**Use stdio/JSON-RPC transport for the MCP server.**

The MCP server is implemented as a simple C# console application that:
- Reads JSON-RPC requests from stdin
- Calls the GraphQL API with Azure Entra ID tokens
- Writes JSON-RPC responses to stdout
- Uses StrawberryShake for strongly-typed GraphQL client

## Rationale

### Why stdio over WebSocket?

**Simplicity:**
- ✅ **Zero infrastructure**: No web server, no ports, no network configuration
- ✅ **Process lifecycle**: Claude Code manages startup/shutdown automatically
- ✅ **Native support**: Claude Code's primary transport is stdio (best documented, most mature)
- ✅ **Minimal code**: ~200 lines total vs. ASP.NET Core + WebSocket (~500+ lines)

**Developer Experience:**
- ✅ **No deployment ceremony**: Just `dotnet run`, no service hosting
- ✅ **Easy debugging**: Attach debugger to subprocess, view logs in console
- ✅ **Familiar pattern**: Standard console app, no framework-specific concepts
- ✅ **Fast iteration**: Modify code → restart Claude Code → test immediately

**Authentication Works Perfectly:**
- ✅ **AzureCliCredential in subprocess**: Can access `~/.azure/` token cache from subprocess
- ✅ **Token pass-through**: MCP reads token via Azure.Identity, passes to API
- ✅ **User tracking**: API extracts user claims (oid, email, name) from validated token
- ✅ **No network security**: Process boundary provides isolation

**Meets All Requirements:**
- ✅ **Local development focus**: Perfect for single-developer local use case
- ✅ **User identity preserved**: Azure Entra ID token contains cryptographically signed user claims
- ✅ **No multi-user needed**: Each developer runs their own MCP instance
- ✅ **Stateless design**: Each tool invocation is independent (no session management)

### Why NOT WebSocket?

**Unnecessary Complexity:**
- ❌ **Infrastructure overhead**: Requires ASP.NET Core host, port management, network config
- ❌ **Deployment burden**: Must run as service, manage lifecycle separately from Claude Code
- ❌ **No current need**: Single-developer local use case doesn't need remote server
- ❌ **More code to maintain**: WebSocket handler, connection management, StreamJsonRpc setup

**Network Adds No Value:**
- ❌ **Latency similar**: stdio pipe vs localhost socket both <1ms
- ❌ **No remote access needed**: Developers work locally, no shared server requirement
- ❌ **Security complexity**: Must secure WebSocket endpoint (though localhost-only reduces this)

**YAGNI (You Aren't Gonna Need It):**
- The WebSocket approach was designed for a hypothetical future requirement (remote/multi-user deployment)
- Current requirements are fully satisfied by stdio
- If remote deployment becomes necessary in the future, WebSocket can be added without breaking stdio implementation
- Prefer simple solution for known requirements over complex solution for speculative requirements

### Why NOT HTTP/SSE?

- ❌ **Two channels**: Requires managing both HTTP POST and SSE connections
- ❌ **More overhead**: HTTP headers on every request vs. persistent stdio pipe
- ❌ **Less natural fit**: Request/response pattern requires adapting to SSE event stream
- ❌ **Same deployment burden as WebSocket**: Requires ASP.NET Core host, port management

## Consequences

### Benefits

✅ **Extreme Simplicity**
- Minimal infrastructure: just a console app
- No service hosting, no port configuration, no network concerns
- Easy to understand, easy to debug, easy to maintain

✅ **Fast Development**
- Rapid iteration: modify → restart → test
- No deployment ceremony
- Familiar console app pattern

✅ **Perfect Claude Code Integration**
- Native transport (stdio is Claude Code's primary protocol)
- Automatic process management (Claude Code starts/stops MCP server)
- No additional configuration beyond command and args

✅ **User Identity Tracking Works**
- AzureCliCredential accesses token cache from subprocess
- API validates tokens and extracts user claims
- Each time entry includes user_id, user_email, user_name
- Audit trail for accountability and billing

✅ **Zero Deployment Complexity**
- Developers just run Claude Code
- No separate service to deploy or monitor
- No port conflicts or network issues

✅ **Lightweight Resource Usage**
- Subprocess spawns only when Claude Code needs it
- Exits cleanly when done
- No persistent service consuming resources

### Costs

⚠️ **Local-Only**
- Cannot deploy MCP server remotely (requires subprocess on same machine)
- Each developer runs their own instance (no shared server)
- **Mitigation**: This is acceptable for current use case (single-developer local development)

⚠️ **Process Startup Overhead**
- MCP server starts on first tool invocation (~100-500ms)
- Subsequent calls are fast (server stays running)
- **Mitigation**: Startup time is acceptable for infrequent tool invocations

⚠️ **No Centralized Monitoring**
- Each developer's MCP server is independent
- Cannot view metrics across all users
- **Mitigation**: API logs capture all time entries with user identity (centralized audit trail)

### Trade-off Assessment

**Decision: stdio perfectly matches current requirements with minimal complexity.**

**Justification:**
- **Local development is the use case**: Developers work locally, no need for remote deployment
- **User tracking works**: Azure Entra ID token pass-through from subprocess to API
- **Simplicity wins**: stdio is dramatically simpler than WebSocket with no functional trade-off
- **YAGNI principle**: Build for current requirements, not speculative future requirements

The stdio approach delivers all required functionality with a fraction of the complexity of WebSocket. If remote deployment becomes necessary in the future, it can be added without breaking the existing stdio implementation.

## Implementation

### 1. Project Structure

```
TimeReportingMcp/
├── Program.cs                    # Entry point, stdio message loop
├── McpServer.cs                  # JSON-RPC handler with tool methods
├── Tools/                        # Tool handlers (7 tools)
│   ├── LogTimeTool.cs
│   ├── QueryEntresTool.cs
│   ├── UpdateEntryTool.cs
│   ├── MoveTaskTool.cs
│   ├── DeleteEntryTool.cs
│   ├── GetProjectsTool.cs
│   └── SubmitEntryTool.cs
├── Services/
│   └── TokenService.cs           # AzureCliCredential token acquisition
├── GraphQL/                      # StrawberryShake generated code
│   ├── *.graphql                 # Operation definitions
│   └── schema.graphql            # API schema
└── TimeReportingMcp.csproj
```

### 2. stdio Message Loop (Program.cs)

```csharp
using System.Text.Json;

// Read JSON-RPC requests from stdin, write responses to stdout
while (true)
{
    var line = await Console.In.ReadLineAsync();
    if (line == null) break;

    var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
    var response = await mcpServer.HandleRequest(request);

    var responseJson = JsonSerializer.Serialize(response);
    await Console.Out.WriteLineAsync(responseJson);
    await Console.Out.FlushAsync();
}
```

### 3. Token Service (AzureCliCredential)

```csharp
using Azure.Core;
using Azure.Identity;

public class TokenService
{
    private static readonly AzureCliCredential _credential = new();
    private AccessToken? _cachedToken;

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Check cache (5-minute expiry buffer)
        if (_cachedToken.HasValue &&
            _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _cachedToken.Value.Token;
        }

        var tokenRequest = new TokenRequestContext(new[] {
            $"api://{_configuration["AzureAd:ClientId"]}/.default"
        });

        _cachedToken = await _credential.GetTokenAsync(tokenRequest, ct);
        return _cachedToken.Value.Token;
    }
}
```

### 4. Tool Handler Pattern

```csharp
public class LogTimeTool
{
    private readonly ITimeReportingClient _client;

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        // StrawberryShake strongly-typed client
        var result = await _client.LogTime.ExecuteAsync(
            projectCode: arguments.GetProperty("projectCode").GetString(),
            task: arguments.GetProperty("task").GetString(),
            standardHours: arguments.GetProperty("standardHours").GetDecimal(),
            startDate: arguments.GetProperty("startDate").GetString(),
            completionDate: arguments.GetProperty("completionDate").GetString()
        );

        if (result.IsErrorResult())
        {
            return ToolResult.Error(result.Errors.First().Message);
        }

        return ToolResult.Success($"Created entry {result.Data.LogTime.Id}");
    }
}
```

### 5. Claude Code Configuration

```json
// .claude/config.json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "TimeReportingMcp/TimeReportingMcp.csproj"
      ]
    }
  }
}
```

### 6. Developer Setup

```bash
# 1. Authenticate with Azure
az login

# 2. Configure appsettings.json with Azure AD settings
cat > TimeReportingMcp/appsettings.json <<EOF
{
  "AzureAd": {
    "ClientId": "YOUR-API-APP-ID"
  },
  "GraphQL": {
    "Endpoint": "http://localhost:5001/graphql"
  }
}
EOF

# 3. Start GraphQL API
/deploy

# 4. Use Claude Code (MCP server starts automatically)
# Claude Code launches MCP server as subprocess when first tool is invoked
```

## Alternatives Considered

### Alternative 1: WebSocket Transport

**Approach**: Implement MCP server as ASP.NET Core application with WebSocket endpoint using StreamJsonRpc.

**Why rejected:**
- **Unnecessary complexity**: Requires ASP.NET Core host, port management, service lifecycle
- **No current benefit**: Local development doesn't need remote deployment
- **More code**: ~500+ lines vs. ~200 lines for stdio
- **Deployment burden**: Must run as service, manage separately from Claude Code
- **YAGNI**: Solves hypothetical future requirement, not current actual requirement

**When it would be appropriate:**
- Multiple developers need to share a single MCP server instance
- MCP server deployed remotely (cloud) for centralized access
- Need for server-to-client push notifications (though MCP doesn't currently support this)

### Alternative 2: HTTP with SSE

**Approach**: HTTP POST for tool invocations, SSE for server-to-client events.

**Why rejected:**
- **Two channels**: Managing both HTTP and SSE adds complexity
- **HTTP overhead**: Request/response headers on every call vs. persistent pipe
- **Less natural fit**: MCP is request/response, not event stream
- **Same deployment burden as WebSocket**: Requires web server, port management

### Alternative 3: No Authentication (Shared Bearer Token)

**Approach**: Use single bearer token in configuration, no per-user identity.

**Why rejected:**
- **No user tracking**: Cannot distinguish which developer logged which hours
- **Defeats purpose**: Time reporting requires individual accountability
- **Security risk**: Shared credential, no audit trail
- **Not production-ready**: Cannot use for billing or compliance

## References

- **MCP Specification**: https://spec.modelcontextprotocol.io/
- **Azure.Identity Library**: https://learn.microsoft.com/en-us/dotnet/api/azure.identity
- **AzureCliCredential**: https://learn.microsoft.com/en-us/dotnet/api/azure.identity.azureclicredential
- **StrawberryShake**: https://chillicream.com/docs/strawberryshake
- **ADR 0002 - C# Mono-Stack**: Explains why C# console app for MCP
- **ADR 0009 - StrawberryShake**: Explains typed GraphQL client

---

**Date**: 2025-01-06
**Deciders**: Development Team
**Status**: Accepted
**Phase**: Phases 7-9 - MCP Server Implementation
