using System.Text.Json;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Utils;

/// <summary>
/// Formats TimeEntry data into consistent JSON responses across all MCP tools.
/// Uses the ITimeEntryFields interface from StrawberryShake entity type generation.
/// </summary>
public static class TimeEntryFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Format a single TimeEntry as JSON.
    /// Uses direct serialization of ITimeEntryFields interface (no flattening).
    /// Returns nested JSON structure matching the GraphQL schema.
    /// </summary>
    /// <param name="entry">TimeEntry object implementing ITimeEntryFields</param>
    /// <returns>JSON string with all TimeEntry fields (nested structure)</returns>
    public static string FormatAsJson(ITimeEntryFields entry)
    {
        return JsonSerializer.Serialize(entry, JsonOptions);
    }

    /// <summary>
    /// Format multiple TimeEntry objects as JSON array.
    /// Uses direct serialization of ITimeEntryFields interface (no flattening).
    /// Returns nested JSON structure matching the GraphQL schema.
    /// </summary>
    /// <param name="entries">Collection of TimeEntry objects implementing ITimeEntryFields</param>
    /// <returns>JSON array string (nested structure)</returns>
    public static string FormatAsJsonArray(IEnumerable<ITimeEntryFields> entries)
    {
        return JsonSerializer.Serialize(entries, JsonOptions);
    }
}
