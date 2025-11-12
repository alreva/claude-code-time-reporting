# MCP Server Implementations Comparison

This repository contains **two side-by-side implementations** of the TimeReporting MCP server:

1. **Custom Implementation** (`TimeReportingMcp/`) - Hand-rolled JSON-RPC stdio handling
2. **SDK Implementation** (`TimeReportingMcpSdk/`) - Using official ModelContextProtocol SDK

## Quick Comparison

| Feature | Custom | SDK |
|---------|--------|-----|
| **Protocol handling** | Manual | Automatic |
| **Server code** | ~255 lines | ~20 lines |
| **Tool registration** | Manual JSON | Attributes + auto-discovery |
| **Shutdown handling** | ⚠️ Has issues | ✅ Built-in graceful shutdown |
| **Maintenance** | High | Low |
| **Dependencies** | Minimal | MCP SDK + Hosting |
| **Learning curve** | Understand protocol details | Focus on business logic |

## The Shutdown Problem (Why SDK?)

### Custom Implementation Issue

The custom implementation has a **shutdown bug** when Claude Code exits:

```csharp
// In McpServerHelpers.ReadLineCancellableAsync():
if (Console.IsInputRedirected)
{
    return await Console.In.ReadLineAsync();  // ❌ Not cancellable!
}
```

**Problem:** When stdin closes, `ReadLineAsync()` blocks forever and never respects the cancellation token. The MCP server becomes a zombie process.

**Attempted fixes:**
- Using `Task.Run()` wrapper
- Adding cancellation token handling
- Multiple shutdown handlers
- None worked consistently

### SDK Solution

The SDK handles this automatically:

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()  // ✅ Handles stdin close properly
    .WithToolsFromAssembly();
```

**Why it works:**
- SDK manages stdio transport lifecycle
- Built-in cancellation handling
- Proper cleanup on stdin close
- No zombie processes

## Code Comparison

### Server Setup

**Custom (~40 lines):**
```csharp
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", optional: true)
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder => {
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// ... 30+ more lines of DI setup ...

var server = serviceProvider.GetRequiredService<McpServer>();
await server.RunAsync(cts.Token);
```

**SDK (~20 lines):**
```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
```

### Tool Definition

**Custom:**
```csharp
// Tool descriptor (separate from implementation)
new McpTool
{
    Name = "hello",
    Description = "Test connectivity...",
    InputSchema = new
    {
        type = "object",
        properties = new { },
        required = new string[] { }
    }
}

// Tool implementation (manual JSON handling)
private async Task<ToolResult> HelloAsync(JsonElement arguments)
{
    return new ToolResult
    {
        Content = new List<ToolResultContent>
        {
            new() { Type = "text", Text = "Hello!" }
        }
    };
}
```

**SDK:**
```csharp
[McpServerToolType]
public static class HelloTool
{
    [McpServerTool, Description("Test connectivity...")]
    public static string Hello()
    {
        return "Hello!";
    }
}
```

## Architecture

### Custom Implementation

```
Program.cs (120 lines)
    ↓
McpServer.cs (255 lines)
    ↓ Manual JSON-RPC
ReadLineCancellableAsync() ← ⚠️ Shutdown bug here
    ↓
Tools/ (7 tool handlers, manual JSON)
    ↓
GraphQL Client (StrawberryShake)
```

### SDK Implementation

```
Program.cs (20 lines)
    ↓
MCP SDK (handles protocol)
    ↓ Automatic
Tools/ (attribute-based, auto-discovered)
    ↓
GraphQL Client (StrawberryShake)
```

## When to Use Which?

### Use Custom Implementation When:
- ❌ Actually, don't use it for new projects
- Only for understanding MCP protocol internals
- Educational purposes

### Use SDK Implementation When:
- ✅ Building production MCP servers
- ✅ Need reliable shutdown behavior
- ✅ Want to focus on business logic, not protocol details
- ✅ Need long-term maintainability

## Testing Both Implementations

### 1. Test Custom Implementation (Current)

`.mcp.json` is currently configured for `time-reporting`:

```bash
# Current MCP server runs custom implementation
# Test shutdown: Exit Claude Code → ⚠️ May leave zombie process
```

### 2. Test SDK Implementation (New)

To switch to SDK-based server:

1. **Update `.mcp.json`** or use the `time-reporting-sdk` server name
2. **Restart Claude Code**
3. **Test shutdown:** Exit Claude Code → ✅ Clean exit

Or use both side-by-side by having two entries in `.mcp.json` (already configured).

## Migration Path

To fully migrate from custom to SDK:

1. ✅ **Basic server setup** - Done
2. ⏳ **Port tool implementations** - In progress
   - Need to convert 7 tools from manual JSON to SDK attributes
   - Add GraphQL client integration
   - Add Azure authentication
3. ⏳ **Add dependency injection** for services
4. ⏳ **Test all tools** work identically
5. ⏳ **Performance comparison**
6. ⏳ **Replace custom implementation** in production config

## Conclusion

The SDK-based implementation:
- ✅ **Solves the shutdown bug**
- ✅ **Reduces code by 90%** (~20 lines vs ~255 lines)
- ✅ **Simpler to maintain**
- ✅ **Future-proof** (SDK updates from Anthropic/Microsoft)
- ✅ **Better developer experience**

**Recommendation:** Use SDK-based implementation going forward.

## Resources

- [Official C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft Blog](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
