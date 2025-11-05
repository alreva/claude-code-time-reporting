# Task 8.4: Add Comprehensive Error Handling

**Phase:** 8 - MCP Server Tools Part 1
**Estimated Time:** 1 hour
**Prerequisites:** Tasks 8.1-8.3 complete
**Status:** Pending

## Objective

Add comprehensive error handling across all MCP tools to provide clear, actionable error messages to users. This includes handling GraphQL errors, network errors, validation errors, and providing helpful suggestions for resolution.

## Acceptance Criteria

- [ ] Create centralized error handling utility
- [ ] Standardize error response format across all tools
- [ ] Parse and categorize GraphQL errors
- [ ] Provide user-friendly error messages with suggestions
- [ ] Handle network/connection errors gracefully
- [ ] Add error type constants
- [ ] Write unit tests for error handling
- [ ] All tests pass

## Error Categories

### 1. Validation Errors
- Invalid input (missing required fields, wrong format)
- Business rule violations (task not in project, invalid tags)
- Status violations (cannot update submitted entry)

### 2. Not Found Errors
- Entry ID not found
- Project code not found

### 3. Authentication Errors
- Missing Azure AD token
- Invalid/expired token

### 4. Network Errors
- API unreachable
- Connection timeout
- GraphQL endpoint not responding

### 5. Internal Errors
- Unexpected exceptions
- JSON parsing failures

## Implementation

### 1. Create Error Handler Utility

Create `TimeReportingMcp/Utils/ErrorHandler.cs`:

```csharp
using System.Text;
using GraphQL;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Utils;

/// <summary>
/// Centralized error handling for MCP tools
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
    /// Create error result from GraphQL errors
    /// </summary>
    public static ToolResult FromGraphQLErrors(GraphQLError[] errors)
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

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message.ToString())
            },
            IsError = true
        };
    }

    /// <summary>
    /// Create error result from exception
    /// </summary>
    public static ToolResult FromException(Exception ex, string? context = null)
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

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message.ToString())
            },
            IsError = true
        };
    }

    /// <summary>
    /// Create validation error result
    /// </summary>
    public static ToolResult ValidationError(string message, string? field = null, string[]? allowedValues = null)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine($"❌ Validation Error");
        errorMessage.AppendLine($"\n{message}");

        if (!string.IsNullOrEmpty(field))
        {
            errorMessage.AppendLine($"\n**Field:** {field}");
        }

        if (allowedValues != null && allowedValues.Length > 0)
        {
            errorMessage.AppendLine($"\n**Allowed values:**");
            foreach (var value in allowedValues)
            {
                errorMessage.AppendLine($"  • {value}");
            }
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(errorMessage.ToString())
            },
            IsError = true
        };
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
                "Check your MCP server configuration and ensure the Azure AD token is valid.",

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
```

### 2. Update Existing Tools

Refactor error handling in all three tools to use `ErrorHandler`:

**LogTimeTool.cs:**
```csharp
// Replace CreateErrorResult and CreateExceptionResult with:
private ToolResult CreateErrorResult(GraphQLError[] errors)
{
    return ErrorHandler.FromGraphQLErrors(errors);
}

private ToolResult CreateExceptionResult(Exception ex)
{
    return ErrorHandler.FromException(ex, "creating time entry");
}
```

**QueryEntriesTool.cs:**
```csharp
// Replace CreateErrorResult and CreateExceptionResult with:
private ToolResult CreateErrorResult(GraphQLError[] errors)
{
    return ErrorHandler.FromGraphQLErrors(errors);
}

private ToolResult CreateExceptionResult(Exception ex)
{
    return ErrorHandler.FromException(ex, "querying time entries");
}
```

**UpdateEntryTool.cs:**
```csharp
// Replace CreateValidationError, CreateErrorResult, and CreateExceptionResult with:
private ToolResult CreateValidationError(string message, string? field = null)
{
    return ErrorHandler.ValidationError(message, field);
}

private ToolResult CreateErrorResult(GraphQLError[] errors)
{
    return ErrorHandler.FromGraphQLErrors(errors);
}

private ToolResult CreateExceptionResult(Exception ex)
{
    return ErrorHandler.FromException(ex, "updating time entry");
}
```

### 3. Add Network Error Handling to GraphQLClientWrapper

Update `TimeReportingMcp/Utils/GraphQLClientWrapper.cs`:

```csharp
public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest request)
{
    try
    {
        Console.Error.WriteLine($"Executing query: {request.Query?.Substring(0, 50)}...");
        var response = await _client.SendQueryAsync<T>(request);

        if (response.Errors != null && response.Errors.Length > 0)
        {
            Console.Error.WriteLine($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
        }

        return response;
    }
    catch (HttpRequestException ex)
    {
        Console.Error.WriteLine($"Network error: {ex.Message}");
        throw new InvalidOperationException("Failed to connect to GraphQL API. Ensure the API is running and accessible.", ex);
    }
    catch (TaskCanceledException ex)
    {
        Console.Error.WriteLine($"Request timeout: {ex.Message}");
        throw new InvalidOperationException("GraphQL request timed out. The API might be unresponsive.", ex);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"GraphQL query failed: {ex.Message}");
        throw;
    }
}

// Same for SendMutationAsync
```

### 4. Create Unit Tests

Create `TimeReportingMcp.Tests/Utils/ErrorHandlerTests.cs`:

```csharp
using GraphQL;
using Xunit;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Utils;

public class ErrorHandlerTests
{
    [Fact]
    public void FromGraphQLErrors_WithValidationError_ReturnsFormattedError()
    {
        // Arrange
        var errors = new[]
        {
            new GraphQLError { Message = "Validation failed: Task is invalid" }
        };

        // Act
        var result = ErrorHandler.FromGraphQLErrors(errors);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("VALIDATION_ERROR", result.Content[0].Text);
        Assert.Contains("get_available_projects", result.Content[0].Text);
    }

    [Fact]
    public void FromGraphQLErrors_WithNotFoundError_IncludesSuggestion()
    {
        // Arrange
        var errors = new[]
        {
            new GraphQLError { Message = "Entry not found" }
        };

        // Act
        var result = ErrorHandler.FromGraphQLErrors(errors);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("NOT_FOUND", result.Content[0].Text);
        Assert.Contains("query_time_entries", result.Content[0].Text);
    }

    [Fact]
    public void FromGraphQLErrors_WithForbiddenError_ExplainsRestriction()
    {
        // Arrange
        var errors = new[]
        {
            new GraphQLError { Message = "Cannot update entry with status SUBMITTED" }
        };

        // Act
        var result = ErrorHandler.FromGraphQLErrors(errors);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("FORBIDDEN", result.Content[0].Text);
        Assert.Contains("NOT_REPORTED or DECLINED", result.Content[0].Text);
    }

    [Fact]
    public void FromException_WithHttpRequestException_SuggestsCheckingAPI()
    {
        // Arrange
        var ex = new HttpRequestException("Connection refused");

        // Act
        var result = ErrorHandler.FromException(ex);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("Network Error", result.Content[0].Text);
        Assert.Contains("API is running", result.Content[0].Text);
    }

    [Fact]
    public void ValidationError_WithAllowedValues_ListsThem()
    {
        // Arrange
        var allowedValues = new[] { "Development", "Testing", "Code Review" };

        // Act
        var result = ErrorHandler.ValidationError(
            "Task is not valid for this project",
            "task",
            allowedValues
        );

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("Validation Error", result.Content[0].Text);
        Assert.Contains("Development", result.Content[0].Text);
        Assert.Contains("Testing", result.Content[0].Text);
        Assert.Contains("Code Review", result.Content[0].Text);
    }

    [Theory]
    [InlineData("Entry not found", "NOT_FOUND")]
    [InlineData("Cannot update submitted entry", "FORBIDDEN")]
    [InlineData("Validation failed", "VALIDATION_ERROR")]
    [InlineData("Unauthorized access", "AUTHENTICATION_ERROR")]
    public void CategorizeGraphQLError_IdentifiesErrorType(string message, string expectedType)
    {
        // Arrange
        var errors = new[] { new GraphQLError { Message = message } };

        // Act
        var result = ErrorHandler.FromGraphQLErrors(errors);

        // Assert
        Assert.Contains(expectedType, result.Content[0].Text);
    }
}
```

## Testing

### Manual Testing

1. **Test validation error:**
   ```bash
   # Create entry with invalid project
   echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"log_time","arguments":{"projectCode":"INVALID","task":"Development","standardHours":8,"startDate":"2025-10-29","completionDate":"2025-10-29"}}}' | dotnet run --project TimeReportingMcp
   ```
   Expected: Validation error with suggestion to use get_available_projects

2. **Test not found error:**
   ```bash
   # Update non-existent entry
   echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"update_time_entry","arguments":{"id":"00000000-0000-0000-0000-000000000000","standardHours":7}}}' | dotnet run --project TimeReportingMcp
   ```
   Expected: Not found error with suggestion to use query_time_entries

3. **Test forbidden error:**
   ```bash
   # Try to update submitted entry
   # (first create and submit an entry, then try to update it)
   ```
   Expected: Forbidden error explaining status restrictions

4. **Test network error:**
   ```bash
   # Stop the API and try any tool
   /stop-api
   echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"query_time_entries","arguments":{}}}' | dotnet run --project TimeReportingMcp
   ```
   Expected: Network error suggesting to check if API is running

### Test Scenarios

1. ✅ **GraphQL validation error:** Clear message with suggestions
2. ✅ **Not found error:** Helpful guidance to find correct IDs
3. ✅ **Forbidden error:** Explains status restrictions
4. ✅ **Network error:** Suggests checking API connectivity
5. ✅ **Timeout error:** Indicates API responsiveness issue
6. ✅ **Invalid JSON:** Parsing error with context
7. ✅ **Authentication error:** Token validation message

## Related Files

**Created:**
- `TimeReportingMcp/Utils/ErrorHandler.cs`
- `TimeReportingMcp.Tests/Utils/ErrorHandlerTests.cs`

**Modified:**
- `TimeReportingMcp/Tools/LogTimeTool.cs` - Use centralized error handling
- `TimeReportingMcp/Tools/QueryEntriesTool.cs` - Use centralized error handling
- `TimeReportingMcp/Tools/UpdateEntryTool.cs` - Use centralized error handling
- `TimeReportingMcp/Utils/GraphQLClientWrapper.cs` - Add network error handling

## Next Steps

1. Run `/test-mcp` to verify all tests pass
2. Test error scenarios manually
3. Commit Phase 8 completion
4. Proceed to Phase 9: MCP Server Tools Part 2

## Reference

- PRD: `docs/prd/mcp-tools.md` (Section 4: Error Handling)
