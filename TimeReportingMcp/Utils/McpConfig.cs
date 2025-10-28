using System;

namespace TimeReportingMcp.Utils;

/// <summary>
/// Configuration for MCP server loaded from environment variables
/// </summary>
public class McpConfig
{
    /// <summary>
    /// GraphQL API URL (e.g., http://localhost:5000/graphql)
    /// </summary>
    public string GraphQLApiUrl { get; }

    /// <summary>
    /// Bearer token for authentication
    /// </summary>
    public string BearerToken { get; }

    public McpConfig()
    {
        GraphQLApiUrl = GetRequiredEnvVar("GRAPHQL_API_URL");
        BearerToken = GetRequiredEnvVar("BEARER_TOKEN");
    }

    private static string GetRequiredEnvVar(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{name}' is not set. " +
                $"Please configure it before running the MCP server.");
        }
        return value;
    }

    /// <summary>
    /// Validate configuration is properly set
    /// </summary>
    public void Validate()
    {
        if (!Uri.TryCreate(GraphQLApiUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"GRAPHQL_API_URL '{GraphQLApiUrl}' is not a valid URL");
        }

        if (BearerToken.Length < 16)
        {
            throw new InvalidOperationException(
                "BEARER_TOKEN appears to be too short. Use a secure token (32+ characters).");
        }

        Console.Error.WriteLine($"Configuration loaded:");
        Console.Error.WriteLine($"  GraphQL API: {GraphQLApiUrl}");
        Console.Error.WriteLine($"  Bearer Token: {BearerToken.Substring(0, 8)}...");
    }
}
