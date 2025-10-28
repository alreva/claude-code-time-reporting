# Foreign Key Property Foot-Gun Warning ⚠️

## The Question

*"Is it possible to shoot myself in the foot? Say, I create a TimeEntry and my TimeEntry.Project is one project with project code 'INTERNAL' and I set the TimeEntry.ProjectCode to a value other than 'INTERNAL'?"*

## The Answer: YES! 🔫

You can create inconsistent state that causes runtime failures if you set **both** the FK property and the navigation property to **conflicting values**.

---

## The Foot-Gun Scenario

```csharp
// 🔫 DANGER: Setting BOTH with CONFLICTING values
var project = await context.Projects.FindAsync("INTERNAL");
var entry = new TimeEntry
{
    Project = project,          // ← Navigation says "INTERNAL"
    ProjectCode = "CLIENT-A",   // ← FK says "CLIENT-A" (CONFLICT!)
    // ... other fields
};
await context.TimeEntries.AddAsync(entry);
await context.SaveChangesAsync();  // 💥 BOOM! FK constraint violation
```

### What Happens?

EF Core's relationship fixup will try to reconcile the conflict, but the **navigation property ALWAYS wins** during change tracking. This creates an inconsistent state where:

1. The navigation property points to "INTERNAL"
2. EF Core syncs the FK to match: `ProjectCode = "INTERNAL"`
3. But if related entities (like ProjectTask) expect "CLIENT-A", you get **FK constraint violations** at runtime

**Result:** `DbUpdateException` with cryptic foreign key constraint errors!

---

## Safe Patterns

### ✅ Pattern 1: Set FK Only (RECOMMENDED for GraphQL mutations)

```csharp
// SAFE: Just set the FK string
var entry = new TimeEntry
{
    ProjectCode = "INTERNAL",  // ← Just the FK
    // Project navigation left null
    ProjectTaskId = taskId,
    StandardHours = 8.0m,
    // ...
};
await context.TimeEntries.AddAsync(entry);
await context.SaveChangesAsync();  // ✅ Works perfectly!
```

**Use when:**
- Creating entities from GraphQL input (most common)
- You have the FK value as a string
- You don't need the navigation property loaded

### ✅ Pattern 2: Set Navigation Only

```csharp
// SAFE: Set navigation property, FK auto-filled
var project = await context.Projects.FindAsync("INTERNAL");
var entry = new TimeEntry
{
    // ProjectCode NOT set
    Project = project,  // ← EF Core fills ProjectCode = "INTERNAL"
    ProjectTaskId = taskId,
    StandardHours = 8.0m,
    // ...
};
await context.TimeEntries.AddAsync(entry);
await context.SaveChangesAsync();  // ✅ Works perfectly!
```

**Use when:**
- You already have the parent entity loaded
- Building object graphs in memory
- Setting up test data with related entities

---

## The Rule: Pick ONE, Not Both!

**✅ DO:**
- Set FK property XOR navigation property
- Trust EF Core's relationship fixup

**❌ DON'T:**
- Set both FK property AND navigation property
- Assume they stay in sync when you set conflicting values

---

## Why We Keep the Explicit FK Property

Even though this foot-gun exists, explicit FK properties are still recommended because:

1. **Direct filtering without joins**
   ```csharp
   // Efficient - no join required
   var tasks = await context.ProjectTasks
       .Where(t => t.ProjectCode == "INTERNAL")
       .ToListAsync();
   ```

2. **GraphQL schema benefits** - Direct field access
3. **Business key visibility** - "INTERNAL" is meaningful
4. **Standard EF Core practice** - Explicit FKs are the norm

---

## For This Project

**Our GraphQL mutations follow the safe pattern:**

```csharp
public async Task<TimeEntry> LogTime(LogTimeInput input)
{
    var entry = new TimeEntry
    {
        ProjectCode = input.ProjectCode,  // ← From input
        ProjectTaskId = input.TaskId,     // ← From input
        // No Project navigation property set
        StandardHours = input.Hours,
        // ...
    };

    await _context.TimeEntries.AddAsync(entry);
    await _context.SaveChangesAsync();  // ✅ Safe!
    return entry;
}
```

**Database FK constraints** enforce referential integrity - if `ProjectCode = "INVALID"` doesn't exist, EF Core will throw `DbUpdateException` on `SaveChangesAsync()`.

---

## Summary

**Yes, you can shoot yourself in the foot!** But only if you:
1. Set BOTH the FK property and navigation property
2. With CONFLICTING values

**The fix:** Pick one pattern and stick with it. For this project, we use Pattern 1 (FK only) in GraphQL mutations.
