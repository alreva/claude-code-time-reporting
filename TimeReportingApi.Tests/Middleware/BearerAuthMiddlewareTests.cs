using System.Net.Http.Headers;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;

namespace TimeReportingApi.Tests.Middleware;

public class BearerAuthMiddlewareTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _validToken;

    public BearerAuthMiddlewareTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _validToken = factory.BearerToken;
    }

    [Fact]
    public async Task GraphQL_GET_WithValidToken_ShouldAllowRequest()
    {
        // Arrange
        var client = _factory.CreateDefaultClient(new AuthenticationHandler(_validToken));

        // Act
        var response = await client.GetAsync("/graphql/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GraphQL_GET_WithoutToken_ShouldAllowRequest_ForPlayground()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No Authorization header - GET requests allowed for GraphQL Playground UI

        // Act
        var response = await client.GetAsync("/graphql");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldNotRequireAuthentication()
    {
        // Arrange
        var client = _factory.CreateClient();
        // No Authorization header set

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GraphQL_POST_WithValidToken_ShouldAllowRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var query = new StringContent(
            "{\"query\":\"{ __typename }\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _validToken);
        request.Content = query;

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GraphQL_POST_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var client = _factory.CreateClient();

        var query = new StringContent(
            "{\"query\":\"{ __typename }\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await client.PostAsync("/graphql", query);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
