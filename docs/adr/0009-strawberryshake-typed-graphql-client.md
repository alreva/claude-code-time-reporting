# ADR 0009: StrawberryShake Typed GraphQL Client

> **Note:** This ADR references bearer token authentication which has been superseded by Azure Entra ID authentication. See current authentication implementation in `Program.cs`, `appsettings.json`, and `TokenService.cs`.

## Status

**Accepted**

## Context

The MCP (Model Context Protocol) server currently uses hardcoded string queries to communicate with the GraphQL API. This approach has several drawbacks:

**Current Implementation:**
```csharp
var mutation = new GraphQLRequest
{
    Query = @"mutation LogTime($input: LogTimeInput!) {
        logTime(input: $input) {
            id
            projectCode
            task
            standardHours
            status
        }
    }",
    Variables = new { input = args }
};
```

**Problems with hardcoded strings:**
1. **No compile-time safety**: Typos in field names or query syntax are only caught at runtime
2. **No IntelliSense**: Developers must manually reference API documentation to know available fields
3. **Fragile refactoring**: Schema changes in the API don't trigger compiler errors in the MCP server
4. **Manual type mapping**: Response parsing requires manual deserialization with no type guarantees
5. **Maintenance burden**: Every API change requires manually updating string queries in multiple places
6. **No schema validation**: Invalid queries can make it to production

**Business Context:**
- The MCP server and GraphQL API are tightly coupled (same codebase, same team)
- Schema changes are frequent during development
- Type safety is a core value of this project (using C# for everything)
- The API is built with HotChocolate, part of the ChilliCream ecosystem

## Decision

**Use StrawberryShake for strongly-typed GraphQL client code generation in the MCP server.**

StrawberryShake will:
1. Introspect the GraphQL schema from `http://localhost:5001/graphql`
2. Generate C# classes from `.graphql` operation files at compile time
3. Provide strongly-typed client methods for all queries and mutations
4. Handle serialization/deserialization automatically

## Rationale

**Why StrawberryShake over other options:**

1. **Ecosystem alignment**: StrawberryShake is from ChilliCream, the same team as HotChocolate (our API framework). This ensures:
   - Seamless compatibility between client and server
   - Consistent conventions and patterns
   - Support for HotChocolate-specific features
   - Long-term ecosystem alignment

2. **Compile-time safety**: Schema changes in the API immediately cause build failures in the MCP server, catching breaking changes before runtime

3. **Developer experience**: IntelliSense provides autocomplete for all available fields, mutations, and queries

4. **Operation-specific types**: Unlike full schema generation, StrawberryShake only generates types for the fields you actually request in your `.graphql` files

5. **Source generator integration**: Uses modern .NET source generators for clean, efficient code generation

6. **C# native**: Pure .NET tooling, no Node.js or external dependencies required

## Consequences

### Benefits

**Type Safety**
- Compile-time validation of all GraphQL operations
- Refactoring support: renaming fields in the API triggers compiler errors
- Impossible to query non-existent fields
- Strong typing for all responses (no dynamic or JObject parsing)

**Developer Experience**
- IntelliSense for all GraphQL operations
- Autocomplete for fields, arguments, and types
- Clear separation: `.graphql` files for operations, generated C# classes for implementation
- Less boilerplate: no manual request/response classes

**Maintainability**
- Schema changes are immediately visible (build breaks if incompatible)
- Centralized operation definitions in `.graphql` files
- Generated code is always in sync with schema
- Easier onboarding: developers can explore API via IntelliSense

**Testing**
- Strongly-typed mocks for testing
- Compile-time verification of test data matches schema
- Better integration test coverage

### Costs

**Build Complexity**
- Adds code generation step to build process
- Requires GraphQL API to be running (or schema file available) during build
- Generated code increases solution size (~10-20KB per operation)

**Learning Curve**
- Developers must learn StrawberryShake conventions
- `.graphqlrc.json` configuration file to maintain
- Different mental model: write `.graphql` files, use generated C# classes

**Dependency**
- Additional NuGet packages (~5-6 packages)
- Tight coupling to StrawberryShake's release cycle
- Schema changes require rebuild (can't deploy MCP server independently without rebuild)

**Development Workflow**
- Must run `/build-mcp` after schema changes to regenerate client
- `.graphql` files must be kept in sync with actual usage
- Potential for merge conflicts in generated code (should be rare)

### Trade-off Assessment

**Decision: Type safety and developer experience vastly outweigh the build complexity costs.**

The MCP server is tightly coupled to the API by design (same codebase, same team). The additional build step is negligible compared to the benefits of catching schema mismatches at compile time instead of discovering them in production. The learning curve is minimal for C# developers already familiar with LINQ and code generation.

## Implementation

### Installation

**1. Install StrawberryShake CLI tools:**
```bash
dotnet tool install StrawberryShake.Tools --global
```

**2. Add NuGet packages to TimeReportingMcp.csproj:**
```xml
<PackageReference Include="StrawberryShake" Version="13.*" />
<PackageReference Include="StrawberryShake.CodeGeneration.CSharp.Analyzers" Version="13.*" />
<PackageReference Include="StrawberryShake.Transport.Http" Version="13.*" />
```

**3. Initialize the client:**
```bash
cd TimeReportingMcp
dotnet graphql init http://localhost:5001/graphql -n TimeReportingClient
```

This creates `.graphqlrc.json` configuration file.

### Configuration

**Update `.graphqlrc.json`:**
```json
{
  "schema": "http://localhost:5001/graphql",
  "documents": "**/*.graphql",
  "extensions": {
    "strawberryShake": {
      "name": "TimeReportingClient",
      "namespace": "TimeReportingMcp.Generated",
      "url": "http://localhost:5001/graphql",
      "dependencyInjection": true
    }
  }
}
```

### Creating Operations

**Create `TimeReportingMcp/GraphQL/LogTime.graphql`:**
```graphql
mutation LogTime($input: LogTimeInput!) {
  logTime(input: $input) {
    id
    projectCode
    task
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    description
    issueId
  }
}
```

**Create `TimeReportingMcp/GraphQL/GetProjects.graphql`:**
```graphql
query GetAvailableProjects($activeOnly: Boolean) {
  projects(activeOnly: $activeOnly) {
    code
    name
    isActive
    tasks {
      name
      description
      isActive
    }
    tags {
      name
      description
      isRequired
      allowedValues {
        value
        description
        isActive
      }
    }
  }
}
```

### Usage in Code

**Before (hardcoded strings):**
```csharp
private async Task<JsonRpcResponse> LogTime(Dictionary<string, JsonElement> args)
{
    var mutation = new GraphQLRequest
    {
        Query = @"mutation LogTime($input: LogTimeInput!) {
            logTime(input: $input) {
                id
                projectCode
                task
            }
        }",
        Variables = new { input = args }
    };

    var response = await _graphqlClient.SendMutationAsync<LogTimeResponse>(mutation);

    return new JsonRpcResponse
    {
        Result = new { content = new[] { new { type = "text", text = $"Created entry {response.Data.LogTime.Id}" } } }
    };
}
```

**After (StrawberryShake):**
```csharp
private readonly ITimeReportingClient _client;

private async Task<JsonRpcResponse> LogTime(Dictionary<string, JsonElement> args)
{
    // Parse input
    var input = new LogTimeInput
    {
        ProjectCode = args["projectCode"].GetString(),
        Task = args["task"].GetString(),
        StandardHours = args["standardHours"].GetDecimal(),
        // ... other fields
    };

    // Execute strongly-typed mutation
    var result = await _client.LogTime.ExecuteAsync(input);

    if (result.IsErrorResult())
    {
        return CreateErrorResponse(result.Errors);
    }

    var entry = result.Data.LogTime;

    return new JsonRpcResponse
    {
        Result = new { content = new[] { new {
            type = "text",
            text = $"Created entry {entry.Id} for {entry.ProjectCode}"
        } } }
    };
}
```

**Dependency Injection Setup:**
```csharp
// Program.cs
var services = new ServiceCollection();

services
    .AddTimeReportingClient()
    .ConfigureHttpClient(client => client.BaseAddress = new Uri("http://localhost:5001/graphql"))
    .AddHttpMessageHandler(() => new AuthenticationHandler(bearerToken));

var serviceProvider = services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<ITimeReportingClient>();
```

### Build Process

**After making schema or operation changes:**
```bash
/build-mcp
```

This will:
1. Introspect the GraphQL schema from the API
2. Parse all `.graphql` files
3. Generate C# client classes in `obj/Generated/`
4. Compile the MCP server with generated code

### File Organization

```
TimeReportingMcp/
├── .graphqlrc.json                    # StrawberryShake config
├── GraphQL/
│   ├── LogTime.graphql                # Mutation operations
│   ├── UpdateTimeEntry.graphql
│   ├── GetProjects.graphql            # Query operations
│   ├── QueryTimeEntries.graphql
│   └── ...
├── Tools/
│   ├── LogTimeTool.cs                 # Uses generated ITimeReportingClient
│   ├── QueryTimeEntriesTools.cs
│   └── ...
├── obj/Generated/                     # Auto-generated (gitignored)
│   ├── ITimeReportingClient.cs
│   ├── LogTimeMutation.cs
│   ├── GetProjectsQuery.cs
│   └── ...
└── Program.cs
```

### Anti-patterns to Avoid

**Don't manually edit generated code** - It will be overwritten on next build

**Don't query fields you don't need** - Keep `.graphql` files minimal to reduce generated code size

**Don't skip error handling** - Always check `result.IsErrorResult()` before accessing data

## Alternatives Considered

### Alternative 1: Continue with Hardcoded String Queries

**Approach**: Keep using `GraphQL.Client` with string-based queries and manual response parsing.

**Why rejected:**
- No compile-time safety for schema changes
- Prone to runtime errors from typos or outdated queries
- Poor developer experience (no IntelliSense)
- High maintenance burden as API evolves
- Doesn't align with project's strong typing philosophy

### Alternative 2: ZeroQL

**Approach**: Pure C# LINQ-like syntax that generates GraphQL queries from C# expressions at compile time.

**Why rejected:**
- Less explicit: queries are embedded in C# code rather than separate `.graphql` files
- Smaller ecosystem and community compared to StrawberryShake
- Not from ChilliCream (ecosystem misalignment)
- Magic string generation can be harder to debug
- Less clear separation between query definition and usage

**Example:**
```csharp
var response = await client.Query(q => q.TimeEntries(filters => filters.ProjectCode == "INTERNAL"));
```

While elegant, this approach hides the GraphQL query structure and makes it harder to reason about exactly what's being requested.

### Alternative 3: GraphQL Code Generator (Node.js) + C# Operations Plugin

**Approach**: Use the GraphQL Code Generator CLI tool (Node.js based) with the C# operations plugin to generate client code.

**Why rejected:**
- Requires Node.js toolchain in addition to .NET (breaks C# mono-stack philosophy)
- More complex build pipeline (npm + dotnet)
- Less .NET-native integration compared to StrawberryShake
- Additional configuration and tooling maintenance
- Not from HotChocolate ecosystem

### Alternative 4: Manual Type Generation from Schema

**Approach**: Write a custom script to introspect the schema and generate C# POCOs, then use them with `GraphQL.Client`.

**Why rejected:**
- Reinventing the wheel (StrawberryShake already solves this problem)
- Custom tooling requires maintenance and testing
- No operation-specific types (would generate entire schema)
- No IntelliSense support for operations
- Significant development time investment

## References

- StrawberryShake Documentation: https://chillicream.com/docs/strawberryshake
- HotChocolate Documentation: https://chillicream.com/docs/hotchocolate
- ChilliCream GitHub: https://github.com/ChilliCream/graphql-platform
- Related Task: Phase 7, Task 7.1 (MCP Server Setup)
- Related ADR: [0002 - C# Mono-Stack](0002-csharp-mono-stack.md)

---

**Note**: This decision reinforces our commitment to type safety, developer experience, and ecosystem alignment within the .NET/C# mono-stack architecture.
