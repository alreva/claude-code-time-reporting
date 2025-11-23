using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tests.Utils;

public class TagHelperTests
{
    [Fact]
    public void ParseTags_ArrayFormat_ReturnsParsedTags()
    {
        // Arrange
        var json = """[{"name": "Type", "value": "Feature"}, {"name": "Environment", "value": "Development"}]""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Type");
        result[0].Value.Should().Be("Feature");
        result[1].Name.Should().Be("Environment");
        result[1].Value.Should().Be("Development");
    }

    [Fact]
    public void ParseTags_DictionaryFormat_ReturnsParsedTags()
    {
        // Arrange
        var json = """{"Type": "Feature", "Environment": "Development"}""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Name == "Type" && t.Value == "Feature");
        result.Should().Contain(t => t.Name == "Environment" && t.Value == "Development");
    }

    [Fact]
    public void ParseTags_ArrayFormat_CaseInsensitive_ReturnsParsedTags()
    {
        // Arrange - lowercase property names
        var json = """[{"name": "Type", "value": "Feature"}]""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Type");
        result[0].Value.Should().Be("Feature");
    }

    [Fact]
    public void ParseTags_ArrayFormat_PascalCase_ReturnsParsedTags()
    {
        // Arrange - PascalCase property names
        var json = """[{"Name": "Type", "Value": "Feature"}]""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Type");
        result[0].Value.Should().Be("Feature");
    }

    [Fact]
    public void ParseTags_DictionaryFormat_CaseInsensitive_ReturnsParsedTags()
    {
        // Arrange - mixed case tag names
        var json = """{"type": "feature", "ENVIRONMENT": "PRODUCTION"}""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Name == "type" && t.Value == "feature");
        result.Should().Contain(t => t.Name == "ENVIRONMENT" && t.Value == "PRODUCTION");
    }

    [Fact]
    public void ParseTags_EmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var json = "[]";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseTags_EmptyDictionary_ReturnsEmptyList()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseTags_SingleTagArray_ReturnsSingleTag()
    {
        // Arrange
        var json = """[{"name": "Type", "value": "Bug"}]""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Type");
        result[0].Value.Should().Be("Bug");
    }

    [Fact]
    public void ParseTags_SingleTagDictionary_ReturnsSingleTag()
    {
        // Arrange
        var json = """{"Billable": "No"}""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Billable");
        result[0].Value.Should().Be("No");
    }

    [Fact]
    public void ParseTags_MultipleTagsDictionary_PreservesAllTags()
    {
        // Arrange
        var json = """{"Type": "Feature", "Environment": "Staging", "Billable": "Yes"}""";

        // Act
        var result = TagHelper.ParseTags(json);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Name == "Type" && t.Value == "Feature");
        result.Should().Contain(t => t.Name == "Environment" && t.Value == "Staging");
        result.Should().Contain(t => t.Name == "Billable" && t.Value == "Yes");
    }

    #region Negative Test Cases

    [Fact]
    public void ParseTags_InvalidJson_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "{not valid json}";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    [Fact]
    public void ParseTags_InvalidStructure_ThrowsArgumentException()
    {
        // Arrange - neither array nor dictionary
        var invalidJson = "\"just a string\"";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    [Fact]
    public void ParseTags_Number_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "123";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    [Fact]
    public void ParseTags_Null_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "null";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    [Fact]
    public void ParseTags_ArrayWithMixedTypes_ThrowsArgumentException()
    {
        // Arrange - array contains non-object elements
        var invalidJson = """[{"name": "Type", "value": "Feature"}, "string", 123]""";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    [Fact]
    public void ParseTags_DictionaryWithNonStringValues_ThrowsArgumentException()
    {
        // Arrange - dictionary with non-string value
        var invalidJson = """{"Type": 123}""";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    [Fact]
    public void ParseTags_UnclosedBrace_ThrowsArgumentException()
    {
        // Arrange
        var invalidJson = "{\"Type\": \"Feature\"";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => TagHelper.ParseTags(invalidJson));
        exception.Message.Should().Contain("Invalid tag format");
    }

    #endregion
}
