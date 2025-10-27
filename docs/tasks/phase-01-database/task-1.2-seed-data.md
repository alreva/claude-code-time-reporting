# Task 1.2: Seed Data Script

**Phase:** 1 - Database & Infrastructure
**Estimated Time:** 1 hour
**Prerequisites:** Task 1.1 (PostgreSQL schema)
**Status:** Pending

---

## Objective

Create seed data scripts to populate the database with sample projects, tasks, and tag configurations for development and testing.

---

## Acceptance Criteria

- [ ] At least 2 sample projects are created (INTERNAL, CLIENT-A)
- [ ] Each project has 4-6 available tasks
- [ ] Each project has 2-3 tag configurations with allowed values
- [ ] Script is idempotent (can run multiple times)
- [ ] Sample time entries are created for testing queries

---

## Implementation

Create file: `db/schema/02-seed-data.sql`

```sql
-- Clean existing data (for idempotency)
TRUNCATE TABLE time_entries CASCADE;
TRUNCATE TABLE tag_configurations CASCADE;
TRUNCATE TABLE project_tasks CASCADE;
TRUNCATE TABLE projects CASCADE;

-- Insert Projects
INSERT INTO projects (code, name, is_active) VALUES
('INTERNAL', 'Internal Development', true),
('CLIENT-A', 'Client A Project', true),
('MAINT', 'Maintenance & Support', true);

-- Insert Tasks for INTERNAL
INSERT INTO project_tasks (project_code, task_name, is_active) VALUES
('INTERNAL', 'Architecture', true),
('INTERNAL', 'Development', true),
('INTERNAL', 'Code Review', true),
('INTERNAL', 'Testing', true),
('INTERNAL', 'Documentation', true),
('INTERNAL', 'DevOps', true);

-- Insert Tasks for CLIENT-A
INSERT INTO project_tasks (project_code, task_name, is_active) VALUES
('CLIENT-A', 'Feature Development', true),
('CLIENT-A', 'Bug Fixing', true),
('CLIENT-A', 'Maintenance', true),
('CLIENT-A', 'Support', true),
('CLIENT-A', 'Code Review', true);

-- Insert Tasks for MAINT
INSERT INTO project_tasks (project_code, task_name, is_active) VALUES
('MAINT', 'Bug Fixing', true),
('MAINT', 'Security Patches', true),
('MAINT', 'Performance Optimization', true),
('MAINT', 'Monitoring', true);

-- Insert Tag Configurations for INTERNAL
INSERT INTO tag_configurations (project_code, tag_name, allowed_values, is_active) VALUES
('INTERNAL', 'Environment', '["Production", "Staging", "Development"]', true),
('INTERNAL', 'Billable', '["Yes", "No"]', true),
('INTERNAL', 'Type', '["Feature", "Bug", "Refactor", "Docs"]', true);

-- Insert Tag Configurations for CLIENT-A
INSERT INTO tag_configurations (project_code, tag_name, allowed_values, is_active) VALUES
('CLIENT-A', 'Priority', '["High", "Medium", "Low"]', true),
('CLIENT-A', 'Sprint', '["Sprint-1", "Sprint-2", "Sprint-3", "Sprint-4"]', true),
('CLIENT-A', 'Billable', '["Yes", "No"]', true);

-- Insert Tag Configurations for MAINT
INSERT INTO tag_configurations (project_code, tag_name, allowed_values, is_active) VALUES
('MAINT', 'Severity', '["Critical", "High", "Medium", "Low"]', true),
('MAINT', 'Billable', '["Yes", "No"]', true);

-- Insert Sample Time Entries
INSERT INTO time_entries (project_code, task, issue_id, standard_hours, overtime_hours, description, start_date, completion_date, status, tags) VALUES
('INTERNAL', 'Development', 'DEV-123', 7.5, 0, 'Implemented user authentication module', '2025-10-21', '2025-10-21', 'APPROVED', '[{"name": "Environment", "value": "Production"}, {"name": "Billable", "value": "Yes"}]'),
('INTERNAL', 'Code Review', null, 2.0, 0, 'Reviewed PR #456', '2025-10-21', '2025-10-21', 'APPROVED', '[{"name": "Billable", "value": "No"}]'),
('CLIENT-A', 'Feature Development', 'JIRA-789', 6.0, 1.5, 'Built new dashboard component', '2025-10-22', '2025-10-22', 'SUBMITTED', '[{"name": "Priority", "value": "High"}, {"name": "Sprint", "value": "Sprint-2"}]'),
('CLIENT-A', 'Bug Fixing', 'JIRA-790', 4.0, 0, 'Fixed login redirect issue', '2025-10-23', '2025-10-23', 'NOT_REPORTED', '[{"name": "Priority", "value": "Medium"}]'),
('MAINT', 'Security Patches', 'SEC-101', 3.0, 0, 'Applied security updates to dependencies', '2025-10-23', '2025-10-23', 'APPROVED', '[{"name": "Severity", "value": "Critical"}, {"name": "Billable", "value": "Yes"}]');

-- Verify insertions
SELECT 'Projects:' as info, COUNT(*) as count FROM projects
UNION ALL
SELECT 'Project Tasks:', COUNT(*) FROM project_tasks
UNION ALL
SELECT 'Tag Configurations:', COUNT(*) FROM tag_configurations
UNION ALL
SELECT 'Time Entries:', COUNT(*) FROM time_entries;
```

---

## Testing

```bash
# Run seed script
psql -U postgres -d time_reporting -f db/schema/02-seed-data.sql

# Verify data
psql -U postgres -d time_reporting -c "SELECT * FROM projects;"
psql -U postgres -d time_reporting -c "SELECT project_code, task_name FROM project_tasks ORDER BY project_code, task_name;"
```

---

## Related Files

- **Seed Script:** `db/schema/02-seed-data.sql`
- **Schema:** `db/schema/01-create-tables.sql`

---

## Next Steps

- ✅ Proceed to **Task 1.3** - Docker Compose PostgreSQL setup
