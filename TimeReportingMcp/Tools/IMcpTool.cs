using System.Text.Json;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

public interface IMcpTool
{
    Task<ToolResult> ExecuteAsync(JsonElement arguments);
}