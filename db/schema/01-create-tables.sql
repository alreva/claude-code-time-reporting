-- Note: Database is automatically created via POSTGRES_DB environment variable
-- This script runs in the context of that database

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
