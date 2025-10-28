-- Seed Data for Time Reporting System
-- This script is idempotent - can be run multiple times safely

-- Clean existing data (for idempotency)
TRUNCATE TABLE time_entries CASCADE;
TRUNCATE TABLE tag_values CASCADE;
TRUNCATE TABLE project_tags CASCADE;
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
INSERT INTO project_tags (project_code, tag_name, is_required, is_active) VALUES
('INTERNAL', 'Environment', false, true),
('INTERNAL', 'Billable', false, true),
('INTERNAL', 'Type', false, true);

-- Insert Tag Values for INTERNAL tags
INSERT INTO tag_values (project_tag_id, value) VALUES
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Environment'), 'Production'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Environment'), 'Staging'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Environment'), 'Development'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Billable'), 'Yes'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Billable'), 'No'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Type'), 'Feature'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Type'), 'Bug'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Type'), 'Refactor'),
((SELECT id FROM project_tags WHERE project_code = 'INTERNAL' AND tag_name = 'Type'), 'Docs');

-- Insert Tag Configurations for CLIENT-A
INSERT INTO project_tags (project_code, tag_name, is_required, is_active) VALUES
('CLIENT-A', 'Priority', true, true),
('CLIENT-A', 'Sprint', false, true),
('CLIENT-A', 'Billable', false, true);

-- Insert Tag Values for CLIENT-A tags
INSERT INTO tag_values (project_tag_id, value) VALUES
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Priority'), 'High'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Priority'), 'Medium'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Priority'), 'Low'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Sprint'), 'Sprint-1'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Sprint'), 'Sprint-2'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Sprint'), 'Sprint-3'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Sprint'), 'Sprint-4'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Billable'), 'Yes'),
((SELECT id FROM project_tags WHERE project_code = 'CLIENT-A' AND tag_name = 'Billable'), 'No');

-- Insert Tag Configurations for MAINT
INSERT INTO project_tags (project_code, tag_name, is_required, is_active) VALUES
('MAINT', 'Severity', true, true),
('MAINT', 'Billable', false, true);

-- Insert Tag Values for MAINT tags
INSERT INTO tag_values (project_tag_id, value) VALUES
((SELECT id FROM project_tags WHERE project_code = 'MAINT' AND tag_name = 'Severity'), 'Critical'),
((SELECT id FROM project_tags WHERE project_code = 'MAINT' AND tag_name = 'Severity'), 'High'),
((SELECT id FROM project_tags WHERE project_code = 'MAINT' AND tag_name = 'Severity'), 'Medium'),
((SELECT id FROM project_tags WHERE project_code = 'MAINT' AND tag_name = 'Severity'), 'Low'),
((SELECT id FROM project_tags WHERE project_code = 'MAINT' AND tag_name = 'Billable'), 'Yes'),
((SELECT id FROM project_tags WHERE project_code = 'MAINT' AND tag_name = 'Billable'), 'No');

-- Insert Sample Time Entries
INSERT INTO time_entries (project_code, project_task_id, task, issue_id, standard_hours, overtime_hours, description, start_date, completion_date, status, tags) VALUES
('INTERNAL', (SELECT id FROM project_tasks WHERE project_code = 'INTERNAL' AND task_name = 'Development'), 'Development', 'DEV-123', 7.5, 0, 'Implemented user authentication module', '2025-10-21', '2025-10-21', 'APPROVED', '[{"name": "Environment", "value": "Production"}, {"name": "Billable", "value": "Yes"}]'),
('INTERNAL', (SELECT id FROM project_tasks WHERE project_code = 'INTERNAL' AND task_name = 'Code Review'), 'Code Review', null, 2.0, 0, 'Reviewed PR #456', '2025-10-21', '2025-10-21', 'APPROVED', '[{"name": "Billable", "value": "No"}]'),
('CLIENT-A', (SELECT id FROM project_tasks WHERE project_code = 'CLIENT-A' AND task_name = 'Feature Development'), 'Feature Development', 'JIRA-789', 6.0, 1.5, 'Built new dashboard component', '2025-10-22', '2025-10-22', 'SUBMITTED', '[{"name": "Priority", "value": "High"}, {"name": "Sprint", "value": "Sprint-2"}]'),
('CLIENT-A', (SELECT id FROM project_tasks WHERE project_code = 'CLIENT-A' AND task_name = 'Bug Fixing'), 'Bug Fixing', 'JIRA-790', 4.0, 0, 'Fixed login redirect issue', '2025-10-23', '2025-10-23', 'NOT_REPORTED', '[{"name": "Priority", "value": "Medium"}]'),
('MAINT', (SELECT id FROM project_tasks WHERE project_code = 'MAINT' AND task_name = 'Security Patches'), 'Security Patches', 'SEC-101', 3.0, 0, 'Applied security updates to dependencies', '2025-10-23', '2025-10-23', 'APPROVED', '[{"name": "Severity", "value": "Critical"}, {"name": "Billable", "value": "Yes"}]');

-- Verify insertions
SELECT 'Projects:' as info, COUNT(*) as count FROM projects
UNION ALL
SELECT 'Project Tasks:', COUNT(*) FROM project_tasks
UNION ALL
SELECT 'Tag Configurations:', COUNT(*) FROM project_tags
UNION ALL
SELECT 'Tag Values:', COUNT(*) FROM tag_values
UNION ALL
SELECT 'Time Entries:', COUNT(*) FROM time_entries;
