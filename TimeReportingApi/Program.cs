using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.GraphQL.Errors;
using TimeReportingApi.Middleware;
using TimeReportingApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001 (5000 is used by macOS AirPlay)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001);
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
