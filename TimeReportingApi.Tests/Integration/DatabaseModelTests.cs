using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Tests for Entity Framework Core model CRUD operations
/// </summary>
public class DatabaseModelTests : IClassFixture<DatabaseFixture>, IDisposable
{
    private readonly DatabaseFixture _fixture;

    public DatabaseModelTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.Cleanup(); // Clean database before each test
    }

    [Fact]
    public async Task Project_CanCreateAndRetrieve()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();
        var project = new Project
        {
            Code = "TEST001",
            Name = "Test Project",
            IsActive = true
        };

        // Act
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.Projects.FindAsync("TEST001");
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Project");
        retrieved.IsActive.Should().BeTrue();
        retrieved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        retrieved.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TimeEntry_CanCreateWithValidData()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ001", Name = "Project 1", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var projectTask = new ProjectTask { ProjectCode = "PROJ001", TaskName = "Development", IsActive = true };
        await context.ProjectTasks.AddAsync(projectTask);
        await context.SaveChangesAsync();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ001",
            ProjectTaskId = projectTask.Id,
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported
        };

        // Act
        await context.TimeEntries.AddAsync(timeEntry);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.TimeEntries
            .Include(te => te.ProjectTask)
            .FirstOrDefaultAsync(te => te.Id == timeEntry.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ProjectCode.Should().Be("PROJ001");
        retrieved.ProjectTaskId.Should().Be(projectTask.Id);
        retrieved.ProjectTask.TaskName.Should().Be("Development");
        retrieved.StandardHours.Should().Be(8.0m);
        retrieved.Status.Should().Be(TimeEntryStatus.NotReported);
    }

    [Fact]
    public async Task TimeEntry_WithNegativeStandardHours_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ002", Name = "Project 2", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var projectTask = new ProjectTask { ProjectCode = "PROJ002", TaskName = "Development", IsActive = true };
        await context.ProjectTasks.AddAsync(projectTask);
        await context.SaveChangesAsync();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ002",
            ProjectTaskId = projectTask.Id,
            StandardHours = -5.0m, // Invalid - violates CHECK constraint
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported
        };

        // Act & Assert - Database-agnostic check constraint validation
        await context.TimeEntries.AddAsync(timeEntry);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "because negative standard_hours violates CHECK constraint");
    }

    [Fact]
    public async Task TimeEntry_WithNegativeOvertimeHours_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ003", Name = "Project 3", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var projectTask = new ProjectTask { ProjectCode = "PROJ003", TaskName = "Development", IsActive = true };
        await context.ProjectTasks.AddAsync(projectTask);
        await context.SaveChangesAsync();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ003",
            ProjectTaskId = projectTask.Id,
            StandardHours = 8.0m,
            OvertimeHours = -2.0m, // Invalid - violates CHECK constraint
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported
        };

        // Act & Assert - Database-agnostic check constraint validation
        await context.TimeEntries.AddAsync(timeEntry);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>(
            "because negative overtime_hours violates CHECK constraint");
    }

    [Fact]
    public async Task TimeEntry_WithInvalidProjectCode_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "INVALID", // Project doesn't exist
            ProjectTaskId = 999, // Non-existent ProjectTask
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported
        };

        // Act & Assert
        await context.TimeEntries.AddAsync(timeEntry);
        var act = async () => await context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ProjectTask_CanCreateAndRetrieve()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ004", Name = "Project 4", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var projectTask = new ProjectTask
        {
            ProjectCode = "PROJ004",
            TaskName = "Bug Fixing",
            IsActive = true
        };

        // Act
        await context.ProjectTasks.AddAsync(projectTask);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.ProjectTasks
            .FirstOrDefaultAsync(pt => pt.ProjectCode == "PROJ004" && pt.TaskName == "Bug Fixing");

        retrieved.Should().NotBeNull();
        retrieved!.TaskName.Should().Be("Bug Fixing");
        retrieved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ProjectTag_CanCreateWithAllowedValues()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ005", Name = "Project 5", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var tagConfig = new ProjectTag
        {
            ProjectCode = "PROJ005",
            TagName = "Priority"
        };
        tagConfig.AllowedValues.Add(new TagValue { Value = "High" });
        tagConfig.AllowedValues.Add(new TagValue { Value = "Medium" });
        tagConfig.AllowedValues.Add(new TagValue { Value = "Low" });

        // Act
        await context.ProjectTags.AddAsync(tagConfig);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.ProjectTags
            .Include(tc => tc.AllowedValues)
            .FirstOrDefaultAsync(tc => tc.ProjectCode == "PROJ005" && tc.TagName == "Priority");

        retrieved.Should().NotBeNull();
        retrieved!.AllowedValues.Should().HaveCount(3);
        retrieved.AllowedValues.Select(v => v.Value).Should().BeEquivalentTo(new[] { "High", "Medium", "Low" });
    }

    [Fact]
    public async Task TimeEntry_WithTags_CanStoreRelationalData()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ006", Name = "Project 6", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var projectTask = new ProjectTask { ProjectCode = "PROJ006", TaskName = "Development", IsActive = true };
        await context.ProjectTasks.AddAsync(projectTask);
        await context.SaveChangesAsync();

        // Create tag configurations with allowed values
        var priorityTag = new ProjectTag { ProjectCode = "PROJ006", TagName = "Priority" };
        priorityTag.AllowedValues.Add(new TagValue { Value = "High" });
        priorityTag.AllowedValues.Add(new TagValue { Value = "Low" });

        var componentTag = new ProjectTag { ProjectCode = "PROJ006", TagName = "Component" };
        componentTag.AllowedValues.Add(new TagValue { Value = "Frontend" });
        componentTag.AllowedValues.Add(new TagValue { Value = "Backend" });

        await context.ProjectTags.AddAsync(priorityTag);
        await context.ProjectTags.AddAsync(componentTag);
        await context.SaveChangesAsync();

        // Get the IDs of specific allowed values
        var highPriorityValueId = priorityTag.AllowedValues.First(v => v.Value == "High").Id;
        var frontendComponentValueId = componentTag.AllowedValues.First(v => v.Value == "Frontend").Id;

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ006",
            ProjectTaskId = projectTask.Id,
            StandardHours = 6.5m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported
        };
        timeEntry.Tags.Add(new TimeEntryTag { TagValueId = highPriorityValueId });
        timeEntry.Tags.Add(new TimeEntryTag { TagValueId = frontendComponentValueId });

        // Act
        await context.TimeEntries.AddAsync(timeEntry);
        await context.SaveChangesAsync();

        // Clear context to force fresh read from database
        context.ChangeTracker.Clear();

        // Assert
        var retrieved = await context.TimeEntries
            .Include(te => te.Tags)
                .ThenInclude(t => t.TagValue)
                    .ThenInclude(av => av.ProjectTag)
            .FirstOrDefaultAsync(te => te.Id == timeEntry.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Tags.Should().HaveCount(2);
        retrieved.Tags.Should().Contain(t =>
            t.TagValue.ProjectTag.TagName == "Priority" &&
            t.TagValue.Value == "High");
        retrieved.Tags.Should().Contain(t =>
            t.TagValue.ProjectTag.TagName == "Component" &&
            t.TagValue.Value == "Frontend");
    }

    [Fact]
    public async Task Project_Delete_CascadesTo_ProjectTasksAndProjectTags()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ007", Name = "Project 7", IsActive = true };
        var projectTask = new ProjectTask { ProjectCode = "PROJ007", TaskName = "Testing", IsActive = true };
        var tagConfig = new ProjectTag { ProjectCode = "PROJ007", TagName = "Type" };
        tagConfig.AllowedValues.Add(new TagValue { Value = "Unit" });
        tagConfig.AllowedValues.Add(new TagValue { Value = "Integration" });

        await context.Projects.AddAsync(project);
        await context.ProjectTasks.AddAsync(projectTask);
        await context.ProjectTags.AddAsync(tagConfig);
        await context.SaveChangesAsync();

        // Get the tag config ID for later assertion
        var tagConfigId = tagConfig.Id;

        // Act - Delete project
        context.Projects.Remove(project);
        await context.SaveChangesAsync();

        // Assert - Related entities should be deleted (cascade)
        var remainingTasks = await context.ProjectTasks
            .Where(pt => pt.ProjectCode == "PROJ007")
            .ToListAsync();

        var remainingTagConfigs = await context.ProjectTags
            .Where(tc => tc.ProjectCode == "PROJ007")
            .ToListAsync();

        var remainingAllowedValues = await context.TagValues
            .Where(av => av.ProjectTagId == tagConfigId)
            .ToListAsync();

        remainingTasks.Should().BeEmpty("ProjectTasks should cascade delete");
        remainingTagConfigs.Should().BeEmpty("ProjectTags should cascade delete");
        remainingAllowedValues.Should().BeEmpty("TagValues should cascade delete");
    }

    public void Dispose()
    {
        _fixture.Cleanup();
    }
}
