using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tests.Utils;

public class DateHelperTests
{
    [Fact]
    public void ParseOptional_WithValidDate_ReturnsDateOnly()
    {
        // Arrange
        var dateString = "2025-01-13";

        // Act
        var result = DateHelper.ParseOptional(dateString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2025, 1, 13), result.Value);
    }

    [Fact]
    public void ParseOptional_WithNullDate_ReturnsNull()
    {
        // Act
        var result = DateHelper.ParseOptional(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseOptional_WithEmptyDate_ReturnsNull()
    {
        // Act
        var result = DateHelper.ParseOptional("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseOptional_WithWhitespaceDate_ReturnsNull()
    {
        // Act
        var result = DateHelper.ParseOptional("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseOptional_WithInvalidFormat_ThrowsFormatException()
    {
        // Arrange
        var invalidDate = "13-01-2025"; // Wrong format

        // Act & Assert
        Assert.Throws<FormatException>(() => DateHelper.ParseOptional(invalidDate));
    }

    [Fact]
    public void ParseRequired_WithValidDate_ReturnsDateOnly()
    {
        // Arrange
        var dateString = "2025-01-13";

        // Act
        var result = DateHelper.ParseRequired(dateString, "startDate");

        // Assert
        Assert.Equal(new DateOnly(2025, 1, 13), result);
    }

    [Fact]
    public void ParseRequired_WithNullDate_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DateHelper.ParseRequired(null!, "startDate"));

        Assert.Contains("startDate is required", ex.Message);
        Assert.Equal("startDate", ex.ParamName);
    }

    [Fact]
    public void ParseRequired_WithEmptyDate_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DateHelper.ParseRequired("", "completionDate"));

        Assert.Contains("completionDate is required", ex.Message);
        Assert.Equal("completionDate", ex.ParamName);
    }

    [Fact]
    public void ParseRequired_WithWhitespaceDate_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DateHelper.ParseRequired("   ", "endDate"));

        Assert.Contains("endDate is required", ex.Message);
    }

    [Fact]
    public void ParseRequired_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var invalidDate = "13-01-2025"; // Wrong format

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DateHelper.ParseRequired(invalidDate, "startDate"));

        Assert.Contains("Invalid date format", ex.Message);
        Assert.Contains("YYYY-MM-DD", ex.Message);
        Assert.Equal("startDate", ex.ParamName);
    }

    [Theory]
    [InlineData("2025-01-13")]
    [InlineData("2025-12-31")]
    [InlineData("2024-02-29")] // Leap year
    [InlineData("2025-01-01")]
    public void ParseRequired_WithVariousValidDates_ReturnsCorrectDateOnly(string dateString)
    {
        // Act
        var result = DateHelper.ParseRequired(dateString, "testDate");

        // Assert
        Assert.Equal(DateOnly.Parse(dateString), result);
    }

    [Theory]
    [InlineData("2025-13-01")] // Invalid month
    [InlineData("2025-01-32")] // Invalid day
    [InlineData("2025-02-29")] // Not a leap year
    [InlineData("2025/01/13")] // Wrong separator
    [InlineData("01-13-2025")] // Wrong order
    public void ParseRequired_WithInvalidDates_ThrowsArgumentException(string invalidDate)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            DateHelper.ParseRequired(invalidDate, "testDate"));
    }

    [Fact]
    public void ValidateDateRange_WithValidRange_DoesNotThrow()
    {
        // Arrange
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 13);

        // Act & Assert
        var exception = Record.Exception(() =>
            DateHelper.ValidateDateRange(startDate, endDate, "startDate", "endDate"));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDateRange_WithSameDate_DoesNotThrow()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 13);

        // Act & Assert
        var exception = Record.Exception(() =>
            DateHelper.ValidateDateRange(date, date, "startDate", "completionDate"));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateDateRange_WithStartAfterEnd_ThrowsArgumentException()
    {
        // Arrange
        var startDate = new DateOnly(2025, 1, 13);
        var endDate = new DateOnly(2025, 1, 1);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            DateHelper.ValidateDateRange(startDate, endDate, "startDate", "completionDate"));

        Assert.Contains("startDate must be less than or equal to completionDate", ex.Message);
    }
}
