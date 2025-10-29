# Task 2.1: ASP.NET + HotChocolate Setup

**Phase:** 2 - GraphQL API Core Setup
**Estimated Time:** 1 hour
**Prerequisites:** Phase 1 complete (PostgreSQL database running)
**Status:** Pending

---

## Objective

Create a new ASP.NET Core 10 Web API project with HotChocolate GraphQL server, establish the basic project structure, and verify the GraphQL endpoint is accessible.

---

## Acceptance Criteria

- [ ] ASP.NET Core 10 project created (`TimeReportingApi`)
- [ ] HotChocolate GraphQL packages installed
- [ ] Project structure established (Models/, GraphQL/, Services/, Data/)
- [ ] GraphQL server configured in Program.cs
- [ ] GraphQL endpoint accessible at http://localhost:5001/graphql
- [ ] Banana Cake Pop (GraphQL IDE) loads successfully
- [ ] Basic health check endpoint works at /health
- [ ] Project builds successfully with `/build-api`
- [ ] Solution file created to manage multiple projects

---

## TDD Approach

For this foundational task, we'll use **integration tests** to verify the setup:

1. **RED:** Write test to verify GraphQL endpoint returns 200 OK
2. **RED:** Write test to verify health check endpoint works
3. **GREEN:** Implement minimal Program.cs configuration to pass tests
4. **REFACTOR:** Organize code into proper structure

---

## Implementation Steps

### Step 1: Create ASP.NET Core Project

```bash
# Create project directory
mkdir TimeReportingApi
cd TimeReportingApi

# Create ASP.NET Core Web API project
dotnet new webapi -n TimeReportingApi -f net8.0

# Create test project
dotnet new xunit -n TimeReportingApi.Tests -f net8.0

# Create solution file
cd ..
dotnet new sln -n TimeReportingSystem

# Add projects to solution
dotnet sln TimeReportingSystem.sln add TimeReportingApi/TimeReportingApi.csproj
dotnet sln TimeReportingSystem.sln add TimeReportingApi.Tests/TimeReportingApi.Tests.csproj

# Add test project reference to API project
dotnet add TimeReportingApi.Tests/TimeReportingApi.Tests.csproj reference TimeReportingApi/TimeReportingApi.csproj
```

### Step 2: Install HotChocolate Packages

```bash
# Navigate to API project
cd TimeReportingApi

# Install HotChocolate packages
dotnet add package HotChocolate.AspNetCore --version 15.1.11
dotnet add package HotChocolate.AspNetCore.Authorization --version 15.1.10

# Install test dependencies
cd ../TimeReportingApi.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.21
dotnet add package FluentAssertions --version 7.0.0
```

### Step 3: Create Project Structure

Create the following directories in `TimeReportingApi/`:

```
TimeReportingApi/
├── Models/              # Entity models (TimeEntry, Project, etc.)
├── GraphQL/             # GraphQL types, queries, mutations
│   ├── Types/           # GraphQL object types
│   ├── Inputs/          # GraphQL input types
│   ├── Query.cs         # Query root type
│   └── Mutation.cs      # Mutation root type
├── Services/            # Business logic services
├── Data/                # DbContext and EF Core configuration
└── Middleware/          # Custom middleware (auth, etc.)
```

```bash
cd ../TimeReportingApi
mkdir -p Models GraphQL/Types GraphQL/Inputs Services Data Middleware
```

### Step 4: Write Tests First (RED Phase)

Create `TimeReportingApi.Tests/Integration/ApiSetupTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TimeReportingApi.Tests.Integration;

public class ApiSetupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiSetupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GraphQL_Endpoint_ShouldBeAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/graphql");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_Endpoint_ShouldReturnHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task GraphQL_Endpoint_ShouldReturnGraphQLIDE()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/graphql");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        content.Should().Contain("Banana Cake Pop"); // HotChocolate's GraphQL IDE
    }
}
```

### Step 5: Make Program.cs Accessible to Tests

Update `TimeReportingApi/Program.cs` to expose the `Program` class:

At the end of `Program.cs`, add:

```csharp
// Make Program class accessible to test project
public partial class Program { }
```

### Step 6: Configure Minimal GraphQL Server (GREEN Phase)

Replace `TimeReportingApi/Program.cs` with:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5001
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000);
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
```

### Step 7: Create Empty Query and Mutation Classes

Create `TimeReportingApi/GraphQL/Query.cs`:

```csharp
namespace TimeReportingApi.GraphQL;

public class Query
{
    public string Hello() => "Hello, GraphQL!";
}
```

Create `TimeReportingApi/GraphQL/Mutation.cs`:

```csharp
namespace TimeReportingApi.GraphQL;

public class Mutation
{
    public string Placeholder() => "Mutations will be implemented in Phase 4";
}
```

### Step 8: Remove Default Files

```bash
# Remove default WeatherForecast files
rm TimeReportingApi/WeatherForecast.cs
rm TimeReportingApi/Controllers/WeatherForecastController.cs
rmdir TimeReportingApi/Controllers
```

### Step 9: Run Tests (Should Pass - GREEN Phase)

```bash
# Build the solution
/build

# Run tests
/test-api
```

**Expected Result:** All 3 tests should PASS ✅

### Step 10: Verify GraphQL Endpoint Manually

```bash
# Start the API
/run-api

# In another terminal, test with curl
curl http://localhost:5001/graphql

# Should return HTML with Banana Cake Pop IDE

# Test health endpoint
curl http://localhost:5001/health

# Should return: Healthy
```

Open browser to http://localhost:5001/graphql and verify Banana Cake Pop loads.

Test the hello query:

```graphql
query {
  hello
}
```

Expected response:

```json
{
  "data": {
    "hello": "Hello, GraphQL!"
  }
}
```

---

## Project Structure After Completion

```
time-reporting-system/
├── TimeReportingApi/
│   ├── Models/
│   ├── GraphQL/
│   │   ├── Types/
│   │   ├── Inputs/
│   │   ├── Query.cs         ✅ Created
│   │   └── Mutation.cs      ✅ Created
│   ├── Services/
│   ├── Data/
│   ├── Middleware/
│   ├── Program.cs           ✅ Configured
│   ├── appsettings.json
│   └── TimeReportingApi.csproj  ✅ Created
├── TimeReportingApi.Tests/
│   ├── Integration/
│   │   └── ApiSetupTests.cs ✅ Created
│   └── TimeReportingApi.Tests.csproj  ✅ Created
├── TimeReportingSystem.sln   ✅ Created
└── [existing files...]
```

---

## Testing Checklist

- [ ] `/build` completes without errors
- [ ] `/test-api` shows all tests passing (3/3)
- [ ] `/run-api` starts server on port 5001
- [ ] http://localhost:5001/graphql loads Banana Cake Pop IDE
- [ ] http://localhost:5001/health returns "Healthy"
- [ ] `hello` query returns "Hello, GraphQL!"
- [ ] No compiler warnings

---

## Common Issues & Troubleshooting

### Issue: "Program class not found in tests"

**Solution:** Ensure `public partial class Program { }` is at the bottom of Program.cs

### Issue: Port 5001 already in use

**Solution:** Stop other services or change port in Program.cs:

```csharp
options.ListenLocalhost(5001); // Use different port
```

### Issue: GraphQL endpoint returns 404

**Solution:** Verify `app.MapGraphQL()` is called in Program.cs

### Issue: Test project can't reference Program

**Solution:** Add to `TimeReportingApi.csproj`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="TimeReportingApi.Tests" />
</ItemGroup>
```

---

## Related Documentation

- **HotChocolate Docs:** https://chillicream.com/docs/hotchocolate/v13
- **ASP.NET Core 10:** https://learn.microsoft.com/en-us/aspnet/core/
- **WebApplicationFactory Testing:** https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests

---

## Next Steps

After completing this task:

1. ✅ Update TASK-INDEX.md to mark Task 2.1 as completed
2. ✅ Commit changes: "Complete Task 2.1: ASP.NET + HotChocolate Setup"
3. ➡️ Proceed to **Task 2.2** - Entity Framework Core Configuration

---

## Notes

- We're using HotChocolate 15.1.11 (latest stable as of Oct 2025)
- FluentAssertions 7.0.0 is used (v8+ requires commercial license for commercial use)
- Port 5001 is hardcoded for consistency with docker-compose.yml
- Banana Cake Pop is HotChocolate's built-in GraphQL IDE (replaces GraphQL Playground)
- The `hello` query is temporary - will be replaced in Phase 3
- Tests use `WebApplicationFactory` for true integration testing
- This setup follows ASP.NET Core 10 minimal API pattern
