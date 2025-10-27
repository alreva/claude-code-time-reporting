using TimeReportingApi.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001 (5000 is used by macOS AirPlay)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001);
});

// Add services to the container
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapHealthChecks("/health");

app.MapGraphQL();

app.Run();

// Make Program class accessible to test project
public partial class Program { }
