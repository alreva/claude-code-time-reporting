using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.Models;
using TimeReportingApi.Services;
using TimeReportingApi.Tests.Fixtures;

namespace TimeReportingApi.Tests.GraphQL;

/// <summary>
/// Integration tests for workflow-related GraphQL mutations:
/// SubmitTimeEntry, ApproveTimeEntry, DeclineTimeEntry.
/// Tests status transitions and business rules.
/// </summary>
public class WorkflowMutationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private TimeReportingDbContext _context = null!;
    private Mutation _mutation = null!;

    public WorkflowMutationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _context = new TimeReportingDbContext(options);
        _mutation = new Mutation();

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await CleanupTestDataAsync();
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Seed project
        var project = new Project { Code = "INTERNAL", Name = "Internal Project", IsActive = true };
        if (!await _context.Projects.AnyAsync(p => p.Code == "INTERNAL"))
        {
            _context.Projects.Add(project);
        }
        else
        {
            project = await _context.Projects.FindAsync("INTERNAL") ?? project;
        }
        await _context.SaveChangesAsync();

        // Seed task
        if (!await _context.ProjectTasks.AnyAsync(t =>
            EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"))
        {
            _context.ProjectTasks.Add(new ProjectTask
            {
                TaskName = "Development",
                IsActive = true,
                Project = project
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task CleanupTestDataAsync()
    {
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        await _context.SaveChangesAsync();
    }

    #region SubmitTimeEntry Tests

    [Fact]
    public async Task SubmitTimeEntry_WithNotReportedStatus_ChangesStatusToSubmitted()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
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

        // Act
        var result = await _mutation.SubmitTimeEntry(entry.Id, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeEntryStatus.Submitted, result.Status);
        Assert.True(result.UpdatedAt > entry.UpdatedAt);
    }

    [Fact]
    public async Task SubmitTimeEntry_WithDeclinedStatus_ChangesStatusToSubmitted()
    {
        // Arrange - DECLINED entries can be resubmitted
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Declined,
            DeclineComment = "Please add more details",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _mutation.SubmitTimeEntry(entry.Id, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeEntryStatus.Submitted, result.Status);
        // Decline comment should be preserved
        Assert.Equal("Please add more details", result.DeclineComment);
    }

    [Fact]
    public async Task SubmitTimeEntry_WithSubmittedStatus_ThrowsBusinessRuleException()
    {
        // Arrange - Already submitted
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.SubmitTimeEntry(entry.Id, _context));

        Assert.Contains("already SUBMITTED", exception.Message);
    }

    [Fact]
    public async Task SubmitTimeEntry_WithApprovedStatus_ThrowsBusinessRuleException()
    {
        // Arrange - Already approved
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.SubmitTimeEntry(entry.Id, _context));

        Assert.Contains("APPROVED", exception.Message);
    }

    [Fact]
    public async Task SubmitTimeEntry_WithNonExistentId_ThrowsValidationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.SubmitTimeEntry(nonExistentId, _context));

        Assert.Equal("id", exception.Field);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    #endregion

    #region ApproveTimeEntry Tests

    [Fact]
    public async Task ApproveTimeEntry_WithSubmittedStatus_ChangesStatusToApproved()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _mutation.ApproveTimeEntry(entry.Id, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeEntryStatus.Approved, result.Status);
        Assert.True(result.UpdatedAt > entry.UpdatedAt);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithNotReportedStatus_ThrowsBusinessRuleException()
    {
        // Arrange - Not yet submitted
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
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
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.ApproveTimeEntry(entry.Id, _context));

        Assert.Contains("SUBMITTED", exception.Message);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithAlreadyApprovedStatus_ThrowsBusinessRuleException()
    {
        // Arrange - Already approved
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.ApproveTimeEntry(entry.Id, _context));

        Assert.Contains("already APPROVED", exception.Message);
    }

    [Fact]
    public async Task ApproveTimeEntry_WithNonExistentId_ThrowsValidationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.ApproveTimeEntry(nonExistentId, _context));

        Assert.Equal("id", exception.Field);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    #endregion

    #region DeclineTimeEntry Tests

    [Fact]
    public async Task DeclineTimeEntry_WithSubmittedStatus_ChangesStatusToDeclined()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var comment = "Please add more details about the work performed";
        var result = await _mutation.DeclineTimeEntry(entry.Id, comment, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeEntryStatus.Declined, result.Status);
        Assert.Equal(comment, result.DeclineComment);
        Assert.True(result.UpdatedAt > entry.UpdatedAt);
    }

    [Fact]
    public async Task DeclineTimeEntry_WithNotReportedStatus_ThrowsBusinessRuleException()
    {
        // Arrange - Not yet submitted
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
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
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.DeclineTimeEntry(entry.Id, "Some comment", _context));

        Assert.Contains("SUBMITTED", exception.Message);
    }

    [Fact]
    public async Task DeclineTimeEntry_WithApprovedStatus_ThrowsBusinessRuleException()
    {
        // Arrange - Already approved (cannot decline)
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.DeclineTimeEntry(entry.Id, "Some comment", _context));

        Assert.Contains("APPROVED", exception.Message);
    }

    [Fact]
    public async Task DeclineTimeEntry_WithNonExistentId_ThrowsValidationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.DeclineTimeEntry(nonExistentId, "Some comment", _context));

        Assert.Equal("id", exception.Field);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    [Fact]
    public async Task DeclineTimeEntry_WithEmptyComment_ThrowsValidationException()
    {
        // Arrange
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FindAsync("INTERNAL") ?? throw new Exception("Project not found"),
            ProjectTask = await _context.ProjectTasks.FirstAsync(t =>
                EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TaskName == "Development"),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.DeclineTimeEntry(entry.Id, "", _context));

        Assert.Equal("comment", exception.Field);
        Assert.Contains("required", exception.Message);
    }

    #endregion
}
