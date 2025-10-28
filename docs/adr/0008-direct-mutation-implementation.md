# ADR 0008: Direct Mutation Implementation Pattern

## Status

**Accepted**

## Context

We need to implement GraphQL mutations for the Time Reporting API (logTime, updateTimeEntry, deleteTimeEntry, etc.). We must decide how to structure the mutation layer and where business logic should live.

**The decision point:**
- Should we use a traditional service layer pattern (Mutations → Services → DbContext)?
- Or should we implement business logic directly in GraphQL mutation resolvers with minimal abstraction?

**Constraints:**
- HotChocolate supports dependency injection directly into resolver methods
- We already have Queries implemented using direct DbContext access with HotChocolate conventions
- The system is simple: single GraphQL API consumed by an MCP server (no multi-interface requirements)
- Validation logic is complex enough to warrant a separate ValidationService
- Integration tests use real PostgreSQL via Testcontainers with GraphQL HTTP calls

**Key question:** How much layering do we need between GraphQL mutations and the database?

## Decision

**We will implement mutations directly in the `Mutation` class with injected `ValidationService` and `DbContext`, without an intermediate service layer.**

Mutations will:
1. Accept input types (e.g., `LogTimeInput`)
2. Inject `ValidationService` and `TimeReportingDbContext` via method parameters
3. Perform validation via `ValidationService`
4. Execute business logic directly in the mutation method
5. Save changes to `DbContext`
6. Return GraphQL types

**Pattern:**
```csharp
public class Mutation
{
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // Validate
        await validator.ValidateProjectAsync(input.ProjectCode);
        await validator.ValidateTaskAsync(input.ProjectCode, input.Task);
        // ... more validation

        // Business logic
        var entry = new TimeEntry { /* ... */ };
        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        return entry;
    }
}
```

## Rationale

**Alignment with HotChocolate conventions:**
- HotChocolate encourages injecting dependencies directly into resolver methods
- Our existing Query implementation follows this pattern (direct DbContext access)
- Consistency across queries and mutations simplifies the codebase

**Simplicity for our use case:**
- Single API consumed by a single MCP server (not multi-interface)
- Business logic is straightforward: validation + CRUD + workflow state transitions
- No need to reuse mutation logic outside GraphQL context

**Testability is equivalent:**
- Integration tests work identically: GraphQL HTTP calls → database verification
- Same test pattern as existing Query tests (already proven in codebase)
- ValidationService independently testable with unit tests
- Fewer layers means less mocking complexity

**Code volume reduction:**
- Eliminates intermediate service interfaces and implementations
- Reduces boilerplate (no service registration, no service method → mutation method mapping)
- Easier to understand the full mutation flow in one place

## Consequences

### Benefits

✅ **Simpler architecture**
- Fewer files, fewer abstractions, fewer lines of code
- Easy to trace execution: Mutation → Validation → DbContext
- New developers can understand mutations by reading one file

✅ **Consistent with existing patterns**
- Queries use direct DbContext access with HotChocolate conventions
- Same dependency injection pattern throughout GraphQL layer
- No paradigm shift between queries and mutations

✅ **Equally testable**
- Integration tests via GraphQL HTTP calls (same as Query tests)
- ValidationService unit tests for complex validation logic
- Real database via Testcontainers eliminates mocking complexity

✅ **Faster development**
- Less boilerplate to write and maintain
- No service layer abstractions to design and coordinate
- Direct mapping from GraphQL schema to implementation

✅ **HotChocolate-idiomatic**
- Follows framework conventions and best practices
- Leverages built-in DI support for resolver methods
- Allows HotChocolate to optimize resolver execution

### Costs

⚠️ **Business logic coupled to GraphQL**
- Cannot easily reuse mutation logic outside GraphQL context
- If we add a REST API later, we'd need to refactor or duplicate logic
- **Mitigation**: Our PRD specifies GraphQL-only API for MCP integration

⚠️ **Mutation classes could grow large**
- All mutation logic lives in `Mutation.cs`
- With 8 mutations, the file could become several hundred lines
- **Mitigation**: Can split into partial classes if needed (`Mutation.LogTime.cs`, etc.)

⚠️ **Less traditional for backend developers**
- Developers from service-oriented backgrounds may expect a service layer
- Could be seen as "skipping a layer"
- **Mitigation**: ADR documents the rationale; pattern is simple to understand

### Trade-off Assessment

**Decision: Simplicity and consistency win for this project.**

We're building a focused GraphQL API for MCP integration, not a multi-interface enterprise system. The benefits of simplicity, consistency with existing code, and reduced boilerplate outweigh the theoretical flexibility of a service layer we don't currently need. If requirements change (e.g., adding REST API), we can refactor with clear separation points (validation logic already extracted).

## Implementation

### Mutation Class Structure

```csharp
// TimeReportingApi/GraphQL/Mutation.cs
namespace TimeReportingApi.GraphQL;

public class Mutation
{
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        [Service] ValidationService validator,
        [Service] TimeReportingDbContext context)
    {
        // 1. Validate inputs
        await validator.ValidateProjectAsync(input.ProjectCode);
        await validator.ValidateTaskAsync(input.ProjectCode, input.Task);
        await validator.ValidateTagsAsync(input.ProjectCode, input.Tags);

        if (input.StartDate > input.CompletionDate)
            throw new ValidationException("StartDate must be <= CompletionDate");

        if (input.StandardHours < 0)
            throw new ValidationException("StandardHours must be >= 0");

        // 2. Create entity
        var entry = new TimeEntry
        {
            Id = Guid.NewGuid(),
            ProjectCode = input.ProjectCode,
            Task = input.Task,
            StandardHours = input.StandardHours,
            OvertimeHours = input.OvertimeHours ?? 0,
            Description = input.Description,
            StartDate = input.StartDate,
            CompletionDate = input.CompletionDate,
            Status = TimeEntryStatus.NotReported,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Save to database
        context.TimeEntries.Add(entry);
        await context.SaveChangesAsync();

        return entry;
    }

    // Other mutations follow same pattern...
}
```

### ValidationService (DI-Injected)

```csharp
// TimeReportingApi/Services/ValidationService.cs
namespace TimeReportingApi.Services;

public class ValidationService
{
    private readonly TimeReportingDbContext _context;

    public ValidationService(TimeReportingDbContext context)
    {
        _context = context;
    }

    public async Task ValidateProjectAsync(string projectCode)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Code == projectCode);

        if (project == null || !project.IsActive)
            throw new ValidationException($"Project '{projectCode}' does not exist or is inactive");
    }

    public async Task ValidateTaskAsync(string projectCode, string taskName)
    {
        var taskExists = await _context.ProjectTasks
            .AnyAsync(t => t.ProjectCode == projectCode && t.TaskName == taskName && t.IsActive);

        if (!taskExists)
            throw new ValidationException($"Task '{taskName}' is not available for project '{projectCode}'");
    }

    // ... other validation methods
}
```

### Program.cs Registration

```csharp
// Register ValidationService in DI
builder.Services.AddScoped<ValidationService>();

// Register GraphQL with Mutation class
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>() // ← Direct mutation class
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

### Integration Test Example

```csharp
[Fact]
public async Task LogTime_WithValidInput_CreatesTimeEntry()
{
    // Arrange
    var mutation = @"
        mutation {
            logTime(input: {
                projectCode: ""TEST""
                task: ""Development""
                standardHours: 8.0
                startDate: ""2025-10-24""
                completionDate: ""2025-10-24""
            }) {
                id
                projectCode
                status
            }
        }";

    // Act
    var response = await _client.PostAsJsonAsync("/graphql", new { query = mutation });

    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadAsStringAsync();
    var result = JsonDocument.Parse(json);

    Assert.Equal("TEST", result.RootElement.GetProperty("data")
        .GetProperty("logTime").GetProperty("projectCode").GetString());
    Assert.Equal("NOT_REPORTED", result.RootElement.GetProperty("data")
        .GetProperty("logTime").GetProperty("status").GetString());
}
```

## Alternatives Considered

### Alternative 1: Service Layer Pattern

**Approach**:
- Create `ITimeEntryService` interface and `TimeEntryService` implementation
- Mutations call service methods
- Service layer contains business logic
- Service is registered in DI

```csharp
public interface ITimeEntryService
{
    Task<TimeEntry> LogTimeAsync(LogTimeInput input);
    Task<TimeEntry> UpdateTimeEntryAsync(Guid id, UpdateTimeEntryInput input);
    // ...
}

public class Mutation
{
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        [Service] ITimeEntryService service)
    {
        return await service.LogTimeAsync(input);
    }
}
```

**Why rejected:**
- Adds abstraction layer without clear benefit for our use case
- More code to write and maintain (interfaces + implementations)
- No requirement for reusing logic outside GraphQL
- Inconsistent with existing Query pattern (queries use direct DbContext)
- Extra mocking required in tests (mock service instead of using real database)

### Alternative 2: CQRS with MediatR

**Approach**:
- Use MediatR library for command/query separation
- Mutations dispatch commands to handlers
- Handlers contain business logic

```csharp
public class Mutation
{
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        [Service] IMediator mediator)
    {
        return await mediator.Send(new LogTimeCommand(input));
    }
}

public class LogTimeCommandHandler : IRequestHandler<LogTimeCommand, TimeEntry>
{
    // Business logic here
}
```

**Why rejected:**
- Overkill for our simple CRUD + validation use case
- Adds dependency on MediatR library
- Introduces significant complexity (command classes, handler classes, registrations)
- No clear architectural benefit for our scope (single API, straightforward workflows)
- Steeper learning curve for contributors unfamiliar with CQRS/MediatR

### Alternative 3: Repository Pattern

**Approach**:
- Create repository abstraction over DbContext
- Mutations use repositories instead of direct DbContext

```csharp
public interface ITimeEntryRepository
{
    Task<TimeEntry> CreateAsync(TimeEntry entry);
    Task<TimeEntry> UpdateAsync(TimeEntry entry);
    // ...
}

public class Mutation
{
    public async Task<TimeEntry> LogTime(
        LogTimeInput input,
        [Service] ITimeEntryRepository repository,
        [Service] ValidationService validator)
    {
        // Validation...
        var entry = new TimeEntry { /* ... */ };
        return await repository.CreateAsync(entry);
    }
}
```

**Why rejected:**
- Entity Framework Core already provides repository and unit of work patterns
- DbContext IS the repository abstraction
- Extra layer doesn't add testability (Testcontainers provides real database)
- More code to maintain without architectural benefit
- Microsoft guidance: "Don't create repository abstractions over EF Core unless necessary"

## References

- Related ADR: [0006 - HotChocolate Conventions Over Custom Resolvers](0006-hotchocolate-conventions-over-resolvers.md)
- Related ADR: [0007 - Testcontainers for Integration Tests](0007-testcontainers-for-integration-tests.md)
- HotChocolate Documentation: [Resolvers](https://chillicream.com/docs/hotchocolate/v13/fetching-data/resolvers)
- Microsoft Docs: [Testing EF Core Applications](https://learn.microsoft.com/en-us/ef/core/testing/)
- PRD: `docs/prd/api-specification.md` - GraphQL mutations specification
