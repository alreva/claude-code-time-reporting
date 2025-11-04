using System;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
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

            // Add StrawberryShake GraphQL client with token from TokenService and Polly resilience
            services
                .AddTimeReportingClient()
                .ConfigureHttpClient(
                    async (sp, client) =>
                    {
                        client.BaseAddress = new Uri(graphqlApiUrl);

                        // Acquire initial token from Azure CLI
                        var tokenService = sp.GetRequiredService<TokenService>();
                        var token = await tokenService.GetTokenAsync();

                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", token);
                    },
                    builder =>
                    {
                        // Add Polly resilience handler for token refresh and transient errors
                        builder.AddResilienceHandler("token-refresh-pipeline", (pipelineBuilder, context) =>
                        {
                            var sp = context.ServiceProvider;
                            var tokenService = sp.GetRequiredService<TokenService>();
                            var logger = sp.GetRequiredService<ILogger<Program>>();

                            // Configure retry strategy for 401 Unauthorized (expired token) and transient errors
                            pipelineBuilder.AddRetry(new HttpRetryStrategyOptions
                            {
                                MaxRetryAttempts = 2,
                                Delay = TimeSpan.FromMilliseconds(500),
                                BackoffType = DelayBackoffType.Exponential,
                                UseJitter = true,

                                // Retry on 401 Unauthorized (expired token) and other transient errors
                                ShouldHandle = args =>
                                {
                                    return ValueTask.FromResult(args.Outcome switch
                                    {
                                        { Result.StatusCode: System.Net.HttpStatusCode.Unauthorized } => true,
                                        { Result.StatusCode: System.Net.HttpStatusCode.RequestTimeout } => true,
                                        { Result.StatusCode: System.Net.HttpStatusCode.TooManyRequests } => true,
                                        { Result.StatusCode: >= System.Net.HttpStatusCode.InternalServerError } => true,
                                        { Exception: HttpRequestException } => true,
                                        _ => false
                                    });
                                },

                                // On each retry attempt, refresh the token
                                OnRetry = async args =>
                                {
                                    logger.LogWarning(
                                        "HTTP request failed with status {StatusCode}. Retry attempt {Attempt} of {MaxRetries} after {Delay}s",
                                        args.Outcome.Result?.StatusCode,
                                        args.AttemptNumber,
                                        2,
                                        args.RetryDelay.TotalSeconds);

                                    // If 401 Unauthorized, refresh the token
                                    if (args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                                    {
                                        logger.LogInformation("Refreshing authentication token from Azure CLI");

                                        // Clear cached token and get a fresh one
                                        tokenService.ClearCache();
                                        var newToken = await tokenService.GetTokenAsync(args.Context.CancellationToken);

                                        logger.LogInformation("Token refreshed successfully");

                                        // Update the Authorization header for the retry
                                        // Note: StrawberryShake will create a new request, so we store the token
                                        // The actual header update happens in the next request creation
                                        args.Context.Properties.Set(new ResiliencePropertyKey<string>("RefreshedToken"), newToken);
                                    }
                                }
                            });

                            // Add timeout strategy
                            pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(30));
                        });
                    });

            var serviceProvider = services.BuildServiceProvider();
            var graphqlClient = serviceProvider.GetRequiredService<ITimeReportingClient>();

            // Create and start MCP server
            var server = new McpServer(graphqlClient);

            Console.Error.WriteLine("MCP Server initialized successfully");
            Console.Error.WriteLine("Waiting for requests...");

            // Run server (blocks until Ctrl+C or stdin closes)
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                Console.Error.WriteLine("\nShutting down MCP server...");
                cts.Cancel();
                e.Cancel = true;
            };

            await server.RunAsync(cts.Token);

            Console.Error.WriteLine("MCP Server exited successfully");
            Environment.Exit(0);
        }
        catch (OperationCanceledException)
        {
            // Expected cancellation - exit cleanly
            Console.Error.WriteLine("MCP Server cancelled - exiting cleanly");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
