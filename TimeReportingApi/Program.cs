using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.GraphQL.Errors;
using TimeReportingApi.Middleware;
using TimeReportingApi.Services;

var builder = WebApplication.CreateBuilder(args);

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

// Add Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<TimeReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TimeReportingDb")));

// Add business services
builder.Services.AddScoped<ValidationService>();

// Add services to the container
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()        // Enable field selection optimization
    .AddFiltering()          // Enable filtering
    .AddSorting()            // Enable sorting
    .AddErrorFilter<GraphQLErrorFilter>();  // Add custom error handling

// Add health checks
builder.Services.AddHealthChecks();

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

// Add Bearer token authentication
app.UseBearerAuthentication();

app.MapHealthChecks("/health");

app.MapGraphQL();

app.Run();

// Make Program class accessible to test project
public partial class Program { }
