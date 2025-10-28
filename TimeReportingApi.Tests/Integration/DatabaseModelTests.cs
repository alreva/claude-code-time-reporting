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

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ001",
            Task = "Development",
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
        var retrieved = await context.TimeEntries.FindAsync(timeEntry.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ProjectCode.Should().Be("PROJ001");
        retrieved.Task.Should().Be("Development");
        retrieved.StandardHours.Should().Be(8.0m);
        retrieved.Status.Should().Be(TimeEntryStatus.NotReported);
    }

    // Note: Check constraint tests (negative hours, date range) are covered by
    // SqlSchemaValidationTests which executes db/tests/test-schema.sql directly.
    // EF Core's EnsureCreated() doesn't apply check constraints from SQL schema,
    // so we focus on EF-specific functionality here.

    [Fact]
    public async Task TimeEntry_WithInvalidProjectCode_ThrowsDbUpdateException()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "INVALID", // Project doesn't exist
            Task = "Development",
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
    public async Task TagConfiguration_CanCreateWithJsonbAllowedValues()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ005", Name = "Project 5", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var tagConfig = new TagConfiguration
        {
            ProjectCode = "PROJ005",
            TagName = "Priority",
            AllowedValues = new List<string> { "High", "Medium", "Low" }
        };

        // Act
        await context.TagConfigurations.AddAsync(tagConfig);
        await context.SaveChangesAsync();

        // Assert
        var retrieved = await context.TagConfigurations
            .FirstOrDefaultAsync(tc => tc.ProjectCode == "PROJ005" && tc.TagName == "Priority");

        retrieved.Should().NotBeNull();
        retrieved!.AllowedValues.Should().BeEquivalentTo(new[] { "High", "Medium", "Low" });
    }

    [Fact]
    public async Task TimeEntry_WithTags_CanStoreJsonbData()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ006", Name = "Project 6", IsActive = true };
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();

        var tags = new List<Tag>
        {
            new Tag { Name = "Priority", Value = "High" },
            new Tag { Name = "Component", Value = "Frontend" }
        };

        var timeEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = "PROJ006",
            Task = "Development",
            StandardHours = 6.5m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            Tags = tags
        };

        // Act
        await context.TimeEntries.AddAsync(timeEntry);
        await context.SaveChangesAsync();

        // Clear context to force fresh read from database
        context.ChangeTracker.Clear();

        // Assert
        var retrieved = await context.TimeEntries.FindAsync(timeEntry.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Tags.Should().HaveCount(2);
        retrieved.Tags.Should().ContainEquivalentOf(new Tag { Name = "Priority", Value = "High" });
        retrieved.Tags.Should().ContainEquivalentOf(new Tag { Name = "Component", Value = "Frontend" });
    }

    [Fact]
    public async Task Project_Delete_CascadesTo_ProjectTasksAndTagConfigurations()
    {
        // Arrange
        using var context = _fixture.CreateNewContext();

        var project = new Project { Code = "PROJ007", Name = "Project 7", IsActive = true };
        var projectTask = new ProjectTask { ProjectCode = "PROJ007", TaskName = "Testing", IsActive = true };
        var tagConfig = new TagConfiguration { ProjectCode = "PROJ007", TagName = "Type", AllowedValues = new List<string> { "Unit", "Integration" } };

        await context.Projects.AddAsync(project);
        await context.ProjectTasks.AddAsync(projectTask);
        await context.TagConfigurations.AddAsync(tagConfig);
        await context.SaveChangesAsync();

        // Act - Delete project
        context.Projects.Remove(project);
        await context.SaveChangesAsync();

        // Assert - Related entities should be deleted (cascade)
        var remainingTasks = await context.ProjectTasks
            .Where(pt => pt.ProjectCode == "PROJ007")
            .ToListAsync();

        var remainingTagConfigs = await context.TagConfigurations
            .Where(tc => tc.ProjectCode == "PROJ007")
            .ToListAsync();

        remainingTasks.Should().BeEmpty("ProjectTasks should cascade delete");
        remainingTagConfigs.Should().BeEmpty("TagConfigurations should cascade delete");
    }

    public void Dispose()
    {
        _fixture.Cleanup();
    }
}
