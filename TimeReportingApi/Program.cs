using TimeReportingApi.Data;
using TimeReportingApi.GraphQL;
using TimeReportingApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001 (5000 is used by macOS AirPlay)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001);
});

// Add Entity Framework Core with PostgreSQL
builder.Services.AddDbContext<TimeReportingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TimeReportingDb")));

// Add services to the container
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()        // Enable field selection optimization
    .AddFiltering()          // Enable filtering
    .AddSorting();           // Enable sorting

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
