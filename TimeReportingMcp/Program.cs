using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using TimeReportingMcp;
using TimeReportingMcp.Extensions;
using TimeReportingMcp.Services;
using TimeReportingMcp.Tools;

try
{
    await Console.Error.WriteLineAsync("TimeReporting MCP Server starting...");

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

    await Console.Error.WriteLineAsync($"GraphQL API: {graphqlApiUrl}");
    await Console.Error.WriteLineAsync($"Azure AD Scope: {azureAdScope}");

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

    // Add GraphQL resilience pipeline for auth retry
    services.AddGraphQLResilience();

    // Register handlers
    services.AddTransient<TimeReportingMcp.Handlers.LoggingHandler>();
    services.AddTransient<TimeReportingMcp.Handlers.AuthTokenHandler>();

    // Add StrawberryShake GraphQL client with handlers
    services
        .AddTimeReportingClient()
        .ConfigureHttpClient((sp, client) =>
        {
            client.BaseAddress = new Uri(graphqlApiUrl);
        });

    // Configure handlers for the StrawberryShake named client
    // StrawberryShake creates a named HttpClient with the pattern: "{ClientName}_{OperationName}"
    // We'll configure all StrawberryShake clients using ConfigureHttpClientDefaults
    services.ConfigureAll<HttpClientFactoryOptions>(options =>
    {
        options.HttpMessageHandlerBuilderActions.Add(builder =>
        {
            builder.AdditionalHandlers.Insert(0, builder.Services.GetRequiredService<TimeReportingMcp.Handlers.AuthTokenHandler>());
            builder.AdditionalHandlers.Add(builder.Services.GetRequiredService<TimeReportingMcp.Handlers.LoggingHandler>());
        });
    });

    services
        .RegisterMcpServer()
        .RegisterToolDefinitions();

    var serviceProvider = services.BuildServiceProvider();

    // Create and start MCP server with service provider for DI
    var server = serviceProvider.GetRequiredService<McpServer>();

    await Console.Error.WriteLineAsync("MCP Server initialized successfully");
    await Console.Error.WriteLineAsync("Waiting for requests...");

    // Run server (blocks until Ctrl+C or stdin closes)
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        Console.Error.WriteLine("\nShutting down immediately...");
        // Console.Error.WriteLine("\nShutting down MCP server...");
        // e.Cancel = true;
        // cts.Cancel();
    };
    
    AppDomain.CurrentDomain.ProcessExit += (s, e) =>
    {
        Console.Error.WriteLine("ProcessExit triggered...");
        cts.Cancel();
    };

    await server.RunAsync(cts.Token);

    await Console.Error.WriteLineAsync("MCP Server exited successfully");
    Environment.Exit(0);
}
catch (OperationCanceledException)
{
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");
    await Console.Error.WriteLineAsync($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}
finally
{
    Console.Error.WriteLine("Server stopped.");
}