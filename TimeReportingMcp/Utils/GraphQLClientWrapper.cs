using System;
using System.Net.Http.Headers;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

namespace TimeReportingMcp.Utils;

/// <summary>
/// Wrapper for GraphQL client with authentication
/// </summary>
public class GraphQLClientWrapper : IDisposable
{
    private readonly GraphQLHttpClient _client;

    public GraphQLClientWrapper(McpConfig config)
    {
        var options = new GraphQLHttpClientOptions
        {
            EndPoint = new Uri(config.GraphQLApiUrl)
        };

        _client = new GraphQLHttpClient(options, new SystemTextJsonSerializer());

        // Add Bearer token authentication
        _client.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.BearerToken);

        Console.Error.WriteLine($"GraphQL client initialized for {config.GraphQLApiUrl}");
    }

    /// <summary>
    /// Execute a GraphQL query
    /// </summary>
    public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest request)
    {
        try
        {
            Console.Error.WriteLine($"Executing query: {request.Query?.Substring(0, 50)}...");
            var response = await _client.SendQueryAsync<T>(request);

            if (response.Errors != null && response.Errors.Length > 0)
            {
                Console.Error.WriteLine($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GraphQL query failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute a GraphQL mutation
    /// </summary>
    public async Task<GraphQLResponse<T>> SendMutationAsync<T>(GraphQLRequest request)
    {
        try
        {
            Console.Error.WriteLine($"Executing mutation: {request.Query?.Substring(0, 50)}...");
            var response = await _client.SendMutationAsync<T>(request);

            if (response.Errors != null && response.Errors.Length > 0)
            {
                Console.Error.WriteLine($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GraphQL mutation failed: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
