using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tests.Utils;

public class ResponseFormatterTests
{
    [Fact]
    public void Success_WithSimpleMessage_ReturnsFormattedString()
    {
        // Act
        var result = ResponseFormatter.Success("Operation completed");

        // Assert
        Assert.StartsWith("✅", result);
        Assert.Contains("Operation completed", result);
    }

    [Fact]
    public void Success_WithMultilineDetails_ReturnsFormattedString()
    {
        // Arrange
        var details = new[]
        {
            "Project: INTERNAL",
            "Task: Development",
            "Hours: 8.5"
        };

        // Act
        var result = ResponseFormatter.Success("Time entry created", details);

        // Assert
        Assert.StartsWith("✅", result);
        Assert.Contains("Time entry created", result);
        Assert.Contains("Project: INTERNAL", result);
        Assert.Contains("Task: Development", result);
        Assert.Contains("Hours: 8.5", result);
    }

    [Fact]
    public void Error_WithSimpleMessage_ReturnsFormattedString()
    {
        // Act
        var result = ResponseFormatter.Error("Operation failed");

        // Assert
        Assert.StartsWith("❌", result);
        Assert.Contains("Operation failed", result);
    }

    [Fact]
    public void Error_WithMultilineDetails_ReturnsFormattedString()
    {
        // Arrange
        var details = new[]
        {
            "Validation failed",
            "Field 'projectCode' is required",
            "Field 'task' is required"
        };

        // Act
        var result = ResponseFormatter.Error("Cannot create entry", details);

        // Assert
        Assert.StartsWith("❌", result);
        Assert.Contains("Cannot create entry", result);
        Assert.Contains("Validation failed", result);
        Assert.Contains("Field 'projectCode' is required", result);
    }

    [Fact]
    public void Info_WithSimpleMessage_ReturnsFormattedString()
    {
        // Act
        var result = ResponseFormatter.Info("No entries found");

        // Assert
        Assert.Contains("No entries found", result);
        Assert.DoesNotContain("✅", result);
        Assert.DoesNotContain("❌", result);
    }

    [Fact]
    public void FormatKeyValue_WithBasicPairs_ReturnsFormattedString()
    {
        // Arrange
        var pairs = new Dictionary<string, string>
        {
            ["ID"] = "123",
            ["Project"] = "INTERNAL",
            ["Hours"] = "8.5"
        };

        // Act
        var result = ResponseFormatter.FormatKeyValue(pairs);

        // Assert
        Assert.Contains("ID: 123", result);
        Assert.Contains("Project: INTERNAL", result);
        Assert.Contains("Hours: 8.5", result);
    }

    [Fact]
    public void FormatKeyValue_WithIndentation_ReturnsIndentedString()
    {
        // Arrange
        var pairs = new Dictionary<string, string>
        {
            ["ID"] = "123",
            ["Project"] = "INTERNAL"
        };

        // Act
        var result = ResponseFormatter.FormatKeyValue(pairs, indent: 2);

        // Assert
        Assert.Contains("  ID: 123", result);
        Assert.Contains("  Project: INTERNAL", result);
    }

    [Fact]
    public void FormatList_WithItems_ReturnsFormattedString()
    {
        // Arrange
        var items = new[] { "Item 1", "Item 2", "Item 3" };

        // Act
        var result = ResponseFormatter.FormatList("Available Items", items);

        // Assert
        Assert.Contains("Available Items", result);
        Assert.Contains("- Item 1", result);
        Assert.Contains("- Item 2", result);
        Assert.Contains("- Item 3", result);
    }

    [Fact]
    public void FormatList_WithEmptyList_ReturnsHeaderOnly()
    {
        // Arrange
        var items = Array.Empty<string>();

        // Act
        var result = ResponseFormatter.FormatList("No Items", items);

        // Assert
        Assert.Contains("No Items", result);
        Assert.DoesNotContain("-", result);
    }

    [Fact]
    public void FormatList_WithNumberedItems_ReturnsNumberedList()
    {
        // Arrange
        var items = new[] { "First", "Second", "Third" };

        // Act
        var result = ResponseFormatter.FormatList("Steps", items, numbered: true);

        // Assert
        Assert.Contains("1. First", result);
        Assert.Contains("2. Second", result);
        Assert.Contains("3. Third", result);
    }
}
