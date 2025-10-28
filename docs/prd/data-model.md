# Data Model Specification

**Version:** 1.0
**Last Updated:** 2025-10-24

---

## Overview

This document provides detailed specifications for all database entities, relationships, and constraints in the Time Reporting System.

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────┐
│                      TimeEntry                          │
├─────────────────────────────────────────────────────────┤
│ PK  Id                    UUID                          │
│ FK  ProjectCode           VARCHAR(10)   ────┐           │
│     Task                  VARCHAR(100)       │           │
│     IssueId               VARCHAR(30)        │           │
│     StandardHours         DECIMAL(10,2)      │           │
│     OvertimeHours         DECIMAL(10,2)      │           │
│     Description           TEXT               │           │
│     StartDate             DATE               │           │
│     CompletionDate        DATE               │           │
│     Status                ENUM               │           │
│     DeclineComment        TEXT               │           │
│     Tags                  JSONB              │           │
│     CreatedAt             TIMESTAMP          │           │
│     UpdatedAt             TIMESTAMP          │           │
│     UserId                VARCHAR(100)       │           │
└─────────────────────────────────────────────┼───────────┘
                                               │
                                               │ 1:N
                                               │
                                               ▼
                              ┌────────────────────────────────┐
                              │          Project               │
                              ├────────────────────────────────┤
                              │ PK  Code         VARCHAR(10)   │
                              │     Name         VARCHAR(200)  │
                              │     IsActive     BOOLEAN       │
                              │     CreatedAt    TIMESTAMP     │
                              │     UpdatedAt    TIMESTAMP     │
                              └────────┬───────────────────────┘
                                       │
                                       │ 1:N
                                       │
                                       ▼
                    ┌──────────────────────────────────────────┐
                    │        ProjectTask                       │
                    ├──────────────────────────────────────────┤
                    │ PK  Id             INT                   │
                    │ FK  ProjectCode    VARCHAR(10)           │
                    │     TaskName       VARCHAR(100)          │
                    │     IsActive       BOOLEAN               │
                    └──────────────────────────────────────────┘

                              ┌────────────────────────────────┐
                              │         ProjectTag             │
                              ├────────────────────────────────┤
                              │ PK  Id             INT         │
                              │ FK  ProjectCode    VARCHAR(10) │
                              │     TagName        VARCHAR(20) │
                              │     IsActive       BOOLEAN     │
                              └────────────────┬───────────────┘
                                               │
                                               │ 1:N
                                               │
                                               ▼
                              ┌────────────────────────────────┐
                              │          TagValue              │
                              ├────────────────────────────────┤
                              │ PK  Id             INT         │
                              │ FK  ProjectTagId   INT         │
                              │     Value          VARCHAR(100)│
                              └────────────────────────────────┘
```

---

## 1. TimeEntry

The core entity representing a single time log entry.

### 1.1 Schema

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| **Id** | UUID | NO | gen_random_uuid() | Primary key |
| **ProjectCode** | VARCHAR(10) | NO | - | Foreign key to Project.Code |
| **Task** | VARCHAR(100) | NO | - | Must exist in ProjectTask for this project |
| **IssueId** | VARCHAR(30) | YES | NULL | External issue ID (e.g., JIRA) |
| **StandardHours** | DECIMAL(10,2) | NO | 0.00 | Regular hours worked |
| **OvertimeHours** | DECIMAL(10,2) | NO | 0.00 | Overtime hours worked |
| **Description** | TEXT | YES | NULL | Free-text description of work |
| **StartDate** | DATE | NO | - | When work started |
| **CompletionDate** | DATE | NO | - | When work completed |
| **Status** | VARCHAR(20) | NO | 'NOT_REPORTED' | Workflow status (see enum below) |
| **DeclineComment** | TEXT | YES | NULL | Admin comment when declined |
| **Tags** | JSONB | YES | '[]' | Array of {name, value} objects |
| **CreatedAt** | TIMESTAMP | NO | CURRENT_TIMESTAMP | Record creation time |
| **UpdatedAt** | TIMESTAMP | NO | CURRENT_TIMESTAMP | Last update time |
| **UserId** | VARCHAR(100) | YES | NULL | User identifier (for future multi-user) |

### 1.2 Constraints

```sql
-- Primary Key
ALTER TABLE time_entries ADD CONSTRAINT pk_time_entries PRIMARY KEY (id);

-- Foreign Key
ALTER TABLE time_entries
  ADD CONSTRAINT fk_time_entries_project
  FOREIGN KEY (project_code) REFERENCES projects(code);

-- Check Constraints
ALTER TABLE time_entries
  ADD CONSTRAINT chk_standard_hours_positive
  CHECK (standard_hours >= 0);

ALTER TABLE time_entries
  ADD CONSTRAINT chk_overtime_hours_positive
  CHECK (overtime_hours >= 0);

ALTER TABLE time_entries
  ADD CONSTRAINT chk_date_range
  CHECK (start_date <= completion_date);

ALTER TABLE time_entries
  ADD CONSTRAINT chk_status_valid
  CHECK (status IN ('NOT_REPORTED', 'SUBMITTED', 'APPROVED', 'DECLINED'));

-- Index for common queries
CREATE INDEX idx_time_entries_project_date
  ON time_entries(project_code, start_date DESC);

CREATE INDEX idx_time_entries_status
  ON time_entries(status);

CREATE INDEX idx_time_entries_user
  ON time_entries(user_id, start_date DESC);
```

### 1.3 Status Enum

| Value | Description | Allowed Transitions |
|-------|-------------|---------------------|
| **NOT_REPORTED** | Initial state, editable | → SUBMITTED |
| **SUBMITTED** | Sent for approval, read-only | → APPROVED, DECLINED |
| **APPROVED** | Approved by manager, immutable | (terminal state) |
| **DECLINED** | Rejected with comment, editable | → SUBMITTED |

### 1.4 Tags Structure

Tags are stored as JSONB array:

```json
[
  {
    "name": "Environment",
    "value": "Production"
  },
  {
    "name": "Billable",
    "value": "Yes"
  }
]
```

**Validation:**
- Each tag's `name` must exist in ProjectTag for the project
- Each tag's `value` must be in TagValue.AllowedValues for that name

### 1.5 C# Entity Model

```csharp
public class TimeEntry
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string ProjectCode { get; set; }

    [Required]
    [MaxLength(100)]
    public string Task { get; set; }

    [MaxLength(30)]
    public string? IssueId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal StandardHours { get; set; }

    [Range(0, double.MaxValue)]
    public decimal OvertimeHours { get; set; }

    public string? Description { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly CompletionDate { get; set; }

    [Required]
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.NotReported;

    public string? DeclineComment { get; set; }

    public List<Tag> Tags { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? UserId { get; set; }

    // Navigation property
    public Project Project { get; set; }
}

public enum TimeEntryStatus
{
    NotReported,
    Submitted,
    Approved,
    Declined
}

public class Tag
{
    [Required]
    [MaxLength(20)]
    public string Name { get; set; }

    [Required]
    [MaxLength(100)]
    public string Value { get; set; }
}
```

---

## 2. Project

Defines available projects that users can log time against.

### 2.1 Schema

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| **Code** | VARCHAR(10) | NO | - | Primary key, unique project code |
| **Name** | VARCHAR(200) | NO | - | Display name of project |
| **IsActive** | BOOLEAN | NO | TRUE | Whether project accepts new entries |
| **CreatedAt** | TIMESTAMP | NO | CURRENT_TIMESTAMP | Record creation time |
| **UpdatedAt** | TIMESTAMP | NO | CURRENT_TIMESTAMP | Last update time |

### 2.2 Constraints

```sql
-- Primary Key
ALTER TABLE projects ADD CONSTRAINT pk_projects PRIMARY KEY (code);

-- Unique constraint on name
ALTER TABLE projects ADD CONSTRAINT uq_projects_name UNIQUE (name);

-- Index for active projects
CREATE INDEX idx_projects_active ON projects(is_active);
```

### 2.3 C# Entity Model

```csharp
public class Project
{
    [Key]
    [MaxLength(10)]
    public string Code { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public List<ProjectTask> AvailableTasks { get; set; } = new();
    public List<ProjectTag> Tags { get; set; } = new();
    public List<TimeEntry> TimeEntries { get; set; } = new();
}
```

---

## 3. ProjectTask

Available tasks within a project.

### 3.1 Schema

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| **Id** | INT | NO | SERIAL | Primary key |
| **ProjectCode** | VARCHAR(10) | NO | - | Foreign key to Project.Code |
| **TaskName** | VARCHAR(100) | NO | - | Task name |
| **IsActive** | BOOLEAN | NO | TRUE | Whether task is available |

### 3.2 Constraints

```sql
-- Primary Key
ALTER TABLE project_tasks ADD CONSTRAINT pk_project_tasks PRIMARY KEY (id);

-- Foreign Key
ALTER TABLE project_tasks
  ADD CONSTRAINT fk_project_tasks_project
  FOREIGN KEY (project_code) REFERENCES projects(code) ON DELETE CASCADE;

-- Unique constraint (one task name per project)
ALTER TABLE project_tasks
  ADD CONSTRAINT uq_project_tasks_project_task
  UNIQUE (project_code, task_name);

-- Index
CREATE INDEX idx_project_tasks_project ON project_tasks(project_code);
```

### 3.3 C# Entity Model

```csharp
public class ProjectTask
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string ProjectCode { get; set; }

    [Required]
    [MaxLength(100)]
    public string TaskName { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public Project Project { get; set; }
}
```

---

## 4. ProjectTag

Defines available metadata tags per project (renamed from TagConfiguration for naming consistency with ProjectTask).

### 4.1 Schema

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| **Id** | INT | NO | SERIAL | Primary key |
| **ProjectCode** | VARCHAR(10) | NO | - | Foreign key to Project.Code |
| **TagName** | VARCHAR(20) | NO | - | Tag name (e.g., "Environment") |
| **IsActive** | BOOLEAN | NO | TRUE | Whether tag is available |

### 4.2 Constraints

```sql
-- Primary Key
ALTER TABLE project_tags ADD CONSTRAINT pk_project_tags PRIMARY KEY (id);

-- Foreign Key
ALTER TABLE project_tags
  ADD CONSTRAINT fk_project_tags_project
  FOREIGN KEY (project_code) REFERENCES projects(code) ON DELETE CASCADE;

-- Unique constraint (one tag name per project)
ALTER TABLE project_tags
  ADD CONSTRAINT uq_project_tags_project_tag
  UNIQUE (project_code, tag_name);

-- Index
CREATE INDEX idx_project_tags_project ON project_tags(project_code);
```

### 4.3 C# Entity Model

```csharp
public class ProjectTag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string ProjectCode { get; set; }

    [Required]
    [MaxLength(20)]
    public string TagName { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Project Project { get; set; }
    public List<TagValue> AllowedValues { get; set; } = new();
}
```

---

## 5. TagValue

Represents allowed values for project tags (normalized from JSONB array).

### 5.1 Schema

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| **Id** | INT | NO | SERIAL | Primary key |
| **ProjectTagId** | INT | NO | - | Foreign key to ProjectTag.Id |
| **Value** | VARCHAR(100) | NO | - | Allowed value (e.g., "Production") |

### 5.2 Constraints

```sql
-- Primary Key
ALTER TABLE tag_values ADD CONSTRAINT pk_tag_values PRIMARY KEY (id);

-- Foreign Key
ALTER TABLE tag_values
  ADD CONSTRAINT fk_tag_values_project_tag
  FOREIGN KEY (project_tag_id) REFERENCES project_tags(id) ON DELETE CASCADE;

-- Index
CREATE INDEX idx_tag_values_project_tag ON tag_values(project_tag_id);
```

### 5.3 C# Entity Model

```csharp
public class TagValue
{
    public int Id { get; set; }

    [Required]
    public int ProjectTagId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Value { get; set; }

    // Navigation property
    public ProjectTag ProjectTag { get; set; }
}
```

---

## 6. Database Initialization

### 6.1 DDL Script

Complete DDL available in: `docs/tasks/phase-01-database/task-1.1-postgresql-schema.md`

### 6.2 Seed Data

Sample seed data available in: `docs/tasks/phase-01-database/task-1.2-seed-data.md`

Example projects:
- **INTERNAL** - Internal Development
  - Tasks: Architecture, Development, Code Review, Testing, Documentation
  - Tags: Environment (Production, Staging, Dev), Billable (Yes, No)

- **CLIENT-A** - Client A Project
  - Tasks: Feature Development, Bug Fixing, Maintenance, Support
  - Tags: Priority (High, Medium, Low), Sprint (Sprint-1, Sprint-2, ...)

---

## 7. Entity Framework Core Configuration

### 7.1 DbContext

```csharp
public class TimeReportingDbContext : DbContext
{
    public DbSet<TimeEntry> TimeEntries { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<ProjectTag> ProjectTags { get; set; }
    public DbSet<TagValue> TagValues { get; set; }
    public DbSet<TimeEntryTag> TimeEntryTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TimeEntry configuration
        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.ToTable("time_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Task).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IssueId).HasMaxLength(30);
            entity.Property(e => e.StandardHours).HasPrecision(10, 2);
            entity.Property(e => e.OvertimeHours).HasPrecision(10, 2);

            entity.Property(e => e.Tags)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<Tag>>(v, (JsonSerializerOptions)null));

            entity.HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(e => e.ProjectCode);

            entity.HasIndex(e => new { e.ProjectCode, e.StartDate });
            entity.HasIndex(e => e.Status);
        });

        // Project configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Code);

            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            entity.HasMany(e => e.AvailableTasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Tags)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectCode)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectTask configuration
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.ToTable("project_tasks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.TaskName).HasMaxLength(100).IsRequired();

            entity.HasIndex(e => new { e.ProjectCode, e.TaskName }).IsUnique();
        });

        // ProjectTag configuration
        modelBuilder.Entity<ProjectTag>(entity =>
        {
            entity.ToTable("project_tags");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectCode).HasMaxLength(10).IsRequired();
            entity.Property(e => e.TagName).HasMaxLength(20).IsRequired();

            entity.HasMany(e => e.AllowedValues)
                .WithOne(v => v.ProjectTag)
                .HasForeignKey(v => v.ProjectTagId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectCode, e.TagName }).IsUnique();
        });

        // TagValue configuration
        modelBuilder.Entity<TagValue>(entity =>
        {
            entity.ToTable("tag_values");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ProjectTagId).IsRequired();
            entity.Property(e => e.Value).HasMaxLength(100).IsRequired();

            entity.HasIndex(e => e.ProjectTagId);
        });

        // TimeEntryTag configuration
        modelBuilder.Entity<TimeEntryTag>(entity =>
        {
            entity.ToTable("time_entry_tags");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TimeEntryId).IsRequired();
            entity.Property(e => e.TagValueId).IsRequired();

            entity.HasOne(e => e.TagValue)
                .WithMany()
                .HasForeignKey(e => e.TagValueId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TimeEntryId, e.TagValueId }).IsUnique();
        });
    }
}
```

---

## 8. Migration Strategy

### 8.1 Initial Migration

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 8.2 Future Migrations

When schema changes:
1. Update entity models
2. Run `dotnet ef migrations add <MigrationName>`
3. Review generated migration code
4. Test migration on dev database
5. Apply to production

---

## 9. Data Validation Service

Beyond database constraints, the API implements business logic validation:

```csharp
public interface ITimeEntryValidator
{
    Task<ValidationResult> ValidateAsync(TimeEntry entry);
}

public class TimeEntryValidator : ITimeEntryValidator
{
    public async Task<ValidationResult> ValidateAsync(TimeEntry entry)
    {
        var errors = new List<string>();

        // Project exists and is active
        var project = await _db.Projects
            .Include(p => p.AvailableTasks)
            .Include(p => p.Tags)
                .ThenInclude(t => t.AllowedValues)
            .FirstOrDefaultAsync(p => p.Code == entry.ProjectCode);

        if (project == null || !project.IsActive)
            errors.Add($"Project '{entry.ProjectCode}' does not exist or is inactive");

        // Task is valid for project
        if (!project.AvailableTasks.Any(t => t.TaskName == entry.Task && t.IsActive))
            errors.Add($"Task '{entry.Task}' is not available for project '{entry.ProjectCode}'");

        // Tags are valid
        foreach (var tag in entry.Tags)
        {
            var tagConfig = project.Tags
                .FirstOrDefault(tc => tc.TagName == tag.Name && tc.IsActive);

            if (tagConfig == null)
                errors.Add($"Tag '{tag.Name}' is not configured for project '{entry.ProjectCode}'");
            else if (!tagConfig.AllowedValues.Any(v => v.Value == tag.Value))
                errors.Add($"Value '{tag.Value}' is not allowed for tag '{tag.Name}'");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
```

---

**Related Documents:**
- [API Specification](./api-specification.md) - GraphQL schema using these models
- [PRD Main](./README.md) - Product requirements overview
