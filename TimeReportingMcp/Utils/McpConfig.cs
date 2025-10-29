using System;
using Microsoft.Extensions.Configuration;

namespace TimeReportingMcp.Utils;

/// <summary>
/// Configuration for MCP server loaded from .NET Configuration system
/// (Consistent with GraphQL API approach)
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

    public McpConfig(IConfiguration configuration)
    {
        GraphQLApiUrl = GetRequiredConfigValue(configuration, "GRAPHQL_API_URL");
        BearerToken = GetRequiredConfigValue(configuration, "Authentication:BearerToken");
    }

    private static string GetRequiredConfigValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required configuration value '{key}' is not set. " +
                $"Please set the corresponding environment variable before running the MCP server.");
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
                "Authentication__BearerToken appears to be too short. Use a secure token (32+ characters).");
        }

        Console.Error.WriteLine($"Configuration loaded:");
        Console.Error.WriteLine($"  GraphQL API: {GraphQLApiUrl}");
        Console.Error.WriteLine($"  Bearer Token: {BearerToken.Substring(0, 8)}...");
    }
}
