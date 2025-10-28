using GraphQL;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tests.Utils;

/// <summary>
/// Tests for centralized error handling
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
        var result = ErrorHandler.CreateValidationError(
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

    [Fact]
    public void FromException_WithTaskCanceledException_SuggestsTimeout()
    {
        // Arrange
        var ex = new TaskCanceledException("Operation canceled");

        // Act
        var result = ErrorHandler.FromException(ex);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("Timeout", result.Content[0].Text);
        Assert.Contains("timed out", result.Content[0].Text);
    }

    [Fact]
    public void ValidationError_WithoutAllowedValues_DoesNotShowList()
    {
        // Act
        var result = ErrorHandler.CreateValidationError("Invalid input");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("Validation Error", result.Content[0].Text);
        Assert.Contains("Invalid input", result.Content[0].Text);
    }

    [Fact]
    public void FromException_WithContext_IncludesContext()
    {
        // Arrange
        var ex = new Exception("Something went wrong");

        // Act
        var result = ErrorHandler.FromException(ex, "creating time entry");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("creating time entry", result.Content[0].Text);
        Assert.Contains("Something went wrong", result.Content[0].Text);
    }
}
