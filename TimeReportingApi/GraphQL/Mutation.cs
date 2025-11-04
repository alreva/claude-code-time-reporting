using System.Security.Claims;
using HotChocolate.Authorization;
using TimeReportingApi.Data;
using TimeReportingApi.Extensions;
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
    /// Phase 14: Requires authentication and automatically captures user identity from token.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        ClaimsPrincipal user,
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

        // Extract user identity from authenticated token (Phase 14)
        var (userId, userEmail, userName) = user.GetUserInfo();

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
            UserId = userId,            // ← Phase 14: From Entra ID 'oid' or 'sub' claim
            UserEmail = userEmail,      // ← Phase 14: From Entra ID 'email' claim
            UserName = userName,        // ← Phase 14: From Entra ID 'name' claim
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
    /// Phase 14: Requires authentication.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> UpdateTimeEntry(
        Guid id,
        UpdateTimeEntryInput input,
        ClaimsPrincipal user,
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
    /// Phase 14: Requires authentication.
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteTimeEntry(
        Guid id,
        ClaimsPrincipal user,
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

    /// <summary>
    /// Move a time entry to a different project and/or task.
    /// This requires revalidation of the project and task, and clears any existing tags
    /// since tag configurations are project-specific.
    /// Only allowed for entries in NOT_REPORTED or DECLINED status.
    /// ADR 0001: Uses navigation properties only for Project and ProjectTask updates.
    /// Phase 14: Requires authentication.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> MoveTaskToProject(
        Guid entryId,
        string newProjectCode,
        string newTask,
        ClaimsPrincipal user,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // Load the existing entry with all navigation properties
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .Include(e => e.Tags)
                .ThenInclude(t => t.TagValue)
                    .ThenInclude(tv => tv.ProjectTag)
            .FirstOrDefaultAsync(e => e.Id == entryId);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{entryId}' not found", "id");
        }

        // Check status - only NOT_REPORTED and DECLINED can be moved
        if (entry.Status == TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot move time entry in SUBMITTED status. Entry must be APPROVED or DECLINED first.");
        }

        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot move time entry in APPROVED status. Approved entries are immutable.");
        }

        // Validate the new project and task
        await validator.ValidateProjectAsync(newProjectCode);
        await validator.ValidateTaskAsync(newProjectCode, newTask);

        // Load the new project entity - ADR 0001: Load entity first
        var newProject = await context.Projects.FindAsync(newProjectCode);
        if (newProject == null)
        {
            throw new Exceptions.ValidationException($"Project '{newProjectCode}' not found", "projectCode");
        }

        // Load the new task entity - ADR 0001: Load entity first
        var newProjectTask = await context.ProjectTasks
            .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == newProjectCode
                          && t.TaskName == newTask);

        // Get the current project code to check if project is changing
        var currentProjectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue;

        // If moving to a different project, clear all tags
        // Tags are project-specific and won't be valid in the new project
        if (currentProjectCode != newProjectCode)
        {
            entry.Tags.Clear();
        }

        // Update the project and task - ADR 0001: Set navigation properties, EF fills shadow FKs
        entry.Project = newProject;
        entry.ProjectTask = newProjectTask;

        // Update timestamp
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Update tags on a time entry.
    /// This is a convenience mutation that only updates tags without requiring other fields.
    /// Tags are validated against the entry's project tag configurations.
    /// Only allowed for entries in NOT_REPORTED or DECLINED status.
    /// ADR 0001: Uses navigation properties for TagValue relationships.
    /// Phase 14: Requires authentication.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> UpdateTags(
        Guid entryId,
        List<TagInput> tags,
        ClaimsPrincipal user,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // Load the existing entry with all navigation properties
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .Include(e => e.Tags)
                .ThenInclude(t => t.TagValue)
                    .ThenInclude(tv => tv.ProjectTag)
            .FirstOrDefaultAsync(e => e.Id == entryId);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{entryId}' not found", "id");
        }

        // Check status - only NOT_REPORTED and DECLINED can be updated
        if (entry.Status == TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot update tags for time entry in SUBMITTED status. Entry must be APPROVED or DECLINED first.");
        }

        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Cannot update tags for time entry in APPROVED status. Approved entries are immutable.");
        }

        // Get the project code from the existing entry (via shadow property)
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Validate tags if provided
        if (tags != null && tags.Count > 0)
        {
            await validator.ValidateTagsAsync(projectCode, tags);
        }

        // Clear existing tags
        entry.Tags.Clear();

        // Add new tags if provided
        if (tags != null && tags.Count > 0)
        {
            foreach (var tagInput in tags)
            {
                var tagValue = await context.TagValues
                    .Include(tv => tv.ProjectTag)
                    .FirstAsync(tv => EF.Property<string>(tv.ProjectTag, "ProjectCode") == projectCode
                                   && tv.ProjectTag.TagName == tagInput.Name
                                   && tv.Value == tagInput.Value);

                entry.Tags.Add(new TimeEntryTag
                {
                    TimeEntry = entry,
                    TagValue = tagValue  // ADR 0001: Navigation property
                });
            }
        }

        // Update timestamp
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Submit a time entry for approval.
    /// Transitions from NOT_REPORTED or DECLINED to SUBMITTED status.
    /// Phase 14: Requires authentication.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> SubmitTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Load the existing entry
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{id}' not found", "id");
        }

        // Check current status - only NOT_REPORTED and DECLINED can be submitted
        if (entry.Status == TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Time entry is already SUBMITTED. Cannot submit again.");
        }

        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Time entry is already APPROVED. Approved entries cannot be resubmitted.");
        }

        // Transition to SUBMITTED status
        entry.Status = TimeEntryStatus.Submitted;
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Approve a submitted time entry.
    /// Transitions from SUBMITTED to APPROVED status.
    /// Once approved, entries become immutable.
    /// Phase 14: Requires authentication (manager role).
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> ApproveTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Load the existing entry
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{id}' not found", "id");
        }

        // Special check for already approved
        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Time entry is already APPROVED.");
        }

        // Check current status - only SUBMITTED can be approved
        if (entry.Status != TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Time entry must be in SUBMITTED status to be approved. Current status: {entry.Status}");
        }

        // Transition to APPROVED status
        entry.Status = TimeEntryStatus.Approved;
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Decline a submitted time entry with a comment.
    /// Transitions from SUBMITTED to DECLINED status.
    /// Declined entries can be edited and resubmitted.
    /// Phase 14: Requires authentication (manager role).
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> DeclineTimeEntry(
        Guid id,
        string comment,
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Validate comment is provided
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new Exceptions.ValidationException("Decline comment is required", "comment");
        }

        // Load the existing entry
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new Exceptions.ValidationException($"Time entry with ID '{id}' not found", "id");
        }

        // Special check for already approved
        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new Exceptions.BusinessRuleException(
                $"Time entry is already APPROVED. Approved entries cannot be declined.");
        }

        // Check current status - only SUBMITTED can be declined
        if (entry.Status != TimeEntryStatus.Submitted)
        {
            throw new Exceptions.BusinessRuleException(
                $"Time entry must be in SUBMITTED status to be declined. Current status: {entry.Status}");
        }

        // Transition to DECLINED status with comment
        entry.Status = TimeEntryStatus.Declined;
        entry.DeclineComment = comment;
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }
}
