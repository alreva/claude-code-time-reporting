using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class SubmitEntryTool
{
    private readonly ITimeReportingClient _client;

    public SubmitEntryTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = false,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description("Submit a time entry for approval")]
    public async Task<string> SubmitTimeEntry(
        [Description("Entry ID to submit")] [Required] string id)
    {
        try
        {
            var entryId = Guid.Parse(id);
            var result = await _client.SubmitTimeEntry.ExecuteAsync(entryId);

            if (result.Errors is { Count: > 0 })
            {
                var errors = string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
                return $"""
                         ❌ Failed to submit time entry:

                         {errors}
                         """;
            }

            var entry = result.Data!.SubmitTimeEntry;
            return $"""
                     ✅ Time entry submitted successfully!

                     ID: {entry.Id}
                     New Status: {entry.Status}
                     Updated At: {entry.UpdatedAt}
                     """;
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
