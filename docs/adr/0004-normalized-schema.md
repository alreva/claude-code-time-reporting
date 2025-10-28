# ADR 0004: Fully Normalized Schema

## Status

**Accepted** (Implemented in commit `f702ab7`)

## Context

The initial schema design had string duplication in two critical areas:

### Problem 1: TimeEntry.Task as String Field

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    public string Task { get; set; }  // ❌ String "Development" repeated 1000x
    // ...
}
```

**Issues:**
- Task name "Development" duplicated across thousands of time entries
- No referential integrity (can insert invalid task names)
- Storage waste (string vs integer FK)
- Can't ensure task belongs to the project
- Updates require string matching instead of FK joins

### Problem 2: TimeEntryTag with Name and Value Strings

```csharp
public class TimeEntryTag
{
    public Guid TimeEntryId { get; set; }
    public string Name { get; set; }   // ❌ "Priority" duplicated
    public string Value { get; set; }  // ❌ "High" duplicated
    // ...
}
```

**Issues:**
- Tag names and values duplicated across all time entry tags
- No validation that tag name/value combinations are valid
- Can't enforce "Priority" tag only has allowed values ("High", "Medium", "Low")
- Inconsistent spelling (e.g., "High" vs "high" vs "HIGH")

### The Denormalization Cost

With 10,000 time entries:
- **Task names**: ~10,000 duplicated strings (avg 12 bytes each = 120 KB)
- **Tag data**: ~20,000 tag assignments × 20 bytes each = 400 KB
- **Total waste**: ~520 KB of duplicated string data

More importantly: **Zero database-level validation** - garbage data can be inserted.

## Decision

**Replace string duplication with proper foreign key relationships:**

1. `TimeEntry.Task` (string) → `TimeEntry.ProjectTaskId` (FK to ProjectTask)
2. `TimeEntryTag.Name, Value` (strings) → `TimeEntryTag.TagValueId` (FK to TagValue)

## Rationale

### Database Normalization Principles

Following Third Normal Form (3NF):
- **1NF**: Atomic values (already satisfied)
- **2NF**: No partial dependencies (already satisfied)
- **3NF**: No transitive dependencies → **Task name depends on ProjectTask, not TimeEntry**

Task name and tag name/value are **not attributes of TimeEntry/TimeEntryTag** - they're attributes of the referenced entities (ProjectTask, TagValue).

### Benefits of Normalized Design

#### 1. Zero String Duplication

```csharp
// Before: 10,000 entries × "Development" string = lots of duplication
// After: 10,000 entries × 4-byte integer ID = minimal storage
```

#### 2. Database-Level Validation

```sql
-- ✅ Foreign key constraint prevents invalid data
ALTER TABLE time_entries
    ADD CONSTRAINT fk_time_entries_project_tasks
    FOREIGN KEY (project_task_id) REFERENCES project_tasks(id);

-- ❌ Can no longer insert:
INSERT INTO time_entries (task) VALUES ('InvalidTask');  -- No FK enforcement

-- ✅ Must insert valid FK:
INSERT INTO time_entries (project_task_id) VALUES (5);  -- FK enforced
```

#### 3. Storage Efficiency

| Field | Before | After | Savings per Row |
|-------|--------|-------|-----------------|
| Task | ~12 bytes (string) | 4 bytes (int) | 8 bytes |
| Tag Name | ~10 bytes | - (in FK target) | 10 bytes |
| Tag Value | ~10 bytes | - (in FK target) | 10 bytes |
| **Total per entry** | ~32 bytes | ~4 bytes | **28 bytes (87% reduction)** |

**10,000 entries**: ~320 KB → ~40 KB = **280 KB saved**

#### 4. Query Performance

```sql
-- Before: String matching (slow, no index optimization)
SELECT * FROM time_entries WHERE task = 'Development';

-- After: Integer FK join (fast, indexed)
SELECT * FROM time_entries
    JOIN project_tasks ON time_entries.project_task_id = project_tasks.id
    WHERE project_tasks.name = 'Development';
```

#### 5. Data Consistency

```csharp
// Before: Renaming a task requires updating ALL time entries
UPDATE time_entries SET task = 'Software Development' WHERE task = 'Development';
// (10,000 row updates)

// After: Renaming a task updates ONE row
UPDATE project_tasks SET name = 'Software Development' WHERE id = 5;
// (1 row update, reflects everywhere)
```

## Consequences

### Benefits

✅ **Zero String Duplication** - Store IDs instead of repeating strings
✅ **Database-Level Validation** - Invalid tasks/tags cannot be inserted
✅ **Storage Efficiency** - 87% reduction in per-entry storage
✅ **Query Performance** - Integer joins faster than string matching
✅ **Data Consistency** - Change task/tag name once, reflects everywhere
✅ **Referential Integrity** - Cascade rules prevent orphaned data

### Costs

⚠️ **Join Complexity** - Queries require joins to get task/tag names
⚠️ **Migration Effort** - Existing string data must be migrated to FKs
⚠️ **GraphQL Schema** - Must expose navigation properties instead of direct strings

### Trade-off Assessment

**Decision: Normalization is worth the join complexity.**

The benefits (validation, consistency, performance, storage) far outweigh the cost of writing JOIN queries. Modern ORMs (Entity Framework) make joins trivial.

## Implementation

### TimeEntry Changes

#### Before

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    public string Task { get; set; }  // ❌ String duplication
    // ...
}
```

#### After

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    // Task string removed - now a FK relationship via shadow property
    public ProjectTask ProjectTask { get; set; }  // ✅ Navigation property
    // ...
}
```

**DbContext configuration:**
```csharp
entity.Property<int>("ProjectTaskId")  // Shadow FK
    .HasColumnName("project_task_id")
    .IsRequired();

entity.HasOne(e => e.ProjectTask)
    .WithMany(pt => pt.TimeEntries)
    .HasForeignKey("ProjectTaskId")
    .OnDelete(DeleteBehavior.Restrict);  // Prevent accidental deletion
```

### TimeEntryTag Changes

#### Before

```csharp
public class TimeEntryTag
{
    public Guid TimeEntryId { get; set; }
    public string Name { get; set; }   // ❌ String duplication
    public string Value { get; set; }  // ❌ String duplication
    // ...
}
```

#### After

```csharp
public class TimeEntryTag
{
    public Guid TimeEntryId { get; set; }
    // Name and Value removed - now a FK relationship via shadow property
    public TagValue TagValue { get; set; }  // ✅ Navigation property
    // ...
}
```

**DbContext configuration:**
```csharp
entity.Property<int>("TagValueId")  // Shadow FK
    .HasColumnName("tag_value_id")
    .IsRequired();

entity.HasOne(e => e.TagValue)
    .WithMany(tv => tv.TimeEntryTags)
    .HasForeignKey("TagValueId")
    .OnDelete(DeleteBehavior.Restrict);

// Unique constraint: one tag value per time entry
entity.HasIndex("TimeEntryId", "TagValueId")
    .IsUnique();
```

### Database Schema

```sql
-- TimeEntry table
CREATE TABLE time_entries (
    id UUID PRIMARY KEY,
    project_task_id INT NOT NULL,  -- ✅ FK instead of string
    -- ...
    CONSTRAINT fk_time_entries_project_tasks
        FOREIGN KEY (project_task_id)
        REFERENCES project_tasks(id)
        ON DELETE RESTRICT
);

-- TimeEntryTag table
CREATE TABLE time_entry_tags (
    time_entry_id UUID NOT NULL,
    tag_value_id INT NOT NULL,  -- ✅ FK instead of name/value strings
    PRIMARY KEY (time_entry_id, tag_value_id),
    CONSTRAINT fk_time_entry_tags_tag_values
        FOREIGN KEY (tag_value_id)
        REFERENCES tag_values(id)
        ON DELETE RESTRICT,
    CONSTRAINT uq_time_entry_tag_value
        UNIQUE (time_entry_id, tag_value_id)
);
```

### Query Examples

#### Before (String Matching)

```csharp
// Query by task name (string matching)
var entries = await context.TimeEntries
    .Where(e => e.Task == "Development")
    .ToListAsync();

// Query by tag (string matching on both name and value)
var entries = await context.TimeEntries
    .Where(e => e.Tags.Any(t => t.Name == "Priority" && t.Value == "High"))
    .ToListAsync();
```

#### After (FK Joins)

```csharp
// Query by task name (integer join, then string match)
var entries = await context.TimeEntries
    .Include(e => e.ProjectTask)
    .Where(e => e.ProjectTask.Name == "Development")
    .ToListAsync();

// Query by tag (integer join via TagValue)
var entries = await context.TimeEntries
    .Include(e => e.TimeEntryTags)
        .ThenInclude(t => t.TagValue)
            .ThenInclude(tv => tv.ProjectTag)
    .Where(e => e.TimeEntryTags.Any(t =>
        t.TagValue.ProjectTag.Name == "Priority" &&
        t.TagValue.Value == "High"))
    .ToListAsync();
```

**Note**: Queries are more verbose but database does the optimization (indexed joins).

## Alternatives Considered

### Alternative 1: Keep String Fields with Validation

**Approach**: Keep `Task` as string, add application-level validation to check against ProjectTask table.

**Why rejected:**
- No database-level enforcement (garbage data can slip through)
- Application bugs can bypass validation
- Still wastes storage with string duplication
- Can't enforce consistency across all database access paths

### Alternative 2: Use Enum for Tasks

**Approach**: Replace `Task` string with C# enum.

**Why rejected:**
- Enums are static - can't add new tasks without code deployment
- Different projects have different tasks (can't use single enum)
- Doesn't solve the tag name/value problem
- Loses configurability (tasks should be data, not code)

### Alternative 3: Partial Normalization

**Approach**: Normalize TimeEntry.Task but keep TimeEntryTag.Name/Value as strings.

**Why rejected:**
- Inconsistent design (some normalized, some not)
- Still has tag validation and duplication problems
- Confusing for developers (why normalize one but not the other?)

### Alternative 4: Denormalized with Triggers

**Approach**: Keep string fields, use database triggers to validate against reference tables.

**Why rejected:**
- Triggers add complexity and performance overhead
- Still wastes storage with duplication
- Trigger logic harder to test and maintain than FK constraints
- Doesn't provide query performance benefits of FKs

## References

- Git commit: `f702ab7` - "Eliminate redundancies with fully normalized schema"
- Related ADR: [0001 - Shadow Foreign Keys](0001-shadow-foreign-keys.md) (implementation pattern)
- Related ADR: [0005 - Relational Over JSONB](0005-relational-over-jsonb.md) (previous normalization step)
- Database migration: `20251028004502_NormalizedSchema.cs`
