using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Services;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.Error.WriteLine("TimeReporting MCP Server starting...");

            // Build configuration from environment variables
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            // Load GraphQL API URL
            var graphqlApiUrl = configuration["GRAPHQL_API_URL"]
                ?? configuration["GraphQL:ApiUrl"]
                ?? throw new InvalidOperationException("GRAPHQL_API_URL not configured");

            var azureAdScope = configuration["AzureAd:ApiScope"]
                ?? "api://8b3f87d7-bc23-4932-88b5-f24056999600/.default";

            Console.Error.WriteLine($"GraphQL API: {graphqlApiUrl}");
            Console.Error.WriteLine($"Azure AD Scope: {azureAdScope}");

            // Configure dependency injection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add TokenService for Azure CLI authentication
            services.AddSingleton<TokenService>(sp =>
            {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AzureAd:ApiScope"] = azureAdScope
                    })
                    .Build();
                var logger = sp.GetRequiredService<ILogger<TokenService>>();
                return new TokenService(config, logger);
            });

            // Add StrawberryShake GraphQL client with token from TokenService
            services
                .AddTimeReportingClient()
                .ConfigureHttpClient(async (sp, client) =>
                {
                    client.BaseAddress = new Uri(graphqlApiUrl);

                    // Acquire token from Azure CLI
                    var tokenService = sp.GetRequiredService<TokenService>();
                    var token = await tokenService.GetTokenAsync();

                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                });

            var serviceProvider = services.BuildServiceProvider();
            var graphqlClient = serviceProvider.GetRequiredService<ITimeReportingClient>();

            // Create and start MCP server
            var server = new McpServer(graphqlClient);

            Console.Error.WriteLine("MCP Server initialized successfully");
            Console.Error.WriteLine("Waiting for requests...");

            // Run server (blocks until Ctrl+C)
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.Error.WriteLine("\nShutting down MCP server...");
                cts.Cancel();
                e.Cancel = true;
            };

            await server.RunAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
