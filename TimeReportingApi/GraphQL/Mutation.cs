using TimeReportingApi.Data;
using TimeReportingApi.GraphQL.Inputs;
using TimeReportingApi.Models;
using TimeReportingApi.Services;

namespace TimeReportingApi.GraphQL;

public class Mutation
{
    /// <summary>
    /// Create a new time entry with validation.
    /// Validates project, task, tags, date range, and hours before creating the entry.
    /// ADR 0001: Uses navigation properties only, never sets FK properties directly.
    /// </summary>
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // Validate all inputs
        await validator.ValidateProjectAsync(input.ProjectCode);
        await validator.ValidateTaskAsync(input.ProjectCode, input.Task);
        await validator.ValidateTagsAsync(input.ProjectCode, input.Tags);
        validator.ValidateDateRange(input.StartDate, input.CompletionDate);
        validator.ValidateHours(input.StandardHours, input.OvertimeHours ?? 0.0m);

        // ADR 0001: Load parent entities first, then set navigation properties
        // This is the ONLY safe way - EF Core will populate shadow FKs automatically
        var project = await context.Projects.FindAsync(input.ProjectCode);
        if (project == null)
        {
            throw new Exceptions.ValidationException($"Project '{input.ProjectCode}' not found", "projectCode");
        }

        var projectTask = await context.ProjectTasks
            .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == input.ProjectCode
                          && t.TaskName == input.Task);

        // Create the time entry - ADR 0001: Set navigation properties, EF fills shadow FKs
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = project,          // ← Navigation property (ADR 0001)
            ProjectTask = projectTask,  // ← Navigation property (ADR 0001)
            IssueId = input.IssueId,
            StandardHours = input.StandardHours,
            OvertimeHours = input.OvertimeHours ?? 0.0m,
            Description = input.Description,
            StartDate = input.StartDate,
            CompletionDate = input.CompletionDate,
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add tags if provided
        if (input.Tags != null && input.Tags.Count > 0)
        {
            foreach (var tagInput in input.Tags)
            {
                // Find the TagValue entity
                var tagValue = await context.TagValues
                    .Include(tv => tv.ProjectTag)
                    .FirstAsync(tv => EF.Property<string>(tv.ProjectTag, "ProjectCode") == input.ProjectCode
                                   && tv.ProjectTag.TagName == tagInput.Name
                                   && tv.Value == tagInput.Value);

                entry.Tags.Add(new TimeEntryTag
                {
                    TimeEntry = entry,
                    TagValue = tagValue
                });
            }
        }

        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        return entry;
    }
}
