namespace TimeReportingMcpSdk.Utils;

/// <summary>
/// Utility for parsing and validating date strings in MCP tool operations.
/// Provides consistent date handling across all tools.
/// </summary>
public static class DateHelper
{
    /// <summary>
    /// Parse an optional date string (nullable or empty returns null)
    /// </summary>
    /// <param name="dateString">Date string in YYYY-MM-DD format</param>
    /// <returns>DateOnly if valid, null if empty/whitespace</returns>
    /// <exception cref="FormatException">If date string is invalid format</exception>
    public static DateOnly? ParseOptional(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return null;
        }

        return DateOnly.Parse(dateString);
    }

    /// <summary>
    /// Parse a required date string (throws if null/empty)
    /// </summary>
    /// <param name="dateString">Date string in YYYY-MM-DD format</param>
    /// <param name="fieldName">Field name for error messages</param>
    /// <returns>DateOnly value</returns>
    /// <exception cref="ArgumentException">If date is null/empty or invalid format</exception>
    public static DateOnly ParseRequired(string? dateString, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new ArgumentException($"{fieldName} is required", fieldName);
        }

        // Strict validation: must be exactly YYYY-MM-DD format with hyphens
        if (!System.Text.RegularExpressions.Regex.IsMatch(dateString, @"^\d{4}-\d{2}-\d{2}$"))
        {
            throw new ArgumentException(
                $"Invalid date format for {fieldName}. Use YYYY-MM-DD",
                fieldName);
        }

        if (!DateOnly.TryParse(dateString, out var date))
        {
            throw new ArgumentException(
                $"Invalid date format for {fieldName}. Use YYYY-MM-DD",
                fieldName);
        }

        return date;
    }

    /// <summary>
    /// Validate that start date is less than or equal to end date
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <param name="startFieldName">Start field name for error messages</param>
    /// <param name="endFieldName">End field name for error messages</param>
    /// <exception cref="ArgumentException">If start date is after end date</exception>
    public static void ValidateDateRange(
        DateOnly startDate,
        DateOnly endDate,
        string startFieldName,
        string endFieldName)
    {
        if (startDate > endDate)
        {
            throw new ArgumentException(
                $"{startFieldName} must be less than or equal to {endFieldName}");
        }
    }
}
