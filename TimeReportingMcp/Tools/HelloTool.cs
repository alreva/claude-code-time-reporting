using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

public class HelloTool : IMcpTool
{
    private readonly ITimeReportingClient _client;

    public HelloTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var result = await _client.Hello.ExecuteAsync();

            if (result.Errors?.Count > 0)
            {
                return new ToolResult
                {
                    Content = new()
                    {
                        ContentItem.CreateText(
                            $"❌ GraphQL error: {string.Join('\n', result.Errors.Select(e => e.Message))}")
                    },
                    IsError = true
                };
            }

            var message = $"✅ GraphQL responded: {result.Data!.Hello}";
            return new ToolResult
            {
                Content = new() { ContentItem.CreateText(message) }
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                Content = new() { ContentItem.CreateText($"❌ Exception: {ex.Message}") },
                IsError = true
            };
        }
    }
}