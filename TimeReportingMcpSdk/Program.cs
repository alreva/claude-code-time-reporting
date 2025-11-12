using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using TimeReportingMcpSdk.Extensions;
using TimeReportingMcpSdk.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging to stderr (MCP protocol requirement)
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Load configuration
var graphqlApiUrl = builder.Configuration["GRAPHQL_API_URL"]
                    ?? builder.Configuration["GraphQL:ApiUrl"]
                    ?? throw new InvalidOperationException("GRAPHQL_API_URL not configured");

var azureAdScope = builder.Configuration["AzureAd:ApiScope"]
                   ?? "api://8b3f87d7-bc23-4932-88b5-f24056999600/.default";

// Add TokenService for Azure CLI authentication
builder.Services.AddSingleton<TokenService>(sp =>
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
builder.Services.AddGraphQLResilience();

// Register HTTP message handlers
builder.Services.AddTransient<TimeReportingMcpSdk.Handlers.LoggingHandler>();
builder.Services.AddTransient<TimeReportingMcpSdk.Handlers.AuthTokenHandler>();

// Add StrawberryShake GraphQL client with handlers
builder.Services
    .AddTimeReportingClient()
    .ConfigureHttpClient((sp, client) =>
    {
        client.BaseAddress = new Uri(graphqlApiUrl);
    });

// Configure handlers for the StrawberryShake named client
builder.Services.ConfigureAll<HttpClientFactoryOptions>(options =>
{
    options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
    {
        handlerBuilder.AdditionalHandlers.Insert(0, handlerBuilder.Services.GetRequiredService<TimeReportingMcpSdk.Handlers.AuthTokenHandler>());
        handlerBuilder.AdditionalHandlers.Add(handlerBuilder.Services.GetRequiredService<TimeReportingMcpSdk.Handlers.LoggingHandler>());
    });
});

// Add MCP Server with stdio transport and tool discovery
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Build and run the MCP server
await builder.Build().RunAsync();
