using System.ComponentModel;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

/// <summary>
/// Hello tool to test GraphQL API connectivity
/// </summary>
[McpServerToolType]
public class HelloTool
{
    private readonly ITimeReportingClient _client;

    public HelloTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("Test connectivity to the GraphQL API. Calls the { hello } query and returns its response.")]
    public async Task<string> Hello()
    {
        try
        {
            var result = await _client.Hello.ExecuteAsync();

            if (result.Errors?.Count > 0)
            {
                return $"❌ GraphQL error: {string.Join('\n', result.Errors.Select(e => e.Message))}";
            }

            return $"✅ GraphQL responded: {result.Data!.Hello}";
        }
        catch (Exception ex)
        {
            return $"❌ Exception: {ex.Message}";
        }
    }
}
