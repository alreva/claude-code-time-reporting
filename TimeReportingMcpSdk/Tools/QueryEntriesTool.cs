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
    [Description("""
                 Query time entries with optional filters, pagination, and sorting

                 Retrieves time entries based on specified criteria. All filters are optional and combined with AND logic.

                 Common Use Cases:
                 - View all your time entries: No filters
                 - View entries for a project: Use projectCode filter
                 - View entries in date range: Use startDate and endDate filters
                 - View entries by status: Use status filter (NOT_REPORTED, SUBMITTED, APPROVED, DECLINED)
                 - View another user's entries (admins): Use userEmail filter
                 - Get earliest 10 entries: Use take=10, sortBy='startDate', sortOrder='asc'
                 - Get latest 5 entries: Use take=5, sortBy='startDate', sortOrder='desc'

                 Pagination:
                 - Use 'take' parameter to limit results (e.g., take=10 for first 10 entries)
                 - Use 'skip' parameter for offset pagination (e.g., skip=10 to skip first 10 entries)
                 - Response includes pageInfo with hasNextPage, hasPreviousPage

                 Sorting:
                 - Use 'sortBy' to specify field: 'startDate', 'completionDate', or 'standardHours'
                 - Use 'sortOrder' to specify direction: 'asc' (ascending, default) or 'desc' (descending)
                 - Sorting is applied before pagination

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

                 4. Earliest 10 entries from November:
                    startDate: '2025-11-01'
                    endDate: '2025-11-30'
                    take: 10
                    sortBy: 'startDate'
                    sortOrder: 'asc'

                 5. Next 10 entries from November (skip first 10):
                    startDate: '2025-11-01'
                    endDate: '2025-11-30'
                    skip: 10
                    take: 10
                    sortBy: 'startDate'
                    sortOrder: 'asc'

                 6. Latest 5 entries with most hours:
                    take: 5
                    sortBy: 'standardHours'
                    sortOrder: 'desc'

                 Returns:
                 - Success: JSON array of time entry objects with all fields (id, projectCode, projectName, task, standardHours, overtimeHours, startDate, completionDate, status, description, issueId, userEmail, userName, tags, createdAt, updatedAt)
                 - No matches: Empty JSON array []
                 - Error: Error message prefixed with ❌

                 Output Format: JSON array that you can parse, filter, aggregate, and format as needed for the user
                 """)]
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
        [Description("Filter by user email (optional)")] string? userEmail = null,
        [Description("Skip first N entries (offset pagination) (optional)")] int? skip = null,
        [Description("Take/limit N entries (optional)")] int? take = null,
        [Description("Sort by field: startDate, completionDate, standardHours (optional)")] string? sortBy = null,
        [Description("Sort order: asc or desc (default: asc) (optional)")] string? sortOrder = null)
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

            // Local helper function to conditionally add filters
            void Add(Func<TimeEntryFilterInput> factory, bool condition)
            {
                if (condition) filters.Add(factory());
            }

            // Add filters using helper
            Add(() => new TimeEntryFilterInput
            {
                StartDate = new LocalDateOperationFilterInput { Gte = startDateParsed!.Value }
            }, startDateParsed.HasValue);

            Add(() => new TimeEntryFilterInput
            {
                CompletionDate = new LocalDateOperationFilterInput { Lte = endDateParsed!.Value }
            }, endDateParsed.HasValue);

            Add(() => new TimeEntryFilterInput
            {
                Project = new ProjectFilterInput
                {
                    Code = new StringOperationFilterInput { Eq = projectCode }
                }
            }, !string.IsNullOrEmpty(projectCode));

            Add(() => new TimeEntryFilterInput
            {
                Status = new TimeEntryStatusOperationFilterInput { Eq = statusParsed!.Value }
            }, statusParsed.HasValue);

            Add(() => new TimeEntryFilterInput
            {
                UserEmail = new StringOperationFilterInput { Eq = userEmail }
            }, !string.IsNullOrEmpty(userEmail));

            Add(() => new TimeEntryFilterInput
            {
                ProjectTask = new ProjectTaskFilterInput
                {
                    TaskName = new StringOperationFilterInput { Eq = task }
                }
            }, !string.IsNullOrEmpty(task));

            Add(() => new TimeEntryFilterInput
            {
                Description = new StringOperationFilterInput { Contains = description }
            }, !string.IsNullOrEmpty(description));

            Add(() => new TimeEntryFilterInput
            {
                OvertimeHours = new DecimalOperationFilterInput { Gt = 0m }
            }, hasOvertime == true);

            Add(() => new TimeEntryFilterInput
            {
                OvertimeHours = new DecimalOperationFilterInput { Eq = 0m }
            }, hasOvertime == false);

            Add(() => new TimeEntryFilterInput
            {
                StandardHours = new DecimalOperationFilterInput { Gte = minHours!.Value }
            }, minHours.HasValue);

            Add(() => new TimeEntryFilterInput
            {
                StandardHours = new DecimalOperationFilterInput { Lte = maxHours!.Value }
            }, maxHours.HasValue);

            // Build the final filter input with AND logic
            var whereClause = filters.Count > 0
                ? new TimeEntryFilterInput { And = filters }
                : null;

            // Build sorting input
            var orderInput = BuildSortInput(sortBy, sortOrder);

            IReadOnlyList<TimeEntrySortInput>? BuildSortInput(string? sortByField, string? sortOrderValue)
            {
                if (string.IsNullOrEmpty(sortByField))
                    return null;

                var order = string.IsNullOrEmpty(sortOrderValue) || sortOrderValue.Equals("asc", StringComparison.OrdinalIgnoreCase)
                    ? SortEnumType.Asc
                    : SortEnumType.Desc;

                var sortInput = sortByField.ToLowerInvariant() switch
                {
                    "startdate" => new TimeEntrySortInput { StartDate = order },
                    "completiondate" => new TimeEntrySortInput { CompletionDate = order },
                    "standardhours" => new TimeEntrySortInput { StandardHours = order },
                    _ => null // Invalid sortBy - ignore sorting
                };

                return sortInput != null ? new[] { sortInput } : null;
            }

            // Execute query with filters, pagination, and sorting
            var result = await _client.QueryTimeEntries.ExecuteAsync(
                whereClause,
                skip,
                take,
                orderInput);

            if (result.Errors is { Count: > 0 })
            {
                return "❌ Failed to query time entries:\n\n" +
                       string.Join("\n", result.Errors.Select(e => $"- {e.Message}"));
            }

            var entries = result.Data!.TimeEntries?.Items?.ToList() ?? new List<IQueryTimeEntries_TimeEntries_Items>();

            // Apply post-filters that can't be done in GraphQL

            // Filter by hasOvertime (defensive post-filter in case GraphQL filter doesn't work)
            if (hasOvertime.HasValue)
            {
                if (hasOvertime.Value)
                {
                    entries = entries.Where(e => e.OvertimeHours > 0m).ToList();
                }
                else
                {
                    entries = entries.Where(e => e.OvertimeHours == 0m).ToList();
                }
            }

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
                    if (tagFilters.Count == 0)
                    {
                        return """
                               ❌ Empty tags filter provided. Provide at least one tag to filter by.

                               Valid formats:
                               {"Type":"Feature"}
                               [{"name":"Type","value":"Feature"}]
                               """;
                    }
                    entries = FilterByTags(entries, tagFilters);
                }
                catch (Exception ex)
                {
                    return $$"""
                            ❌ Invalid tags format: {{ex.Message}}

                            Valid formats:
                            {"Type":"Feature"}
                            [{"name":"Type","value":"Feature"}]
                            """;
                }
            }

            static List<IQueryTimeEntries_TimeEntries_Items> FilterByTags(
                List<IQueryTimeEntries_TimeEntries_Items> entries,
                List<TagInput> tagFilters)
            {
                return entries.Where(entry =>
                    tagFilters.All(filter =>
                        entry.Tags?.Any(entryTag =>
                            string.Equals(entryTag.TagValue.ProjectTag.TagName, filter.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(entryTag.TagValue.Value, filter.Value, StringComparison.OrdinalIgnoreCase)) == true
                    )
                ).ToList();
            }

            if (entries.Count == 0)
            {
                return "[]";  // Empty JSON array
            }

            // Build structured JSON response using TimeEntryFormatter
            return TimeEntryFormatter.FormatAsJsonArray(entries);
        }
        catch (Exception ex)
        {
            return $"❌ Error: {ex.Message}";
        }
    }
}
