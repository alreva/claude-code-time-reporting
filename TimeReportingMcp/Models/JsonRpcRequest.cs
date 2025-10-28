using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// JSON-RPC 2.0 request structure
/// </summary>
public class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

/// <summary>
/// Parameters for tools/call method
/// </summary>
public class ToolCallParams
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public Dictionary<string, object?>? Arguments { get; set; }
}

/// <summary>
/// Parameters for tools/list method
/// </summary>
public class ToolsListParams
{
    // MCP tools/list has no parameters, but we keep for future extensions
}
