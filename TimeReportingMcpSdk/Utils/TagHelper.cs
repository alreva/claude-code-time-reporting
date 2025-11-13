using System.Linq;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Utils;

/// <summary>
/// Helper for parsing tag input in multiple formats
/// </summary>
public static class TagHelper
{
    /// <summary>
    /// Parse tags from JSON string supporting multiple formats:
    /// 1. Dictionary format: {"Type": "Feature", "Environment": "Development"}
    /// 2. Array format: [{"name": "Type", "value": "Feature"}]
    /// Both formats are case-insensitive.
    /// </summary>
    public static List<TagInput> ParseTags(string tagsJson)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Try parsing as array format first: [{"name": "Type", "value": "Feature"}]
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<TagInput>>(tagsJson, options)
                   ?? new List<TagInput>();
        }
        catch
        {
            // If that fails, try parsing as dictionary format: {"Type": "Feature", "Environment": "Production"}
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, options);
            if (dict == null) return new List<TagInput>();

            return dict.Select(kvp => new TagInput
            {
                Name = kvp.Key,
                Value = kvp.Value
            }).ToList();
        }
    }
}
