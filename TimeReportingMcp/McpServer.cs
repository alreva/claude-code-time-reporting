using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Models;
using TimeReportingMcp.Tools;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

public static class McpServerDi
{
    public static IServiceCollection RegisterMcpServer(this IServiceCollection services) =>
        services.AddSingleton<McpServer>();
}

public static class McpServerHelpers
{
    public static async Task<string?> ReadLineCancellableAsync(CancellationToken token)
    {
        // If input is redirected (not an interactive console),
        // use normal async read — works for MCP stdin pipes.
        if (Console.IsInputRedirected)
        {
            return await Console.In.ReadLineAsync();
        }

        // Otherwise (interactive terminal), use polling approach.
        var sb = new StringBuilder();

        while (!token.IsCancellationRequested)
        {
            await Task.Delay(50, token).ContinueWith(_ => { });

            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                    return sb.ToString();

                if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                    sb.Length--;
                else if (key.Key != ConsoleKey.Backspace)
                    sb.Append(key.KeyChar);
            }
        }

        token.ThrowIfCancellationRequested();
        return null;
    }
}

/// <summary>
/// MCP server that handles JSON-RPC communication via stdio
/// </summary>
public class McpServer(McpToolList availableTools)
{
    /// <summary>
    /// Start the MCP server and process requests from stdin
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await Console.Error.WriteLineAsync("MCP Server ready - listening on stdin");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Read one line from stdin (JSON-RPC request)
                var line = await McpServerHelpers.ReadLineCancellableAsync(cancellationToken);
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    await Console.Error.WriteLineAsync("stdin closed by client, shutting down gracefully...");
                    break;
                }

                // Process request and write response to stdout
                await HandleRequestAsync(line);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                await Console.Error.WriteLineAsync("Cancellation requested, shutting down...");
            }

            await Console.Error.WriteLineAsync("MCP Server shutdown complete");
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token is triggered
            await Console.Error.WriteLineAsync("MCP Server cancelled gracefully");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Fatal error in MCP server: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Handle a single JSON-RPC request
    /// </summary>
    private async Task HandleRequestAsync(string requestJson)
    {
        JsonRpcResponse response;
        int? requestId = null;

        try
        {
            await Console.Error.WriteLineAsync($"Received request: {requestJson.Substring(0, Math.Min(100, requestJson.Length))}...");

            // Deserialize request
            var request = JsonHelper.DeserializeRequest(requestJson);
            if (request == null)
            {
                response = JsonHelper.ErrorResponse(
                    null,
                    JsonRpcError.InvalidRequest("Failed to parse JSON-RPC request")
                );
            }
            else
            {
                requestId = request.Id;
                response = await ProcessRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Error handling request: {ex.Message}");
            response = JsonHelper.ErrorResponse(
                requestId,
                JsonRpcError.InternalError(ex.Message)
            );
        }

        // Write response to stdout (one line)
        var responseJson = JsonHelper.SerializeResponse(response);
        Console.WriteLine(responseJson);
        await Console.Out.FlushAsync();
    }

    /// <summary>
    /// Process a JSON-RPC request and return response
    /// </summary>
    private async Task<JsonRpcResponse> ProcessRequestAsync(JsonRpcRequest request)
    {
        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "tools/list" => HandleToolsList(request),
            "tools/call" => await HandleToolCallAsync(request),
            _ => JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.MethodNotFound(request.Method)
            )
        };
    }

    /// <summary>
    /// Handle initialize - MCP protocol handshake
    /// </summary>
    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        Console.Error.WriteLine("Handling initialize request");

        var result = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { }
            },
            serverInfo = new
            {
                name = "time-reporting-mcp",
                version = "1.0.0"
            }
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// Handle tools/list - return list of available tools
    /// </summary>
    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        Console.Error.WriteLine("Handling tools/list");

        var result = new ToolsListResult
        {
            Tools = availableTools.GetToolDescriptions()
        };

        return new JsonRpcResponse
        {
            Id = request.Id,
            Result = result
        };
    }

    /// <summary>
    /// Handle tools/call - route to a specific tool handler
    /// </summary>
    private async Task<JsonRpcResponse> HandleToolCallAsync(JsonRpcRequest request)
    {
        var toolParams = JsonHelper.ParseToolCallParams(request.Params);

        if (toolParams == null || string.IsNullOrEmpty(toolParams.Name))
        {
            return JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.InvalidParams("Missing tool name")
            );
        }

        await Console.Error.WriteLineAsync($"Handling tool call: {toolParams.Name}");

        try
        {
            var result = await ExecuteToolAsync(toolParams);
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Tool execution failed: {ex.Message}");
            return JsonHelper.ErrorResponse(
                request.Id,
                JsonRpcError.InternalError($"Tool execution failed: {ex.Message}")
            );
        }
    }

    /// <summary>
    /// Execute a specific tool by name
    /// </summary>
    private Task<ToolResult> ExecuteToolAsync(ToolCallParams toolParams)
    {
        // Convert arguments dictionary to JsonElement for tool handlers
        var argumentsJson = JsonSerializer.Serialize(toolParams.Arguments ?? []);
        var argumentsElement = JsonSerializer.Deserialize<JsonElement>(argumentsJson);

        return availableTools.CallTool(toolParams.Name, argumentsElement);
    }
}
