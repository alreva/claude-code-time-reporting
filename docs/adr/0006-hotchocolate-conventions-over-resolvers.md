# ADR 0006: HotChocolate Conventions Over Custom Resolvers

## Status

**Accepted**

## Context

When implementing GraphQL queries in Phase 3 of the Time Reporting System, we faced a decision about how to expose database entities through the GraphQL API:

**The initial approach** documented in the task files involved writing custom query resolvers with manual filtering logic:
- Manual `Where()` clauses for each filter parameter
- Manual `OrderBy()` for sorting
- Manual `Skip()`/`Take()` for pagination
- Explicit `.Include()` for navigation properties
- ~400 lines of repetitive resolver code for 4 queries

**The problem:**
- Custom resolvers require significant boilerplate code
- Each filter parameter must be explicitly coded
- Adding new filters requires code changes and redeployment
- Manual includes risk N+1 query problems
- Inconsistent filtering/sorting across different queries
- More code to write, test, and maintain

**Forces at play:**
- Need powerful, flexible filtering for time entries
- Want to minimize code while maximizing functionality
- Must prevent N+1 query problems
- Should follow GraphQL best practices (Relay spec for pagination)
- Want type-safe, compile-time validated queries
- Need efficient SQL translation (no in-memory filtering)

## Decision

**Use HotChocolate's built-in conventions for all queries:**
- `[UseFiltering]` - Auto-generate filtering for all fields
- `[UseSorting]` - Auto-generate sorting for all fields
- `[UsePaging]` - Cursor-based pagination following Relay spec
- `[UseProjection]` - Automatic navigation property loading and field selection
- `[UseDbContext]` - Proper DbContext scoping
- Return `IQueryable<T>` from query methods (not materialized lists)

**Implementation pattern:**
```csharp
[UseDbContext(typeof(TimeReportingDbContext))]
[UseProjection]
[UseFiltering]
[UseSorting]
[UsePaging(DefaultPageSize = 50, MaxPageSize = 200)]
public IQueryable<TimeEntry> GetTimeEntries(
    [ScopedService] TimeReportingDbContext context)
{
    return context.TimeEntries;
}
```

## Rationale

**Why HotChocolate conventions over custom resolvers?**

1. **Principle of Least Code**: Write the minimum code necessary. HotChocolate conventions eliminate 90% of boilerplate while providing more functionality.

2. **Convention Over Configuration**: HotChocolate follows GraphQL best practices out of the box (Relay spec, proper pagination, type-safe filtering).

3. **Flexibility Without Deployment**: Clients can create any filter combination without requiring API changes. New filtering needs don't require code deployments.

4. **Performance Optimization**: HotChocolate translates filters directly to SQL WHERE clauses. Projection prevents N+1 queries automatically.

5. **Type Safety**: Filtering and sorting respect entity field types at compile time. No runtime parsing errors.

6. **Maintainability**: ~40 lines of query code instead of ~400 lines. Less code = fewer bugs, easier to understand.

7. **Consistency**: All queries use the same pattern, making the codebase predictable and easy to navigate.

8. **Future-Proof**: As entity models evolve, filtering automatically extends to new fields without code changes.

## Consequences

### Benefits

✅ **90% Code Reduction**
- Custom resolvers: ~400 lines for 4 queries
- HotChocolate conventions: ~40 lines for 4 queries
- Less code to write, test, review, and maintain

✅ **More Powerful Filtering**
- Clients can filter by ANY field combination
- Complex `and`/`or` operators automatically available
- String operators: `eq`, `neq`, `contains`, `startsWith`, `endsWith`, `in`, `nin`
- Numeric operators: `eq`, `neq`, `gt`, `gte`, `lt`, `lte`, `in`, `nin`
- Date operators: Full range comparisons
- No code changes needed for new filter requirements

✅ **Optimal Performance**
- Filters translate to SQL WHERE clauses (no in-memory filtering)
- Projection loads only requested fields
- Automatic prevention of N+1 queries
- Efficient SQL generation by EF Core

✅ **GraphQL Best Practices**
- Cursor-based pagination (Relay specification)
- Proper `pageInfo` with `hasNextPage`, `hasPreviousPage`
- Field selection optimization (only query what's needed)
- Standardized filter/sort syntax

✅ **Type Safety**
- Compile-time validation of field types
- IDE autocomplete for entity properties
- Refactoring support (rename fields safely)

✅ **Consistency**
- All queries follow the same pattern
- Predictable API behavior
- Easy for new developers to understand

### Costs

⚠️ **Learning Curve**
- Developers need to understand HotChocolate conventions
- Different from traditional REST or manual GraphQL approaches
- Requires understanding of `IQueryable<T>` and deferred execution
- **Mitigation**: Well-documented in task files, examples provided

⚠️ **Less Control Over Query Generation**
- HotChocolate decides how to translate GraphQL to SQL
- Custom optimization strategies require workarounds
- Can't easily implement complex business logic in filters
- **Mitigation**: For complex scenarios, use custom resolvers selectively. Most queries don't need this.

⚠️ **GraphQL Schema Auto-Generation**
- Schema structure is inferred from C# types
- Less explicit control over GraphQL schema shape
- Requires understanding how HotChocolate maps types
- **Mitigation**: Use attributes (`[GraphQLName]`, `[GraphQLIgnore]`) for fine-tuning when needed

⚠️ **Debugging Complexity**
- Stack traces go through HotChocolate internals
- Filter translation happens at runtime
- Generated SQL can be non-obvious for complex filters
- **Mitigation**: Enable EF Core SQL logging during development, use profiler tools

⚠️ **Package Dependency**
- Depends on HotChocolate.Data packages
- Framework lock-in (switching GraphQL libraries would require rewrite)
- Need to stay updated with HotChocolate versions
- **Mitigation**: HotChocolate is mature, actively maintained, and widely adopted

### Trade-off Assessment

**Decision: The benefits overwhelmingly outweigh the costs.**

The 90% code reduction alone justifies this approach. Combined with more powerful filtering, better performance, and GraphQL best practices, this is the clear winner. The learning curve is acceptable given the excellent HotChocolate documentation and the examples we provide in task files.

For edge cases requiring complex business logic, we can selectively use custom resolvers while keeping the convention-based approach for standard queries.

## Implementation

### Configuration (Program.cs)

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()        // ✅ Enable field selection optimization
    .AddFiltering()          // ✅ Enable filtering
    .AddSorting()            // ✅ Enable sorting
    .RegisterDbContext<TimeReportingDbContext>(DbContextKind.Pooled);
```

### Query Pattern (Query.cs)

```csharp
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using TimeReportingApi.Data;
using TimeReportingApi.Models;

namespace TimeReportingApi.GraphQL;

public class Query
{
    /// <summary>
    /// Get time entries with automatic filtering, sorting, and pagination.
    /// </summary>
    [UseDbContext(typeof(TimeReportingDbContext))]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200)]
    public IQueryable<TimeEntry> GetTimeEntries(
        [ScopedService] TimeReportingDbContext context)
    {
        return context.TimeEntries;
    }

    /// <summary>
    /// Get a single time entry by ID.
    /// </summary>
    [UseDbContext(typeof(TimeReportingDbContext))]
    [UseProjection]
    public async Task<TimeEntry?> GetTimeEntry(
        Guid id,
        [ScopedService] TimeReportingDbContext context)
    {
        return await context.TimeEntries
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
```

### GraphQL Query Examples

**Simple filter:**
```graphql
query {
  timeEntries(where: { status: { eq: SUBMITTED } }) {
    nodes {
      id
      projectCode
      standardHours
    }
  }
}
```

**Complex filter with AND/OR:**
```graphql
query {
  timeEntries(where: {
    and: [
      { status: { in: [NOT_REPORTED, SUBMITTED] } }
      { standardHours: { gte: 4 } }
      { startDate: { gte: "2025-10-01" } }
    ]
  }) {
    nodes {
      id
      standardHours
    }
  }
}
```

**Sorting by multiple fields:**
```graphql
query {
  timeEntries(order: [
    { startDate: DESC }
    { projectCode: ASC }
  ]) {
    nodes {
      startDate
      projectCode
    }
  }
}
```

**Cursor-based pagination:**
```graphql
query {
  timeEntries(first: 10, after: "cursor-value") {
    nodes {
      id
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

**Projection (automatic navigation loading):**
```graphql
query {
  timeEntries {
    nodes {
      id
      project {
        name
        availableTasks {
          taskName
        }
      }
    }
  }
}
```

HotChocolate automatically:
- Includes only the `Project` navigation when `project` is requested
- Includes `AvailableTasks` only when nested query requests it
- Generates optimal SQL with proper JOINs
- No N+1 queries

### Anti-Patterns to Avoid

❌ **Don't materialize queries in resolvers:**
```csharp
// ❌ BAD - materializes immediately
public List<TimeEntry> GetTimeEntries(...)
{
    return context.TimeEntries.ToList(); // Loads everything!
}
```

✅ **Do return IQueryable:**
```csharp
// ✅ GOOD - deferred execution
public IQueryable<TimeEntry> GetTimeEntries(...)
{
    return context.TimeEntries; // HotChocolate applies filters
}
```

❌ **Don't mix custom filtering with conventions:**
```csharp
// ❌ BAD - custom filter defeats convention benefits
public IQueryable<TimeEntry> GetTimeEntries(string? project, ...)
{
    var query = context.TimeEntries;
    if (project != null)
        query = query.Where(e => e.ProjectCode == project);
    return query; // Client can't filter other fields
}
```

✅ **Do use pure conventions:**
```csharp
// ✅ GOOD - client controls all filtering
public IQueryable<TimeEntry> GetTimeEntries(...)
{
    return context.TimeEntries;
}
```

### When to Use Custom Resolvers

Use custom resolvers selectively when:
- Complex business logic must be applied (e.g., authorization rules)
- Need to aggregate or transform data not representable in entity model
- Performance optimization requires hand-crafted SQL
- Specific filter semantics beyond standard operators

Example (custom authorization filter):
```csharp
[UseDbContext(typeof(TimeReportingDbContext))]
[UseProjection]
[UseFiltering]
[UseSorting]
[UsePaging]
public IQueryable<TimeEntry> GetMyTimeEntries(
    [ScopedService] TimeReportingDbContext context,
    [Service] IHttpContextAccessor httpContext)
{
    var userId = httpContext.HttpContext.User.FindFirst("sub")?.Value;
    return context.TimeEntries.Where(e => e.UserId == userId);
}
```

## Alternatives Considered

### Alternative 1: Manual Custom Resolvers

**Approach**: Write explicit resolver methods with parameter-based filtering:

```csharp
public async Task<List<TimeEntry>> TimeEntries(
    TimeReportingDbContext context,
    DateOnly? startDate = null,
    DateOnly? endDate = null,
    string? projectCode = null,
    TimeEntryStatus? status = null,
    int limit = 50,
    int offset = 0)
{
    var query = context.TimeEntries.AsQueryable();

    if (startDate.HasValue)
        query = query.Where(e => e.StartDate >= startDate.Value);
    if (endDate.HasValue)
        query = query.Where(e => e.CompletionDate <= endDate.Value);
    // ... more filters

    return await query
        .OrderByDescending(e => e.StartDate)
        .Skip(offset)
        .Take(limit)
        .ToListAsync();
}
```

**Why rejected:**
- Requires ~100 lines per query (400 lines total for 4 queries)
- Each filter must be explicitly coded
- Adding new filters requires code changes and deployment
- Offset-based pagination is less efficient than cursor-based
- Manual includes risk N+1 problems
- No support for complex AND/OR logic without extensive code
- Clients can't compose filters flexibly

### Alternative 2: GraphQL Filter Input Types

**Approach**: Define explicit input types for filtering:

```csharp
public class TimeEntryFilterInput
{
    public DateOnly? StartDateFrom { get; set; }
    public DateOnly? StartDateTo { get; set; }
    public string? ProjectCode { get; set; }
    public List<TimeEntryStatus>? Statuses { get; set; }
}

public async Task<List<TimeEntry>> TimeEntries(
    TimeReportingDbContext context,
    TimeEntryFilterInput? filter = null)
{
    var query = context.TimeEntries.AsQueryable();

    if (filter != null)
    {
        if (filter.StartDateFrom.HasValue)
            query = query.Where(e => e.StartDate >= filter.StartDateFrom.Value);
        // ... more filters
    }

    return await query.ToListAsync();
}
```

**Why rejected:**
- Still requires manual filter implementation
- Input type definition adds more boilerplate
- Inflexible - can't add new filters without changing input type
- No support for complex AND/OR operators
- Requires explicit null checks for optional filters
- Pagination still needs manual implementation
- ~200 lines of code (better than Alternative 1, but still verbose)

### Alternative 3: Hybrid Approach (Common Filters + HotChocolate)

**Approach**: Use custom parameters for common filters, HotChocolate for advanced:

```csharp
[UseFiltering]
[UseSorting]
public IQueryable<TimeEntry> GetTimeEntries(
    TimeReportingDbContext context,
    string? projectCode = null,  // Common filter
    TimeEntryStatus? status = null) // Common filter
{
    var query = context.TimeEntries.AsQueryable();

    if (projectCode != null)
        query = query.Where(e => e.ProjectCode == projectCode);
    if (status.HasValue)
        query = query.Where(e => e.Status == status.Value);

    return query; // HotChocolate adds additional filters
}
```

**Why rejected:**
- Confusing for clients - some filters are parameters, others are `where` clause
- Defeats the purpose of conventions (inconsistent API)
- No significant benefit over pure conventions
- More complex to document and understand
- Maintenance burden: which filters deserve parameters?
- Still requires manual filter code for common cases

## References

- **HotChocolate Filtering**: https://chillicream.com/docs/hotchocolate/v13/fetching-data/filtering
- **HotChocolate Sorting**: https://chillicream.com/docs/hotchocolate/v13/fetching-data/sorting
- **HotChocolate Pagination**: https://chillicream.com/docs/hotchocolate/v13/fetching-data/pagination
- **HotChocolate Projections**: https://chillicream.com/docs/hotchocolate/v13/fetching-data/projections
- **Relay Cursor Connections Spec**: https://relay.dev/graphql/connections.htm
- Task files updated: `docs/tasks/phase-03-queries/task-3.1-*.md` through `task-3.5-*.md`

---

**Date**: 2025-10-28
**Decision Maker**: Development Team
**Review Date**: After Phase 3 completion (or if significant performance issues arise)
