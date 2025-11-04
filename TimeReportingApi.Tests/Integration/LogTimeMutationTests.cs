using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Data;
using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Integration tests for LogTime GraphQL mutation using real PostgreSQL database.
/// Tests validation, business rules, and data persistence following ADR 0001.
/// </summary>
public class LogTimeMutationTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private TimeReportingDbContext _context = null!;

    public LogTimeMutationTests(PostgresContainerFixture fixture)
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
        // Clean up test data
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        _context.ProjectTasks.RemoveRange(_context.ProjectTasks);
        _context.Projects.RemoveRange(_context.Projects);
        await _context.SaveChangesAsync();

        await _context.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create test project
        var project = new Project
        {
            Code = "INTERNAL",
            Name = "Internal Project",
            IsActive = true
        };
        _context.Projects.Add(project);

        // Create project tasks
        var task1 = new ProjectTask { TaskName = "Development", IsActive = true, Project = project };
        var task2 = new ProjectTask { TaskName = "Testing", IsActive = true, Project = project };
        var inactiveTask = new ProjectTask { TaskName = "Deprecated", IsActive = false, Project = project };
        _context.ProjectTasks.AddRange(task1, task2, inactiveTask);

        // Create inactive project for testing
        var inactiveProject = new Project
        {
            Code = "INACTIVE",
            Name = "Inactive Project",
            IsActive = false
        };
        _context.Projects.Add(inactiveProject);

        await _context.SaveChangesAsync();
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
    public async Task LogTime_WithValidMinimalInput_CreatesTimeEntry()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                    project { code }
                    projectTask { taskName }
                    standardHours
                    overtimeHours
                    status
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("logTime");

        Assert.Equal("INTERNAL", data.GetProperty("project").GetProperty("code").GetString());
        Assert.Equal("Development", data.GetProperty("projectTask").GetProperty("taskName").GetString());
        Assert.Equal(8.0, data.GetProperty("standardHours").GetDouble());
        Assert.Equal(0.0, data.GetProperty("overtimeHours").GetDouble());
        Assert.Equal("NOT_REPORTED", data.GetProperty("status").GetString());

        // Verify in database - ADR 0001: shadow FK populated automatically
        var entryId = Guid.Parse(data.GetProperty("id").GetString()!);
        var dbEntry = await _context.TimeEntries
            .Include(e => e.Project)
            .Include(e => e.ProjectTask)
            .FirstAsync(e => e.Id == entryId);

        Assert.NotNull(dbEntry);
        Assert.Equal("INTERNAL", dbEntry.Project.Code);
        Assert.Equal("Development", dbEntry.ProjectTask.TaskName);
    }

    [Fact]
    public async Task LogTime_WithAllFields_CreatesCompleteTimeEntry()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    issueId: ""DEV-123""
                    standardHours: 6.5
                    overtimeHours: 1.5
                    description: ""Implemented authentication feature""
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                    project { code }
                    projectTask { taskName }
                    issueId
                    standardHours
                    overtimeHours
                    description
                    startDate
                    completionDate
                    status
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("logTime");

        Assert.Equal("INTERNAL", data.GetProperty("project").GetProperty("code").GetString());
        Assert.Equal("Development", data.GetProperty("projectTask").GetProperty("taskName").GetString());
        Assert.Equal("DEV-123", data.GetProperty("issueId").GetString());
        Assert.Equal(6.5, data.GetProperty("standardHours").GetDouble());
        Assert.Equal(1.5, data.GetProperty("overtimeHours").GetDouble());
        Assert.Equal("Implemented authentication feature", data.GetProperty("description").GetString());
        Assert.Equal("2025-10-24", data.GetProperty("startDate").GetString());
        Assert.Equal("2025-10-24", data.GetProperty("completionDate").GetString());
        Assert.Equal("NOT_REPORTED", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task LogTime_WithDateRange_CreatesTimeEntry()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 16.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-25""
                }) {
                    id
                    startDate
                    completionDate
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("logTime");

        Assert.Equal("2025-10-24", data.GetProperty("startDate").GetString());
        Assert.Equal("2025-10-25", data.GetProperty("completionDate").GetString());
    }

    #endregion

    #region Validation Tests - Project

    [Fact]
    public async Task LogTime_WithNonExistentProject_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INVALID""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("INVALID", errorMessage);
        Assert.Contains("does not exist", errorMessage);
    }

    [Fact]
    public async Task LogTime_WithInactiveProject_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INACTIVE""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("INACTIVE", errorMessage);
        Assert.Contains("inactive", errorMessage);
    }

    #endregion

    #region Validation Tests - Task

    [Fact]
    public async Task LogTime_WithInvalidTask_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""InvalidTask""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("InvalidTask", errorMessage);
        Assert.Contains("not available", errorMessage);
    }

    [Fact]
    public async Task LogTime_WithInactiveTask_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Deprecated""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("Deprecated", errorMessage);
        Assert.Contains("not available", errorMessage);
    }

    #endregion

    #region Validation Tests - Hours

    [Fact]
    public async Task LogTime_WithNegativeStandardHours_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: -1.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("StandardHours", errorMessage);
    }

    [Fact]
    public async Task LogTime_WithNegativeOvertimeHours_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 8.0
                    overtimeHours: -2.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("OvertimeHours", errorMessage);
    }

    [Fact]
    public async Task LogTime_WithZeroHours_CreatesTimeEntry()
    {
        // Arrange - Edge case: 0 hours is valid
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 0.0
                    overtimeHours: 0.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                    standardHours
                    overtimeHours
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
        var data = result.RootElement.GetProperty("data").GetProperty("logTime");
        Assert.Equal(0.0, data.GetProperty("standardHours").GetDouble());
        Assert.Equal(0.0, data.GetProperty("overtimeHours").GetDouble());
    }

    #endregion

    #region Validation Tests - Dates

    [Fact]
    public async Task LogTime_WithStartDateAfterCompletionDate_ReturnsValidationError()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-25""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.True(result.RootElement.TryGetProperty("errors", out var errors));
        var errorMessage = errors[0].GetProperty("message").GetString();
        Assert.Contains("StartDate", errorMessage);
        Assert.Contains("CompletionDate", errorMessage);
    }

    [Fact]
    public async Task LogTime_WithSameStartAndCompletionDate_CreatesTimeEntry()
    {
        // Arrange - Edge case: same date is valid
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        Assert.False(result.RootElement.TryGetProperty("errors", out _));
    }

    #endregion

    #region Status and Timestamps Tests

    [Fact]
    public async Task LogTime_SetsStatusToNotReported()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    status
                }
            }";

        // Act
        var result = await ExecuteGraphQL(mutation);

        // Assert
        var data = result.RootElement.GetProperty("data").GetProperty("logTime");
        Assert.Equal("NOT_REPORTED", data.GetProperty("status").GetString());
    }

    [Fact]
    public async Task LogTime_SetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var mutation = @"
            mutation {
                logTime(input: {
                    projectCode: ""INTERNAL""
                    task: ""Development""
                    standardHours: 8.0
                    startDate: ""2025-10-24""
                    completionDate: ""2025-10-24""
                }) {
                    id
                    createdAt
                    updatedAt
                }
            }";

        // Act
        var beforeExecution = DateTime.UtcNow.AddSeconds(-1); // Add 1 second buffer for timing
        var result = await ExecuteGraphQL(mutation);
        var afterExecution = DateTime.UtcNow.AddSeconds(1); // Add 1 second buffer for timing

        // Assert
        var data = result.RootElement.GetProperty("data").GetProperty("logTime");
        var createdAt = DateTime.Parse(data.GetProperty("createdAt").GetString()!).ToUniversalTime();
        var updatedAt = DateTime.Parse(data.GetProperty("updatedAt").GetString()!).ToUniversalTime();

        Assert.True(createdAt >= beforeExecution && createdAt <= afterExecution,
            $"CreatedAt {createdAt:O} (UTC) should be between {beforeExecution:O} and {afterExecution:O}");
        Assert.True(updatedAt >= beforeExecution && updatedAt <= afterExecution,
            $"UpdatedAt {updatedAt:O} (UTC) should be between {beforeExecution:O} and {afterExecution:O}");
        Assert.Equal(createdAt, updatedAt); // Should be same on creation
    }

    #endregion
}
