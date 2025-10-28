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

    /// <summary>
    /// Get a single time entry by ID.
    /// Returns null if entry not found.
    /// </summary>
    [UseProjection]
    public async Task<TimeEntry?> GetTimeEntry(
        Guid id,
        [Service] TimeReportingDbContext context)
    {
        return await context.TimeEntries
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Get all projects with filtering, sorting, and projection capabilities.
    /// Supports filtering by any field (e.g., isActive) and sorting.
    /// </summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Project> GetProjects([Service] TimeReportingDbContext context)
    {
        return context.Projects;
    }

    /// <summary>
    /// Get a single project by code with all navigation properties.
    /// Returns null if project not found.
    /// </summary>
    public async Task<Project?> GetProject(
        string code,
        [Service] TimeReportingDbContext context)
    {
        return await context.Projects
            .Include(p => p.AvailableTasks)
            .Include(p => p.Tags)
                .ThenInclude(t => t.AllowedValues)
            .FirstOrDefaultAsync(p => p.Code == code);
    }
}
