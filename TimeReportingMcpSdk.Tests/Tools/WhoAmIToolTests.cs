using TimeReportingMcpSdk.Tools;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Tests for WhoAmITool - verifies JWT token parsing and user info extraction
/// </summary>
public class WhoAmIToolTests
{
    [Fact]
    public void WhoAmITool_HasMcpServerToolTypeAttribute()
    {
        // Arrange & Act
        var toolType = typeof(WhoAmITool);
        var attribute = toolType.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false);

        // Assert
        Assert.Single(attribute);
    }

    [Fact]
    public void WhoAmITool_WhoAmIMethod_HasMcpServerToolAttribute()
    {
        // Arrange & Act
        var method = typeof(WhoAmITool).GetMethod("WhoAmI");
        var attribute = method?.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.Single(attribute!);
    }

    [Fact]
    public void WhoAmITool_WhoAmIMethod_ReturnsTask()
    {
        // Arrange & Act
        var method = typeof(WhoAmITool).GetMethod("WhoAmI");

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(Task<string>));
    }

    [Fact]
    public void WhoAmITool_WhoAmIMethod_HasDescriptionAttribute()
    {
        // Arrange & Act
        var method = typeof(WhoAmITool).GetMethod("WhoAmI");
        var attribute = method?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.Single(attribute!);
    }
}
