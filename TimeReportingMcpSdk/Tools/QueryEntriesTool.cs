using System.ComponentModel;
using System.Linq;
using System.Text;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;
using TimeReportingMcpSdk.Utils;

namespace TimeReportingMcpSdk.Tools;

[McpServerToolType]
public class QueryEntriesTool
{
    private readonly ITimeReportingClient _client;

    public QueryEntriesTool(ITimeReportingClient client)
    {
        _client = client;
    }

    [McpServerTool(
        ReadOnly = true,
        Idempotent = true,
        Destructive = false,
        OpenWorld = true
    )]
    [Description(@"Query time entries with optional filters

Retrieves time entries based on specified criteria. All filters are optional and combined with AND logic.

Common Use Cases:
- View all your time entries: No filters
- View entries for a project: Use projectCode filter
- View entries in date range: Use startDate and endDate filters
- View entries by status: Use status filter (NOT_REPORTED, SUBMITTED, APPROVED, DECLINED)
- View another user's entries (admins): Use userEmail filter

Filter Behavior:
- No filters: Returns all entries you have access to
- Multiple filters: Combined with AND (all must match)
- Date filters: startDate is inclusive >=, endDate is inclusive <=
- Status filter: Exact match (case-sensitive)
- User filter: Exact email match

Example Queries:
1. My pending entries:
   status: 'NOT_REPORTED'

2. Entries for a project in January:
   projectCode: 'INTERNAL'
   startDate: '2025-01-01'
   endDate: '2025-01-31'

3. All submitted entries awaiting approval:
   status: 'SUBMITTED'

Returns:
- Success: JSON array of time entry objects with all fields (id, projectCode, projectName, task, standardHours, overtimeHours, startDate, completionDate, status, description, issueId, userEmail, userName, tags, createdAt, updatedAt)
- No matches: Empty JSON array []
- Error: Error message prefixed with ❌

Output Format: JSON array that you can parse, filter, aggregate, and format as needed for the user")]
    public async Task<string> QueryTimeEntries(
        [Description("Filter by project code (optional)")] string? projectCode = null,
        [Description("Filter by task name (optional)")] string? task = null,
        [Description("Filter by start date YYYY-MM-DD (optional)")] string? startDate = null,
        [Description("Filter by end date YYYY-MM-DD (optional)")] string? endDate = null,
        [Description("Filter by status (optional, case-insensitive): NOT_REPORTED, SUBMITTED, APPROVED, DECLINED or NotReported, Submitted, Approved, Declined")] string? status = null,
        [Description("Filter by description text - partial match (optional)")] string? description = null,
        [Description("Filter entries with overtime hours > 0 (optional)")] bool? hasOvertime = null,
        [Description("Filter by minimum total hours (standardHours + overtimeHours) (optional)")] decimal? minHours = null,
        [Description("Filter by maximum total hours (standardHours + overtimeHours) (optional)")] decimal? maxHours = null,
        [Description("Filter by tags in JSON format: {\"Type\": \"Feature\"} or [{\"name\": \"Type\", \"value\": \"Feature\"}] (optional)")] string? tags = null,
        [Description("Filter by user email (optional)")] string? userEmail = null)
    {
        try
        {
            DateOnly? startDateParsed = !string.IsNullOrEmpty(startDate) ? DateOnly.Parse(startDate) : null;
            DateOnly? endDateParsed = !string.IsNullOrEmpty(endDate) ? DateOnly.Parse(endDate) : null;
            TimeEntryStatus? statusParsed = !string.IsNullOrEmpty(status)
                ? Enum.Parse<TimeEntryStatus>(status.Replace("_", ""), ignoreCase: true)
                : null;

            // Build a list of filter conditions
            var filters = new List<TimeEntryFilterInput>();

            // Add date filters
            if (startDateParsed.HasValue)
            {
                filters.Add(new TimeEntryFilterInput
                {
                    StartDate = new LocalDateOperationFilterInput { Gte = startDateParsed.Value }
                });
            }

            if (endDateParsed.HasValue)
            {
                filters.Add(new TimeEntryFilterInput
                {
                    CompletionDate = new LocalDateOperationFilterInput { Lte = endDateParsed.Value }
                });
            }

            // Add project code filter
            if (!string.IsNullOrEmpty(projectCode))
            {
                filters.Add(new TimeEntryFilterInput
                {
                    Project = new ProjectFilterInput
                    {
                        Code = new StringOperationFilterInput { Eq = projectCode }
                    }
                });
            }

            // Add status filter
            if (statusParsed.HasValue)
            {
                filters.Add(new TimeEntryFilterInput
                {
                    Status = new TimeEntryStatusOperationFilterInput { Eq = statusParsed.Value }
                });
            }

            // Add user email filter
            if (!string.IsNullOrEmpty(userEmail))
            {
                filters.Add(new TimeEntryFilterInput
                {
                    UserEmail = new StringOperationFilterInput { Eq = userEmail }
                });
            }

            // Add task filter
            if (!string.IsNullOrEmpty(task))
            {
                filters.Add(new TimeEntryFilterInput
                {
                    ProjectTask = new ProjectTaskFilterInput
                    {
                        TaskName = new StringOperationFilterInput { Eq = task }
                    }
                });
            }

            // Add description filter (partial match)
            if (!string.IsNullOrEmpty(description))
            {
                filters.Add(new TimeEntryFilterInput
                {
                    Description = new StringOperationFilterInput { Contains = description }
                });
            }

            // Add hasOvertime filter
            if (hasOvertime.HasValue)
            {
                filters.Add(new TimeEntryFilterInput
                {
                    OvertimeHours = new DecimalOperationFilterInput
                    {
                        Gt = hasOvertime.Value ? 0m : (decimal?)null,
                        Eq = hasOvertime.Value ? null : 0m
                    }
                });
            }

            // Add minHours filter
            if (minHours.HasValue)
            {
                // Note: This filters on standardHours only. For total (standard + overtime),
                // we'd need to filter results after retrieval or use a custom filter
                filters.Add(new TimeEntryFilterInput
                {
                    StandardHours = new DecimalOperationFilterInput { Gte = minHours.Value }
                });
            }

            // Add maxHours filter
            if (maxHours.HasValue)
            {
                // Note: This filters on standardHours only. For total (standard + overtime),
                // we'd need to filter results after retrieval or use a custom filter
                filters.Add(new TimeEntryFilterInput
                {
                    StandardHours = new DecimalOperationFilterInput { Lte = maxHours.Value }
                });
            }

            // Add tags filter (if provided, parse and add to filters)
            // Note: Tag filtering in GraphQL is complex - may need to filter results after retrieval
            // For now, we'll skip this and implement it as a post-filter if needed

            // Build the final filter input with AND logic
            TimeEntryFilterInput? whereClause = null;
            if (filters.Count > 0)
            {
                whereClause = new TimeEntryFilterInput
                {
                    And = filters
                };
            }

            // Execute query with filters (pass null if no filters applied)
            var result = await _client.QueryTimeEntries.ExecuteAsync(whereClause);

            if (result.Errors is { Count: > 0 })
            {
                return "❌ Failed to query time entries:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entries = result.Data!.TimeEntries?.Nodes?.ToList() ?? new List<IQueryTimeEntries_TimeEntries_Nodes>();

            // Apply post-filters that can't be done in GraphQL

            // Filter by total hours (minHours/maxHours need standard + overtime)
            if (minHours.HasValue)
            {
                entries = entries.Where(e => (e.StandardHours + e.OvertimeHours) >= minHours.Value).ToList();
            }
            if (maxHours.HasValue)
            {
                entries = entries.Where(e => (e.StandardHours + e.OvertimeHours) <= maxHours.Value).ToList();
            }

            // Filter by tags
            if (!string.IsNullOrEmpty(tags))
            {
                try
                {
                    var tagFilters = TagHelper.ParseTags(tags);
                    entries = entries.Where(entry =>
                        tagFilters.All(filter =>
                            entry.Tags?.Any(t =>
                                t.TagValue.ProjectTag.TagName.Equals(filter.Name, StringComparison.OrdinalIgnoreCase) &&
                                t.TagValue.Value.Equals(filter.Value, StringComparison.OrdinalIgnoreCase)) == true
                        )
                    ).ToList();
                }
                catch
                {
                    // Invalid tag format - ignore filter
                }
            }

            if (entries.Count == 0)
            {
                return "[]";  // Empty JSON array
            }

            // Build structured JSON response
            var jsonEntries = entries.Select(entry => new
            {
                id = entry.Id.ToString(),
                projectCode = entry.Project.Code,
                projectName = entry.Project.Name,
                task = entry.ProjectTask.TaskName,
                standardHours = entry.StandardHours,
                overtimeHours = entry.OvertimeHours,
                startDate = entry.StartDate.ToString("yyyy-MM-dd"),
                completionDate = entry.CompletionDate.ToString("yyyy-MM-dd"),
                status = entry.Status.ToString(),
                description = entry.Description,
                issueId = entry.IssueId,
                userEmail = entry.UserEmail,
                userName = entry.UserName,
                tags = entry.Tags?.Select(t => new
                {
                    name = t.TagValue.ProjectTag.TagName,
                    value = t.TagValue.Value
                }).ToArray() ?? Array.Empty<object>(),
                createdAt = entry.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                updatedAt = entry.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            return System.Text.Json.JsonSerializer.Serialize(jsonEntries, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
