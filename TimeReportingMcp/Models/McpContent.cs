using System.Text.Json.Serialization;

namespace TimeReportingMcp.Models;

/// <summary>
/// MCP tool result wrapper
/// </summary>
public class ToolResult
{
    [JsonPropertyName("content")]
    public List<ContentItem> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    public bool? IsError { get; set; }
}

/// <summary>
/// MCP content item (text or resource)
/// </summary>
public class ContentItem
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Create text content
    /// </summary>
    public static ContentItem CreateText(string text) =>
        new ContentItem { Type = "text", Text = text };

    /// <summary>
    /// Create error content
    /// </summary>
    public static ContentItem CreateError(string message) =>
        new ContentItem { Type = "text", Text = $"Error: {message}" };
}
