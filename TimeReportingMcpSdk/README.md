# TimeReporting MCP Server (SDK-based)

This is an **alternative implementation** of the TimeReporting MCP server using the **official Model Context Protocol C# SDK** from Anthropic/Microsoft.

## Comparison: Custom vs SDK Implementation

| Aspect | Custom (`TimeReportingMcp`) | SDK-based (`TimeReportingMcpSdk`) |
|--------|----------------------------|-----------------------------------|
| **Implementation** | Hand-rolled JSON-RPC stdio handling | Official MCP SDK with hosting |
| **Lines of Code** | ~255 lines for server core | ~20 lines for server core |
| **Tool Definition** | Manual JSON serialization | Attribute-based with auto-discovery |
| **Protocol Handling** | Manual request/response parsing | Automatic via SDK |
| **Shutdown** | Custom cancellation handling | Built-in graceful shutdown |
| **Maintenance** | Manual updates for protocol changes | SDK handles updates |
| **Dependencies** | Minimal (just .NET libraries) | MCP SDK + Hosting extensions |

## Why Two Implementations?

This project includes both implementations to:

1. **Compare approaches** - See the difference between hand-rolled vs SDK-based
2. **Test shutdown behavior** - The custom implementation has shutdown issues, SDK should handle it properly
3. **Learn the SDK** - Understand how the official SDK works
4. **Future-proof** - SDK will receive updates and improvements from Anthropic/Microsoft

## Project Structure

```
TimeReportingMcpSdk/
├── Program.cs              # MCP server host setup (20 lines!)
├── Tools/
│   └── HelloTool.cs        # Example tool using SDK attributes
└── TimeReportingMcpSdk.csproj
```

## SDK Features Used

### 1. Host Builder Pattern
```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

### 2. Attribute-Based Tool Registration
```csharp
[McpServerToolType]
public static class HelloTool
{
    [McpServerTool, Description("Test connectivity...")]
    public static string Hello() => "Hello!";
}
```

### 3. Automatic Tool Discovery
- SDK scans assembly for `[McpServerToolType]` classes
- Automatically registers all `[McpServerTool]` methods
- Generates JSON schema from method signatures

### 4. Graceful Shutdown
- SDK handles stdin close events properly
- Cancellation token propagation works correctly
- No zombie processes on exit

## Configuration

The SDK-based server is configured in `.mcp.json`:

```json
{
  "mcpServers": {
    "time-reporting-sdk": {
      "command": "dotnet",
      "args": ["run", "--project", "TimeReportingMcpSdk"],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql"
      }
    }
  }
}
```

## Testing the SDK Server

To test the SDK-based implementation:

1. **Switch MCP server** in Claude Code settings to `time-reporting-sdk`
2. **Restart Claude Code** to load the new server
3. **Test the hello tool**:
   ```
   Can you call the hello tool?
   ```
4. **Test shutdown** by exiting Claude Code (should exit cleanly)

## Next Steps

To complete this implementation:

1. **Port all tools** from `TimeReportingMcp/Tools/` to SDK-based versions
2. **Add GraphQL client** using StrawberryShake (same as custom implementation)
3. **Add authentication** using Azure CLI credentials
4. **Add dependency injection** for services (TokenService, GraphQL client)
5. **Compare performance** and shutdown behavior

## Dependencies

```xml
<PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
```

## Resources

- [Official C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [Microsoft Blog: Build MCP Server in C#](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/)
- [NuGet: ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol)
