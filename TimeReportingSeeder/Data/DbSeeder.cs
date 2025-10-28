using TimeReportingApi.Models;
using TimeReportingApi.Data;

namespace TimeReportingSeeder.Data;

public static class DbSeeder
{
    public static void SeedData(TimeReportingDbContext context)
    {
        Console.WriteLine("DbSeeder: Starting seed data process...");

        // Upsert Projects (idempotent - safe to run multiple times)
        Console.WriteLine("DbSeeder: Upserting projects...");
        UpsertProject(context, "INTERNAL", "Internal Development", true);
        UpsertProject(context, "CLIENT-A", "Client A Project", true);
        UpsertProject(context, "MAINT", "Maintenance & Support", true);
        context.SaveChanges();
        Console.WriteLine("DbSeeder: Projects upserted successfully");

        // Get projects (reload to get identity values)
        var internalProject = context.Projects.Single(p => p.Code == "INTERNAL");
        var clientProject = context.Projects.Single(p => p.Code == "CLIENT-A");
        var maintProject = context.Projects.Single(p => p.Code == "MAINT");

        // Upsert Tasks for INTERNAL
        Console.WriteLine("DbSeeder: Upserting INTERNAL tasks...");
        UpsertProjectTask(context, internalProject, "Architecture", true);
        UpsertProjectTask(context, internalProject, "Development", true);
        UpsertProjectTask(context, internalProject, "Code Review", true);
        UpsertProjectTask(context, internalProject, "Testing", true);
        UpsertProjectTask(context, internalProject, "Documentation", true);
        UpsertProjectTask(context, internalProject, "DevOps", true);

        // Upsert Tasks for CLIENT-A
        Console.WriteLine("DbSeeder: Upserting CLIENT-A tasks...");
        UpsertProjectTask(context, clientProject, "Feature Development", true);
        UpsertProjectTask(context, clientProject, "Bug Fixing", true);
        UpsertProjectTask(context, clientProject, "Maintenance", true);
        UpsertProjectTask(context, clientProject, "Support", true);
        UpsertProjectTask(context, clientProject, "Code Review", true);

        // Upsert Tasks for MAINT
        Console.WriteLine("DbSeeder: Upserting MAINT tasks...");
        UpsertProjectTask(context, maintProject, "Bug Fixing", true);
        UpsertProjectTask(context, maintProject, "Security Patches", true);
        UpsertProjectTask(context, maintProject, "Performance Optimization", true);
        UpsertProjectTask(context, maintProject, "Monitoring", true);

        context.SaveChanges();

        // Upsert Tag Configurations for INTERNAL
        Console.WriteLine("DbSeeder: Upserting INTERNAL tags...");
        var envTag = UpsertProjectTag(context, internalProject, "Environment", false, true);
        var billableTagInternal = UpsertProjectTag(context, internalProject, "Billable", false, true);
        var typeTag = UpsertProjectTag(context, internalProject, "Type", false, true);
        context.SaveChanges();

        // Upsert Tag Values for INTERNAL tags
        UpsertTagValue(context, envTag, "Production");
        UpsertTagValue(context, envTag, "Staging");
        UpsertTagValue(context, envTag, "Development");
        UpsertTagValue(context, billableTagInternal, "Yes");
        UpsertTagValue(context, billableTagInternal, "No");
        UpsertTagValue(context, typeTag, "Feature");
        UpsertTagValue(context, typeTag, "Bug");
        UpsertTagValue(context, typeTag, "Refactor");
        UpsertTagValue(context, typeTag, "Docs");

        // Upsert Tag Configurations for CLIENT-A
        Console.WriteLine("DbSeeder: Upserting CLIENT-A tags...");
        var priorityTag = UpsertProjectTag(context, clientProject, "Priority", true, true);
        var sprintTag = UpsertProjectTag(context, clientProject, "Sprint", false, true);
        var billableTagClient = UpsertProjectTag(context, clientProject, "Billable", false, true);
        context.SaveChanges();

        // Upsert Tag Values for CLIENT-A tags
        UpsertTagValue(context, priorityTag, "High");
        UpsertTagValue(context, priorityTag, "Medium");
        UpsertTagValue(context, priorityTag, "Low");
        UpsertTagValue(context, sprintTag, "Sprint-1");
        UpsertTagValue(context, sprintTag, "Sprint-2");
        UpsertTagValue(context, sprintTag, "Sprint-3");
        UpsertTagValue(context, sprintTag, "Sprint-4");
        UpsertTagValue(context, billableTagClient, "Yes");
        UpsertTagValue(context, billableTagClient, "No");

        // Upsert Tag Configurations for MAINT
        Console.WriteLine("DbSeeder: Upserting MAINT tags...");
        var severityTag = UpsertProjectTag(context, maintProject, "Severity", true, true);
        var billableTagMaint = UpsertProjectTag(context, maintProject, "Billable", false, true);
        context.SaveChanges();

        // Upsert Tag Values for MAINT tags
        UpsertTagValue(context, severityTag, "Critical");
        UpsertTagValue(context, severityTag, "High");
        UpsertTagValue(context, severityTag, "Medium");
        UpsertTagValue(context, severityTag, "Low");
        UpsertTagValue(context, billableTagMaint, "Yes");
        UpsertTagValue(context, billableTagMaint, "No");
        context.SaveChanges();

        // Add sample time entries with tags (skip if already exist)
        Console.WriteLine("DbSeeder: Adding sample time entries...");

        if (context.TimeEntries.Any())
        {
            Console.WriteLine("  - Time entries already exist, skipping sample data");
            Console.WriteLine("DbSeeder: Seed data completed successfully!");
            return;
        }

        var developmentTask = context.ProjectTasks.Single(t => t.TaskName == "Development" && t.Project.Code == "INTERNAL");
        var codeReviewTask = context.ProjectTasks.Single(t => t.TaskName == "Code Review" && t.Project.Code == "INTERNAL");
        var featureDevTask = context.ProjectTasks.Single(t => t.TaskName == "Feature Development" && t.Project.Code == "CLIENT-A");
        var bugFixingTaskClient = context.ProjectTasks.Single(t => t.TaskName == "Bug Fixing" && t.Project.Code == "CLIENT-A");
        var securityPatchesTask = context.ProjectTasks.Single(t => t.TaskName == "Security Patches" && t.Project.Code == "MAINT");

        // Sample time entry 1: INTERNAL Development with Environment=Production, Billable=Yes, Type=Feature
        var entry1 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = internalProject,
            ProjectTask = developmentTask,
            IssueId = "DEV-123",
            StandardHours = 7.5m,
            OvertimeHours = 0m,
            Description = "Implemented user authentication module",
            StartDate = new DateOnly(2025, 10, 21),
            CompletionDate = new DateOnly(2025, 10, 21),
            Status = TimeEntryStatus.Approved,
            UserId = "user-1"
        };
        context.TimeEntries.Add(entry1);
        context.SaveChanges();

        // Add tags to entry1
        var envProdValue = context.TagValues.Single(v => v.Value == "Production" && v.ProjectTag.TagName == "Environment");
        var billableYesInternal = context.TagValues.Single(v => v.Value == "Yes" && v.ProjectTag.TagName == "Billable" && v.ProjectTag.Project.Code == "INTERNAL");
        var typeFeature = context.TagValues.Single(v => v.Value == "Feature");

        var entry1Tags = new[]
        {
            new TimeEntryTag { TimeEntry = entry1, TagValue = envProdValue },
            new TimeEntryTag { TimeEntry = entry1, TagValue = billableYesInternal },
            new TimeEntryTag { TimeEntry = entry1, TagValue = typeFeature }
        };
        context.TimeEntryTags.AddRange(entry1Tags);

        // Sample time entry 2: INTERNAL Code Review with Billable=No
        var entry2 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = internalProject,
            ProjectTask = codeReviewTask,
            StandardHours = 2.0m,
            OvertimeHours = 0m,
            Description = "Reviewed PR #456",
            StartDate = new DateOnly(2025, 10, 21),
            CompletionDate = new DateOnly(2025, 10, 21),
            Status = TimeEntryStatus.Approved,
            UserId = "user-1"
        };
        context.TimeEntries.Add(entry2);
        context.SaveChanges();

        var billableNoInternal = context.TagValues.Single(v => v.Value == "No" && v.ProjectTag.TagName == "Billable" && v.ProjectTag.Project.Code == "INTERNAL");
        var entry2Tags = new[]
        {
            new TimeEntryTag { TimeEntry = entry2, TagValue = billableNoInternal }
        };
        context.TimeEntryTags.AddRange(entry2Tags);

        // Sample time entry 3: CLIENT-A Feature Development with Priority=High, Sprint=Sprint-2
        var entry3 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = clientProject,
            ProjectTask = featureDevTask,
            IssueId = "JIRA-789",
            StandardHours = 6.0m,
            OvertimeHours = 1.5m,
            Description = "Built new dashboard component",
            StartDate = new DateOnly(2025, 10, 22),
            CompletionDate = new DateOnly(2025, 10, 22),
            Status = TimeEntryStatus.Submitted,
            UserId = "user-2"
        };
        context.TimeEntries.Add(entry3);
        context.SaveChanges();

        var priorityHigh = context.TagValues.Single(v => v.Value == "High" && v.ProjectTag.TagName == "Priority");
        var sprint2 = context.TagValues.Single(v => v.Value == "Sprint-2");
        var entry3Tags = new[]
        {
            new TimeEntryTag { TimeEntry = entry3, TagValue = priorityHigh },
            new TimeEntryTag { TimeEntry = entry3, TagValue = sprint2 }
        };
        context.TimeEntryTags.AddRange(entry3Tags);

        // Sample time entry 4: CLIENT-A Bug Fixing with Priority=Medium (NOT_REPORTED)
        var entry4 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = clientProject,
            ProjectTask = bugFixingTaskClient,
            IssueId = "JIRA-790",
            StandardHours = 4.0m,
            OvertimeHours = 0m,
            Description = "Fixed login redirect issue",
            StartDate = new DateOnly(2025, 10, 23),
            CompletionDate = new DateOnly(2025, 10, 23),
            Status = TimeEntryStatus.NotReported,
            UserId = "user-2"
        };
        context.TimeEntries.Add(entry4);
        context.SaveChanges();

        var priorityMedium = context.TagValues.Single(v => v.Value == "Medium" && v.ProjectTag.TagName == "Priority");
        var entry4Tags = new[]
        {
            new TimeEntryTag { TimeEntry = entry4, TagValue = priorityMedium }
        };
        context.TimeEntryTags.AddRange(entry4Tags);

        // Sample time entry 5: MAINT Security Patches with Severity=Critical, Billable=Yes
        var entry5 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = maintProject,
            ProjectTask = securityPatchesTask,
            IssueId = "SEC-101",
            StandardHours = 3.0m,
            OvertimeHours = 0m,
            Description = "Applied security updates to dependencies",
            StartDate = new DateOnly(2025, 10, 23),
            CompletionDate = new DateOnly(2025, 10, 23),
            Status = TimeEntryStatus.Approved,
            UserId = "user-3"
        };
        context.TimeEntries.Add(entry5);
        context.SaveChanges();

        var severityCritical = context.TagValues.Single(v => v.Value == "Critical" && v.ProjectTag.TagName == "Severity");
        var billableYesMaint = context.TagValues.Single(v => v.Value == "Yes" && v.ProjectTag.TagName == "Billable" && v.ProjectTag.Project.Code == "MAINT");
        var entry5Tags = new[]
        {
            new TimeEntryTag { TimeEntry = entry5, TagValue = severityCritical },
            new TimeEntryTag { TimeEntry = entry5, TagValue = billableYesMaint }
        };
        context.TimeEntryTags.AddRange(entry5Tags);

        context.SaveChanges();
        Console.WriteLine("DbSeeder: Seed data completed successfully!");
    }

    // Helper methods for upsert operations (idempotent)
    private static void UpsertProject(TimeReportingDbContext context, string code, string name, bool isActive)
    {
        var existing = context.Projects.Find(code);
        if (existing == null)
        {
            context.Projects.Add(new Project { Code = code, Name = name, IsActive = isActive });
            Console.WriteLine($"  - Created project: {code}");
        }
        else
        {
            existing.Name = name;
            existing.IsActive = isActive;
            Console.WriteLine($"  - Updated project: {code}");
        }
    }

    private static void UpsertProjectTask(TimeReportingDbContext context, Project project, string taskName, bool isActive)
    {
        var existing = context.ProjectTasks
            .FirstOrDefault(t => t.Project.Code == project.Code && t.TaskName == taskName);

        if (existing == null)
        {
            context.ProjectTasks.Add(new ProjectTask
            {
                Project = project,
                TaskName = taskName,
                IsActive = isActive
            });
            Console.WriteLine($"  - Created task: {project.Code}/{taskName}");
        }
        else
        {
            existing.IsActive = isActive;
            Console.WriteLine($"  - Updated task: {project.Code}/{taskName}");
        }
    }

    private static ProjectTag UpsertProjectTag(TimeReportingDbContext context, Project project, string tagName, bool isRequired, bool isActive)
    {
        var existing = context.ProjectTags
            .FirstOrDefault(t => t.Project.Code == project.Code && t.TagName == tagName);

        if (existing == null)
        {
            var newTag = new ProjectTag
            {
                Project = project,
                TagName = tagName,
                IsRequired = isRequired,
                IsActive = isActive
            };
            context.ProjectTags.Add(newTag);
            Console.WriteLine($"  - Created tag: {project.Code}/{tagName}");
            return newTag;
        }
        else
        {
            existing.IsRequired = isRequired;
            existing.IsActive = isActive;
            Console.WriteLine($"  - Updated tag: {project.Code}/{tagName}");
            return existing;
        }
    }

    private static void UpsertTagValue(TimeReportingDbContext context, ProjectTag tag, string value)
    {
        var existing = context.TagValues
            .FirstOrDefault(v => v.ProjectTag.Id == tag.Id && v.Value == value);

        if (existing == null)
        {
            context.TagValues.Add(new TagValue
            {
                ProjectTag = tag,
                Value = value
            });
            Console.WriteLine($"    - Created tag value: {tag.TagName}={value}");
        }
    }
}
