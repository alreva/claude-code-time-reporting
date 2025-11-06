using System.Text.Json;
using System.Text.Json.Serialization;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Utils;

/// <summary>
/// JSON serialization helpers for MCP protocol
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Shared JSON serializer options with camelCase naming policy
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Deserialize JSON-RPC request from string
    /// </summary>
    public static JsonRpcRequest? DeserializeRequest(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonRpcRequest>(json, Options);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Failed to deserialize request: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Serialize JSON-RPC response to string
    /// </summary>
    public static string SerializeResponse(JsonRpcResponse response)
    {
        return JsonSerializer.Serialize(response, Options);
    }

    /// <summary>
    /// Parse tool call parameters from request
    /// </summary>
    public static ToolCallParams? ParseToolCallParams(object? paramsObj)
    {
        if (paramsObj == null) return null;

        try
        {
            var json = JsonSerializer.Serialize(paramsObj, Options);
            return JsonSerializer.Deserialize<ToolCallParams>(json, Options);
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"Failed to parse tool call params: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Create success response with text content
    /// </summary>
    public static JsonRpcResponse SuccessResponse(int? requestId, string message)
    {
        return new JsonRpcResponse
        {
            Id = requestId,
            Result = new ToolResult
            {
                Content = new List<ContentItem> { ContentItem.CreateText(message) }
            }
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static JsonRpcResponse ErrorResponse(int? requestId, JsonRpcError error)
    {
        return new JsonRpcResponse
        {
            Id = requestId,
            Error = error
        };
    }
}
