# Task 7.2: Install Dependencies and Configure GraphQL Client

**Phase:** 7 - MCP Server Setup
**Estimated Time:** 30 minutes
**Prerequisites:** Task 7.1 complete (MCP project initialized)
**Status:** ✅ Complete

---

## Objective

Install necessary NuGet packages and configure the GraphQL client for communicating with the TimeReporting API.

---

## Acceptance Criteria

- [ ] GraphQL.Client NuGet package installed
- [ ] GraphQL.Client.Serializer.SystemTextJson installed
- [ ] Configuration class created for API URL and bearer token
- [ ] GraphQL client wrapper implemented
- [ ] Project builds successfully
- [ ] Client can be instantiated with environment variables

---

## Implementation Steps

### 1. Install NuGet Packages

```bash
cd TimeReportingMcp

# Install GraphQL client
dotnet add package GraphQL.Client --version 6.0.0

# Install JSON serializer for GraphQL client
dotnet add package GraphQL.Client.Serializer.SystemTextJson --version 6.0.0
```

**Verify packages in `TimeReportingMcp.csproj`:**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>TimeReportingMcp</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="GraphQL.Client" Version="6.0.0" />
    <PackageReference Include="GraphQL.Client.Serializer.SystemTextJson" Version="6.0.0" />
  </ItemGroup>

</Project>
```

### 2. Create Configuration Class

Create `Utils/McpConfig.cs`:

```csharp
using System;

namespace TimeReportingMcp.Utils;

/// <summary>
/// Configuration for MCP server loaded from environment variables
/// </summary>
public class McpConfig
{
    /// <summary>
    /// GraphQL API URL (e.g., http://localhost:5001/graphql)
    /// </summary>
    public string GraphQLApiUrl { get; }

    /// <summary>
    /// Bearer token for authentication
    /// </summary>
    public string BearerToken { get; }

    public McpConfig()
    {
        GraphQLApiUrl = GetRequiredEnvVar("GRAPHQL_API_URL");
        BearerToken = GetRequiredEnvVar("BEARER_TOKEN");
    }

    private static string GetRequiredEnvVar(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required environment variable '{name}' is not set. " +
                $"Please configure it before running the MCP server.");
        }
        return value;
    }

    /// <summary>
    /// Validate configuration is properly set
    /// </summary>
    public void Validate()
    {
        if (!Uri.TryCreate(GraphQLApiUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"GRAPHQL_API_URL '{GraphQLApiUrl}' is not a valid URL");
        }

        if (BearerToken.Length < 16)
        {
            throw new InvalidOperationException(
                "BEARER_TOKEN appears to be too short. Use a secure token (32+ characters).");
        }

        Console.Error.WriteLine($"Configuration loaded:");
        Console.Error.WriteLine($"  GraphQL API: {GraphQLApiUrl}");
        Console.Error.WriteLine($"  Bearer Token: {BearerToken.Substring(0, 8)}...");
    }
}
```

### 3. Create GraphQL Client Wrapper

Create `Utils/GraphQLClientWrapper.cs`:

```csharp
using System;
using System.Net.Http.Headers;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

namespace TimeReportingMcp.Utils;

/// <summary>
/// Wrapper for GraphQL client with authentication
/// </summary>
public class GraphQLClientWrapper : IDisposable
{
    private readonly GraphQLHttpClient _client;

    public GraphQLClientWrapper(McpConfig config)
    {
        var options = new GraphQLHttpClientOptions
        {
            EndPoint = new Uri(config.GraphQLApiUrl)
        };

        _client = new GraphQLHttpClient(options, new SystemTextJsonSerializer());

        // Add Bearer token authentication
        _client.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.BearerToken);

        Console.Error.WriteLine($"GraphQL client initialized for {config.GraphQLApiUrl}");
    }

    /// <summary>
    /// Execute a GraphQL query
    /// </summary>
    public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest request)
    {
        try
        {
            Console.Error.WriteLine($"Executing query: {request.Query?.Substring(0, 50)}...");
            var response = await _client.SendQueryAsync<T>(request);

            if (response.Errors != null && response.Errors.Length > 0)
            {
                Console.Error.WriteLine($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GraphQL query failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute a GraphQL mutation
    /// </summary>
    public async Task<GraphQLResponse<T>> SendMutationAsync<T>(GraphQLRequest request)
    {
        try
        {
            Console.Error.WriteLine($"Executing mutation: {request.Query?.Substring(0, 50)}...");
            var response = await _client.SendMutationAsync<T>(request);

            if (response.Errors != null && response.Errors.Length > 0)
            {
                Console.Error.WriteLine($"GraphQL errors: {string.Join(", ", response.Errors.Select(e => e.Message))}");
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GraphQL mutation failed: {ex.Message}");
            throw;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

### 4. Update Program.cs

Update `Program.cs` to test configuration and client initialization:

```csharp
using System;
using TimeReportingMcp.Utils;

namespace TimeReportingMcp;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.Error.WriteLine("TimeReporting MCP Server starting...");

            // Load and validate configuration
            var config = new McpConfig();
            config.Validate();

            // Initialize GraphQL client
            using var graphQLClient = new GraphQLClientWrapper(config);

            Console.Error.WriteLine("MCP Server initialized successfully");
            Console.Error.WriteLine("Ready to receive MCP requests on stdin");

            // McpServer will be implemented in Task 7.3
            // For now, just keep alive
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
```

### 5. Build and Test

```bash
cd TimeReportingMcp

# Restore packages
dotnet restore

# Build project
dotnet build
```

**Expected output:**
```
Restore succeeded.
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Testing

### Test 1: Missing Environment Variables

```bash
# Run without env vars (should fail gracefully)
dotnet run
```

**Expected output:**
```
TimeReporting MCP Server starting...
Fatal error: Required environment variable 'GRAPHQL_API_URL' is not set. Please configure it before running the MCP server.
```

### Test 2: With Environment Variables

```bash
# Set environment variables
export GRAPHQL_API_URL="http://localhost:5001/graphql"
export BEARER_TOKEN="test-token-1234567890abcdef"

# Run with env vars
dotnet run
```

**Expected output:**
```
TimeReporting MCP Server starting...
Configuration loaded:
  GraphQL API: http://localhost:5001/graphql
  Bearer Token: test-tok...
GraphQL client initialized for http://localhost:5001/graphql
MCP Server initialized successfully
Ready to receive MCP requests on stdin
```

Press `Ctrl+C` to stop.

### Test 3: Test with API Running

Ensure your GraphQL API is running:

```bash
# In another terminal
/run-api
```

Then test MCP server connection:

```bash
# Run MCP server
export GRAPHQL_API_URL="http://localhost:5001/graphql"
export BEARER_TOKEN="your-actual-token-from-.env"

cd TimeReportingMcp
dotnet run
```

Verify logs show successful initialization.

---

## Verification Checklist

- [ ] `dotnet restore` succeeds
- [ ] `dotnet build` succeeds with no warnings
- [ ] Running without env vars shows clear error message
- [ ] Running with env vars shows successful initialization
- [ ] Configuration validates API URL format
- [ ] Configuration validates bearer token length
- [ ] GraphQL client logs connection to API

---

## Related Files

**Created:**
- `Utils/McpConfig.cs` - Configuration management
- `Utils/GraphQLClientWrapper.cs` - GraphQL client wrapper

**Modified:**
- `TimeReportingMcp.csproj` - Added NuGet packages
- `Program.cs` - Added config and client initialization

---

## Next Steps

Proceed to [Task 7.3: JSON-RPC Request/Response Models](./task-7.3-json-rpc-models.md) to define MCP protocol models.

---

## Troubleshooting

### Issue: Package restore fails

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

### Issue: GraphQL.Client version conflict

**Solution:** Use exact versions specified (6.0.0) or latest stable version from NuGet.org.

### Issue: Environment variables not set in IDE

**Solution:** Configure launch settings in `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "TimeReportingMcp": {
      "commandName": "Project",
      "environmentVariables": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "BEARER_TOKEN": "your-token-here"
      }
    }
  }
}
```

---

## Notes

- **Security:** Never commit bearer tokens to version control
- **Logging:** Use `Console.Error` for logs to avoid interfering with stdio communication
- **Error Handling:** Fail fast with clear error messages
- **Validation:** Check configuration at startup, not during requests
