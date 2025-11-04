using System.Net.Http.Json;
using System.Text.Json;
using TimeReportingApi.Tests.Fixtures;
using TimeReportingApi.Tests.Handlers;

namespace TimeReportingApi.Tests.Integration;

/// <summary>
/// Simple test to verify GraphQL is working before testing complex queries
/// </summary>
public class SimpleQueryTest : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SimpleQueryTest(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateDefaultClient(new AuthenticationHandler("test-bearer-token-12345"));
    }

    [Fact]
    public async Task HelloQuery_Works()
    {
        // Arrange
        var query = @"
            query {
                hello
            }";

        var request = new { query };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", request);

        // Assert - Check the response
        var statusCode = response.StatusCode;
        var content = await response.Content.ReadAsStringAsync();

        statusCode.Should().Be(HttpStatusCode.OK, $"Response: {content}");

        var json = JsonDocument.Parse(content);
        var hello = json.RootElement.GetProperty("data").GetProperty("hello").GetString();
        hello.Should().Be("Hello, GraphQL!");
    }
}
