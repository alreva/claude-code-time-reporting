using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using HotChocolate.Execution;
using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.GraphQL.Errors;
using TimeReportingApi.Services;

// Check for schema export command (used by MSBuild to export schema for MCP sync validation)
if (args.Length > 0 && args[0] == "export-schema")
{
    await ExportSchemaAsync();
    return;
}

// Normal API startup
var builder = WebApplication.CreateBuilder(args);

// Disable default claim type mapping to preserve original JWT claim names
// This allows us to access claims like "email", "oid", "name" directly
// instead of mapped URIs like "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configure Kestrel to listen on port 5001 (5000 is used by macOS AirPlay)
// In production/Docker, listen on all interfaces (0.0.0.0)
// In development, listen on localhost only for security
builder.WebHost.ConfigureKestrel(options =>
{
    if (builder.Environment.IsProduction())
    {
        options.ListenAnyIP(5001);  // Listen on 0.0.0.0:5001 (all interfaces)
    }
    else
    {
        options.ListenLocalhost(5001);  // Listen on 127.0.0.1:5001 (localhost only)
    }
});

ConfigureServices(builder);

var app = builder.Build();

// Apply database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TimeReportingDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add authentication and authorization middleware
// Order matters: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapGraphQL();

app.Run();

// Export GraphQL schema to stdout (used by MSBuild for MCP sync validation)
static async Task ExportSchemaAsync()
{
    var builder = WebApplication.CreateBuilder();
    ConfigureServices(builder);
    var app = builder.Build();

    var schema = await app.Services.GetRequiredService<IRequestExecutorResolver>()
        .GetRequestExecutorAsync();

    Console.Write(schema.Schema.Print());
}

// Shared service configuration used by both schema export and normal API startup
static void ConfigureServices(WebApplicationBuilder builder)
{
    // Add Entity Framework Core with PostgreSQL
    builder.Services.AddDbContext<TimeReportingDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("TimeReportingDb")));

    // Add business services
    builder.Services.AddScoped<ValidationService>();

    // Add HTTP context accessor for accessing user claims in services
    builder.Services.AddHttpContextAccessor();

    // Add Microsoft.Identity.Web authentication
    // This validates Azure Entra ID JWT tokens and extracts user claims
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    // Add authorization services
    builder.Services.AddAuthorization();

    // Add GraphQL server
    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddMutationType<Mutation>()
        .AddAuthorization()      // Enable @authorize directive for HotChocolate
        .AddProjections()        // Enable field selection optimization
        .AddFiltering()          // Enable filtering
        .AddSorting()            // Enable sorting
        .AddErrorFilter<GraphQLErrorFilter>() // Add custom error handling
        .ModifyCostOptions(options =>
        {
            // Disable cost limits for simple time tracking app
            options.MaxFieldCost = int.MaxValue;      // Effectively unlimited
            options.MaxTypeCost = int.MaxValue;       // Effectively unlimited
            options.EnforceCostLimits = false;        // Disable enforcement
            options.ApplyCostDefaults = false;        // Don't apply default costs
        });

    // Add health checks
    builder.Services.AddHealthChecks();
}

// Make Program class accessible to test project
public partial class Program { }
