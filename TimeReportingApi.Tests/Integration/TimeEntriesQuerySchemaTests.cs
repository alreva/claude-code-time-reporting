using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Tests that verify the TimeEntries query schema is correctly exposed in GraphQL.
/// These tests validate that HotChocolate filtering, sorting, and pagination are configured.
/// </summary>
public class TimeEntriesQuerySchemaTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TimeEntriesQuerySchemaTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateDefaultClient(new AuthenticationHandler("test-bearer-token-12345"));
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
    public async Task TimeEntriesQuery_IsExposed()
    {
        // Arrange
        var introspectionQuery = """

                                             query {
                                                 __type(name: "Query") {
                                                     fields {
                                                         name
                                                     }
                                                 }
                                             }
                                 """;

        // Act
        var result = await ExecuteGraphQL(introspectionQuery);

        // Assert
        var fields = result.RootElement
            .GetProperty("data")
            .GetProperty("__type")
            .GetProperty("fields");

        var fieldNames = fields.EnumerateArray()
            .Select(f => f.GetProperty("name").GetString())
            .ToList();

        fieldNames.Should().Contain("timeEntries");
    }

    [Fact]
    public async Task TimeEntriesQuery_HasPagination()
    {
        // Arrange
        var introspectionQuery = """

                                             query {
                                                 __type(name: "Query") {
                                                     fields {
                                                         name
                                                         type {
                                                             name
                                                             kind
                                                             ofType {
                                                                 name
                                                             }
                                                         }
                                                     }
                                                 }
                                             }
                                 """;

        // Act
        var result = await ExecuteGraphQL(introspectionQuery);

        // Assert
        var fields = result.RootElement
            .GetProperty("data")
            .GetProperty("__type")
            .GetProperty("fields");

        var timeEntriesField = fields.EnumerateArray()
            .FirstOrDefault(f => f.GetProperty("name").GetString() == "timeEntries");

        timeEntriesField.ValueKind.Should().NotBe(JsonValueKind.Undefined);

        // Verify it returns a connection type (has pageInfo and nodes)
        var type = timeEntriesField.GetProperty("type");

        // Handle both direct types and wrapped types (NON_NULL, LIST)
        string? typeName = null;
        if (type.TryGetProperty("name", out var nameElement) && nameElement.ValueKind != JsonValueKind.Null)
        {
            typeName = nameElement.GetString();
        }
        else if (type.TryGetProperty("ofType", out var ofType) && ofType.ValueKind != JsonValueKind.Null)
        {
            if (ofType.TryGetProperty("name", out var ofTypeName))
            {
                typeName = ofTypeName.GetString();
            }
        }

        typeName.Should().NotBeNullOrEmpty();
        typeName.Should().Contain("Connection");
    }

    [Fact]
    public async Task TimeEntriesQuery_SupportsFiltering()
    {
        // Arrange - Query with where clause to verify filtering is configured
        var query = """

                                query {
                                    timeEntries(where: { status: { eq: NOT_REPORTED } }) {
                                        nodes {
                                            id
                                        }
                                    }
                                }
                    """;

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert - Query should execute without errors
        var data = result.RootElement.GetProperty("data");
        data.GetProperty("timeEntries").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task TimeEntriesQuery_SupportsSorting()
    {
        // Arrange - Query with order clause to verify sorting is configured
        var query = """

                                query {
                                    timeEntries(order: { startDate: DESC }) {
                                        nodes {
                                            id
                                        }
                                    }
                                }
                    """;

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert - Query should execute without errors
        var data = result.RootElement.GetProperty("data");
        data.GetProperty("timeEntries").ValueKind.Should().NotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task TimeEntriesQuery_SupportsPageInfo()
    {
        // Arrange
        var query = """

                                query {
                                    timeEntries(first: 10) {
                                        pageInfo {
                                            hasNextPage
                                            hasPreviousPage
                                        }
                                        nodes {
                                            id
                                        }
                                    }
                                }
                    """;

        // Act
        var result = await ExecuteGraphQL(query);

        // Assert
        var timeEntries = result.RootElement.GetProperty("data").GetProperty("timeEntries");
        timeEntries.TryGetProperty("pageInfo", out var pageInfo).Should().BeTrue();
        pageInfo.TryGetProperty("hasNextPage", out _).Should().BeTrue();
        pageInfo.TryGetProperty("hasPreviousPage", out _).Should().BeTrue();
    }
}
