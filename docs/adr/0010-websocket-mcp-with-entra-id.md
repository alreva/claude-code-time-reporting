# ADR 0010: WebSocket MCP Transport with Azure Entra ID Authentication

## Status

**Accepted**

## Context

The current v1 implementation uses **stdio transport** for the MCP (Model Context Protocol) server, where Claude Code launches the server as a subprocess and communicates via stdin/stdout using JSON-RPC. This works well for single-developer local development but has limitations:

**Current Limitations:**
- **No user tracking**: All time entries use a shared bearer token, making it impossible to distinguish which developer logged which hours
- **Local-only**: stdio transport requires the MCP server to run as a subprocess on the same machine as Claude Code
- **No multi-user support**: Cannot share a single MCP server instance across multiple developers
- **No audit trail**: Time reporting requires knowing who logged what hours for accountability and billing

**Triggering Discussion:**
The project maintainer raised the possibility of using **HTTP-based MCP with OAuth2** to enable:
1. Individual user identity tracking (essential for time reporting)
2. Remote MCP server deployment (shared team server)
3. Secure authentication via existing enterprise identity provider

**Key Requirements:**
- Preserve user identity in time entries (user_id, email, name)
- Leverage existing Azure Entra ID authentication (developers already use `az login`)
- Maintain simplicity where possible (avoid complex OAuth2 flows)
- Keep v1 stdio implementation unchanged (backward compatibility)

**Transport Options:**
MCP supports multiple transports beyond stdio:
- **HTTP with SSE** (Server-Sent Events): One-way server-to-client messages
- **WebSocket**: Full bidirectional communication
- **SignalR**: .NET-specific real-time framework

## Decision

We will implement a **new WebSocket-based MCP server** (Phase 14) using:

1. **Transport**: WebSocket with **StreamJsonRpc** library
2. **Authentication**: **Azure Entra ID token pass-through** via `AzureCliCredential`
3. **Architecture**: Separate WebSocket MCP server project alongside v1 stdio server
4. **User Tracking**: Extract user claims from validated tokens and store in database

**Key Technologies:**
- **StreamJsonRpc** (not SignalR) for native JSON-RPC 2.0 protocol support
- **Azure.Identity** library with `AzureCliCredential` for token acquisition
- **Microsoft.Identity.Web** for token validation in GraphQL API
- **.NET 10** (current RC2, GA November 11, 2025)

## Rationale

### Why WebSocket over HTTP/SSE?

**WebSocket Advantages:**
- **Full bidirectional communication**: Native support for both client-to-server and server-to-client JSON-RPC calls
- **Persistent connection**: Lower latency for repeated tool invocations
- **Native JSON-RPC fit**: WebSocket's message-based model aligns with JSON-RPC 2.0
- **Future-proof**: MCP WebSocket transport proposal (SEP-1288) is under consideration

**HTTP/SSE Limitations:**
- **One-way server push**: SSE only supports server-to-client, requires separate HTTP POST for client-to-server
- **More overhead**: Each tool call requires separate HTTP request/response cycle
- **Less natural fit**: Requires adapting request/response to SSE event stream

### Why StreamJsonRpc over SignalR?

**StreamJsonRpc Benefits:**
- **Native JSON-RPC 2.0**: Direct protocol match with MCP specification (no adaptation layer needed)
- **Mature and stable**: Used in Visual Studio products, 2.22.23 stable release
- **Transport-agnostic**: Works with WebSocket, streams, pipes (flexibility)
- **Request/response pattern**: Built-in support for bidirectional RPC (perfect for MCP)

**SignalR Limitations:**
- **Protocol mismatch**: SignalR uses `invocationId`, `target`, `arguments` (not JSON-RPC's `id`, `method`, `params`)
- **No server-to-client RPC**: SignalR's server invocations are fire-and-forget (no return values)
- **Requires adaptation layer**: Need custom mapping between SignalR protocol and JSON-RPC
- **More complex**: Additional protocol translation increases surface area for bugs

### Why Azure Entra ID Token Pass-Through?

**The "Aha" Insight:**
Developers already authenticate with Azure via `az login` for accessing Azure resources. We can **reuse these tokens** instead of implementing custom OAuth2 flows!

**Pattern:**
```
Developer runs: az login (authenticates with Entra ID)
         ↓
   Token stored in Azure CLI cache (~/.azure/)
         ↓
MCP Server reads token using AzureCliCredential
         ↓
   Passes token to GraphQL API (Authorization: Bearer {token})
         ↓
API validates token with Entra ID (issuer, audience, signature)
         ↓
Extracts user claims (oid, email, name) and stores in time_entries
```

**Benefits:**
- **No custom OAuth2 flow needed**: Leverage existing Azure CLI authentication
- **User identity preserved**: Token contains cryptographically signed user claims
- **Automatic token refresh**: Azure.Identity library handles token expiration
- **SSO experience**: Developers already logged in via `az login`
- **Enterprise-ready**: Uses organization's Entra ID tenant and policies

**Why NOT Device Authorization Flow?**
Initially considered OAuth2 Device Authorization Flow (the "enter code in browser" pattern), but token pass-through is superior because:
- No user interruption (no "visit URL, enter code" ceremony)
- Developers already authenticated for Azure access
- Simpler implementation (no polling, no device code endpoints)
- Consistent with existing Azure tooling

**Why NOT Client Credentials Flow?**
Client Credentials = **machine identity**, not user identity. This defeats the purpose of time tracking:
- All developers share same service account
- Cannot distinguish who logged which hours
- No individual accountability or audit trail

### Why Separate Project (Not Modify v1)?

**Rationale:**
- **Backward compatibility**: v1 stdio server remains unchanged and working
- **Gradual migration**: Teams can adopt WebSocket when ready
- **Different use cases**: stdio for local/single-user, WebSocket for remote/multi-user
- **Clearer separation**: Different transport = different deployment model

## Consequences

### Benefits

✅ **Individual User Tracking**
- Each time entry includes `user_id`, `user_email`, `user_name` from validated Entra ID token
- Essential for time reporting: know who logged what hours
- Audit trail for accountability and billing

✅ **Secure Authentication**
- Tokens issued and cryptographically signed by Microsoft Entra ID
- API validates token signature, issuer, audience, expiration
- Enterprise-grade security via Entra ID policies (MFA, conditional access)

✅ **Seamless Developer Experience**
- No additional authentication step beyond `az login`
- Automatic token refresh (developers don't see expiration)
- Consistent with existing Azure CLI workflows

✅ **Native JSON-RPC Protocol**
- StreamJsonRpc provides direct JSON-RPC 2.0 implementation
- No protocol adaptation or translation layer
- Bidirectional RPC with request/response pattern built-in

✅ **Remote Deployment Ready**
- WebSocket transport allows MCP server to run on different machine
- Can deploy to shared team server or cloud (Azure Container Apps, etc.)
- Multiple developers can connect to same server instance

✅ **Future-Proof**
- Aligns with proposed MCP WebSocket transport (SEP-1288)
- Mature libraries (StreamJsonRpc 2.22.23, Azure.Identity 1.17.0)
- Easy to migrate from `AzureCliCredential` to `ManagedIdentityCredential` in production

### Costs

⚠️ **Local Development Focus (AzureCliCredential Limitation)**
- `AzureCliCredential` designed for local development, not production
- Token acquisition can take 13+ seconds (performance issue)
- Requires Azure CLI installed and developer logged in via `az login`
- **Mitigation**: For production, switch to `ManagedIdentityCredential` or `ChainedTokenCredential`

⚠️ **Two MCP Servers to Maintain**
- v1 stdio server (console app, ~200 lines)
- v2 WebSocket server (ASP.NET Core, StreamJsonRpc)
- Different deployment models, different configurations
- **Mitigation**: Share core tool implementations between both servers

⚠️ **Database Schema Changes**
- Adding `user_id`, `user_email`, `user_name` columns to `time_entries` table
- Existing entries need default value or migration strategy
- **Mitigation**: Additive changes, backward compatible (nullable columns)

⚠️ **Azure Entra ID Setup Required**
- Must configure App Registration in Azure AD
- Expose API with scope: `api://<app-id>/.default`
- Configure token validation settings (tenant ID, audience)
- **Mitigation**: Document setup steps in `docs/AZURE-AD-SETUP.md`

⚠️ **Increased Complexity**
- More moving parts: WebSocket server, token service, API authentication middleware
- Debugging across network boundaries (WebSocket → API → Database)
- **Mitigation**: Comprehensive logging, integration tests, clear error messages

⚠️ **Claude Code CLI Limitation**
- Claude Code CLI does not support OAuth2 UI flows (as of October 2025)
- WebSocket MCP configuration relies on external authentication (`az login`)
- **Mitigation**: Token pass-through approach works perfectly with CLI (no UI needed)

### Trade-off Assessment

**Decision: Benefits outweigh costs for time reporting use case.**

**Justification:**
- **User tracking is non-negotiable**: Time reporting REQUIRES knowing who logged hours
- **Token pass-through is elegant**: Reuses existing `az login` authentication (no custom OAuth2 flow)
- **Local dev limitation acceptable**: `AzureCliCredential` perfect for development, production uses managed identity
- **Separation is cleaner**: v1 stdio remains simple, Phase 14 adds enterprise features without breaking v1

The added complexity is justified by enabling the **core value proposition** of the time reporting system: accurate, auditable time tracking with individual developer accountability.

## Implementation

### 1. Project Structure

```
TimeReportingMcp.WebSocket/
├── Program.cs                    # ASP.NET Core host + WebSocket endpoint
├── McpServer.cs                  # StreamJsonRpc target with MCP methods
├── Services/
│   ├── TokenService.cs           # AzureCliCredential token acquisition
│   └── GraphQLClientFactory.cs   # Creates StrawberryShake client with token
├── appsettings.json
└── TimeReportingMcp.WebSocket.csproj
```

### 2. WebSocket Server Setup (Program.cs)

```csharp
using System.Net;
using System.Net.WebSockets;
using StreamJsonRpc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<McpServer>();

var app = builder.Build();
app.UseWebSockets();

app.Map("/mcp", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var mcpServer = context.RequestServices.GetRequiredService<McpServer>();

        using var handler = new WebSocketMessageHandler(webSocket);
        using var jsonRpc = new JsonRpc(handler, mcpServer);

        jsonRpc.StartListening();
        await jsonRpc.Completion;
    }
    else
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
    }
});

await app.RunAsync();
```

### 3. Token Service (AzureCliCredential)

```csharp
using Azure.Core;
using Azure.Identity;

public class TokenService
{
    private static readonly AzureCliCredential _credential = new();
    private AccessToken? _cachedToken;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string[] _scopes;

    public TokenService(IConfiguration configuration)
    {
        var apiScope = configuration["AzureAd:ApiScope"]
            ?? throw new InvalidOperationException("AzureAd:ApiScope not configured");
        _scopes = new[] { apiScope };
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Check cache (5-minute expiry buffer)
        if (_cachedToken.HasValue &&
            _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _cachedToken.Value.Token;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken.HasValue &&
                _cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _cachedToken.Value.Token;
            }

            var tokenRequest = new TokenRequestContext(_scopes);
            _cachedToken = await _credential.GetTokenAsync(tokenRequest, ct);
            return _cachedToken.Value.Token;
        }
        catch (AuthenticationFailedException ex)
        {
            throw new InvalidOperationException(
                "Azure CLI authentication required. Run 'az login' first.", ex);
        }
        finally
        {
            _lock.Release();
        }
    }
}
```

### 4. MCP Server (StreamJsonRpc Attributes)

```csharp
using StreamJsonRpc;

public class McpServer
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;

    public McpServer(TokenService tokenService, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _configuration = configuration;
    }

    [JsonRpcMethod("initialize")]
    public object Initialize(object? clientInfo)
    {
        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new { tools = new { } },
            serverInfo = new
            {
                name = "time-reporting-mcp-websocket",
                version = "2.0.0"
            }
        };
    }

    [JsonRpcMethod("tools/list")]
    public ToolsListResult ListTools()
    {
        return new ToolsListResult { Tools = /* ... */ };
    }

    [JsonRpcMethod("tools/call")]
    public async Task<ToolResult> CallTool(ToolCallParams toolParams)
    {
        // Get fresh token for each request
        var token = await _tokenService.GetTokenAsync();

        // Create GraphQL client with token
        var client = CreateGraphQLClient(token);

        // Execute tool logic...
        return await ExecuteToolAsync(client, toolParams);
    }
}
```

### 5. API Authentication (Microsoft.Identity.Web)

```csharp
// Program.cs
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Identity.Web;

// Disable claim type mapping
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

```json
// appsettings.json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-api-app-id>",
    "Audience": "api://<your-api-app-id>"
  }
}
```

### 6. Extract User Claims in GraphQL Mutations

```csharp
[Authorize] // Require authentication
public async Task<TimeEntry> LogTime(
    LogTimeInput input,
    [Service] AppDbContext dbContext,
    ClaimsPrincipal user)
{
    // Extract user identity from validated token
    var userId = user.FindFirstValue("oid") ?? user.FindFirstValue("sub");
    var userEmail = user.FindFirstValue("email");
    var userName = user.FindFirstValue("name");

    var entry = new TimeEntry
    {
        Id = Guid.NewGuid(),
        ProjectCode = input.ProjectCode,
        Task = input.Task,
        // ... other properties
        UserId = userId,
        UserEmail = userEmail,
        UserName = userName,
        CreatedAt = DateTime.UtcNow
    };

    await dbContext.TimeEntries.AddAsync(entry);
    await dbContext.SaveChangesAsync();

    return entry;
}
```

### 7. Database Schema (Migration)

```sql
-- Migration: 003_add_user_tracking.sql
ALTER TABLE time_entries ADD COLUMN user_id VARCHAR(100);
ALTER TABLE time_entries ADD COLUMN user_email VARCHAR(255);
ALTER TABLE time_entries ADD COLUMN user_name VARCHAR(255);

CREATE INDEX idx_time_entries_user_id ON time_entries(user_id);

-- Optional: Set default for existing entries
UPDATE time_entries SET user_id = 'system' WHERE user_id IS NULL;
```

### 8. Developer Setup

```bash
# 1. Authenticate with Azure
az login

# 2. Verify authentication
az account show

# 3. Configure appsettings.json with tenant ID and API app ID

# 4. Start API
/run-api

# 5. Start WebSocket MCP server
dotnet run --project TimeReportingMcp.WebSocket

# 6. WebSocket MCP server listens on ws://localhost:8080/mcp
```

### 9. Claude Code Configuration

```json
// .claude/config.json (user's local config)
{
  "mcpServers": {
    "time-reporting-websocket": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "TimeReportingMcp.WebSocket/TimeReportingMcp.WebSocket.csproj"
      ]
    }
  }
}
```

**Note**: WebSocket URL (`ws://localhost:8080/mcp`) configured in MCP server's appsettings.json.

## Alternatives Considered

### Alternative 1: Keep stdio Transport Only

**Approach**: Continue using stdio transport from v1, add user_id as configuration parameter.

**Why rejected:**
- **No real user identity**: Still relies on shared bearer token or manual user_id configuration
- **Error-prone**: Developers could forget to set user_id or set wrong value
- **No cryptographic validation**: Cannot verify user identity claims
- **Defeats purpose**: Time reporting requires knowing who *actually* logged hours, not who *configured* user_id

### Alternative 2: SignalR for WebSocket Transport

**Approach**: Use SignalR Hubs instead of StreamJsonRpc for WebSocket communication.

**Why rejected:**
- **Protocol mismatch**: SignalR protocol (`invocationId`, `target`, `arguments`) ≠ JSON-RPC 2.0 (`id`, `method`, `params`)
- **No server-to-client RPC**: SignalR's server invocations are fire-and-forget (no return values)
- **Requires adapter**: Need custom protocol translation layer between SignalR and JSON-RPC
- **More complexity**: Additional translation increases surface area for bugs
- **Not JSON-RPC native**: StreamJsonRpc is purpose-built for JSON-RPC 2.0

### Alternative 3: HTTP with SSE (Server-Sent Events)

**Approach**: Use HTTP POST for client-to-server (tool calls) and SSE for server-to-client (notifications).

**Why rejected:**
- **Two channels**: Requires managing separate HTTP and SSE connections
- **Higher latency**: Each tool call requires separate HTTP request/response cycle
- **More overhead**: HTTP headers on every request vs persistent WebSocket connection
- **Less natural fit**: Adapting request/response to event stream adds complexity
- **WebSocket superior**: Full bidirectional, lower latency, better fit for JSON-RPC

### Alternative 4: OAuth2 Device Authorization Flow

**Approach**: Implement custom OAuth2 Device Authorization Flow (visit URL, enter code).

**Why rejected:**
- **User interruption**: Requires developer to open browser, enter code each session
- **Redundant**: Developers already authenticated via `az login` for Azure access
- **More complexity**: Need to implement device code request, polling, token refresh
- **Worse UX**: "Enter code: ABCD-1234" ceremony vs seamless token reuse
- **Token pass-through superior**: Leverage existing authentication instead of creating new flow

**Note**: Device Flow would be appropriate if developers weren't already using Azure CLI, but since they are, token pass-through is more elegant.

### Alternative 5: Merge v1 stdio and v2 WebSocket into Single Project

**Approach**: Make transport configurable via flag (stdio vs WebSocket) in single project.

**Why rejected:**
- **Different deployment models**: stdio = subprocess, WebSocket = ASP.NET Core service
- **Complicates codebase**: Mixing two transports in one project increases complexity
- **Harder to maintain**: Changes to one transport affect the other
- **Clearer separation**: Separate projects make deployment and testing clearer
- **Backward compatibility**: Easier to keep v1 stable while evolving v2

## References

- **StreamJsonRpc Documentation**: https://github.com/microsoft/vs-streamjsonrpc
- **Azure.Identity Library**: https://learn.microsoft.com/en-us/dotnet/api/azure.identity
- **Microsoft.Identity.Web**: https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web
- **MCP WebSocket Proposal (SEP-1288)**: https://github.com/modelcontextprotocol/specification/pull/1287
- **OAuth2 Device Authorization Flow (RFC 8628)**: https://datatracker.ietf.org/doc/html/rfc8628
- **MCP Specification**: https://spec.modelcontextprotocol.io/

---

**Date**: 2025-10-31
**Author**: Claude Code (with user guidance)
**Phase**: Phase 14 - WebSocket MCP with Azure Entra ID Authentication
