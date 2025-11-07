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
    /// Validates that the user owns the entry or has Manage (M) permission for the project.
    /// Throws GraphQLException if validation fails.
    /// </summary>
    private void ValidateOwnership(
        TimeEntry entry,
        string projectCode,
        ClaimsPrincipal user,
        string operation)
    {
        var userId = user.GetUserId();
        var isOwner = entry.UserId == userId;
        var hasManagePermission = user.HasPermission($"Project/{projectCode}", Permissions.Manage);

        if (!isOwner && !hasManagePermission)
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to {operation} this time entry. You can only {operation} your own entries unless you have Manage permission.")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Manage (for others' entries)")
                .Build());
        }
    }


    /// <summary>
    /// Create a new time entry with validation.
    /// Validates project, task, tags, date range, and hours before creating the entry.
    /// ADR 0001: Uses navigation properties only, never sets FK properties directly.
    /// Requires authentication and automatically captures user identity from token.
    /// Requires Track (T) permission for the project.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        ClaimsPrincipal user,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // Check ACL permission: User must have "Track" permission for this project
        var resourcePath = $"Project/{input.ProjectCode}";
        if (!user.HasPermission(resourcePath, Permissions.Track))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to log time for project '{input.ProjectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", input.ProjectCode)
                .SetExtension("requiredPermission", "Track")
                .Build());
        }

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

        // Extract user identity from authenticated token
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
            UserId = userId,            // From Entra ID 'oid' or 'sub' claim
            UserEmail = userEmail,      // From Entra ID 'email' claim
            UserName = userName,        // From Entra ID 'name' claim
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
    /// Requires authentication and Edit (E) permission for the project.
    /// Users can only update their own entries unless they have Manage (M) permission.
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

        // Get the project code from the existing entry (via shadow property)
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Check ACL permission: User must have "Edit" permission for this project
        var resourcePath = $"Project/{projectCode}";
        if (!user.HasPermission(resourcePath, Permissions.Edit))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to edit time entries for project '{projectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Edit")
                .Build());
        }

        // Validate ownership: User must own the entry or have Manage permission
        ValidateOwnership(entry, projectCode, user, "update");

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

        // If the entry was DECLINED, reset it to NOT_REPORTED
        // This allows the user to resubmit after making corrections
        if (entry.Status == TimeEntryStatus.Declined)
        {
            entry.Status = TimeEntryStatus.NotReported;
            entry.DeclineComment = null; // Clear the decline comment since it's being corrected
        }

        // Update timestamp
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Delete a time entry.
    /// Only allowed for entries in NOT_REPORTED or DECLINED status.
    /// Requires authentication and Edit (E) permission for the project.
    /// Users can only delete their own entries unless they have Manage (M) permission.
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

        // Get the project code from the existing entry (via shadow property)
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Check ACL permission: User must have "Edit" permission for this project
        var resourcePath = $"Project/{projectCode}";
        if (!user.HasPermission(resourcePath, Permissions.Edit))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to delete time entries for project '{projectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Edit")
                .Build());
        }

        // Validate ownership: User must own the entry or have Manage permission
        ValidateOwnership(entry, projectCode, user, "delete");

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
    /// Requires authentication and Edit (E) permission for both old and new projects.
    /// Users can only move their own entries unless they have Manage (M) permission.
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

        // Get the current project code from the existing entry (via shadow property)
        var currentProjectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Check ACL permission: User must have "Edit" permission for the current project
        var currentResourcePath = $"Project/{currentProjectCode}";
        if (!user.HasPermission(currentResourcePath, Permissions.Edit))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to edit time entries for project '{currentProjectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", currentProjectCode)
                .SetExtension("requiredPermission", "Edit")
                .Build());
        }

        // Check ACL permission: User must have "Edit" permission for the new project
        var newResourcePath = $"Project/{newProjectCode}";
        if (!user.HasPermission(newResourcePath, Permissions.Edit))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to edit time entries for project '{newProjectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", newProjectCode)
                .SetExtension("requiredPermission", "Edit")
                .Build());
        }

        // Validate ownership: User must own the entry or have Manage permission for the current project
        ValidateOwnership(entry, currentProjectCode, user, "move");

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

        // If moving to a different project, clear all tags
        // Tags are project-specific and won't be valid in the new project
        if (currentProjectCode != newProjectCode)
        {
            entry.Tags.Clear();
        }

        // Update the project and task - ADR 0001: Set navigation properties, EF fills shadow FKs
        entry.Project = newProject;
        entry.ProjectTask = newProjectTask;

        // If the entry was DECLINED, reset it to NOT_REPORTED
        // This allows the user to resubmit after making corrections
        if (entry.Status == TimeEntryStatus.Declined)
        {
            entry.Status = TimeEntryStatus.NotReported;
            entry.DeclineComment = null; // Clear the decline comment since it's being corrected
        }

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
    /// Requires authentication and Edit (E) permission for the project.
    /// Users can only update tags on their own entries unless they have Manage (M) permission.
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

        // Get the project code from the existing entry (via shadow property)
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Check ACL permission: User must have "Edit" permission for this project
        var resourcePath = $"Project/{projectCode}";
        if (!user.HasPermission(resourcePath, Permissions.Edit))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to edit time entries for project '{projectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Edit")
                .Build());
        }

        // Validate ownership: User must own the entry or have Manage permission
        ValidateOwnership(entry, projectCode, user, "update tags for");

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
    /// Requires authentication and Track (T) or Edit (E) permission for the project.
    /// Users can only submit their own entries.
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

        // Get the project code from the existing entry (via shadow property)
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue!;

        // Check ACL permission: User must have "Track" or "Edit" permission for this project
        var resourcePath = $"Project/{projectCode}";
        var hasTrackPermission = user.HasPermission(resourcePath, Permissions.Track);
        var hasEditPermission = user.HasPermission(resourcePath, Permissions.Edit);

        if (!hasTrackPermission && !hasEditPermission)
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to submit time entries for project '{projectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Track or Edit")
                .Build());
        }

        // Validate ownership: User must own the entry (no Manage override for submit)
        var userId = user.GetUserId();
        if (entry.UserId != userId)
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage("You can only submit your own time entries")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .Build());
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
    /// Requires authentication and "Approve" permission for the project.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> ApproveTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Load entry with all navigation properties for full data
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .Include(e => e.Tags)
                .ThenInclude(t => t.TagValue)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new GraphQLException($"Time entry {id} not found");
        }

        // Get project code from shadow property
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue;
        if (string.IsNullOrEmpty(projectCode))
        {
            throw new GraphQLException("Time entry has no associated project");
        }

        // Check ACL permission: User must have "Approve" permission for this project
        var resourcePath = $"Project/{projectCode}";
        if (!user.HasPermission(resourcePath, Permissions.Approve))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to approve time entries for project '{projectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Approve")
                .Build());
        }

        // Existing business logic validation
        if (entry.Status != TimeEntryStatus.Submitted)
        {
            throw new GraphQLException($"Cannot approve entry in status {entry.Status}. Only SUBMITTED entries can be approved.");
        }

        // Approve the entry
        entry.Status = TimeEntryStatus.Approved;
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }

    /// <summary>
    /// Decline a submitted time entry with a comment.
    /// Transitions from SUBMITTED to DECLINED status.
    /// Declined entries can be edited and resubmitted.
    /// Requires authentication and "Approve" permission for the project.
    /// </summary>
    [Authorize]
    public async Task<TimeEntry> DeclineTimeEntry(
        Guid id,
        string comment,
        ClaimsPrincipal user,
        [Service] TimeReportingDbContext context)
    {
        // Validate comment
        if (string.IsNullOrWhiteSpace(comment))
        {
            throw new GraphQLException("A comment is required when declining a time entry");
        }

        // Load entry with all navigation properties for full data
        var entry = await context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .Include(e => e.Tags)
                .ThenInclude(t => t.TagValue)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entry == null)
        {
            throw new GraphQLException($"Time entry {id} not found");
        }

        // Get project code from shadow property
        var projectCode = context.Entry(entry).Property<string>("ProjectCode").CurrentValue;
        if (string.IsNullOrEmpty(projectCode))
        {
            throw new GraphQLException("Time entry has no associated project");
        }

        // Check ACL permission: User must have "Approve" permission for this project
        var resourcePath = $"Project/{projectCode}";
        if (!user.HasPermission(resourcePath, Permissions.Approve))
        {
            throw new GraphQLException(new ErrorBuilder()
                .SetMessage($"You are not authorized to decline time entries for project '{projectCode}'")
                .SetCode("AUTH_FORBIDDEN")
                .SetExtension("projectCode", projectCode)
                .SetExtension("requiredPermission", "Approve")
                .Build());
        }

        // Existing business logic validation
        if (entry.Status != TimeEntryStatus.Submitted)
        {
            throw new GraphQLException($"Cannot decline entry in status {entry.Status}. Only SUBMITTED entries can be declined.");
        }

        // Decline the entry
        entry.Status = TimeEntryStatus.Declined;
        entry.DeclineComment = comment;
        entry.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return entry;
    }
}
