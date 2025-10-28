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

    /// <summary>
    /// Update an existing time entry.
    /// Only allowed for entries in NOT_REPORTED or DECLINED status.
    /// All fields are optional - only provided fields will be updated.
    /// ADR 0001: Uses navigation properties only for ProjectTask updates.
    /// </summary>
    public async Task<TimeEntry> UpdateTimeEntry(
        Guid id,
        UpdateTimeEntryInput input,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // Load the existing entry with navigation properties
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .Include(e => e.Tags)
                .ThenInclude(t => t.TagValue)
                    .ThenInclude(tv => tv.ProjectTag)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{id}' not found", "id");
        }

        // Check status - only NOT_REPORTED and DECLINED can be updated
        if (entry.Status == TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot update time entry in SUBMITTED status. Entry must be APPROVED or DECLINED first.");
        }

        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot update time entry in APPROVED status. Approved entries are immutable.");
        }

        // Get the project code from the existing entry (via shadow property)
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Validate task if provided
        if (input.Task != null)
        {
            await validator.ValidateTaskAsync(projectCode, input.Task);
        }

        // Validate tags if provided
        if (input.Tags != null)
        {
            await validator.ValidateTagsAsync(projectCode, input.Tags);
        }

        // Validate hours if provided
        var standardHours = input.StandardHours ?? entry.StandardHours;
        var overtimeHours = input.OvertimeHours ?? entry.OvertimeHours;
        validator.ValidateHours(standardHours, overtimeHours);

        // Validate date range if either date is provided
        var startDate = input.StartDate ?? entry.StartDate;
        var completionDate = input.CompletionDate ?? entry.CompletionDate;
        validator.ValidateDateRange(startDate, completionDate);

        // Update task if provided - ADR 0001: Load entity and set navigation property
        if (input.Task != null && input.Task != entry.ProjectTask.TaskName)
        {
            var newTask = await context.ProjectTasks
                .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == projectCode
                              && t.TaskName == input.Task);
            entry.ProjectTask = newTask;  // ← Navigation property (ADR 0001)
        }

        // Update simple fields
        if (input.IssueId != null)
        {
            entry.IssueId = input.IssueId;
        }

        if (input.StandardHours.HasValue)
        {
            entry.StandardHours = input.StandardHours.Value;
        }

        if (input.OvertimeHours.HasValue)
        {
            entry.OvertimeHours = input.OvertimeHours.Value;
        }

        if (input.Description != null)
        {
            entry.Description = input.Description;
        }

        if (input.StartDate.HasValue)
        {
            entry.StartDate = input.StartDate.Value;
        }

        if (input.CompletionDate.HasValue)
        {
            entry.CompletionDate = input.CompletionDate.Value;
        }

        // Update tags if provided
        if (input.Tags != null)
        {
            // Remove existing tags
            entry.Tags.Clear();

            // Add new tags
            foreach (var tagInput in input.Tags)
            {
                var tagValue = await context.TagValues
                    .Include(tv => tv.ProjectTag)
                    .FirstAsync(tv => EF.Property<string>(tv.ProjectTag, "ProjectCode") == projectCode
                                   && tv.ProjectTag.TagName == tagInput.Name
                                   && tv.Value == tagInput.Value);

                entry.Tags.Add(new TimeEntryTag
                {
                    TimeEntry = entry,
                    TagValue = tagValue
                });
            }
        }

        // Update timestamp
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Delete a time entry.
    /// Only allowed for entries in NOT_REPORTED or DECLINED status.
    /// </summary>
    public async Task<bool> DeleteTimeEntry(
        Guid id,
        [Service] TimeReportingDbContext context)
    {
        // Load the existing entry
        var entry = await context.TimeEntries
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{id}' not found", "id");
        }

        // Check status - only NOT_REPORTED and DECLINED can be deleted
        if (entry.Status == TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot delete time entry in SUBMITTED status. Entry must be APPROVED or DECLINED first.");
        }

        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot delete time entry in APPROVED status. Approved entries are immutable.");
        }

        // Delete the entry
        context.TimeEntries.Remove(entry);
        await context.SaveChangesAsync();

        return true;
    }
}
