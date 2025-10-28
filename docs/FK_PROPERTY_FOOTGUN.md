# Shadow Foreign Keys - Eliminating the Foot-Gun ✅

## The Original Question

*"Is it possible to shoot myself in the foot? Say, I create a TimeEntry and my TimeEntry.Project is one project with project code 'INTERNAL' and I set the TimeEntry.ProjectCode to a value other than 'INTERNAL'?"*

## The Answer: NOT ANYMORE! 🛡️

**This foot-gun has been eliminated by using EF Core shadow properties instead of explicit FK properties.**

---

## What Changed?

### Before: Explicit FK Properties (DANGEROUS)

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    public string ProjectCode { get; set; }  // ← Explicit FK property
    public int ProjectTaskId { get; set; }    // ← Explicit FK property

    public Project Project { get; set; }      // ← Navigation property
    public ProjectTask ProjectTask { get; set; }  // ← Navigation property
}
```

**Problem:** You could set both `ProjectCode` and `Project` to **conflicting values**, causing:
- Navigation property always wins during relationship fixup
- Unexpected FK values in database
- FK constraint violations
- Confusing runtime errors

### After: Shadow Properties (SAFE)

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

---

## Current Pattern: Set Navigation Property Only

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

**Key points:**
1. Load parent entities first (Project, ProjectTask)
2. Validate they exist before creating child entity
3. Set navigation properties only
4. EF Core handles FK values automatically via relationship fixup
5. **Impossible to create conflicting FK/navigation state!**

---

## Querying with Shadow Properties

### Filtering by Shadow FK

```csharp
// Use EF.Property<T> to query shadow properties
var entries = await context.TimeEntries
    .Where(e => EF.Property<string>(e, "ProjectCode") == "INTERNAL")
    .ToListAsync();
```

### Joining via Navigation Properties (Recommended)

```csharp
// Better: Use navigation property for filtering (HotChocolate will handle this)
var entries = await context.TimeEntries
    .Include(e => e.Project)
    .Where(e => e.Project.Code == "INTERNAL")
    .ToListAsync();
```

---

## Benefits of Shadow Properties

### ✅ Safety First
- **Eliminates foot-gun entirely** - Can't set conflicting FK and navigation values
- Only one way to set relationships (via navigation properties)
- Cleaner entity models without FK clutter

### ⚠️ Trade-offs Accepted
- Filtering requires `EF.Property<T>` or navigation property joins
- No direct `projectCode` field exposed in GraphQL schema (requires navigation)
- Slightly more verbose queries for shadow property access

---

## For HotChocolate GraphQL

### Mutations: Validation Required

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

### Queries: Use Navigation Properties

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

---

## Summary

| Aspect | Explicit FK | Shadow FK (Current) |
|--------|-------------|---------------------|
| **Foot-gun risk** | ⚠️ High | ✅ Eliminated |
| **Entity model** | Cluttered with FKs | Clean, navigation only |
| **Mutation code** | Could skip validation | Forces proper validation |
| **Query filtering** | Direct FK access | `EF.Property<T>` or navigation |
| **GraphQL schema** | Direct FK field | Navigation required |

**Decision:** **Safety first.** Shadow properties eliminate the foot-gun at the cost of slightly more verbose queries.
