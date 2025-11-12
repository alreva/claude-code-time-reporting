using System.Text;
using System.Text.Json;
using GraphQL;

namespace TimeReportingMcpSdk.Utils;

/// <summary>
/// Centralized error handling for MCP SDK tools.
/// Returns formatted error strings instead of ToolResult objects.
/// </summary>
public static class ErrorHandler
{
    // Error type constants
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFoundError = "NOT_FOUND";
    public const string ForbiddenError = "FORBIDDEN";
    public const string AuthenticationError = "AUTHENTICATION_ERROR";
    public const string NetworkError = "NETWORK_ERROR";
    public const string InternalError = "INTERNAL_ERROR";

    /// <summary>
    /// Create error message from GraphQL errors
    /// </summary>
    public static string FromGraphQLErrors(GraphQLError[] errors)
    {
        var message = new StringBuilder();
        message.AppendLine("❌ Operation failed:\n");

        foreach (var error in errors)
        {
            var errorType = CategorizeGraphQLError(error);
            message.AppendLine($"**{errorType}:** {error.Message}");

            // Add helpful suggestions based on error type
            var suggestion = GetErrorSuggestion(errorType, error.Message);
            if (!string.IsNullOrEmpty(suggestion))
            {
                message.AppendLine($"💡 {suggestion}\n");
            }
        }

        return message.ToString();
    }

    /// <summary>
    /// Create error message from exception
    /// </summary>
    public static string FromException(Exception ex, string? context = null)
    {
        var message = new StringBuilder();
        message.AppendLine($"❌ {DetermineErrorType(ex)}");

        if (!string.IsNullOrEmpty(context))
        {
            message.AppendLine($"\n**Context:** {context}");
        }

        message.AppendLine($"\n**Error:** {ex.Message}");

        // Add stack trace for debugging (only in development)
        if (IsDebugMode())
        {
            message.AppendLine($"\n**Stack Trace:**");
            message.AppendLine(ex.StackTrace);
        }

        // Add suggestion based on exception type
        var suggestion = GetExceptionSuggestion(ex);
        if (!string.IsNullOrEmpty(suggestion))
        {
            message.AppendLine($"\n💡 {suggestion}");
        }

        return message.ToString();
    }

    /// <summary>
    /// Create validation error message
    /// </summary>
    public static string CreateValidationError(string errorMessage, string? field = null, string[]? allowedValues = null)
    {
        var message = new StringBuilder();
        message.AppendLine($"❌ Validation Error");
        message.AppendLine($"\n{errorMessage}");

        if (!string.IsNullOrEmpty(field))
        {
            message.AppendLine($"\n**Field:** {field}");
        }

        if (allowedValues != null && allowedValues.Length > 0)
        {
            message.AppendLine($"\n**Allowed values:**");
            foreach (var value in allowedValues)
            {
                message.AppendLine($"  • {value}");
            }
        }

        return message.ToString();
    }

    /// <summary>
    /// Categorize GraphQL error by analyzing error message
    /// </summary>
    private static string CategorizeGraphQLError(GraphQLError error)
    {
        var message = error.Message.ToLowerInvariant();

        if (message.Contains("not found"))
            return NotFoundError;

        if (message.Contains("cannot update") || message.Contains("cannot delete") ||
            message.Contains("not allowed") || message.Contains("read-only"))
            return ForbiddenError;

        if (message.Contains("validation") || message.Contains("invalid") ||
            message.Contains("required") || message.Contains("must be"))
            return ValidationError;

        if (message.Contains("unauthorized") || message.Contains("authentication") ||
            message.Contains("token"))
            return AuthenticationError;

        return InternalError;
    }

    /// <summary>
    /// Get helpful suggestion based on error type and message
    /// </summary>
    private static string? GetErrorSuggestion(string errorType, string errorMessage)
    {
        return errorType switch
        {
            NotFoundError => "Use query_time_entries or get_available_projects to find valid IDs and codes.",

            ForbiddenError when errorMessage.Contains("Cannot update") =>
                "Only entries with status NOT_REPORTED or DECLINED can be updated. Submitted or approved entries are read-only.",

            ForbiddenError when errorMessage.Contains("Cannot delete") =>
                "Only entries with status NOT_REPORTED can be deleted.",

            ValidationError when errorMessage.Contains("task") =>
                "Use get_available_projects to see valid tasks for each project.",

            ValidationError when errorMessage.Contains("tag") =>
                "Use get_available_projects to see valid tags and values for the project.",

            ValidationError when errorMessage.Contains("project") =>
                "Use get_available_projects to see all available project codes.",

            ValidationError when errorMessage.Contains("date") =>
                "Ensure dates are in YYYY-MM-DD format and start_date <= completion_date.",

            AuthenticationError =>
                "Check your MCP server configuration and ensure the bearer token is valid.",

            _ => null
        };
    }

    /// <summary>
    /// Determine error type from exception
    /// </summary>
    private static string DetermineErrorType(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "Network Error",
            TaskCanceledException => "Request Timeout",
            JsonException => "Data Parsing Error",
            ArgumentException => "Invalid Input",
            InvalidOperationException => "Operation Error",
            _ => "Unexpected Error"
        };
    }

    /// <summary>
    /// Get suggestion based on exception type
    /// </summary>
    private static string? GetExceptionSuggestion(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "Check that the GraphQL API is running and accessible.",
            TaskCanceledException => "The request timed out. Check your network connection or API responsiveness.",
            JsonException => "The response format was unexpected. This might indicate an API version mismatch.",
            ArgumentException => "Check the input parameters and try again.",
            _ => "If this error persists, contact support."
        };
    }

    /// <summary>
    /// Check if debug mode is enabled
    /// </summary>
    private static bool IsDebugMode()
    {
        var env = Environment.GetEnvironmentVariable("MCP_DEBUG");
        return !string.IsNullOrEmpty(env) && (env == "1" || env.ToLowerInvariant() == "true");
    }
}
