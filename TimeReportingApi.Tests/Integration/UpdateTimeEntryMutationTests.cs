using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Data;
using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;
using TimeReportingApi.Tests.Helpers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Integration tests for UpdateTimeEntry GraphQL mutation using real PostgreSQL database.
/// Tests validation, business rules, and data persistence following ADR 0001.
/// </summary>
public class UpdateTimeEntryMutationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private TimeReportingDbContext _context = null!;
    private Guid _testEntryId;

    public UpdateTimeEntryMutationTests(PostgresContainerFixture fixture)
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

                    // Add test authentication to bypass Azure AD
                    services.AddTestAuthentication();
                });
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
        var projectCode = "UPD-TEST";

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
        var projectCode = "UPD-TEST";

        // Check if project already exists (in case of parallel test execution)
        var existingProject = await _context.Projects.FindAsync(projectCode);
        Project project;
        ProjectTask task1;

        if (existingProject == null)
        {
            // Create test project
            project = new Project
            {
                Code = projectCode,
                Name = "Update Test Project",
                IsActive = true
            };
            _context.Projects.Add(project);

            // Create project tasks
            task1 = new ProjectTask { TaskName = "Development", IsActive = true, Project = project };
            var task2 = new ProjectTask { TaskName = "Testing", IsActive = true, Project = project };
            _context.ProjectTasks.AddRange(task1, task2);

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
            IssueId = "DEV-123",
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            Description = "Original description",
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
    public async Task UpdateTimeEntry_WithValidInput_UpdatesEntry()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        standardHours: 7.5
                        overtimeHours: 0.5
                        description: ""Updated description""
                    }}
                ) {{
                    id
                    standardHours
                    overtimeHours
                    description
                    updatedAt
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("updateTimeEntry");

        Assert.Equal(_testEntryId.ToString(), data.GetProperty("id").GetString());
        Assert.Equal(7.5, data.GetProperty("standardHours").GetDouble());
        Assert.Equal(0.5, data.GetProperty("overtimeHours").GetDouble());
        Assert.Equal("Updated description", data.GetProperty("description").GetString());

        // Verify in database - clear change tracker to force reload
        _context.ChangeTracker.Clear();
        var dbEntry = await _context.TimeEntries.FindAsync(_testEntryId);
        Assert.NotNull(dbEntry);
        Assert.Equal(7.5m, dbEntry.StandardHours);
        Assert.Equal(0.5m, dbEntry.OvertimeHours);
        Assert.Equal("Updated description", dbEntry.Description);
    }

    [Fact]
    public async Task UpdateTimeEntry_WithPartialInput_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        description: ""Partial update""
                    }}
                ) {{
                    id
                    standardHours
                    description
                    issueId
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("updateTimeEntry");

        Assert.Equal("Partial update", data.GetProperty("description").GetString());
        Assert.Equal(8.0, data.GetProperty("standardHours").GetDouble()); // Original value
        Assert.Equal("DEV-123", data.GetProperty("issueId").GetString()); // Original value
    }

    [Fact]
    public async Task UpdateTimeEntry_WithTaskChange_UpdatesTask()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        task: ""Testing""
                    }}
                ) {{
                    id
                    projectTask {{ taskName }}
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("updateTimeEntry");

        Assert.Equal("Testing", data.GetProperty("projectTask").GetProperty("taskName").GetString());

        // Verify in database - ADR 0001: shadow FK updated automatically
        _context.ChangeTracker.Clear();
        var dbEntry = await _context.TimeEntries
            .Include(e => e.ProjectTask)
            .FirstAsync(e => e.Id == _testEntryId);
        Assert.Equal("Testing", dbEntry.ProjectTask.TaskName);
    }

    [Fact]
    public async Task UpdateTimeEntry_WithDateChange_UpdatesDates()
    {
        // Arrange
        var newStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var newEndDate = DateOnly.FromDateTime(DateTime.Today);

        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        startDate: ""{newStartDate:yyyy-MM-dd}""
                        completionDate: ""{newEndDate:yyyy-MM-dd}""
                    }}
                ) {{
                    id
                    startDate
                    completionDate
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("updateTimeEntry");

        Assert.Equal(newStartDate.ToString("yyyy-MM-dd"), data.GetProperty("startDate").GetString());
        Assert.Equal(newEndDate.ToString("yyyy-MM-dd"), data.GetProperty("completionDate").GetString());
    }

    [Fact]
    public async Task UpdateTimeEntry_UpdatesUpdatedAtTimestamp()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        description: ""Testing timestamp""
                    }}
                ) {{
                    id
                    createdAt
                    updatedAt
                }}
            }}";

        // Act
        var beforeUpdate = DateTime.UtcNow.AddSeconds(-1);
        var result = await ExecuteGraphQL(mutation);
        var afterUpdate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var data = result.RootElement.GetProperty("data").GetProperty("updateTimeEntry");
        var updatedAt = DateTime.Parse(data.GetProperty("updatedAt").GetString()!).ToUniversalTime();

        Assert.True(updatedAt >= beforeUpdate && updatedAt <= afterUpdate,
            $"UpdatedAt {updatedAt:O} should be between {beforeUpdate:O} and {afterUpdate:O}");
    }

    #endregion

    #region Validation Tests - Status

    [Fact]
    public async Task UpdateTimeEntry_WithSubmittedStatus_ReturnsError()
    {
        // Arrange - Create a submitted entry
        var submittedEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FirstAsync(),
            ProjectTask = await _context.ProjectTasks.FirstAsync(),
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
                updateTimeEntry(
                    id: ""{submittedEntry.Id}""
                    input: {{
                        description: ""Should not work""
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("SUBMITTED", errorMessage);
    }

    [Fact]
    public async Task UpdateTimeEntry_WithApprovedStatus_ReturnsError()
    {
        // Arrange - Create an approved entry
        var approvedEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FirstAsync(),
            ProjectTask = await _context.ProjectTasks.FirstAsync(),
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
                updateTimeEntry(
                    id: ""{approvedEntry.Id}""
                    input: {{
                        description: ""Should not work""
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("APPROVED", errorMessage);
    }

    [Fact]
    public async Task UpdateTimeEntry_WithDeclinedStatus_AllowsUpdate()
    {
        // Arrange - Create a declined entry
        var declinedEntry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            Project = await _context.Projects.FirstAsync(),
            ProjectTask = await _context.ProjectTasks.FirstAsync(),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m,
            Description = "Original",
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
                updateTimeEntry(
                    id: ""{declinedEntry.Id}""
                    input: {{
                        description: ""Revised per feedback""
                    }}
                ) {{
                    id
                    description
                    status
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("updateTimeEntry");
        Assert.Equal("Revised per feedback", data.GetProperty("description").GetString());
        // DECLINED entries should reset to NOT_REPORTED when updated
        Assert.Equal("NOT_REPORTED", data.GetProperty("status").GetString());

        // Verify decline comment is also cleared - reload from database
        _context.ChangeTracker.Clear();
        var updatedEntry = await _context.TimeEntries.FindAsync(declinedEntry.Id);
        Assert.Null(updatedEntry!.DeclineComment);
        Assert.Equal(TimeEntryStatus.NotReported, updatedEntry.Status);
    }

    #endregion

    #region Validation Tests - Not Found

    [Fact]
    public async Task UpdateTimeEntry_WithNonExistentId_ReturnsError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{nonExistentId}""
                    input: {{
                        description: ""Should not work""
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("not found", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Validation Tests - Task

    [Fact]
    public async Task UpdateTimeEntry_WithInvalidTask_ReturnsError()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        task: ""InvalidTask""
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("InvalidTask", errorMessage);
        Assert.Contains("not available", errorMessage);
    }

    #endregion

    #region Validation Tests - Hours

    [Fact]
    public async Task UpdateTimeEntry_WithNegativeStandardHours_ReturnsError()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        standardHours: -1.0
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("StandardHours", errorMessage);
    }

    [Fact]
    public async Task UpdateTimeEntry_WithNegativeOvertimeHours_ReturnsError()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        overtimeHours: -2.0
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("OvertimeHours", errorMessage);
    }

    #endregion

    #region Validation Tests - Dates

    [Fact]
    public async Task UpdateTimeEntry_WithStartDateAfterCompletionDate_ReturnsError()
    {
        // Arrange
        var mutation = $@"
            mutation {{
                updateTimeEntry(
                    id: ""{_testEntryId}""
                    input: {{
                        startDate: ""2025-10-25""
                        completionDate: ""2025-10-24""
                    }}
                ) {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("StartDate", errorMessage);
        Assert.Contains("CompletionDate", errorMessage);
    }

    #endregion
}
