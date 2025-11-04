using System.Security.Claims;
using HotChocolate.Authorization;
using TimeReportingApi.Data;
using TimeReportingApi.Extensions;
using TimeReportingApi.Models;

namespace TimeReportingApi.GraphQL;

public class Query
{
    public string Hello() => "Hello, GraphQL!";

    /// <summary>
    /// Get time entries with filtering, sorting, and pagination.
    /// HotChocolate automatically generates filtering and sorting capabilities.
    /// Order matters: UsePaging -> UseProjection -> UseFiltering -> UseSorting
    /// Phase 14: Requires authentication and automatically filters by authenticated user.
    /// Security: Users can only see their own time entries.
    /// </summary>
    [Authorize]
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TimeEntry> GetTimeEntries(
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Extract authenticated user's ID from JWT token
        var userId = user.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User ID not found in authentication token");
        }

        // Security: Filter to only return entries belonging to the authenticated user
        return context.TimeEntries.Where(e => e.UserId == userId);
    }

    /// <summary>
    /// Get a single time entry by ID.
    /// Returns null if entry not found or doesn't belong to authenticated user.
    /// Phase 14: Requires authentication and filters by authenticated user.
    /// Security: Users can only access their own time entries.
    /// </summary>
    [Authorize]
    [UseProjection]
    public async Task<TimeEntry?> GetTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Extract authenticated user's ID from JWT token
        var userId = user.GetUserId();

        if (string.IsNullOrEmpty(userId))
        {
            throw new GraphQLException("User ID not found in authentication token");
        }

        // Security: Only return entry if it belongs to the authenticated user
        return await context.TimeEntries
            .Include(e => e.Tags)
                .ThenInclude(t => t.TagValue)
                    .ThenInclude(tv => tv.ProjectTag)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
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
