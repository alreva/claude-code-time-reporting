using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Data;
using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;
using TimeReportingApi.Tests.Helpers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Integration tests for TimeEntries GraphQL query using real PostgreSQL database.
/// Each test class gets its own isolated PostgreSQL container via Testcontainers.
/// </summary>
public class TimeEntriesQueryTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private TimeReportingDbContext _context = null!;

    public TimeEntriesQueryTests(PostgresContainerFixture fixture)
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
        // Clean up test data
        _context.TimeEntries.RemoveRange(_context.TimeEntries);
        _context.Projects.RemoveRange(_context.Projects);
        await _context.SaveChangesAsync();

        await _context.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create project first
        var project = new Project { Code = "TEST", Name = "Test Project", IsActive = true };
        _context.Projects.Add(project);

        // Create project tasks
        var task1 = new ProjectTask { TaskName = "Development", IsActive = true, Project = project };
        var task2 = new ProjectTask { TaskName = "Testing", IsActive = true, Project = project };
        _context.ProjectTasks.AddRange(task1, task2);

        await _context.SaveChangesAsync();

        // Create time entries
        // Use the same userId that TestAuthHandler creates (oid claim)
        var entry1 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 8.0m,
            StartDate = new DateOnly(2025, 10, 20),
            CompletionDate = new DateOnly(2025, 10, 20),
            Status = TimeEntryStatus.NotReported,
            UserId = "test-oid-123",  // Must match oid claim from TestAuthHandler
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task1
        };

        var entry2 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 6.0m,
            StartDate = new DateOnly(2025, 10, 21),
            CompletionDate = new DateOnly(2025, 10, 21),
            Status = TimeEntryStatus.Submitted,
            UserId = "test-oid-123",  // Must match oid claim from TestAuthHandler
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task2
        };

        _context.TimeEntries.AddRange(entry1, entry2);
        await _context.SaveChangesAsync();
    }

    private async Task<JsonDocument> ExecuteGraphQL(string query)
    {
        var request = new { query };
        var response = await _client.PostAsJsonAsync("/graphql", request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json);
    }

    [Fact]
    public async Task TimeEntries_WithNoFilters_ReturnsAllEntries()
    {
        // Arrange
        var query = @"
            query {
                timeEntries {
                    nodes {
                        id
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task TimeEntries_FilterByStatus_Works()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(where: { status: { eq: SUBMITTED } }) {
                    nodes {
                        status
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(1);
        nodes[0].GetProperty("status").GetString().Should().Be("SUBMITTED");
    }

    [Fact]
    public async Task TimeEntries_FilterByProjectNavigation_Works()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(where: { project: { code: { eq: ""TEST"" } } }) {
                    nodes {
                        project {
                            code
                        }
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task TimeEntries_SortByStartDate_Works()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(order: { startDate: DESC }) {
                    nodes {
                        startDate
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");

        var firstDate = DateOnly.Parse(nodes[0].GetProperty("startDate").GetString()!);
        var secondDate = DateOnly.Parse(nodes[1].GetProperty("startDate").GetString()!);
        firstDate.Should().BeAfter(secondDate);
    }

    [Fact]
    public async Task TimeEntries_WithPagination_ReturnsFirstPage()
    {
        // Arrange
        var query = @"
            query {
                timeEntries(first: 1) {
                    nodes {
                        id
                    }
                    pageInfo {
                        hasNextPage
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var data = result.RootElement.GetProperty("data").GetProperty("timeEntries");
        var nodes = data.GetProperty("nodes");
        nodes.GetArrayLength().Should().Be(1);

        var hasNextPage = data.GetProperty("pageInfo").GetProperty("hasNextPage").GetBoolean();
        hasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task TimeEntry_WithValidId_ReturnsEntry()
    {
        // Arrange
        var existingEntry = await _context.TimeEntries.FirstAsync();
        var query = $@"
            query {{
                timeEntry(id: ""{existingEntry.Id}"") {{
                    id
                    standardHours
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var json = result.RootElement.GetRawText();
        var data = result.RootElement.GetProperty("data");

        // Check for GraphQL errors
        if (result.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = errors.EnumerateArray().First().GetProperty("message").GetString();
            throw new Exception($"GraphQL error: {errorMessage}");
        }

        var timeEntry = data.GetProperty("timeEntry");
        timeEntry.ValueKind.Should().NotBe(JsonValueKind.Null, $"timeEntry should not be null. Query returned: {json}");
        timeEntry.GetProperty("id").GetString().Should().Be(existingEntry.Id.ToString());
        timeEntry.GetProperty("standardHours").GetDecimal().Should().Be(existingEntry.StandardHours);
    }

    [Fact]
    public async Task TimeEntry_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var query = $@"
            query {{
                timeEntry(id: ""{Guid.NewGuid()}"") {{
                    id
                }}
            }}";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var data = result.RootElement.GetProperty("data").GetProperty("timeEntry");
        data.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task TimeEntries_FilterByOvertimeHoursGreaterThanZero_ReturnsOnlyOvertimeEntries()
    {
        // Arrange - Add entries with and without overtime
        var project = await _context.Projects.FirstAsync();
        var task = await _context.ProjectTasks.FirstAsync();

        var entryWithOvertime = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 6.0m,
            OvertimeHours = 2.0m, // Has overtime
            StartDate = new DateOnly(2025, 10, 25),
            CompletionDate = new DateOnly(2025, 10, 25),
            Status = TimeEntryStatus.NotReported,
            UserId = "test-oid-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task
        };

        var entryWithoutOvertime = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m, // No overtime
            StartDate = new DateOnly(2025, 10, 26),
            CompletionDate = new DateOnly(2025, 10, 26),
            Status = TimeEntryStatus.NotReported,
            UserId = "test-oid-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task
        };

        _context.TimeEntries.AddRange(entryWithOvertime, entryWithoutOvertime);
        await _context.SaveChangesAsync();

        var query = @"
            query {
                timeEntries(where: { overtimeHours: { gt: 0 } }) {
                    nodes {
                        id
                        standardHours
                        overtimeHours
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");

        // Should only return entry with overtime
        nodes.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var node in nodes.EnumerateArray())
        {
            node.GetProperty("overtimeHours").GetDecimal().Should().BeGreaterThan(0m);
        }
    }

    [Fact]
    public async Task TimeEntries_FilterByOvertimeHoursEqualToZero_ReturnsOnlyNonOvertimeEntries()
    {
        // Arrange - Add entries with and without overtime
        var project = await _context.Projects.FirstAsync();
        var task = await _context.ProjectTasks.FirstAsync();

        var entryWithOvertime = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 6.0m,
            OvertimeHours = 3.0m, // Has overtime
            StartDate = new DateOnly(2025, 10, 27),
            CompletionDate = new DateOnly(2025, 10, 27),
            Status = TimeEntryStatus.NotReported,
            UserId = "test-oid-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task
        };

        var entryWithoutOvertime = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 8.0m,
            OvertimeHours = 0.0m, // No overtime
            StartDate = new DateOnly(2025, 10, 28),
            CompletionDate = new DateOnly(2025, 10, 28),
            Status = TimeEntryStatus.NotReported,
            UserId = "test-oid-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task
        };

        _context.TimeEntries.AddRange(entryWithOvertime, entryWithoutOvertime);
        await _context.SaveChangesAsync();

        var query = @"
            query {
                timeEntries(where: { overtimeHours: { eq: 0 } }) {
                    nodes {
                        id
                        standardHours
                        overtimeHours
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var nodes = result.RootElement
            .GetProperty("data")
            .GetProperty("timeEntries")
            .GetProperty("nodes");

        // Should only return entries without overtime
        nodes.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var node in nodes.EnumerateArray())
        {
            node.GetProperty("overtimeHours").GetDecimal().Should().Be(0m);
        }
    }
}
