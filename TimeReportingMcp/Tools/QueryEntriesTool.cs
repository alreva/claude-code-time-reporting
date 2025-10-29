using System.Text;
using System.Text.Json;
using GraphQL;
using TimeReportingMcp.Models;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp.Tools;

/// <summary>
/// Tool for querying time entries with optional filters
/// </summary>
public class QueryEntriesTool
{
    private readonly GraphQLClientWrapper _client;

    public QueryEntriesTool(GraphQLClientWrapper client)
    {
        _client = client;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // 1. Parse filters (all optional)
            var filters = ParseFilters(arguments);

            // 2. Build GraphQL query (using first: 50 for simple pagination)
            var query = new GraphQLRequest
            {
                Query = @"
                    query TimeEntries($first: Int) {
                        timeEntries(first: $first) {
                            nodes {
                                id
                                project {
                                    code
                                    name
                                }
                                projectTask {
                                    taskName
                                }
                                issueId
                                standardHours
                                overtimeHours
                                description
                                startDate
                                completionDate
                                status
                                createdAt
                            }
                        }
                    }",
                Variables = new { first = 50 }
            };

            // 3. Execute query
            var response = await _client.SendQueryAsync<QueryTimeEntriesResponse>(query);

            // 4. Handle errors
            if (response.Errors != null && response.Errors.Length > 0)
            {
                return CreateErrorResult(response.Errors);
            }

            // 5. Return formatted results
            return CreateSuccessResult(response.Data.TimeEntries.Nodes, filters);
        }
        catch (Exception ex)
        {
            return CreateExceptionResult(ex);
        }
    }

    private object? ParseFilters(JsonElement arguments)
    {
        // Return null if no arguments provided (query all)
        if (arguments.ValueKind == JsonValueKind.Undefined ||
            arguments.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var filters = new Dictionary<string, object>();

        if (arguments.TryGetProperty("projectCode", out var proj))
        {
            filters["projectCode"] = proj.GetString()!;
        }

        if (arguments.TryGetProperty("status", out var stat))
        {
            filters["status"] = stat.GetString()!;
        }

        if (arguments.TryGetProperty("startDate", out var start))
        {
            filters["startDate"] = start.GetString()!;
        }

        if (arguments.TryGetProperty("endDate", out var end))
        {
            filters["endDate"] = end.GetString()!;
        }

        if (arguments.TryGetProperty("limit", out var lim))
        {
            filters["limit"] = lim.GetInt32();
        }

        if (arguments.TryGetProperty("offset", out var off))
        {
            filters["offset"] = off.GetInt32();
        }

        return filters.Count > 0 ? filters : null;
    }

    private ToolResult CreateSuccessResult(List<TimeEntryData> entries, object? filters)
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

    private ToolResult CreateErrorResult(GraphQL.GraphQLError[] errors)
    {
        var errorMessage = "❌ Failed to query time entries:\n\n";
        errorMessage += string.Join("\n", errors.Select(e => $"- {e.Message}"));

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

// Response type
public class QueryTimeEntriesResponse
{
    public TimeEntriesConnection TimeEntries { get; set; } = null!;
}

public class TimeEntriesConnection
{
    public List<TimeEntryData> Nodes { get; set; } = new();
}
