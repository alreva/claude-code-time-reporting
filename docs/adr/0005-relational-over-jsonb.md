# ADR 0005: Relational Schema Over JSONB

## Status

**Accepted** (Implemented in commit `c712f15`)

## Context

The initial database schema used PostgreSQL-specific JSONB columns to store complex data structures:

### Problem 1: TimeEntry.Tags as JSONB

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }
    public List<Tag> Tags { get; set; }  // ❌ Stored as JSONB in PostgreSQL
    // ...
}

// Tag value object
public class Tag
{
    public string Name { get; set; }
    public string Value { get; set; }
}
```

**Database column:**
```sql
CREATE TABLE time_entries (
    id UUID PRIMARY KEY,
    tags JSONB,  -- ❌ PostgreSQL-specific type
    -- Example value: [{"Name": "Priority", "Value": "High"}, {"Name": "Sprint", "Value": "23"}]
    ...
);
```

### Problem 2: TagConfiguration.AllowedValues as JSONB Array

```csharp
public class TagConfiguration
{
    public int TagConfigurationId { get; set; }
    public string Name { get; set; }
    public List<string> AllowedValues { get; set; }  // ❌ Stored as JSONB array
}
```

**Database column:**
```sql
CREATE TABLE tag_configurations (
    tag_configuration_id SERIAL PRIMARY KEY,
    name VARCHAR(50),
    allowed_values JSONB,  -- ❌ ["High", "Medium", "Low"]
    ...
);
```

### Issues with JSONB Approach

1. **Database-Specific**: JSONB is PostgreSQL-only
   - Can't switch to SQLite for development/testing
   - Can't migrate to SQL Server if requirements change
   - Locks project to PostgreSQL ecosystem

2. **No Referential Integrity**: JSONB contents aren't validated by database
   - Can insert `{"Name": "InvalidTag", "Value": "BadValue"}` without validation
   - No FK constraints to enforce tag configuration rules
   - Application logic must validate everything

3. **No Cascade Deletes**: Can't use database cascade rules
   - Deleting a tag configuration doesn't clean up orphaned tags
   - Must implement cleanup logic in application code

4. **Abstraction Leakage**: PostgreSQL types leak into C# domain model
   - Entity Framework requires special value comparers for JSONB
   - ORM can't treat JSONB as first-class collections
   - Complex configuration required for change tracking

5. **Query Limitations**: JSONB queries are less intuitive
   - Can't use standard SQL JOIN operations
   - Must use PostgreSQL-specific JSONB operators (`@>`, `?`, etc.)
   - Harder to index and optimize

6. **Violates Normalization**: Storing structured data as JSON violates relational principles
   - Tags should be entities, not serialized objects
   - Allowed values should be in separate table with FK relationship

## Decision

**Replace PostgreSQL-specific JSONB columns with proper relational entities:**

1. `TimeEntry.Tags` (JSONB) → `TimeEntryTag` entity with `time_entry_tags` table
2. `TagConfiguration.AllowedValues` (JSONB array) → `TagAllowedValue` entity with `tag_allowed_values` table

## Rationale

### Relational Design Principles

Tags and allowed values are **entities, not value objects**:
- They have identity (can be referenced by FKs)
- They have lifecycle (can be created, updated, deleted)
- They have relationships (tags belong to entries, values belong to configurations)

### Benefits of Relational Approach

#### 1. Database-Agnostic Design

```csharp
// ✅ Works with ANY relational database
public class TimeEntryTag
{
    public Guid Id { get; set; }
    public Guid TimeEntryId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    // Standard SQL types, no JSONB!
}
```

**Supported databases:**
- PostgreSQL (production)
- SQLite (development, testing)
- SQL Server (if needed)
- MySQL/MariaDB (if needed)

#### 2. Referential Integrity

```sql
-- ✅ Foreign key constraint enforces validity
CREATE TABLE time_entry_tags (
    id UUID PRIMARY KEY,
    time_entry_id UUID NOT NULL,
    name VARCHAR(50) NOT NULL,
    value VARCHAR(100) NOT NULL,
    CONSTRAINT fk_time_entry_tags_time_entries
        FOREIGN KEY (time_entry_id)
        REFERENCES time_entries(id)
        ON DELETE CASCADE  -- ✅ Automatic cleanup!
);
```

#### 3. Cascade Delete Support

```csharp
// ✅ Deleting a time entry automatically deletes its tags
entity.HasOne(t => t.TimeEntry)
    .WithMany(e => e.TimeEntryTags)
    .HasForeignKey(t => t.TimeEntryId)
    .OnDelete(DeleteBehavior.Cascade);  // Database handles cleanup
```

#### 4. No Abstraction Leakage

```csharp
// ❌ Before: JSONB value comparers required
modelBuilder.Entity<TimeEntry>()
    .Property(e => e.Tags)
    .HasColumnType("jsonb")
    .HasConversion(
        v => JsonSerializer.Serialize(v, options),
        v => JsonSerializer.Deserialize<List<Tag>>(v, options))
    .Metadata.SetValueComparer(new JsonValueComparer<List<Tag>>());

// ✅ After: Standard EF Core configuration
modelBuilder.Entity<TimeEntryTag>()
    .HasKey(t => t.Id);  // Simple, standard configuration
```

#### 5. Standard SQL Queries

```sql
-- ❌ Before: PostgreSQL-specific JSONB operators
SELECT * FROM time_entries
WHERE tags @> '[{"Name": "Priority", "Value": "High"}]'::jsonb;

-- ✅ After: Standard SQL JOIN
SELECT te.* FROM time_entries te
    JOIN time_entry_tags tet ON te.id = tet.time_entry_id
    WHERE tet.name = 'Priority' AND tet.value = 'High';
```

#### 6. Better Indexing and Performance

```sql
-- ✅ Can create standard indexes on relational tables
CREATE INDEX idx_time_entry_tags_name_value
    ON time_entry_tags(name, value);

-- ✅ Can create FK indexes for joins
CREATE INDEX idx_time_entry_tags_time_entry_id
    ON time_entry_tags(time_entry_id);
```

## Consequences

### Benefits

✅ **Database-Agnostic** - Works with PostgreSQL, SQLite, SQL Server, MySQL
✅ **Referential Integrity** - FK constraints enforce validity
✅ **Cascade Deletes** - Database handles cleanup automatically
✅ **No Abstraction Leakage** - Pure C# entities, no database-specific types
✅ **Standard SQL** - No need for JSONB-specific operators
✅ **Better Performance** - Standard indexes, optimized joins
✅ **Proper Normalization** - Follows relational database principles

### Costs

⚠️ **More Tables** - 2 additional tables (`time_entry_tags`, `tag_allowed_values`)
⚠️ **JOIN Queries** - Must join to access tag data (vs. single JSONB column)
⚠️ **More Code** - Additional entity classes and DbContext configuration

### Trade-off Assessment

**Decision: Database portability and referential integrity are worth the additional tables.**

The benefits (database-agnostic design, FK constraints, cascade deletes) far outweigh the cost of additional tables. Modern databases handle joins efficiently, and Entity Framework makes the extra tables transparent to application code.

## Implementation

### TimeEntry Tags Refactoring

#### Before (JSONB)

```csharp
// Value object (not an entity)
public class Tag
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public class TimeEntry
{
    public Guid Id { get; set; }
    public List<Tag> Tags { get; set; }  // ❌ JSONB
}

// DbContext configuration
modelBuilder.Entity<TimeEntry>()
    .Property(e => e.Tags)
    .HasColumnType("jsonb")
    .HasConversion(/* complex serialization */);
```

**Database:**
```sql
CREATE TABLE time_entries (
    id UUID PRIMARY KEY,
    tags JSONB  -- ❌ [{"Name": "Priority", "Value": "High"}]
);
```

#### After (Relational)

```csharp
// Entity with identity and relationships
public class TimeEntryTag
{
    public Guid Id { get; set; }
    public Guid TimeEntryId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }

    public TimeEntry TimeEntry { get; set; }  // ✅ Navigation property
}

public class TimeEntry
{
    public Guid Id { get; set; }
    public ICollection<TimeEntryTag> TimeEntryTags { get; set; }  // ✅ Navigation collection
}

// DbContext configuration
modelBuilder.Entity<TimeEntryTag>()
    .HasKey(t => t.Id);

modelBuilder.Entity<TimeEntryTag>()
    .HasOne(t => t.TimeEntry)
    .WithMany(e => e.TimeEntryTags)
    .HasForeignKey(t => t.TimeEntryId)
    .OnDelete(DeleteBehavior.Cascade);  // ✅ Automatic cleanup
```

**Database:**
```sql
CREATE TABLE time_entry_tags (
    id UUID PRIMARY KEY,
    time_entry_id UUID NOT NULL,
    name VARCHAR(50) NOT NULL,
    value VARCHAR(100) NOT NULL,
    CONSTRAINT fk_time_entry_tags_time_entries
        FOREIGN KEY (time_entry_id) REFERENCES time_entries(id)
        ON DELETE CASCADE
);
```

### TagConfiguration Allowed Values Refactoring

#### Before (JSONB Array)

```csharp
public class TagConfiguration
{
    public int TagConfigurationId { get; set; }
    public string Name { get; set; }
    public List<string> AllowedValues { get; set; }  // ❌ JSONB array
}

// DbContext configuration
modelBuilder.Entity<TagConfiguration>()
    .Property(tc => tc.AllowedValues)
    .HasColumnType("jsonb");
```

**Database:**
```sql
CREATE TABLE tag_configurations (
    tag_configuration_id SERIAL PRIMARY KEY,
    name VARCHAR(50),
    allowed_values JSONB  -- ❌ ["High", "Medium", "Low"]
);
```

#### After (Relational)

```csharp
public class TagAllowedValue
{
    public int TagAllowedValueId { get; set; }
    public int TagConfigurationId { get; set; }
    public string Value { get; set; }

    public TagConfiguration TagConfiguration { get; set; }  // ✅ Navigation property
}

public class TagConfiguration
{
    public int TagConfigurationId { get; set; }
    public string Name { get; set; }
    public ICollection<TagAllowedValue> AllowedValues { get; set; }  // ✅ Navigation collection
}

// DbContext configuration
modelBuilder.Entity<TagAllowedValue>()
    .HasKey(tav => tav.TagAllowedValueId);

modelBuilder.Entity<TagAllowedValue>()
    .HasOne(tav => tav.TagConfiguration)
    .WithMany(tc => tc.AllowedValues)
    .HasForeignKey(tav => tav.TagConfigurationId)
    .OnDelete(DeleteBehavior.Cascade);  // ✅ Automatic cleanup
```

**Database:**
```sql
CREATE TABLE tag_allowed_values (
    tag_allowed_value_id SERIAL PRIMARY KEY,
    tag_configuration_id INT NOT NULL,
    value VARCHAR(100) NOT NULL,
    CONSTRAINT fk_tag_allowed_values_tag_configurations
        FOREIGN KEY (tag_configuration_id) REFERENCES tag_configurations(tag_configuration_id)
        ON DELETE CASCADE
);
```

### Query Examples

#### Before (JSONB Queries)

```csharp
// Query tags using PostgreSQL JSONB operators
var entries = await context.TimeEntries
    .FromSqlRaw(@"
        SELECT * FROM time_entries
        WHERE tags @> '[{""Name"": ""Priority"", ""Value"": ""High""}]'::jsonb
    ")
    .ToListAsync();
```

#### After (Standard SQL Joins)

```csharp
// Query tags using standard EF Core navigation properties
var entries = await context.TimeEntries
    .Include(e => e.TimeEntryTags)
    .Where(e => e.TimeEntryTags.Any(t => t.Name == "Priority" && t.Value == "High"))
    .ToListAsync();
```

## Alternatives Considered

### Alternative 1: Keep JSONB, Add Application Validation

**Approach**: Keep JSONB columns, implement validation logic in application code.

**Why rejected:**
- No database-level enforcement (bugs can bypass validation)
- Still locks to PostgreSQL
- Can't use cascade deletes
- Doesn't solve query and indexing limitations

### Alternative 2: Use EAV Pattern

**Approach**: Entity-Attribute-Value pattern with generic attribute storage.

**Why rejected:**
- EAV is an anti-pattern for well-structured data
- Tags have known structure (name/value pairs)
- Queries become complex and slow
- Loses type safety and validation

### Alternative 3: Use XML Instead of JSONB

**Approach**: Store tags as XML instead of JSONB (XML is more widely supported).

**Why rejected:**
- Still violates normalization principles
- XML parsing is slower than JSONB
- Doesn't solve referential integrity problem
- Less elegant than proper relational design

### Alternative 4: Hybrid Approach

**Approach**: Use relational tables for tag definitions, JSONB for tag values on entries.

**Why rejected:**
- Inconsistent design (some relational, some JSONB)
- Still has abstraction leakage issues
- Doesn't fully solve database portability
- Confusing for developers

## References

- Git commit: `c712f15` - "Refactor tags from JSONB to fully relational schema"
- Previous commit: `1001db6` - "Add value comparers for JSONB collection properties" (complexity that was removed)
- Related ADR: [0004 - Normalized Schema](0004-normalized-schema.md) (further normalization step)
- Database migration: `20251028003722_RelationalTagSchema.cs`
