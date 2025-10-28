using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.GraphQL.Inputs;
using TimeReportingApi.Models;
using TimeReportingApi.Services;
using TimeReportingApi.Tests.Fixtures;

namespace TimeReportingApi.Tests.GraphQL;

/// <summary>
/// Integration tests for UpdateTags GraphQL mutation.
/// Tests tag validation, status checks, and tag replacement behavior.
/// </summary>
public class UpdateTagsMutationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private TimeReportingDbContext _context = null!;
    private Mutation _mutation = null!;
    private ValidationService _validator = null!;

    public UpdateTagsMutationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TimeReportingDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _context = new TimeReportingDbContext(options);
        _validator = new ValidationService(_context);
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

        // Seed tags
        if (!await _context.ProjectTags.AnyAsync(t =>
            EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TagName == "Environment"))
        {
            var envTag = new ProjectTag { TagName = "Environment", IsActive = true, Project = project };
            _context.ProjectTags.Add(envTag);
            await _context.SaveChangesAsync();

            _context.TagValues.AddRange(
                new TagValue { Value = "Development", ProjectTag = envTag },
                new TagValue { Value = "Production", ProjectTag = envTag }
            );
        }

        if (!await _context.ProjectTags.AnyAsync(t =>
            EF.Property<string>(t, "ProjectCode") == "INTERNAL" && t.TagName == "Billable"))
        {
            var billableTag = new ProjectTag { TagName = "Billable", IsActive = true, Project = project };
            _context.ProjectTags.Add(billableTag);
            await _context.SaveChangesAsync();

            _context.TagValues.AddRange(
                new TagValue { Value = "Yes", ProjectTag = billableTag },
                new TagValue { Value = "No", ProjectTag = billableTag }
            );
        }

        await _context.SaveChangesAsync();
    }

    private async Task CleanupTestDataAsync()
    {
        _context.TimeEntryTags.RemoveRange(_context.TimeEntryTags);
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateTags_WithValidTags_UpdatesTagsSuccessfully()
    {
        // Arrange - Create entry with one tag
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

        var tagValue = await _context.TagValues
            .Include(tv => tv.ProjectTag)
            .FirstAsync(tv => EF.Property<string>(tv.ProjectTag, "ProjectCode") == "INTERNAL"
                           && tv.ProjectTag.TagName == "Environment"
                           && tv.Value == "Development");

        entry.Tags.Add(new TimeEntryTag { TimeEntry = entry, TagValue = tagValue });
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act - Update to different tags
        var newTags = new List<TagInput>
        {
            new() { Name = "Environment", Value = "Production" },
            new() { Name = "Billable", Value = "Yes" }
        };

        var result = await _mutation.UpdateTags(entry.Id, newTags, _validator, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Tags.Count);

        var envTag = result.Tags.FirstOrDefault(t => t.TagValue.ProjectTag.TagName == "Environment");
        Assert.NotNull(envTag);
        Assert.Equal("Production", envTag.TagValue.Value);

        var billableTag = result.Tags.FirstOrDefault(t => t.TagValue.ProjectTag.TagName == "Billable");
        Assert.NotNull(billableTag);
        Assert.Equal("Yes", billableTag.TagValue.Value);

        Assert.True(result.UpdatedAt > entry.UpdatedAt);
    }

    [Fact]
    public async Task UpdateTags_WithEmptyTagList_ClearsAllTags()
    {
        // Arrange - Create entry with tags
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

        var tagValue = await _context.TagValues
            .Include(tv => tv.ProjectTag)
            .FirstAsync(tv => EF.Property<string>(tv.ProjectTag, "ProjectCode") == "INTERNAL"
                           && tv.ProjectTag.TagName == "Environment");

        entry.Tags.Add(new TimeEntryTag { TimeEntry = entry, TagValue = tagValue });
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act - Clear all tags
        var result = await _mutation.UpdateTags(entry.Id, new List<TagInput>(), _validator, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public async Task UpdateTags_WithInvalidTagName_ThrowsValidationException()
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

        // Act & Assert
        var invalidTags = new List<TagInput>
        {
            new() { Name = "InvalidTag", Value = "SomeValue" }
        };

        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.UpdateTags(entry.Id, invalidTags, _validator, _context));

        Assert.Equal("tags", exception.Field);
        Assert.Contains("InvalidTag", exception.Message);
    }

    [Fact]
    public async Task UpdateTags_WithInvalidTagValue_ThrowsValidationException()
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

        // Act & Assert
        var invalidTags = new List<TagInput>
        {
            new() { Name = "Environment", Value = "InvalidValue" }
        };

        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.UpdateTags(entry.Id, invalidTags, _validator, _context));

        Assert.Equal("tags", exception.Field);
        Assert.Contains("InvalidValue", exception.Message);
    }

    [Fact]
    public async Task UpdateTags_WithSubmittedStatus_ThrowsBusinessRuleException()
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
            Status = TimeEntryStatus.Submitted,  // SUBMITTED
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var tags = new List<TagInput> { new() { Name = "Environment", Value = "Production" } };

        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.UpdateTags(entry.Id, tags, _validator, _context));

        Assert.Contains("SUBMITTED", exception.Message);
    }

    [Fact]
    public async Task UpdateTags_WithApprovedStatus_ThrowsBusinessRuleException()
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
            Status = TimeEntryStatus.Approved,  // APPROVED
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act & Assert
        var tags = new List<TagInput> { new() { Name = "Environment", Value = "Production" } };

        var exception = await Assert.ThrowsAsync<Exceptions.BusinessRuleException>(
            () => _mutation.UpdateTags(entry.Id, tags, _validator, _context));

        Assert.Contains("APPROVED", exception.Message);
        Assert.Contains("immutable", exception.Message);
    }

    [Fact]
    public async Task UpdateTags_WithDeclinedStatus_AllowsUpdate()
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
            Status = TimeEntryStatus.Declined,  // DECLINED - should be allowed
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var tags = new List<TagInput> { new() { Name = "Environment", Value = "Production" } };
        var result = await _mutation.UpdateTags(entry.Id, tags, _validator, _context);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tags);
        Assert.Equal("Production", result.Tags.First().TagValue.Value);
    }

    [Fact]
    public async Task UpdateTags_WithNonExistentEntryId_ThrowsValidationException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var tags = new List<TagInput> { new() { Name = "Environment", Value = "Production" } };

        var exception = await Assert.ThrowsAsync<Exceptions.ValidationException>(
            () => _mutation.UpdateTags(nonExistentId, tags, _validator, _context));

        Assert.Equal("id", exception.Field);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }
}
