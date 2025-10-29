using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TimeReportingMcp.Generated;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.Error.WriteLine("TimeReporting MCP Server starting...");

            // Load configuration
            var config = new McpConfig();
            config.Validate();

            // Configure dependency injection with StrawberryShake
            var services = new ServiceCollection();

            // Add StrawberryShake GraphQL client
            services
                .AddTimeReportingClient()
                .ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(config.ApiUrl);
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", config.BearerToken);
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
