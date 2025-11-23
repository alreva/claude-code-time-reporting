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
    /// <exception cref="System.Text.Json.JsonException">Thrown when JSON is invalid</exception>
    /// <exception cref="ArgumentException">Thrown when JSON is valid but doesn't match expected formats</exception>
    public static List<TagInput> ParseTags(string tagsJson)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        Exception? arrayException = null;
        Exception? dictException = null;

        // Try parsing as array format first: [{"name": "Type", "value": "Feature"}]
        try
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<List<TagInput>>(tagsJson, options);
            if (result != null)
            {
                // Validate that all TagInput objects have required properties
                if (result.Any(t => string.IsNullOrEmpty(t.Name) || string.IsNullOrEmpty(t.Value)))
                {
                    throw new ArgumentException(
                        "Invalid tag format: All tags must have both 'name' and 'value' properties. " +
                        "Valid formats: [{\"name\":\"Type\",\"value\":\"Feature\"}] or {\"Type\":\"Feature\"}");
                }
                return result; // Return even if empty - let caller decide if that's an error
            }
        }
        catch (ArgumentException)
        {
            throw; // Re-throw ArgumentException to preserve our error message
        }
        catch (Exception ex)
        {
            arrayException = ex;
        }

        // If that fails, try parsing as dictionary format: {"Type": "Feature", "Environment": "Production"}
        try
        {
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(tagsJson, options);
            if (dict != null)
            {
                return dict.Select(kvp => new TagInput
                {
                    Name = kvp.Key,
                    Value = kvp.Value
                }).ToList(); // Return even if empty - let caller decide if that's an error
            }
        }
        catch (Exception ex)
        {
            dictException = ex;
        }

        // Both formats failed - throw helpful error
        throw new ArgumentException(
            $"Invalid tag format. Must be either array [{{'name':'Type','value':'Feature'}}] or dictionary {{'Type':'Feature'}}. " +
            $"Array parse error: {arrayException?.Message}. Dictionary parse error: {dictException?.Message}");
    }
}
