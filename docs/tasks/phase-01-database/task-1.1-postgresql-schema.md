# Task 1.1: PostgreSQL Schema Setup

**Phase:** 1 - Database & Infrastructure
**Estimated Time:** 1-2 hours
**Prerequisites:** None
**Status:** Pending

---

## Objective

Create the PostgreSQL database schema including all tables, constraints, indexes, and relationships for the Time Reporting System.

---

## Acceptance Criteria

- [ ] Database `time_reporting` is created
- [ ] All four tables are created with correct column types
- [ ] Primary keys are defined on all tables
- [ ] Foreign key relationships are established
- [ ] Check constraints are in place for data integrity
- [ ] Indexes are created for common query patterns
- [ ] DDL script is idempotent (can run multiple times safely)

---

## Implementation Steps

### 1. Create DDL Script

Create file: `db/schema/01-create-tables.sql`

```sql
-- Database creation (if not exists)
CREATE DATABASE time_reporting;

-- Connect to database
\c time_reporting;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Table: projects
CREATE TABLE IF NOT EXISTS projects (
    code VARCHAR(10) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_projects_name UNIQUE (name)
);

-- Table: project_tasks
CREATE TABLE IF NOT EXISTS project_tasks (
    id SERIAL PRIMARY KEY,
    project_code VARCHAR(10) NOT NULL,
    task_name VARCHAR(100) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT fk_project_tasks_project
        FOREIGN KEY (project_code)
        REFERENCES projects(code)
        ON DELETE CASCADE,
    CONSTRAINT uq_project_tasks_project_task
        UNIQUE (project_code, task_name)
);

-- Table: tag_configurations
CREATE TABLE IF NOT EXISTS tag_configurations (
    id SERIAL PRIMARY KEY,
    project_code VARCHAR(10) NOT NULL,
    tag_name VARCHAR(20) NOT NULL,
    allowed_values JSONB NOT NULL DEFAULT '[]',
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT fk_tag_configurations_project
        FOREIGN KEY (project_code)
        REFERENCES projects(code)
        ON DELETE CASCADE,
    CONSTRAINT uq_tag_configurations_project_tag
        UNIQUE (project_code, tag_name)
);

-- Table: time_entries
CREATE TABLE IF NOT EXISTS time_entries (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    project_code VARCHAR(10) NOT NULL,
    task VARCHAR(100) NOT NULL,
    issue_id VARCHAR(30),
    standard_hours DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    overtime_hours DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    description TEXT,
    start_date DATE NOT NULL,
    completion_date DATE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'NOT_REPORTED',
    decline_comment TEXT,
    tags JSONB DEFAULT '[]',
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    user_id VARCHAR(100),
    CONSTRAINT fk_time_entries_project
        FOREIGN KEY (project_code)
        REFERENCES projects(code),
    CONSTRAINT chk_standard_hours_positive
        CHECK (standard_hours >= 0),
    CONSTRAINT chk_overtime_hours_positive
        CHECK (overtime_hours >= 0),
    CONSTRAINT chk_date_range
        CHECK (start_date <= completion_date),
    CONSTRAINT chk_status_valid
        CHECK (status IN ('NOT_REPORTED', 'SUBMITTED', 'APPROVED', 'DECLINED'))
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_time_entries_project_date
    ON time_entries(project_code, start_date DESC);

CREATE INDEX IF NOT EXISTS idx_time_entries_status
    ON time_entries(status);

CREATE INDEX IF NOT EXISTS idx_time_entries_user
    ON time_entries(user_id, start_date DESC);

CREATE INDEX IF NOT EXISTS idx_projects_active
    ON projects(is_active);

CREATE INDEX IF NOT EXISTS idx_project_tasks_project
    ON project_tasks(project_code);

CREATE INDEX IF NOT EXISTS idx_tag_configurations_project
    ON tag_configurations(project_code);

-- Trigger for updated_at
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_projects_updated_at
    BEFORE UPDATE ON projects
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_time_entries_updated_at
    BEFORE UPDATE ON time_entries
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Comments for documentation
COMMENT ON TABLE projects IS 'Available projects for time tracking';
COMMENT ON TABLE project_tasks IS 'Allowed tasks per project';
COMMENT ON TABLE tag_configurations IS 'Metadata tag configurations per project';
COMMENT ON TABLE time_entries IS 'Individual time log entries';

COMMENT ON COLUMN time_entries.status IS 'Workflow status: NOT_REPORTED, SUBMITTED, APPROVED, DECLINED';
COMMENT ON COLUMN time_entries.tags IS 'JSONB array of {name, value} objects';
COMMENT ON COLUMN tag_configurations.allowed_values IS 'JSONB array of allowed string values';
```

### 2. Test the Schema

```bash
# Connect to PostgreSQL
psql -U postgres -h localhost

# Run the schema script
\i db/schema/01-create-tables.sql

# Verify tables
\dt

# Verify constraints
\d time_entries
\d projects
\d project_tasks
\d tag_configurations

# Test insertions work
INSERT INTO projects (code, name) VALUES ('TEST', 'Test Project');
SELECT * FROM projects WHERE code = 'TEST';

# Clean up test data
DELETE FROM projects WHERE code = 'TEST';
```

---

## Testing Requirements

### Test Cases

1. **Schema Creation**
   - Run script on fresh database → all tables created
   - Run script again → no errors (idempotent)

2. **Constraints**
   - Insert time entry with negative hours → rejected
   - Insert time entry with start_date > completion_date → rejected
   - Insert time entry with invalid status → rejected
   - Insert time entry with invalid project_code → rejected (FK violation)

3. **Indexes**
   - Query `\di` → all indexes present
   - EXPLAIN ANALYZE on common queries → indexes used

4. **Triggers**
   - Update project → `updated_at` changes
   - Update time entry → `updated_at` changes

### SQL Test Script

```sql
-- Test 1: Foreign key constraint
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date)
VALUES ('NONEXISTENT', 'Task', 8.0, '2025-10-24', '2025-10-24');
-- Expected: ERROR - violates foreign key constraint

-- Test 2: Check constraint (negative hours)
INSERT INTO projects (code, name) VALUES ('TEST', 'Test Project');
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date)
VALUES ('TEST', 'Task', -5.0, '2025-10-24', '2025-10-24');
-- Expected: ERROR - violates check constraint "chk_standard_hours_positive"

-- Test 3: Check constraint (date range)
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date)
VALUES ('TEST', 'Task', 8.0, '2025-10-25', '2025-10-24');
-- Expected: ERROR - violates check constraint "chk_date_range"

-- Test 4: Unique constraint
INSERT INTO projects (code, name) VALUES ('TEST2', 'Test Project');
-- Expected: ERROR - duplicate key value violates unique constraint "uq_projects_name"

-- Clean up
DELETE FROM projects WHERE code LIKE 'TEST%';
```

---

## Related Files

- **Schema Script:** `db/schema/01-create-tables.sql`
- **Test Script:** `db/tests/test-schema.sql`
- **Documentation:** `docs/prd/data-model.md`

---

## Next Steps

After completing this task:
- ✅ Proceed to **Task 1.2** - Create seed data script
- Use this schema for **Phase 2** - GraphQL API entity models

---

## Notes

- PostgreSQL 16 is required for full JSONB support
- UUID extension must be enabled for `uuid_generate_v4()`
- Indexes improve query performance but use disk space - monitor in production
- `updated_at` triggers ensure automatic timestamp updates
