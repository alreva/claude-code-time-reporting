using System.Security.Claims;
using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.GraphQL.Inputs;
using TimeReportingApi.Models;
using TimeReportingApi.Services;
using TimeReportingApi.Tests.Fixtures;

namespace TimeReportingApi.Tests.GraphQL;

/// <summary>
/// Integration tests for MoveTaskToProject GraphQL mutation.
/// Tests validation, status checks, tag clearing, and project/task reassignment.
/// </summary>
public class MoveTaskToProjectMutationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private TimeReportingDbContext _context = null!;
    private Mutation _mutation = null!;
    private ValidationService _validator = null!;
    private ClaimsPrincipal _testUser = null!;

    public MoveTaskToProjectMutationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private static ClaimsPrincipal CreateTestUser(string email = "test@example.com", string name = "Test User", string oid = "test-oid-123")
    {
        var claims = new List<Claim>
        {
            new Claim("email", email),
            new Claim("name", name),
            new Claim("oid", oid)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        return new ClaimsPrincipal(identity);
    }

    public async Task InitializeAsync()
    {
        // Create a fresh context for this test
        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _context = new TimeReportingDbContext(options);
        _validator = new ValidationService(_context);
        _mutation = new Mutation();
        _testUser = CreateTestUser();

        // Seed test data
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data
        await CleanupTestDataAsync();
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Seed projects
        var internalProject = new Project { Code = "INTERNAL", Name = "Internal Project", IsActive = true };
        var clientAProject = new Project { Code = "CLIENT-A", Name = "Client A Project", IsActive = true };

        if (!await _context.Projects.AnyAsync(p => p.Code == "INTERNAL"))
        {
            _context.Projects.Add(internalProject);
        }
        else
        {
            internalProject = await _context.Projects.FindAsync("INTERNAL") ?? internalProject;
        }

        if (!await _context.Projects.AnyAsync(p => p.Code == "CLIENT-A"))
        {
            _context.Projects.Add(clientAProject);
        }
        else
        {
            clientAProject = await _context.Projects.FindAsync("CLIENT-A") ?? clientAProject;
        }

        await _context.SaveChangesAsync();

        // Seed tasks - ADR 0001: Set navigation property, EF fills shadow FK
        var tasks = new[]
        {
            new { TaskName = "Development", Project = internalProject },
            new { TaskName = "Bug Fixing", Project = internalProject },
            new { TaskName = "Bug Fixing", Project = clientAProject },
            new { TaskName = "Testing", Project = clientAProject }
        };

        foreach (var task in tasks)
        {
            var projectCode = task.Project.Code;
            if (!await _context.ProjectTasks.AnyAsync(t =>
                EF.Property<string>(t, "ProjectCode") == projectCode && t.TaskName == task.TaskName))
            {
                _context.ProjectTasks.Add(new ProjectTask
                {
                    TaskName = task.TaskName,
                    IsActive = true,
                    Project = task.Project  // ADR 0001: Navigation property
                });
            }
        }

        await _context.SaveChangesAsync();

        // Seed tags for INTERNAL project - ADR 0001: Set navigation property
        if (!await _context.ProjectTags.AnyAsync(t =>
            EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TagName == "Environment"))
        {
            var environmentTag = new ProjectTag
            {
                TagName = "Environment",
                IsActive = true,
                Project = internalProject  // ADR 0001: Navigation property
            };
            _context.ProjectTags.Add(environmentTag);
            await _context.SaveChangesAsync();

            // Seed tag values - ADR 0001: Set navigation property
            var tagValues = new[]
            {
                new TagValue { Value = "Development", ProjectTag = environmentTag },
                new TagValue { Value = "Production", ProjectTag = environmentTag }
            };

            _context.TagValues.AddRange(tagValues);
            await _context.SaveChangesAsync();
        }
    }

    private async Task CleanupTestDataAsync()
    {
        // Delete in order respecting foreign key constraints
        _context.TimeEntryTags.RemoveRange(_context.TimeEntryTags);
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task MoveTaskToProject_WithValidInput_MovesEntryToNewProject()
    {
        // Arrange - Create time entry in INTERNAL project
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act - Move to CLIENT-A project with Bug Fixing task
        var result = await _mutation.MoveTaskToProject(
            entry.Id,
            "CLIENT-A",
            "Bug Fixing",
            _testUser,
            _validator,
            _context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entry.Id, result.Id);

        // Verify the project changed
        var projectCode = _context.Entry(result).Property<string>("ProjectCode").CurrentValue;
        Assert.Equal("CLIENT-A", projectCode);

        // Verify the task changed
        Assert.Equal("Bug Fixing", result.ProjectTask.TaskName);

        // Verify timestamp updated
        Assert.True(result.UpdatedAt > entry.UpdatedAt);
    }

    [Fact]
    public async Task MoveTaskToProject_WithInvalidProjectCode_ThrowsValidationException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.MoveTaskToProject(entry.Id, "INVALID-PROJECT", "Development", _testUser, _validator, _context));

        Assert.Equal("projectCode", exception.Field);
        Assert.Contains("INVALID-PROJECT", exception.Message);
    }

    [Fact]
    public async Task MoveTaskToProject_WithInvalidTask_ThrowsValidationException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert - Try to move to CLIENT-A with a task that doesn't exist there
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.MoveTaskToProject(entry.Id, "CLIENT-A", "Invalid Task", _testUser, _validator, _context));

        Assert.Equal("task", exception.Field);
        Assert.Contains("Invalid Task", exception.Message);
    }

    [Fact]
    public async Task MoveTaskToProject_WithInactiveProject_ThrowsValidationException()
    {
        // Arrange - Create an inactive project first
        var inactiveProject = new Project
        {
            Code = "INACTIVE",
            Name = "Inactive Project",
            IsActive = false
        };
        _context.Projects.Add(inactiveProject);
        await _context.SaveChangesAsync();

        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.MoveTaskToProject(entry.Id, "INACTIVE", "Development", _testUser, _validator, _context));

        Assert.Equal("projectCode", exception.Field);
        Assert.Contains("is inactive", exception.Message);
    }

    [Fact]
    public async Task MoveTaskToProject_WithSubmittedStatus_ThrowsBusinessRuleException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Submitted,  // SUBMITTED status
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.MoveTaskToProject(entry.Id, "CLIENT-A", "Bug Fixing", _testUser, _validator, _context));

        Assert.Contains("SUBMITTED", exception.Message);
    }

    [Fact]
    public async Task MoveTaskToProject_WithApprovedStatus_ThrowsBusinessRuleException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Approved,  // APPROVED status
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.MoveTaskToProject(entry.Id, "CLIENT-A", "Bug Fixing", _testUser, _validator, _context));

        Assert.Contains("APPROVED", exception.Message);
        Assert.Contains("immutable", exception.Message);
    }

    [Fact]
    public async Task MoveTaskToProject_WithDeclinedStatus_AllowsMove()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Declined,  // DECLINED status - should be allowed
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _mutation.MoveTaskToProject(
            entry.Id,
            "CLIENT-A",
            "Bug Fixing",
            _testUser,
            _validator,
            _context);

        // Assert
        Assert.NotNull(result);
        var projectCode = _context.Entry(result).Property<string>("ProjectCode").CurrentValue;
        Assert.Equal("CLIENT-A", projectCode);
    }

    [Fact]
    public async Task MoveTaskToProject_WithNonExistentEntryId_ThrowsValidationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.MoveTaskToProject(nonExistentId, "CLIENT-A", "Bug Fixing", _testUser, _validator, _context));

        Assert.Equal("id", exception.Field);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    [Fact]
    public async Task MoveTaskToProject_ClearsTagsWhenMovingProjects()
    {
        // Arrange - Create entry with tags in INTERNAL project
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add tags from INTERNAL project
        var tagValue = await _context.TagValues
            .Include(tv => tv.ProjectTag)
            .FirstAsync(tv => EF.Property<string>(tv.ProjectTag, "ProjectCode") == "INTERNAL"
                           && tv.ProjectTag.TagName == "Environment");

        entry.Tags.Add(new TimeEntryTag
        {
            TimeEntry = entry,
            TagValue = tagValue
        });

        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act - Move to different project
        var result = await _mutation.MoveTaskToProject(
            entry.Id,
            "CLIENT-A",
            "Bug Fixing",
            _testUser,
            _validator,
            _context);

        // Assert - Tags should be cleared
        var reloadedEntry = await _context.TimeEntries
            .Include(e => e.Tags)
            .FirstAsync(e => e.Id == entry.Id);

        Assert.Empty(reloadedEntry.Tags);
    }

    [Fact]
    public async Task MoveTaskToProject_ToSameProject_UpdatesTaskOnly()
    {
        // Arrange - Create entry in INTERNAL with Development task
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("INTERNAL project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act - Move to same project (INTERNAL) but different task (Bug Fixing)
        var result = await _mutation.MoveTaskToProject(
            entry.Id,
            "INTERNAL",
            "Bug Fixing",
            _testUser,
            _validator,
            _context);

        // Assert - Project stays the same, task changes
        var projectCode = _context.Entry(result).Property<string>("ProjectCode").CurrentValue;
        Assert.Equal("INTERNAL", projectCode);
        Assert.Equal("Bug Fixing", result.ProjectTask.TaskName);
    }
}
