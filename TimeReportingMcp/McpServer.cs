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
                var line = await Console.In.ReadLineAsync(cancellationToken);

                // stdin closed by client (graceful shutdown signal per MCP spec)
                if (line == null)
                {
                    await Console.Error.WriteLineAsync("stdin closed, shutting down gracefully...");
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    await Console.Error.WriteLineAsync("Received empty line, continuing...");
                    continue;
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
