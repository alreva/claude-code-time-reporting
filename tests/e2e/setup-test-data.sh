#!/bin/bash

set -e

echo "=== Setting up E2E Test Data ==="
echo

# Detect container command
if command -v podman &> /dev/null; then
    CONTAINER_CMD="podman"
elif command -v docker &> /dev/null; then
    CONTAINER_CMD="docker"
else
    echo "❌ FAIL: Neither podman nor docker found"
    exit 1
fi

# Step 1: Verify database connection
echo "1. Verifying database connection..."
if ! $CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "SELECT 1;" > /dev/null 2>&1; then
    echo "❌ FAIL: Cannot connect to database"
    echo "   Run: /db-start"
    exit 1
fi
echo "✅ Database connection OK"
echo

# Step 2: Clean existing test entries (keep projects)
echo "2. Cleaning existing test entries..."
$CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE user_id = 'test-user';
" > /dev/null
echo "✅ Test entries cleaned"
echo

# Step 3: Verify projects exist
echo "3. Verifying projects exist..."
PROJECT_COUNT=$($CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT COUNT(*) FROM projects WHERE code IN ('INTERNAL', 'CLIENT-A', 'CLIENT-B');
" | xargs)

if [[ "$PROJECT_COUNT" -ne 3 ]]; then
    echo "⚠️  Warning: Expected 3 projects, found $PROJECT_COUNT"
    echo "   Run: /seed-db"
fi
echo "✅ Projects verified ($PROJECT_COUNT found)"
echo

# Step 4: Create sample entries for update/delete tests
echo "4. Creating sample test entries..."
$CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "
INSERT INTO time_entries (id, project_code, project_task_id, standard_hours, overtime_hours, start_date, completion_date, status, user_id, created_at, updated_at)
VALUES
  ('00000000-0000-0000-0000-000000000001', 'INTERNAL', 7, 8.0, 0.0, CURRENT_DATE, CURRENT_DATE, 'NOT_REPORTED', 'test-user', NOW(), NOW()),
  ('00000000-0000-0000-0000-000000000002', 'CLIENT-A', 13, 6.5, 1.5, CURRENT_DATE - 1, CURRENT_DATE - 1, 'NOT_REPORTED', 'test-user', NOW(), NOW()),
  ('00000000-0000-0000-0000-000000000003', 'INTERNAL', 10, 4.0, 0.0, CURRENT_DATE - 2, CURRENT_DATE - 2, 'SUBMITTED', 'test-user', NOW(), NOW());
" > /dev/null
echo "✅ Sample entries created"
echo

echo "=== Test Data Setup Complete ==="
echo
echo "Sample test entries created:"
echo "  - Entry 1: INTERNAL, Development (task_id=7), 8h, NOT_REPORTED"
echo "  - Entry 2: CLIENT-A, Bug Fixing (task_id=13), 8h (6.5+1.5 OT), NOT_REPORTED"
echo "  - Entry 3: INTERNAL, Documentation (task_id=10), 4h, SUBMITTED"
echo
echo "Test entry IDs:"
echo "  00000000-0000-0000-0000-000000000001"
echo "  00000000-0000-0000-0000-000000000002"
echo "  00000000-0000-0000-0000-000000000003"
echo
echo "Ready to run E2E tests!"
