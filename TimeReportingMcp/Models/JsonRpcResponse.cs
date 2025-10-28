using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// JSON-RPC 2.0 response structure
/// </summary>
public class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC error structure
/// </summary>
public class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    public static JsonRpcError InvalidRequest(string message) =>
        new JsonRpcError { Code = -32600, Message = message };

    public static JsonRpcError MethodNotFound(string method) =>
        new JsonRpcError { Code = -32601, Message = $"Method not found: {method}" };

    public static JsonRpcError InvalidParams(string message) =>
        new JsonRpcError { Code = -32602, Message = message };

    public static JsonRpcError InternalError(string message) =>
        new JsonRpcError { Code = -32603, Message = message };
}
