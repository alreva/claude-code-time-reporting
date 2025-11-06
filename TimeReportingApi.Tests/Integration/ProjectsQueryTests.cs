using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Data;
using TimeReportingApi.Models;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;
using TimeReportingApi.Tests.Helpers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Integration tests for Projects GraphQL query using real PostgreSQL database.
/// </summary>
public class ProjectsQueryTests : IClassFixture<PostgresContainerFixture>, IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private TimeReportingDbContext _context = null!;

    public ProjectsQueryTests(PostgresContainerFixture fixture)
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
        _context.Projects.RemoveRange(_context.Projects);
        await _context.SaveChangesAsync();

        await _context.DisposeAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Create active project
        var activeProject = new Project { Code = "ACTIVE", Name = "Active Project", IsActive = true };
        _context.Projects.Add(activeProject);

        // Create inactive project
        var inactiveProject = new Project { Code = "INACTIVE", Name = "Inactive Project", IsActive = false };
        _context.Projects.Add(inactiveProject);

        // Save projects first to ensure they exist in database
        await _context.SaveChangesAsync();

        // Add tasks to active project using the navigation property
        // EF Core will automatically set the shadow property "ProjectCode"
        var task1 = new ProjectTask { Project = activeProject, TaskName = "Development", IsActive = true };
        var task2 = new ProjectTask { Project = activeProject, TaskName = "Testing", IsActive = true };
        _context.ProjectTasks.AddRange(task1, task2);

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
    public async Task Projects_WithNoFilters_ReturnsAllProjects()
    {
        // Arrange
        var query = @"
            query {
                projects {
                    code
                    name
                    isActive
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var projects = result.RootElement.GetProperty("data").GetProperty("projects");
        projects.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Projects_FilterByActive_Works()
    {
        // Arrange
        var query = @"
            query {
                projects(where: { isActive: { eq: true } }) {
                    code
                    name
                    isActive
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var projects = result.RootElement.GetProperty("data").GetProperty("projects");
        projects.GetArrayLength().Should().BeGreaterThan(0);
        foreach (var project in projects.EnumerateArray())
        {
            project.GetProperty("isActive").GetBoolean().Should().BeTrue();
        }
    }

    [Fact]
    public async Task Projects_SortByName_Works()
    {
        // Arrange
        var query = @"
            query {
                projects(order: { name: ASC }) {
                    code
                    name
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var projects = result.RootElement.GetProperty("data").GetProperty("projects");
        var projectArray = projects.EnumerateArray().ToArray();

        projectArray.Length.Should().Be(2);
        projectArray[0].GetProperty("name").GetString().Should().Be("Active Project");
        projectArray[1].GetProperty("name").GetString().Should().Be("Inactive Project");
    }

    [Fact]
    public async Task Projects_IncludesNavigationProperties()
    {
        // Arrange
        var query = @"
            query {
                projects(where: { code: { eq: ""ACTIVE"" } }) {
                    code
                    name
                    availableTasks {
                        taskName
                        isActive
                    }
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var projects = result.RootElement.GetProperty("data").GetProperty("projects");
        projects.GetArrayLength().Should().Be(1);

        var project = projects[0];
        var tasks = project.GetProperty("availableTasks");
        tasks.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Project_WithValidCode_ReturnsProjectWithDetails()
    {
        // Arrange
        var query = @"
            query {
                project(code: ""ACTIVE"") {
                    code
                    name
                    isActive
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var project = result.RootElement.GetProperty("data").GetProperty("project");
        project.GetProperty("code").GetString().Should().Be("ACTIVE");
        project.GetProperty("name").GetString().Should().Be("Active Project");
        project.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Project_WithInvalidCode_ReturnsNull()
    {
        // Arrange
        var query = @"
            query {
                project(code: ""NONEXISTENT"") {
                    code
                }
            }";

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var project = result.RootElement.GetProperty("data").GetProperty("project");
        project.ValueKind.Should().Be(JsonValueKind.Null);
    }
}
