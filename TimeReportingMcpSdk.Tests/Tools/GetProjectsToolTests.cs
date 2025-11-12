using TimeReportingMcpSdk.Tools;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Tests for GetProjectsTool SDK-based implementation
/// </summary>
public class GetProjectsToolTests
{
    [Fact]
    public void GetProjectsTool_HasMcpServerToolTypeAttribute()
    {
        // Arrange & Act
        var toolType = typeof(GetProjectsTool);
        var attribute = toolType.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false);

        // Assert
        Assert.Single(attribute);
    }

    [Fact]
    public void GetProjectsTool_GetAvailableProjectsMethod_HasMcpServerToolAttribute()
    {
        // Arrange & Act
        var method = typeof(GetProjectsTool).GetMethod("GetAvailableProjects");
        var attribute = method?.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.Single(attribute!);
    }

    [Fact]
    public void GetProjectsTool_GetAvailableProjectsMethod_ReturnsTaskOfString()
    {
        // Arrange & Act
        var method = typeof(GetProjectsTool).GetMethod("GetAvailableProjects");

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(Task<string>));
    }
}
