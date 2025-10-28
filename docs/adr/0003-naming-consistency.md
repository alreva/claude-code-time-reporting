# ADR 0003: Consistent Entity Naming Pattern

## Status

**Accepted** (Implemented in commit `b35cf1a`)

## Context

The domain model had inconsistent naming for project-related entities:

**Before:**
- `Project` - Main entity
- `ProjectTask` - Related entity ✅ (consistent with Project prefix)
- `TagConfiguration` - Related entity ❌ (no Project prefix)
- `TagAllowedValue` - Related entity ❌ (verbose, no clear prefix)

This inconsistency made the domain model less intuitive:
- Hard to discover related entities (no common prefix pattern)
- Naming didn't reflect relationship structure
- `TagConfiguration` and `TagAllowedValue` were verbose and unclear

The entities all belong to a `Project` and follow a one-to-many hierarchy:
```
Project
  ├── ProjectTask (many tasks per project)
  └── TagConfiguration (many tag configs per project)
       └── TagAllowedValue (many allowed values per tag config)
```

## Decision

**Rename entities to follow a consistent naming pattern with the `Project` prefix:**
- `TagConfiguration` → `ProjectTag`
- `TagAllowedValue` → `TagValue`

## Rationale

### Parallel Naming Structure

All project-related entities now follow the same pattern:

```
Project
  ├── ProjectTask
  └── ProjectTag
       └── TagValue
```

**Benefits of this structure:**
1. **Intuitive discovery**: All project-related entities start with `Project`
2. **Shorter names**: `ProjectTag` vs `TagConfiguration`, `TagValue` vs `TagAllowedValue`
3. **Clear hierarchy**: Parent-child relationships are evident
4. **Consistent with existing pattern**: `ProjectTask` already used this approach

### Why "ProjectTag" vs "TagConfiguration"?

- "Configuration" implies settings/options, not a domain entity
- "Tag" is clearer - it's a tag that belongs to a project
- Parallel with `ProjectTask` (not `ProjectTaskConfiguration`)

### Why "TagValue" vs "TagAllowedValue"?

- Shorter and clearer
- "Allowed" is implied by the relationship (if it exists, it's allowed)
- Parallel with other value objects in the domain

## Consequences

### Benefits

✅ **Improved Discoverability**
- IntelliSense shows all project-related entities together
- New developers can quickly understand the domain model
- Grep/search for "Project*" finds all related entities

✅ **Shorter, Clearer Names**
- `ProjectTag` (10 chars) vs `TagConfiguration` (16 chars)
- `TagValue` (8 chars) vs `TagAllowedValue` (16 chars)
- Less typing, easier to read

✅ **Consistent Domain Language**
- All project-related entities follow same pattern
- Parallel structure with `ProjectTask`
- Reflects actual domain relationships

✅ **Better Code Organization**
- Entities naturally group together in file explorers
- Navigation properties are more intuitive
- Test files follow same naming pattern

### Costs

⚠️ **Migration Required**
- Database tables renamed: `tag_configurations` → `project_tags`, `tag_allowed_values` → `tag_values`
- Column names updated: `tag_configuration_id` → `project_tag_id`, `tag_allowed_value_id` → `tag_value_id`
- All foreign keys and indexes updated
- All test files updated (51 tests)

⚠️ **Potential Confusion for External References**
- Any external documentation referring to old names needs updating
- API clients may need to update field names (if schema exposed these directly)

### Impact Assessment

**Migration cost**: ~1-2 hours (completed)
**Long-term benefit**: Improved maintainability and clarity for entire project lifetime

**Decision: Worth the migration cost for long-term clarity.**

## Implementation

### Before

```csharp
public class Project
{
    public string Code { get; set; }
    public string Name { get; set; }

    public ICollection<ProjectTask> ProjectTasks { get; set; }      // ✅ Consistent
    public ICollection<TagConfiguration> TagConfigurations { get; set; }  // ❌ Inconsistent
}

public class TagConfiguration
{
    public int TagConfigurationId { get; set; }
    public string ProjectCode { get; set; }
    public string Name { get; set; }

    public Project Project { get; set; }
    public ICollection<TagAllowedValue> AllowedValues { get; set; }
}

public class TagAllowedValue
{
    public int TagAllowedValueId { get; set; }
    public int TagConfigurationId { get; set; }
    public string Value { get; set; }

    public TagConfiguration TagConfiguration { get; set; }
}
```

### After

```csharp
public class Project
{
    public string Code { get; set; }
    public string Name { get; set; }

    public ICollection<ProjectTask> ProjectTasks { get; set; }  // ✅ Consistent
    public ICollection<ProjectTag> ProjectTags { get; set; }    // ✅ Consistent
}

public class ProjectTag
{
    public int ProjectTagId { get; set; }
    public string ProjectCode { get; set; }
    public string Name { get; set; }

    public Project Project { get; set; }
    public ICollection<TagValue> TagValues { get; set; }
}

public class TagValue
{
    public int TagValueId { get; set; }
    public int ProjectTagId { get; set; }
    public string Value { get; set; }

    public ProjectTag ProjectTag { get; set; }
}
```

### Database Changes

```sql
-- Tables renamed
ALTER TABLE tag_configurations RENAME TO project_tags;
ALTER TABLE tag_allowed_values RENAME TO tag_values;

-- Columns renamed
ALTER TABLE project_tags
    RENAME COLUMN tag_configuration_id TO project_tag_id;

ALTER TABLE tag_values
    RENAME COLUMN tag_allowed_value_id TO tag_value_id;
ALTER TABLE tag_values
    RENAME COLUMN tag_configuration_id TO project_tag_id;

ALTER TABLE time_entry_tags
    RENAME COLUMN tag_allowed_value_id TO tag_value_id;

-- Foreign keys and indexes updated accordingly
```

### DbContext Configuration

```csharp
// Before
public DbSet<TagConfiguration> TagConfigurations { get; set; }
public DbSet<TagAllowedValue> TagAllowedValues { get; set; }

// After
public DbSet<ProjectTag> ProjectTags { get; set; }
public DbSet<TagValue> TagValues { get; set; }
```

### Navigation Properties

```csharp
// TimeEntryTag navigation
// Before: public TagAllowedValue TagAllowedValue { get; set; }
// After:  public TagValue TagValue { get; set; }

// ProjectTag navigation
// Before: public ICollection<TagAllowedValue> AllowedValues { get; set; }
// After:  public ICollection<TagValue> TagValues { get; set; }
```

## Alternatives Considered

### Alternative 1: Keep Original Names

**Approach**: Leave `TagConfiguration` and `TagAllowedValue` as-is.

**Why rejected:**
- Inconsistent with `ProjectTask` naming
- Harder to discover related entities
- Verbose names (`TagAllowedValue` has 16 characters)
- Doesn't reflect domain structure clearly

### Alternative 2: Prefix Everything with "Time"

**Approach**: Use `TimeProject`, `TimeProjectTask`, `TimeProjectTag`, etc.

**Why rejected:**
- Overly verbose (adds 5 characters to every entity name)
- "Time" is implied by the project context
- Makes code harder to read: `timeProject.TimeProjectTasks.FirstOrDefault(t => t.TimeProjectTaskName == "Dev")`

### Alternative 3: Remove All Prefixes

**Approach**: Use `Project`, `Task`, `Tag`, `Value`.

**Why rejected:**
- Too generic - `Task` and `Tag` conflict with common C# types/keywords
- Loses important context (what kind of task? what kind of tag?)
- Doesn't reflect relationship structure

### Alternative 4: Use Namespaces Instead

**Approach**: Use `Entities.Project.Task`, `Entities.Project.Tag`, etc.

**Why rejected:**
- C# doesn't support nested type declarations in this way
- Namespace structure doesn't eliminate need for class names
- Still need to disambiguate in using statements
- Doesn't solve the inconsistency problem

## References

- Git commit: `b35cf1a` - "Fix naming inconsistency: TagConfiguration → ProjectTag, TagAllowedValue → TagValue"
- Related ADR: [0004 - Normalized Schema](0004-normalized-schema.md) (introduced these entities)
- Migration: `20251028010336_NormalizedSchema.cs`
