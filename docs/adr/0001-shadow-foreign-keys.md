# ADR 0001: Shadow Foreign Keys - Eliminating the FK/Navigation Foot-Gun

## Status

**Accepted** (Implemented in commit `d7b4fa0`)

## Context

Entity Framework Core allows developers to define both:
1. **Explicit FK properties** (e.g., `TimeEntry.ProjectCode` as a string property)
2. **Navigation properties** (e.g., `TimeEntry.Project` as a Project object)

This creates a dangerous scenario: developers can set both properties to **conflicting values**.

### The Original Question

*"Is it possible to shoot myself in the foot? Say, I create a TimeEntry and my TimeEntry.Project is one project with project code 'INTERNAL' and I set the TimeEntry.ProjectCode to a value other than 'INTERNAL'?"*

### The Problem

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    public string ProjectCode { get; set; }  // ← Explicit FK property
    public int ProjectTaskId { get; set; }    // ← Explicit FK property

    public Project Project { get; set; }      // ← Navigation property
    public ProjectTask ProjectTask { get; set; }  // ← Navigation property
}

// Dangerous usage:
var entry = new TimeEntry
{
    ProjectCode = "PROJECT-A",          // ← FK says PROJECT-A
    Project = projectB,                 // ← Navigation says PROJECT-B
    // ... which wins?
};
```

**What happens:**
- Navigation property always wins during EF Core relationship fixup
- `ProjectCode` gets overwritten to `projectB.Code`
- Unexpected FK values saved to database
- FK constraint violations at save time
- Confusing runtime errors

## Decision

**Use EF Core shadow properties for all foreign keys instead of explicit FK properties in entity classes.**

## Rationale

### Shadow Properties Approach

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    // ProjectCode removed - now a shadow property!
    // ProjectTaskId removed - now a shadow property!

    public Project Project { get; set; }      // ← Only way to set relationship
    public ProjectTask ProjectTask { get; set; }  // ← Only way to set relationship
}
```

**DbContext configuration:**
```csharp
entity.Property<string>("ProjectCode")  // ← Shadow property
    .HasColumnName("project_code")
    .HasMaxLength(10)
    .IsRequired();

entity.HasOne(e => e.Project)
    .WithMany(p => p.TimeEntries)
    .HasForeignKey("ProjectCode")  // ← Reference shadow property by name
    .IsRequired();
```

**Why this solves the problem:**
1. **Only one way to set relationships** - via navigation properties
2. **Impossible to create conflicting state** - no FK property to conflict with
3. **Forces proper validation** - must load parent entity first
4. **Cleaner entity models** - no FK property clutter

## Consequences

### Benefits

✅ **Eliminates foot-gun entirely** - Can't set conflicting FK and navigation values
✅ **Cleaner entity models** - No FK property clutter
✅ **Forces proper validation patterns** - Must load parent entities first
✅ **Better encapsulation** - Relationships managed only through navigation properties

### Costs

⚠️ **Filtering requires `EF.Property<T>`** - Can't directly query FK values
⚠️ **Slightly more verbose queries** - ~5-10% more code for shadow property access
⚠️ **GraphQL schema impact** - No direct FK fields, requires navigation properties
⚠️ **Learning curve** - Team must understand shadow property pattern

### Trade-off Assessment

**Decision: Safety first.** The foot-gun elimination is worth the slight increase in query verbosity.

## Implementation

### Creating Entities: Load Parents First

```csharp
// ✅ SAFE: Load parent entity first, then set navigation property
var project = await context.Projects.FindAsync("INTERNAL");
if (project == null)
    throw new ValidationException($"Project 'INTERNAL' not found");

var task = await context.ProjectTasks
    .FirstOrDefaultAsync(t => EF.Property<string>(t, "ProjectCode") == "INTERNAL"
        && t.TaskName == "Development");
if (task == null)
    throw new ValidationException($"Task 'Development' not found");

var entry = new TimeEntry
{
    Project = project,      // ← Set navigation property
    ProjectTask = task,     // ← Set navigation property
    StandardHours = 8.0m,
    // ...
};

await context.TimeEntries.AddAsync(entry);
await context.SaveChangesAsync();  // ✅ EF Core fills shadow FK properties automatically
```

### Querying with Shadow Properties

#### Option 1: Filter by Shadow FK (verbose)

```csharp
// Use EF.Property<T> to query shadow properties
var entries = await context.TimeEntries
    .Where(e => EF.Property<string>(e, "ProjectCode") == "INTERNAL")
    .ToListAsync();
```

#### Option 2: Join via Navigation Properties (recommended)

```csharp
// Better: Use navigation property for filtering
var entries = await context.TimeEntries
    .Include(e => e.Project)
    .Where(e => e.Project.Code == "INTERNAL")
    .ToListAsync();
```

### HotChocolate GraphQL Integration

#### Mutations: Validation Required

```csharp
[Mutation]
public async Task<TimeEntry> LogTime(
    string projectCode,
    string taskName,
    decimal hours,
    [Service] TimeReportingDbContext context)
{
    // ✅ Proper validation - load entities first
    var project = await context.Projects.FindAsync(projectCode);
    if (project == null || !project.IsActive)
        throw new GraphQLException($"Project '{projectCode}' not found or inactive");

    var task = await context.ProjectTasks
        .FirstOrDefaultAsync(t =>
            EF.Property<string>(t, "ProjectCode") == projectCode
            && t.TaskName == taskName
            && t.IsActive);
    if (task == null)
        throw new GraphQLException($"Task '{taskName}' not found for project '{projectCode}'");

    var entry = new TimeEntry
    {
        Project = project,     // ← Navigation properties only
        ProjectTask = task,
        StandardHours = hours,
        StartDate = DateOnly.FromDateTime(DateTime.Today),
        CompletionDate = DateOnly.FromDateTime(DateTime.Today),
        Status = TimeEntryStatus.NotReported
    };

    await context.TimeEntries.AddAsync(entry);
    await context.SaveChangesAsync();  // ✅ Safe - FKs filled automatically
    return entry;
}
```

#### Queries: Use Navigation Properties

```csharp
[Query]
[UseProjection]
[UseFiltering]
public IQueryable<TimeEntry> GetTimeEntries([Service] TimeReportingDbContext context)
    => context.TimeEntries;
```

**GraphQL query (filtering via navigation):**
```graphql
query {
  timeEntries(where: { project: { code: { eq: "INTERNAL" } } }) {
    id
    standardHours
    project {
      code
      name
    }
  }
}
```

## Alternatives Considered

### Alternative 1: Keep Explicit FK Properties with Validation

**Approach**: Keep both FK and navigation properties, add validation to check consistency.

**Why rejected:**
- Still possible to create inconsistent state in code
- Validation only catches errors at runtime, not compile time
- Doesn't prevent the foot-gun, just adds guards around it

### Alternative 2: Use Only Navigation Properties (No Shadow FKs)

**Approach**: Remove FK properties entirely, don't define shadow properties.

**Why rejected:**
- Database still needs FK columns
- EF Core would auto-generate shadow properties anyway
- Loses explicit control over FK column configuration (name, type, constraints)

### Alternative 3: Documentation Only

**Approach**: Keep current design, document "don't set both" in comments.

**Why rejected:**
- Relies on developers reading and following documentation
- No compile-time or runtime enforcement
- High risk of bugs slipping through code review

## References

- [EF Core Shadow Properties Documentation](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
- Git commit: `d7b4fa0` - "Eliminate FK/navigation foot-gun by using EF Core shadow properties"
- Original document: `docs/FK_PROPERTY_FOOTGUN.md`
