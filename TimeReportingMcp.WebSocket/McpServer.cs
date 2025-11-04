using StreamJsonRpc;
using TimeReportingMcp.WebSocket.Services;

namespace TimeReportingMcp.WebSocket;

/// <summary>
/// MCP (Model Context Protocol) server implementation using StreamJsonRpc.
/// Handles JSON-RPC 2.0 messages over WebSocket connections.
/// </summary>
/// <remarks>
/// This server exposes MCP tools for time tracking via JSON-RPC methods.
/// StreamJsonRpc automatically maps method names to JSON-RPC method calls.
///
/// MCP Protocol Flow:
/// 1. Client connects via WebSocket
/// 2. Client sends "initialize" request
/// 3. Client calls "tools/list" to discover available tools
/// 4. Client calls "tools/call" to execute specific tools
///
/// Authentication: Tokens are acquired via TokenService (Azure CLI)
/// and passed to the GraphQL API for each request.
/// </remarks>
public class McpServer
{
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<McpServer> _logger;

    public McpServer(
        TokenService tokenService,
        IConfiguration configuration,
        ILogger<McpServer> logger)
    {
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// MCP initialize method. Called when client first connects.
    /// Returns server capabilities and version information.
    /// </summary>
    [JsonRpcMethod("initialize")]
    public object Initialize(object? clientInfo)
    {
        _logger.LogInformation("MCP client initializing: {ClientInfo}", clientInfo);

        return new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }  // We support tools capability
            },
            serverInfo = new
            {
                name = "time-reporting-mcp-websocket",
                version = "2.0.0"
            }
        };
    }

    /// <summary>
    /// MCP tools/list method. Returns list of available tools.
    /// </summary>
    [JsonRpcMethod("tools/list")]
    public object ListTools()
    {
        _logger.LogDebug("Client requested tools list");

        return new
        {
            tools = new object[]
            {
                new
                {
                    name = "log_time",
                    description = "Log time spent on a project task",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            projectCode = new { type = "string", description = "Project code (e.g., INTERNAL)" },
                            task = new { type = "string", description = "Task name (e.g., Development)" },
                            standardHours = new { type = "number", description = "Regular hours worked" },
                            overtimeHours = new { type = "number", description = "Overtime hours (optional)" },
                            startDate = new { type = "string", format = "date", description = "Start date (YYYY-MM-DD)" },
                            completionDate = new { type = "string", format = "date", description = "Completion date (YYYY-MM-DD)" },
                            description = new { type = "string", description = "Work description (optional)" },
                            issueId = new { type = "string", description = "Issue/ticket ID (optional)" }
                        },
                        required = new[] { "projectCode", "task", "standardHours", "startDate", "completionDate" }
                    }
                },
                new
                {
                    name = "get_available_projects",
                    description = "Get list of available projects and their tasks",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            activeOnly = new { type = "boolean", description = "Filter to active projects only (default: true)" }
                        }
                    }
                }
                // TODO: Add remaining 5 tools (query_time_entries, update_time_entry, etc.)
            }
        };
    }

    /// <summary>
    /// MCP tools/call method. Executes a specific tool.
    /// </summary>
    [JsonRpcMethod("tools/call")]
    public Task<object> CallTool(object toolParams)
    {
        _logger.LogInformation("Tool call received: {Params}", toolParams);

        // TODO: Parse toolParams, extract tool name and arguments
        // TODO: Acquire token via TokenService
        // TODO: Call GraphQL API with token
        // TODO: Return formatted result

        // Placeholder response
        var result = new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = "Tool execution not yet implemented. Coming in Task 14.5."
                }
            }
        };

        return Task.FromResult<object>(result);
    }
}
