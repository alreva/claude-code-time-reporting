using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Data;
using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Integration tests for DeleteTimeEntry GraphQL mutation using real PostgreSQL database.
/// Tests validation, business rules, and cascade deletion following ADR 0001.
/// </summary>
public class DeleteTimeEntryMutationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private TimeReportingDbContext _context = null!;
    private Guid _testEntryId;

    public DeleteTimeEntryMutationTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext configuration
                    var dbContextDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<TimeReportingDbContext>));

                    if (dbContextDescriptor != null)
                    {
                        services.Remove(dbContextDescriptor);
                    }

                    var dbContextServiceDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(TimeReportingDbContext));

                    if (dbContextServiceDescriptor != null)
                    {
                        services.Remove(dbContextServiceDescriptor);
                    }

                    // Add PostgreSQL DbContext pointing to the test container
                    services.AddDbContext<TimeReportingDbContext>(options =>
                    {
                        options.UseNpgsql(_fixture.ConnectionString);
                    });
                });

                // Use test bearer token
                builder.UseSetting("Authentication:BearerToken", "test-bearer-token-12345");
            });

        _client = _factory.CreateDefaultClient(new AuthenticationHandler("test-bearer-token-12345"));
    }

    public async Task InitializeAsync()
    {
        // Create a new DbContext for seeding
        var optionsBuilder = new DbContextOptionsBuilder<TimeReportingDbContext>();
        optionsBuilder.UseNpgsql(_fixture.ConnectionString);
        _context = new TimeReportingDbContext(optionsBuilder.Options);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test data - must delete in correct order due to FK constraints
        var projectCode = "DEL-TEST";

        // Delete time entries first
        var entries = await _context.TimeEntries
            .Where(e => EF.Property<string>(e, "ProjectCode") == projectCode)
            .ToListAsync();
        _context.TimeEntries.RemoveRange(entries);
        await _context.SaveChangesAsync();

        // Delete project tasks
        var tasks = await _context.ProjectTasks
            .Where(t => EF.Property<string>(t, "ProjectCode") == projectCode)
            .ToListAsync();
        _context.ProjectTasks.RemoveRange(tasks);
        await _context.SaveChangesAsync();

        // Delete project
        var project = await _context.Projects.FindAsync(projectCode);
        if (project != null)
        {
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        await _context.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Use unique project code to avoid conflicts with other test classes
        var projectCode = "DEL-TEST";

        // Check if project already exists
        var existingProject = await _context.Projects.FindAsync(projectCode);
        Project project;
        ProjectTask task1;

        if (existingProject == null)
        {
            // Create test project
            project = new Project
            {
                Code = projectCode,
                Name = "Delete Test Project",
                IsActive = true
            };
            _context.Projects.Add(project);

            // Create project task
            task1 = new ProjectTask { TaskName = "Development", IsActive = true, Project = project };
            _context.ProjectTasks.Add(task1);

            await _context.SaveChangesAsync();
        }
        else
        {
            project = existingProject;
            // Load existing task
            task1 = await _context.ProjectTasks
                .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == projectCode
                              && t.TaskName == "Development");
        }

        // Create a test time entry
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = project,
            ProjectTask = task1,
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            Description = "To be deleted",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(entry);
        await _context.SaveChangesAsync();

        _testEntryId = entry.Id;
    }

    private async Task<JsonDocument> ExecuteGraphQL(string query)
    {
        var request = new { query };
        var response = await _client.PostAsJsonAsync("/graphql", request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    #region Happy Path Tests

    [Fact]
    public async Task DeleteTimeEntry_WithNotReportedStatus_DeletesEntry()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                deleteTimeEntry(id: ""{_testEntryId}"")
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("deleteTimeEntry");
        Assert.True(data.GetBoolean());

        // Verify in database - entry should be deleted
        _context.ChangeTracker.Clear();
        var dbEntry = await _context.TimeEntries.FindAsync(_testEntryId);
        Assert.Null(dbEntry);
    }

    [Fact]
    public async Task DeleteTimeEntry_WithDeclinedStatus_DeletesEntry()
    {
        // Arrange - Create a declined entry
        var project = await _context.Projects.FindAsync("DEL-TEST");
        var task = await _context.ProjectTasks
            .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == "DEL-TEST");

        var declinedEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = project!,
            ProjectTask = task,
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Declined,
            DeclineComment = "Please revise",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(declinedEntry);
        await _context.SaveChangesAsync();

        var mutation = $@"
            mutation {{
                deleteTimeEntry(id: ""{declinedEntry.Id}"")
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("deleteTimeEntry");
        Assert.True(data.GetBoolean());

        // Verify in database
        _context.ChangeTracker.Clear();
        var dbEntry = await _context.TimeEntries.FindAsync(declinedEntry.Id);
        Assert.Null(dbEntry);
    }

    #endregion

    #region Validation Tests - Status

    [Fact]
    public async Task DeleteTimeEntry_WithSubmittedStatus_ReturnsError()
    {
        // Arrange - Create a submitted entry
        var project = await _context.Projects.FindAsync("DEL-TEST");
        var task = await _context.ProjectTasks
            .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == "DEL-TEST");

        var submittedEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = project!,
            ProjectTask = task,
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Submitted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(submittedEntry);
        await _context.SaveChangesAsync();

        var mutation = $@"
            mutation {{
                deleteTimeEntry(id: ""{submittedEntry.Id}"")
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("SUBMITTED", errorMessage);

        // Verify entry still exists
        _context.ChangeTracker.Clear();
        var dbEntry = await _context.TimeEntries.FindAsync(submittedEntry.Id);
        Assert.NotNull(dbEntry);
    }

    [Fact]
    public async Task DeleteTimeEntry_WithApprovedStatus_ReturnsError()
    {
        // Arrange - Create an approved entry
        var project = await _context.Projects.FindAsync("DEL-TEST");
        var task = await _context.ProjectTasks
            .FirstAsync(t => EF.Property<string>(t, "ProjectCode") == "DEL-TEST");

        var approvedEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = project!,
            ProjectTask = task,
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            CompletionDate = DateOnly.FromDateTime(DateTime.Today),
            Status = TimeEntryStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.TimeEntries.Add(approvedEntry);
        await _context.SaveChangesAsync();

        var mutation = $@"
            mutation {{
                deleteTimeEntry(id: ""{approvedEntry.Id}"")
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("APPROVED", errorMessage);

        // Verify entry still exists
        _context.ChangeTracker.Clear();
        var dbEntry = await _context.TimeEntries.FindAsync(approvedEntry.Id);
        Assert.NotNull(dbEntry);
    }

    #endregion

    #region Validation Tests - Not Found

    [Fact]
    public async Task DeleteTimeEntry_WithNonExistentId_ReturnsError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var mutation = $@"
            mutation {{
                deleteTimeEntry(id: ""{nonExistentId}"")
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("not found", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
