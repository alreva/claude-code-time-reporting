using GraphQL;
using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tests.Utils;

/// <summary>
/// Tests for centralized error handling (SDK version returns strings)
/// </summary>
public class ErrorHandlerTests
{
    [Fact]
    public void FromGraphQLErrors_WithValidationError_ReturnsFormattedError()
    {
        // Arrange
        var errors = new[]
        {
            new GraphQLError { Message = "Validation failed: task is invalid" }
        };

        // Act
        var result = ErrorHandler.FromGraphQLErrors(errors);

        // Assert
        Assert.Contains("VALIDATION_ERROR", result);
        Assert.Contains("get_available_projects", result);
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
        Assert.Contains("NOT_FOUND", result);
        Assert.Contains("query_time_entries", result);
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
        Assert.Contains("FORBIDDEN", result);
        Assert.Contains("NOT_REPORTED or DECLINED", result);
    }

    [Fact]
    public void FromException_WithHttpRequestException_SuggestsCheckingAPI()
    {
        // Arrange
        var ex = new HttpRequestException("Connection refused");

        // Act
        var result = ErrorHandler.FromException(ex);

        // Assert
        Assert.Contains("Network Error", result);
        Assert.Contains("API is running", result);
    }

    [Fact]
    public void ValidationError_WithAllowedValues_ListsThem()
    {
        // Arrange
        var allowedValues = new[] { "Development", "Testing", "Code Review" };

        // Act
        var result = ErrorHandler.CreateValidationError(
            "Task is not valid for this project",
            "task",
            allowedValues
        );

        // Assert
        Assert.Contains("Validation Error", result);
        Assert.Contains("Development", result);
        Assert.Contains("Testing", result);
        Assert.Contains("Code Review", result);
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
        Assert.Contains(expectedType, result);
    }

    [Fact]
    public void FromException_WithTaskCanceledException_SuggestsTimeout()
    {
        // Arrange
        var ex = new TaskCanceledException("Operation canceled");

        // Act
        var result = ErrorHandler.FromException(ex);

        // Assert
        Assert.Contains("Timeout", result);
        Assert.Contains("timed out", result);
    }

    [Fact]
    public void ValidationError_WithoutAllowedValues_DoesNotShowList()
    {
        // Act
        var result = ErrorHandler.CreateValidationError("Invalid input");

        // Assert
        Assert.Contains("Validation Error", result);
        Assert.Contains("Invalid input", result);
    }

    [Fact]
    public void FromException_WithContext_IncludesContext()
    {
        // Arrange
        var ex = new Exception("Something went wrong");

        // Act
        var result = ErrorHandler.FromException(ex, "creating time entry");

        // Assert
        Assert.Contains("creating time entry", result);
        Assert.Contains("Something went wrong", result);
    }
}
