using TimeReportingApi.Data;
using TimeReportingApi.Models;

namespace TimeReportingApi.GraphQL;

public class Query
{
    public string Hello() => "Hello, GraphQL!";

    /// <summary>
    /// Get time entries with filtering, sorting, and pagination.
    /// HotChocolate automatically generates filtering and sorting capabilities.
    /// Order matters: UsePaging -> UseProjection -> UseFiltering -> UseSorting
    /// </summary>
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TimeEntry> GetTimeEntries([Service] TimeReportingDbContext context)
    {
        return context.TimeEntries;
    }
}
