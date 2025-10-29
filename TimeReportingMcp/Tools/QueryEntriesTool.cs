using System.Text;
using System.Text.Json;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Models;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for querying time entries with optional filters
/// </summary>
public class QueryEntriesTool
{
    private readonly ITimeReportingClient _client;

    public QueryEntriesTool(ITimeReportingClient client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse filters (all optional)
            string? projectCode = null;
            DateOnly? startDate = null;
            DateOnly? endDate = null;
            TimeEntryStatus? status = null;

            if (arguments.TryGetProperty("projectCode", out var proj))
            {
                projectCode = proj.GetString();
            }

            if (arguments.TryGetProperty("startDate", out var sd))
            {
                startDate = DateOnly.Parse(sd.GetString()!);
            }

            if (arguments.TryGetProperty("endDate", out var ed))
            {
                endDate = DateOnly.Parse(ed.GetString()!);
            }

            if (arguments.TryGetProperty("status", out var stat))
            {
                status = Enum.Parse<TimeEntryStatus>(stat.GetString()!, true);
            }

            // 2. Execute strongly-typed query
            var result = await _client.QueryTimeEntries.ExecuteAsync(projectCode, startDate, endDate, status);

            // 3. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 4. Return formatted results
            return CreateSuccessResult(result.Data!.TimeEntries?.Nodes?.ToList() ?? new List<IQueryTimeEntries_TimeEntries_Nodes>());
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private ToolResult CreateSuccessResult(List<IQueryTimeEntries_TimeEntries_Nodes> entries)
    {
        if (entries.Count == 0)
        {
            return new ToolResult
            {
                Content = new List<ContentItem>
                {
                    ContentItem.CreateText("📋 No time entries found matching your criteria.")
                }
            };
        }

        var message = new StringBuilder();
        message.AppendLine($"📋 Found {entries.Count} time entries:\n");

        // Group by project for better readability
        var groupedEntries = entries.GroupBy(e => e.Project.Code).OrderBy(g => g.Key);

        foreach (var group in groupedEntries)
        {
            message.AppendLine($"**{group.Key}**");
            foreach (var entry in group.OrderByDescending(e => e.StartDate))
            {
                message.Append($"  • {entry.StartDate}");

                if (entry.StartDate != entry.CompletionDate)
                {
                    message.Append($" to {entry.CompletionDate}");
                }

                message.Append($" - {entry.ProjectTask.TaskName}");
                message.Append($" - {entry.StandardHours}h");

                if (entry.OvertimeHours > 0)
                {
                    message.Append($" (+{entry.OvertimeHours}h OT)");
                }

                message.Append($" [{entry.Status}]");

                if (!string.IsNullOrEmpty(entry.IssueId))
                {
                    message.Append($" - {entry.IssueId}");
                }

                message.AppendLine();

                if (!string.IsNullOrEmpty(entry.Description))
                {
                    var shortDesc = entry.Description.Length > 60
                        ? entry.Description.Substring(0, 57) + "..."
                        : entry.Description;
                    message.AppendLine($"    {shortDesc}");
                }

                message.AppendLine($"    ID: {entry.Id}");
            }
            message.AppendLine();
        }

        // Add summary
        var totalStandard = entries.Sum(e => e.StandardHours);
        var totalOvertime = entries.Sum(e => e.OvertimeHours);
        message.AppendLine($"**Summary:** {totalStandard}h standard");
        if (totalOvertime > 0)
        {
            message.Append($", {totalOvertime}h overtime");
        }
        message.Append($" | Total: {totalStandard + totalOvertime}h");

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(message.ToString())
            }
        };
    }

    private ToolResult CreateErrorResult(global::System.Collections.Generic.IReadOnlyList<global::StrawberryShake.IClientError>? errors)
    {
        var errorMessage = "❌ Failed to query time entries:\n\n";
        if (errors != null)
        {
            errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));
        }

        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText(errorMessage)
            },
            IsError = true
        };
    }

    private ToolResult CreateExceptionResult(Exception ex)
    {
        return new ToolResult
        {
            Content = new List<ContentItem>
            {
                ContentItem.CreateText($"❌ Error: {ex.Message}")
            },
            IsError = true
        };
    }
}
