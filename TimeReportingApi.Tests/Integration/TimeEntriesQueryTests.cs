using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Data;
using TimeReportingApi.Models;

namespace TimeReportingApi.Tests.Integration;

public class TimeEntriesQueryTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly TimeReportingDbContext _context;

    public TimeEntriesQueryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
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

                // Also remove the DbContext registration itself
                var dbContextServiceDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(TimeReportingDbContext));

                if (dbContextServiceDescriptor != null)
                {
                    services.Remove(dbContextServiceDescriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<TimeReportingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<TimeReportingDbContext>();

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create project first
        var project = new Project { Code = "TEST", Name = "Test Project", IsActive = true };
        _context.Projects.Add(project);

        // Create project tasks
        var task1 = new ProjectTask { TaskName = "Development", IsActive = true, Project = project };
        var task2 = new ProjectTask { TaskName = "Testing", IsActive = true, Project = project };
        _context.ProjectTasks.AddRange(task1, task2);

        _context.SaveChanges();

        // Create time entries - using Add then setting shadow properties
        var entry1 = new TimeEntry
        {
            Id = Guid.NewGuid(),
            StandardHours = 8.0m,
            StartDate = new DateOnly(2025, 10, 20),
            CompletionDate = new DateOnly(2025, 10, 20),
            Status = TimeEntryStatus.NotReported,
            UserId = "user1",
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
            UserId = "user2",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Project = project,
            ProjectTask = task2
        };

        _context.TimeEntries.AddRange(entry1, entry2);
        _context.SaveChanges();
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

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _scope.Dispose();
        _client.Dispose();
    }
}
