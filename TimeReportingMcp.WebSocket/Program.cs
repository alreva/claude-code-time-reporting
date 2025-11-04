using System.Net;
using StreamJsonRpc;
using TimeReportingMcp.WebSocket;
using TimeReportingMcp.WebSocket.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<McpServer>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Enable WebSocket support
app.UseWebSockets();

// MCP WebSocket endpoint
app.Map("/mcp", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await context.Response.WriteAsync("WebSocket connection required");
        return;
    }

    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("WebSocket connection request from {RemoteIp}", context.Connection.RemoteIpAddress);

    // Accept WebSocket connection
    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    logger.LogInformation("WebSocket connection established");

    try
    {
        // Create MCP server instance
        var mcpServer = context.RequestServices.GetRequiredService<McpServer>();

        // Create StreamJsonRpc handler for WebSocket
        var handler = new WebSocketMessageHandler(webSocket);

        // Create JSON-RPC connection
        using var jsonRpc = new JsonRpc(handler, mcpServer);

        // Add error handler
        jsonRpc.Disconnected += (sender, args) =>
        {
            logger.LogInformation("JSON-RPC connection disconnected: {Reason}", args.Reason);
        };

        // Start listening for JSON-RPC messages
        jsonRpc.StartListening();

        logger.LogInformation("StreamJsonRpc started, waiting for messages...");

        // Wait for connection to close
        await jsonRpc.Completion;

        logger.LogInformation("JSON-RPC connection completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error handling WebSocket connection");
    }
    finally
    {
        if (webSocket.State == WebSocketState.Open)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Connection closed",
                CancellationToken.None);
        }
    }
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "time-reporting-mcp-websocket",
    timestamp = DateTime.UtcNow
}));

// Root endpoint with service info
app.MapGet("/", () => Results.Ok(new
{
    service = "Time Reporting MCP Server (WebSocket)",
    version = "2.0.0",
    protocol = "Model Context Protocol over WebSocket",
    transport = "StreamJsonRpc",
    authentication = "Azure Entra ID (via Azure CLI)",
    endpoints = new
    {
        websocket = "/mcp (ws://localhost:8080/mcp)",
        health = "/health"
    },
    instructions = new[]
    {
        "1. Ensure you are logged in: az login",
        "2. Configure Claude Code to connect to ws://localhost:8080/mcp",
        "3. Use MCP tools to log time against projects"
    }
}));

app.Logger.LogInformation("Starting Time Reporting MCP WebSocket Server");
app.Logger.LogInformation("WebSocket endpoint: ws://localhost:8080/mcp");
app.Logger.LogInformation("Health check: http://localhost:8080/health");

await app.RunAsync();
