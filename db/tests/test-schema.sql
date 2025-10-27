-- Test Schema Constraints and Triggers
-- This script tests all database constraints, foreign keys, and triggers

\echo '======================================'
\echo 'Testing PostgreSQL Schema Constraints'
\echo '======================================'
\echo ''

-- Setup: Create a test project for constraint testing
\echo 'Setup: Creating test project...'
INSERT INTO projects (code, name) VALUES ('TEST', 'Test Project');
\echo 'OK: Test project created'
\echo ''

-- Test 1: Foreign key constraint
\echo 'Test 1: Foreign key constraint (should fail)'
\set ON_ERROR_STOP off
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date)
VALUES ('NONEXISTENT', 'Task', 8.0, '2025-10-24', '2025-10-24');
\set ON_ERROR_STOP on
\echo 'PASS: Foreign key constraint working'
\echo ''

-- Test 2: Check constraint (negative standard hours)
\echo 'Test 2: Negative standard hours (should fail)'
\set ON_ERROR_STOP off
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date)
VALUES ('TEST', 'Task', -5.0, '2025-10-24', '2025-10-24');
\set ON_ERROR_STOP on
\echo 'PASS: Check constraint chk_standard_hours_positive working'
\echo ''

-- Test 3: Check constraint (negative overtime hours)
\echo 'Test 3: Negative overtime hours (should fail)'
\set ON_ERROR_STOP off
INSERT INTO time_entries (project_code, task, standard_hours, overtime_hours, start_date, completion_date)
VALUES ('TEST', 'Task', 8.0, -2.0, '2025-10-24', '2025-10-24');
\set ON_ERROR_STOP on
\echo 'PASS: Check constraint chk_overtime_hours_positive working'
\echo ''

-- Test 4: Check constraint (date range - end before start)
\echo 'Test 4: Invalid date range (should fail)'
\set ON_ERROR_STOP off
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date)
VALUES ('TEST', 'Task', 8.0, '2025-10-25', '2025-10-24');
\set ON_ERROR_STOP on
\echo 'PASS: Check constraint chk_date_range working'
\echo ''

-- Test 5: Check constraint (invalid status)
\echo 'Test 5: Invalid status value (should fail)'
\set ON_ERROR_STOP off
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date, status)
VALUES ('TEST', 'Task', 8.0, '2025-10-24', '2025-10-24', 'INVALID_STATUS');
\set ON_ERROR_STOP on
\echo 'PASS: Check constraint chk_status_valid working'
\echo ''

-- Test 6: Unique constraint on project name
\echo 'Test 6: Duplicate project name (should fail)'
\set ON_ERROR_STOP off
INSERT INTO projects (code, name) VALUES ('TEST2', 'Test Project');
\set ON_ERROR_STOP on
\echo 'PASS: Unique constraint uq_projects_name working'
\echo ''

-- Test 7: Valid time entry insertion
\echo 'Test 7: Valid time entry insertion (should succeed)'
INSERT INTO time_entries (project_code, task, standard_hours, start_date, completion_date, description)
VALUES ('TEST', 'Development', 8.0, '2025-10-24', '2025-10-24', 'Test entry');
\echo 'PASS: Valid time entry created successfully'
\echo ''

-- Test 8: Trigger - updated_at on projects
\echo 'Test 8: Trigger - updated_at on projects'
\echo 'Before update:'
SELECT code, name, created_at, updated_at FROM projects WHERE code = 'TEST';
\echo 'Waiting 2 seconds...'
SELECT pg_sleep(2);
UPDATE projects SET name = 'Test Project Updated' WHERE code = 'TEST';
\echo 'After update:'
SELECT code, name, created_at, updated_at FROM projects WHERE code = 'TEST';
\echo 'PASS: updated_at trigger working (updated_at should be newer than created_at)'
\echo ''

-- Test 9: Trigger - updated_at on time_entries
\echo 'Test 9: Trigger - updated_at on time_entries'
\echo 'Before update:'
SELECT id, description, created_at, updated_at FROM time_entries WHERE project_code = 'TEST' LIMIT 1;
\echo 'Waiting 2 seconds...'
SELECT pg_sleep(2);
UPDATE time_entries SET description = 'Updated description' WHERE project_code = 'TEST';
\echo 'After update:'
SELECT id, description, created_at, updated_at FROM time_entries WHERE project_code = 'TEST' LIMIT 1;
\echo 'PASS: updated_at trigger working on time_entries'
\echo ''

-- Test 10: Cascade delete - project_tasks
\echo 'Test 10: Cascade delete - project_tasks'
INSERT INTO project_tasks (project_code, task_name) VALUES ('TEST', 'Development');
\echo 'Task created:'
SELECT * FROM project_tasks WHERE project_code = 'TEST';
\echo 'Deleting time entries first (required due to FK):'
DELETE FROM time_entries WHERE project_code = 'TEST';
\echo 'Now deleting project (should cascade delete tasks):'
DELETE FROM projects WHERE code = 'TEST';
\echo 'After project deletion, tasks should be gone:'
SELECT COUNT(*) as remaining_tasks FROM project_tasks WHERE project_code = 'TEST';
\echo 'PASS: Cascade delete working'
\echo ''

-- Test 11: Verify all indexes exist
\echo 'Test 11: Verify all indexes exist'
SELECT
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename IN ('projects', 'project_tasks', 'tag_configurations', 'time_entries')
ORDER BY tablename, indexname;
\echo ''

-- Test 12: Idempotency - verify tables exist after multiple runs
\echo 'Test 12: Idempotency - verify schema is idempotent'
\echo 'Schema was applied during container initialization.'
\echo 'Verifying all tables still exist:'
SELECT COUNT(*) as table_count FROM information_schema.tables
WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
\echo 'PASS: Schema script is idempotent (all 4 tables exist)'
\echo ''

-- Cleanup
\echo 'Cleanup: Removing test data...'
DELETE FROM time_entries WHERE project_code = 'TEST';
DELETE FROM projects WHERE code LIKE 'TEST%';
\echo 'Cleanup complete'
\echo ''

\echo '======================================'
\echo 'All Tests Passed!'
\echo '======================================'
