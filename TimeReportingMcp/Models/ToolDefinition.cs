using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// MCP tool definition for tools/list response
/// </summary>
public class ToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };
}

/// <summary>
/// Response for tools/list method
/// </summary>
public class ToolsListResult
{
    [JsonPropertyName("tools")]
    public List<ToolDefinition> Tools { get; set; } = new();
}
