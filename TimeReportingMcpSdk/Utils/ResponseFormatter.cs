using System.Text;

namespace TimeReportingMcpSdk.Utils;

/// <summary>
/// Utility for formatting consistent, human-readable MCP tool responses.
/// Provides standardized success/error/info formatting with emojis and structure.
/// </summary>
public static class ResponseFormatter
{
    /// <summary>
    /// Format a success response with optional details
    /// </summary>
    /// <param name="message">Success message</param>
    /// <param name="details">Optional details lines</param>
    /// <returns>Formatted success response with ✅ prefix</returns>
    public static string Success(string message, params string[] details)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"✅ {message}");

        if (details.Length > 0)
        {
            sb.AppendLine();
            foreach (var detail in details)
            {
                sb.AppendLine(detail);
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Format an error response with optional details
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="details">Optional error details</param>
    /// <returns>Formatted error response with ❌ prefix</returns>
    public static string Error(string message, params string[] details)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"❌ {message}");

        if (details.Length > 0)
        {
            sb.AppendLine();
            foreach (var detail in details)
            {
                sb.AppendLine(detail);
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Format an informational response (no emoji prefix)
    /// </summary>
    /// <param name="message">Info message</param>
    /// <returns>Plain info message</returns>
    public static string Info(string message)
    {
        return message;
    }

    /// <summary>
    /// Format key-value pairs in a readable format
    /// </summary>
    /// <param name="pairs">Dictionary of key-value pairs</param>
    /// <param name="indent">Number of spaces to indent (default: 0)</param>
    /// <returns>Formatted key-value string</returns>
    public static string FormatKeyValue(Dictionary<string, string> pairs, int indent = 0)
    {
        var sb = new StringBuilder();
        var indentString = new string(' ', indent);

        foreach (var kvp in pairs)
        {
            sb.AppendLine($"{indentString}{kvp.Key}: {kvp.Value}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Format a list of items with optional header
    /// </summary>
    /// <param name="header">List header</param>
    /// <param name="items">Items to list</param>
    /// <param name="numbered">Use numbered list instead of bullets (default: false)</param>
    /// <returns>Formatted list string</returns>
    public static string FormatList(string header, string[] items, bool numbered = false)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);

        if (items.Length > 0)
        {
            sb.AppendLine();
            for (int i = 0; i < items.Length; i++)
            {
                var prefix = numbered ? $"{i + 1}." : "-";
                sb.AppendLine($"{prefix} {items[i]}");
            }
        }

        return sb.ToString().TrimEnd();
    }
}
