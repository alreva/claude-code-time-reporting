using TimeReportingMcpSdk.Tools;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Tests for HelloTool SDK-based implementation
/// </summary>
public class HelloToolTests
{
    [Fact]
    public void HelloTool_HasMcpServerToolTypeAttribute()
    {
        // Arrange & Act
        var toolType = typeof(HelloTool);
        var attribute = toolType.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false);

        // Assert
        Assert.Single(attribute);
    }

    [Fact]
    public void HelloTool_HelloMethod_HasMcpServerToolAttribute()
    {
        // Arrange & Act
        var method = typeof(HelloTool).GetMethod("Hello");
        var attribute = method?.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.Single(attribute!);
    }

    [Fact]
    public void HelloTool_HelloMethod_ReturnsTask()
    {
        // Arrange & Act
        var method = typeof(HelloTool).GetMethod("Hello");

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(Task<string>));
    }
}
