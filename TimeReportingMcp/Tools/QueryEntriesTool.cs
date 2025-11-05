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

            if (arguments.TryGetProperty("projectCode", out var proj) && proj.ValueKind == JsonValueKind.String)
            {
                projectCode = proj.GetString();
            }

            if (arguments.TryGetProperty("startDate", out var sd) && sd.ValueKind == JsonValueKind.String)
            {
                var dateStr = sd.GetString();
                if (!string.IsNullOrEmpty(dateStr))
                {
                    startDate = DateOnly.Parse(dateStr);
                }
            }

            if (arguments.TryGetProperty("endDate", out var ed) && ed.ValueKind == JsonValueKind.String)
            {
                var dateStr = ed.GetString();
                if (!string.IsNullOrEmpty(dateStr))
                {
                    endDate = DateOnly.Parse(dateStr);
                }
            }

            if (arguments.TryGetProperty("status", out var stat) && stat.ValueKind == JsonValueKind.String)
            {
                var statusStr = stat.GetString();
                if (!string.IsNullOrEmpty(statusStr))
                {
                    status = Enum.Parse<TimeEntryStatus>(statusStr, true);
                }
            }

            // 2. Execute query (fetch all entries, filter client-side)
            // Note: StrawberryShake input type generation would allow server-side filtering
            var result = await _client.QueryTimeEntries.ExecuteAsync(null);

            // 3. Handle errors
            if (result.Errors is { Count: > 0 })
            {
                return CreateErrorResult(result.Errors);
            }

            // 4. Get all entries
            var allEntries = result.Data!.TimeEntries?.Nodes?.ToList() ?? new List<IQueryTimeEntries_TimeEntries_Nodes>();

            // 5. Apply client-side filtering
            var filteredEntries = allEntries.AsEnumerable();

            if (!string.IsNullOrEmpty(projectCode))
            {
                filteredEntries = filteredEntries.Where(e => e.Project.Code == projectCode);
            }

            if (startDate.HasValue)
            {
                filteredEntries = filteredEntries.Where(e => e.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                filteredEntries = filteredEntries.Where(e => e.CompletionDate <= endDate.Value);
            }

            if (status.HasValue)
            {
                filteredEntries = filteredEntries.Where(e => e.Status == status.Value);
            }

            // 6. Return formatted results
            return CreateSuccessResult(filteredEntries.ToList());
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

                // Display decline comment if present
                if (!string.IsNullOrEmpty(entry.DeclineComment))
                {
                    message.AppendLine($"    ⚠️  Decline reason: {entry.DeclineComment}");
                }

                if (!string.IsNullOrEmpty(entry.Description))
                {
                    var shortDesc = entry.Description.Length > 60
                        ? entry.Description.Substring(0, 57) + "..."
                        : entry.Description;
                    message.AppendLine($"    {shortDesc}");
                }

                // Display tags if present
                if (entry.Tags != null && entry.Tags.Any())
                {
                    var tagStrings = entry.Tags
                        .Select(t => $"{t.TagValue.ProjectTag.TagName}: {t.TagValue.Value}")
                        .ToList();
                    message.AppendLine($"    Tags: {string.Join(", ", tagStrings)}");
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
