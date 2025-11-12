using TimeReportingMcpSdk.Tools;

namespace TimeReportingMcpSdk.Tests.Tools;

/// <summary>
/// Tests for LogTimeTool SDK-based implementation
/// </summary>
public class LogTimeToolTests
{
    [Fact]
    public void LogTimeTool_HasMcpServerToolTypeAttribute()
    {
        // Arrange & Act
        var toolType = typeof(LogTimeTool);
        var attribute = toolType.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolTypeAttribute), false);

        // Assert
        Assert.Single(attribute);
    }

    [Fact]
    public void LogTimeTool_LogTimeMethod_HasMcpServerToolAttribute()
    {
        // Arrange & Act
        var method = typeof(LogTimeTool).GetMethod("LogTime");
        var attribute = method?.GetCustomAttributes(typeof(ModelContextProtocol.Server.McpServerToolAttribute), false);

        // Assert
        Assert.NotNull(method);
        Assert.Single(attribute!);
    }

    [Fact]
    public void LogTimeTool_LogTimeMethod_HasRequiredParameters()
    {
        // Arrange & Act
        var method = typeof(LogTimeTool).GetMethod("LogTime");
        var parameters = method?.GetParameters();

        // Assert
        Assert.NotNull(parameters);
        Assert.True(parameters!.Length >= 5, "LogTime should have at least 5 required parameters");

        var parameterNames = parameters.Select(p => p.Name).ToList();
        Assert.Contains("projectCode", parameterNames);
        Assert.Contains("task", parameterNames);
        Assert.Contains("standardHours", parameterNames);
        Assert.Contains("startDate", parameterNames);
        Assert.Contains("completionDate", parameterNames);
    }

    [Fact]
    public void LogTimeTool_LogTimeMethod_ReturnsTaskOfString()
    {
        // Arrange & Act
        var method = typeof(LogTimeTool).GetMethod("LogTime");

        // Assert
        Assert.NotNull(method);
        Assert.True(method.ReturnType == typeof(Task<string>));
    }
}
