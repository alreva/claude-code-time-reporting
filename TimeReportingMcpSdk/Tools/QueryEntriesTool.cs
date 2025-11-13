using System.ComponentModel;
using System.Linq;
using System.Text;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Generated;

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
- Success: List of matching entries with ID, project, task, hours, dates, status, user
- No matches: 'No time entries found matching the criteria'
- Error: Detailed error message with troubleshooting suggestions")]
    public async Task<string> QueryTimeEntries(
        [Description("Filter by project code (optional)")] string? projectCode = null,
        [Description("Filter by start date YYYY-MM-DD (optional)")] string? startDate = null,
        [Description("Filter by end date YYYY-MM-DD (optional)")] string? endDate = null,
        [Description("Filter by status: NOT_REPORTED, SUBMITTED, APPROVED, DECLINED (optional)")] string? status = null,
        [Description("Filter by user email (optional)")] string? userEmail = null)
    {
        try
        {
            DateOnly? startDateParsed = !string.IsNullOrEmpty(startDate) ? DateOnly.Parse(startDate) : null;
            DateOnly? endDateParsed = !string.IsNullOrEmpty(endDate) ? DateOnly.Parse(endDate) : null;
            TimeEntryStatus? statusParsed = !string.IsNullOrEmpty(status) ? Enum.Parse<TimeEntryStatus>(status) : null;

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
            if (entries.Count == 0)
            {
                return "No time entries found matching the criteria.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Time Entries ({entries.Count}):\n");

            // Table header
            sb.AppendLine("| Date       | Project       | Task         | Hours | Status       | Tags                  | User         |");
            sb.AppendLine("|------------|---------------|--------------|-------|--------------|----------------------|--------------|");

            foreach (var entry in entries)
            {
                // Format dates
                var dateRange = entry.StartDate == entry.CompletionDate
                    ? $"{entry.StartDate}"
                    : $"{entry.StartDate}-{entry.CompletionDate}";

                // Format hours
                var hoursFormatted = entry.OvertimeHours > 0
                    ? $"{entry.StandardHours}+{entry.OvertimeHours}OT"
                    : $"{entry.StandardHours}";

                // Format tags
                var tagsFormatted = entry.Tags?.Any() == true
                    ? string.Join(", ", entry.Tags.Select(t => $"{t.TagValue.ProjectTag.TagName}:{t.TagValue.Value}"))
                    : "-";

                // Format project code
                var projectFormatted = entry.Project.Code.Length > 13
                    ? entry.Project.Code.Substring(0, 10) + "..."
                    : entry.Project.Code;

                // Format task name
                var taskFormatted = entry.ProjectTask.TaskName.Length > 12
                    ? entry.ProjectTask.TaskName.Substring(0, 9) + "..."
                    : entry.ProjectTask.TaskName;

                // Format user email (just username part)
                var userFormatted = entry.UserEmail?.Contains('@') == true
                    ? entry.UserEmail.Substring(0, entry.UserEmail.IndexOf('@'))
                    : entry.UserEmail ?? "";
                userFormatted = userFormatted.Length > 12 ? userFormatted.Substring(0, 9) + "..." : userFormatted;

                // Format status
                var statusFormatted = entry.Status.ToString();

                sb.AppendLine($"| {dateRange,-10} | {projectFormatted,-13} | {taskFormatted,-12} | {hoursFormatted,-5} | {statusFormatted,-12} | {tagsFormatted,-20} | {userFormatted,-12} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
