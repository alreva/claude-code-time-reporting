using TimeReportingApi.Data;
using TimeReportingApi.GraphQL.Inputs;
using ValidationException = TimeReportingApi.Exceptions.ValidationException;

namespace TimeReportingApi.Services;

/// <summary>
/// Service for validating business rules related to time entries, projects, tasks, and tags.
/// Injected via DI into mutation resolvers.
/// </summary>
public class ValidationService
{
    private readonly TimeReportingDbContext _context;

    public ValidationService(TimeReportingDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Validates that a project exists and is active.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if project does not exist or is inactive.</exception>
    public async Task ValidateProjectAsync(string projectCode)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Code == projectCode);

        if (project == null)
        {
            throw new ValidationException(
                $"Project '{projectCode}' does not exist",
                "projectCode");
        }

        if (!project.IsActive)
        {
            throw new ValidationException(
                $"Project '{projectCode}' is inactive",
                "projectCode");
        }
    }

    /// <summary>
    /// Validates that a task exists for a project and is active.
    /// Uses EF.Property to query shadow FK (ADR 0001).
    /// </summary>
    /// <exception cref="ValidationException">Thrown if task is not available for the project.</exception>
    public async Task ValidateTaskAsync(string projectCode, string taskName)
    {
        var taskExists = await _context.ProjectTasks
            .AnyAsync(t => EF.Property<string>(t, "ProjectCode") == projectCode
                        && t.TaskName == taskName
                        && t.IsActive);

        if (!taskExists)
        {
            throw new ValidationException(
                $"Task '{taskName}' is not available for project '{projectCode}'",
                "task");
        }
    }

    /// <summary>
    /// Validates that tags match the project's tag configuration.
    /// Ensures tag names exist and values are in the allowed list.
    /// Uses EF.Property to query shadow FK (ADR 0001).
    /// </summary>
    /// <exception cref="ValidationException">Thrown if tags are invalid.</exception>
    public async Task ValidateTagsAsync(string projectCode, List<TagInput>? tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return; // Tags are optional
        }

        // Load all project tags with their allowed values
        var projectTags = await _context.ProjectTags
            .Include(pt => pt.AllowedValues)
            .Where(pt => EF.Property<string>(pt, "ProjectCode") == projectCode && pt.IsActive)
            .ToListAsync();

        foreach (var tag in tags)
        {
            // Check if tag name exists for this project
            var projectTag = projectTags.FirstOrDefault(pt => pt.TagName == tag.Name);

            if (projectTag == null)
            {
                throw new ValidationException(
                    $"Tag '{tag.Name}' is not configured for project '{projectCode}'",
                    "tags");
            }

            // Check if value is in allowed values
            var allowedValue = projectTag.AllowedValues
                .FirstOrDefault(v => v.Value == tag.Value);

            if (allowedValue == null)
            {
                var allowedValues = string.Join(", ", projectTag.AllowedValues.Select(v => v.Value));
                throw new ValidationException(
                    $"Value '{tag.Value}' is not allowed for tag '{tag.Name}'. Allowed values: {allowedValues}",
                    "tags");
            }
        }
    }

    /// <summary>
    /// Validates that start date is before or equal to completion date.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if date range is invalid.</exception>
    public void ValidateDateRange(DateOnly startDate, DateOnly completionDate)
    {
        if (startDate > completionDate)
        {
            throw new ValidationException(
                "StartDate must be less than or equal to CompletionDate",
                "startDate");
        }
    }

    /// <summary>
    /// Validates that hours values are non-negative.
    /// </summary>
    /// <exception cref="ValidationException">Thrown if hours are negative.</exception>
    public void ValidateHours(decimal standardHours, decimal overtimeHours)
    {
        if (standardHours < 0)
        {
            throw new ValidationException(
                "StandardHours must be greater than or equal to 0",
                "standardHours");
        }

        if (overtimeHours < 0)
        {
            throw new ValidationException(
                "OvertimeHours must be greater than or equal to 0",
                "overtimeHours");
        }
    }
}
