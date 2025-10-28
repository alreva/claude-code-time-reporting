# Task 11.2: E2E Test Scenarios

**Phase:** 11 - Integration & Testing
**Estimated Time:** 2 hours
**Prerequisites:** Task 11.1 complete (Claude Code configured), GraphQL API deployed
**Status:** Pending

---

## Objective

Write comprehensive end-to-end (E2E) test scenarios that validate the complete Time Reporting System from user interaction through Claude Code to database persistence.

---

## Acceptance Criteria

- [ ] Test scenarios cover all 7 MCP tools
- [ ] Happy path scenarios documented for each tool
- [ ] Error handling scenarios included
- [ ] Integration with GraphQL API validated
- [ ] Database state verification included
- [ ] Test data setup and teardown scripts created
- [ ] Expected results clearly documented for each scenario
- [ ] Test scenarios can be executed manually or automated

---

## Implementation Steps

### Step 1: Create Test Scenarios Directory Structure

```
tests/
├── e2e/
│   ├── README.md                          # Overview and test execution guide
│   ├── setup-test-data.sh                 # Script to seed test data
│   ├── teardown-test-data.sh              # Script to clean up test data
│   ├── scenarios/
│   │   ├── 01-log-time.md                 # Log time tool scenarios
│   │   ├── 02-query-entries.md            # Query time entries scenarios
│   │   ├── 03-update-entry.md             # Update entry scenarios
│   │   ├── 04-move-task.md                # Move task scenarios
│   │   ├── 05-delete-entry.md             # Delete entry scenarios
│   │   ├── 06-get-projects.md             # Get projects scenarios
│   │   └── 07-submit-workflow.md          # Submit/approve/decline scenarios
│   └── scripts/
│       ├── verify-entry-exists.sh         # Helper: Check entry in DB
│       ├── verify-entry-status.sh         # Helper: Check entry status
│       └── cleanup-test-entries.sh        # Helper: Remove test entries
```

### Step 2: Create E2E Test Overview (tests/e2e/README.md)

```markdown
# End-to-End Test Scenarios

## Overview

This directory contains end-to-end (E2E) test scenarios for the Time Reporting System. These tests validate the complete integration:

```
Claude Code → MCP Server → GraphQL API → PostgreSQL
```

## Prerequisites

**Before running E2E tests:**

1. ✅ GraphQL API running (`/deploy` or `docker-compose up -d`)
2. ✅ Claude Code configured with MCP server (see `docs/integration/CLAUDE-CODE-SETUP.md`)
3. ✅ Test database seeded with projects (`/seed-db`)
4. ✅ Bearer token configured in both API and Claude Code

**Verify setup:**
```bash
./tests/integration/verify-mcp-connection.sh
```

## Test Execution

### Manual Test Execution

Each scenario file (`scenarios/*.md`) contains:
- **User prompt** - What to say to Claude Code
- **Expected behavior** - What should happen
- **Verification** - How to confirm success

**Example workflow:**
1. Open Claude Code
2. Read scenario from `scenarios/01-log-time.md`
3. Execute user prompt in Claude Code
4. Verify expected behavior
5. Run verification script to check database

### Automated Test Execution

Use the test runner script:

```bash
# Run all E2E tests
./tests/e2e/run-all-tests.sh

# Run specific scenario
./tests/e2e/run-test.sh scenarios/01-log-time.md
```

## Test Data Management

### Setup Test Data

Create fresh test data before running scenarios:

```bash
./tests/e2e/setup-test-data.sh
```

This script:
- Clears existing test entries
- Seeds projects (INTERNAL, CLIENT-A, CLIENT-B)
- Creates sample time entries for testing updates/deletes

### Teardown Test Data

Clean up test data after scenarios:

```bash
./tests/e2e/teardown-test-data.sh
```

This script:
- Removes test time entries created during scenarios
- Preserves project configuration

## Test Scenarios

| Scenario | Tools Tested | Duration |
|----------|-------------|----------|
| [01-log-time.md](scenarios/01-log-time.md) | `log_time` | 10 min |
| [02-query-entries.md](scenarios/02-query-entries.md) | `query_time_entries` | 10 min |
| [03-update-entry.md](scenarios/03-update-entry.md) | `update_time_entry` | 10 min |
| [04-move-task.md](scenarios/04-move-task.md) | `move_task_to_project` | 10 min |
| [05-delete-entry.md](scenarios/05-delete-entry.md) | `delete_time_entry` | 5 min |
| [06-get-projects.md](scenarios/06-get-projects.md) | `get_available_projects` | 5 min |
| [07-submit-workflow.md](scenarios/07-submit-workflow.md) | `submit_time_entry` | 15 min |

**Total estimated time:** ~65 minutes for full E2E test suite

## Verification Helpers

Helper scripts in `scripts/` directory:

- `verify-entry-exists.sh <entry-id>` - Check if entry exists in database
- `verify-entry-status.sh <entry-id> <expected-status>` - Verify entry status
- `cleanup-test-entries.sh` - Remove all test entries

## Troubleshooting

### Tests Failing?

**Check connectivity:**
```bash
./tests/integration/verify-mcp-connection.sh
```

**Check API logs:**
```bash
docker-compose logs -f graphql-api
```

**Check MCP server output:**
- Claude Code logs (location varies by platform)

**Verify database state:**
```bash
/db-psql
SELECT * FROM time_entries ORDER BY created_at DESC LIMIT 10;
```

### Common Issues

1. **Authentication failures** - Verify BEARER_TOKEN matches in API and Claude Code config
2. **Connection refused** - Ensure GraphQL API is running on port 5000
3. **Invalid project codes** - Run `/seed-db` to populate test projects
4. **Stale test data** - Run `./teardown-test-data.sh` then `./setup-test-data.sh`
```

### Step 3: Create Individual Test Scenarios

#### Scenario 1: Log Time (tests/e2e/scenarios/01-log-time.md)

```markdown
# E2E Scenario 01: Log Time

## Overview

Test the `log_time` MCP tool to create time entries through Claude Code.

## Test Cases

### Test Case 1.1: Log Time - Happy Path

**User Prompt:**
```
"Log 8 hours of development on INTERNAL for today"
```

**Expected Behavior:**
1. Claude Code calls `log_time` tool
2. MCP server sends GraphQL mutation to API
3. API validates project code "INTERNAL" exists
4. API validates task "Development" is in INTERNAL's available tasks
5. API creates time entry in database
6. Response returns entry ID and confirmation

**Expected Response:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: INTERNAL
Task: Development
Hours: 8.0
Status: NOT_REPORTED
Date: 2025-10-29
```

**Verification:**
```bash
# Run verification script
./tests/e2e/scripts/verify-entry-exists.sh <entry-id>

# Or manual verification
/db-psql
SELECT id, project_code, task, standard_hours, status
FROM time_entries
WHERE id = '<entry-id>';
```

**Expected Database State:**
```
id                   | project_code | task        | standard_hours | status
---------------------|--------------|-------------|----------------|-------------
<uuid>               | INTERNAL     | Development | 8.00           | NOT_REPORTED
```

---

### Test Case 1.2: Log Time with Overtime

**User Prompt:**
```
"Log 6.5 standard hours and 2 overtime hours of bug fixing on CLIENT-A for yesterday"
```

**Expected Behavior:**
1. Claude Code parses "yesterday" to actual date
2. Tool call includes both `standardHours: 6.5` and `overtimeHours: 2.0`
3. Entry created with both hour types

**Expected Response:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: CLIENT-A
Task: Bug Fixing
Standard Hours: 6.5
Overtime Hours: 2.0
Total Hours: 8.5
Status: NOT_REPORTED
Date: 2025-10-28
```

**Verification:**
```bash
/db-psql
SELECT standard_hours, overtime_hours, start_date
FROM time_entries
WHERE id = '<entry-id>';
```

**Expected:**
```
standard_hours | overtime_hours | start_date
---------------|----------------|------------
6.50           | 2.00           | 2025-10-28
```

---

### Test Case 1.3: Log Time with Date Range

**User Prompt:**
```
"Log 8 hours of code review on INTERNAL from Monday to Friday this week"
```

**Expected Behavior:**
1. Claude Code interprets date range
2. Creates single entry with start_date and completion_date
3. Entry spans multiple days

**Expected Response:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: INTERNAL
Task: Code Review
Hours: 8.0
Start Date: 2025-10-27
Completion Date: 2025-10-31
Status: NOT_REPORTED
```

---

### Test Case 1.4: Log Time with Tags

**User Prompt:**
```
"Log 4 hours of development on CLIENT-B for today, tag it as Sprint-5, feature ticket"
```

**Expected Behavior:**
1. Claude Code extracts tags from natural language
2. Tags validated against CLIENT-B's tag configuration
3. Entry created with tags in JSONB field

**Expected Response:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: CLIENT-B
Task: Development
Hours: 4.0
Tags: Sprint: Sprint-5, Type: Feature
Status: NOT_REPORTED
```

**Verification:**
```bash
/db-psql
SELECT tags FROM time_entries WHERE id = '<entry-id>';
```

**Expected:**
```json
{
  "sprint": "Sprint-5",
  "type": "Feature"
}
```

---

### Test Case 1.5: Log Time - Invalid Project (Error Scenario)

**User Prompt:**
```
"Log 8 hours of development on INVALID-PROJECT for today"
```

**Expected Behavior:**
1. Claude Code calls `log_time` tool
2. API validation fails: project "INVALID-PROJECT" doesn't exist
3. Error returned to Claude Code
4. Claude Code explains error to user

**Expected Response:**
```
❌ Failed to create time entry.

Error: Invalid project code "INVALID-PROJECT"

Available projects:
- INTERNAL: Internal Development
- CLIENT-A: Client A Project
- CLIENT-B: Client B Project

Please use one of these project codes.
```

**Verification:**
```bash
# No entry should be created
/db-psql
SELECT COUNT(*) FROM time_entries WHERE project_code = 'INVALID-PROJECT';
```

**Expected:** `0`

---

### Test Case 1.6: Log Time - Invalid Task (Error Scenario)

**User Prompt:**
```
"Log 8 hours of invalid-task-name on INTERNAL for today"
```

**Expected Behavior:**
1. API validation fails: task not in INTERNAL's available tasks
2. Error message suggests valid tasks

**Expected Response:**
```
❌ Failed to create time entry.

Error: Invalid task "invalid-task-name" for project INTERNAL

Available tasks for INTERNAL:
- Development
- Bug Fixing
- Code Review
- Documentation
- Meetings

Please use one of these tasks.
```

---

## Summary

**Total Test Cases:** 6
- **Happy Path:** 4
- **Error Scenarios:** 2

**Coverage:**
- ✅ Basic time logging
- ✅ Overtime hours
- ✅ Date ranges
- ✅ Tags
- ✅ Project validation
- ✅ Task validation

**Next:** Proceed to [02-query-entries.md](./02-query-entries.md)
```

### Step 4: Create Setup Script (tests/e2e/setup-test-data.sh)

```bash
#!/bin/bash

set -e

echo "=== Setting up E2E Test Data ==="
echo

# Step 1: Verify database connection
echo "1. Verifying database connection..."
if ! docker exec time-reporting-db psql -U postgres -d time_reporting -c "SELECT 1;" > /dev/null 2>&1; then
    echo "❌ FAIL: Cannot connect to database"
    echo "   Run: /db-start"
    exit 1
fi
echo "✅ Database connection OK"
echo

# Step 2: Clean existing test entries (keep projects)
echo "2. Cleaning existing test entries..."
docker exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE user_id = 'test-user';
" > /dev/null
echo "✅ Test entries cleaned"
echo

# Step 3: Verify projects exist
echo "3. Verifying projects exist..."
PROJECT_COUNT=$(docker exec time-reporting-db psql -U postgres -d time_reporting -t -c "
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
docker exec time-reporting-db psql -U postgres -d time_reporting -c "
INSERT INTO time_entries (id, project_code, task, standard_hours, overtime_hours, start_date, completion_date, status, user_id, created_at, updated_at)
VALUES
  ('00000000-0000-0000-0000-000000000001', 'INTERNAL', 'Development', 8.0, 0.0, CURRENT_DATE, CURRENT_DATE, 'NOT_REPORTED', 'test-user', NOW(), NOW()),
  ('00000000-0000-0000-0000-000000000002', 'CLIENT-A', 'Bug Fixing', 6.5, 1.5, CURRENT_DATE - 1, CURRENT_DATE - 1, 'NOT_REPORTED', 'test-user', NOW(), NOW()),
  ('00000000-0000-0000-0000-000000000003', 'CLIENT-B', 'Documentation', 4.0, 0.0, CURRENT_DATE - 2, CURRENT_DATE - 2, 'SUBMITTED', 'test-user', NOW(), NOW());
" > /dev/null
echo "✅ Sample entries created"
echo

echo "=== Test Data Setup Complete ==="
echo
echo "Sample test entries created:"
echo "  - Entry 1: INTERNAL, Development, 8h, NOT_REPORTED"
echo "  - Entry 2: CLIENT-A, Bug Fixing, 8h (6.5+1.5 OT), NOT_REPORTED"
echo "  - Entry 3: CLIENT-B, Documentation, 4h, SUBMITTED"
echo
echo "Test entry IDs:"
echo "  00000000-0000-0000-0000-000000000001"
echo "  00000000-0000-0000-0000-000000000002"
echo "  00000000-0000-0000-0000-000000000003"
echo
echo "Ready to run E2E tests!"
```

### Step 5: Create Teardown Script (tests/e2e/teardown-test-data.sh)

```bash
#!/bin/bash

set -e

echo "=== Tearing Down E2E Test Data ==="
echo

# Step 1: Delete test entries
echo "1. Deleting test entries..."
docker exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE user_id = 'test-user';
" > /dev/null
echo "✅ Test entries deleted"
echo

# Step 2: Delete entries created during scenarios (by ID pattern)
echo "2. Deleting scenario entries..."
docker exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE id IN (
  '00000000-0000-0000-0000-000000000001',
  '00000000-0000-0000-0000-000000000002',
  '00000000-0000-0000-0000-000000000003'
);
" > /dev/null
echo "✅ Scenario entries deleted"
echo

# Step 3: Verify cleanup
REMAINING_COUNT=$(docker exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT COUNT(*) FROM time_entries WHERE user_id = 'test-user';
" | xargs)

if [[ "$REMAINING_COUNT" -eq 0 ]]; then
    echo "✅ All test entries removed"
else
    echo "⚠️  Warning: $REMAINING_COUNT test entries still remain"
fi
echo

echo "=== Teardown Complete ==="
```

### Step 6: Create Verification Helper Scripts

**verify-entry-exists.sh:**
```bash
#!/bin/bash

ENTRY_ID=$1

if [[ -z "$ENTRY_ID" ]]; then
    echo "Usage: $0 <entry-id>"
    exit 1
fi

EXISTS=$(docker exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT EXISTS(SELECT 1 FROM time_entries WHERE id = '$ENTRY_ID');
" | xargs)

if [[ "$EXISTS" == "t" ]]; then
    echo "✅ Entry $ENTRY_ID exists"
    docker exec time-reporting-db psql -U postgres -d time_reporting -c "
    SELECT id, project_code, task, standard_hours, status
    FROM time_entries
    WHERE id = '$ENTRY_ID';
    "
    exit 0
else
    echo "❌ Entry $ENTRY_ID does not exist"
    exit 1
fi
```

**verify-entry-status.sh:**
```bash
#!/bin/bash

ENTRY_ID=$1
EXPECTED_STATUS=$2

if [[ -z "$ENTRY_ID" ]] || [[ -z "$EXPECTED_STATUS" ]]; then
    echo "Usage: $0 <entry-id> <expected-status>"
    exit 1
fi

ACTUAL_STATUS=$(docker exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT status FROM time_entries WHERE id = '$ENTRY_ID';
" | xargs)

if [[ "$ACTUAL_STATUS" == "$EXPECTED_STATUS" ]]; then
    echo "✅ Entry $ENTRY_ID status is $EXPECTED_STATUS (as expected)"
    exit 0
else
    echo "❌ Entry $ENTRY_ID status is $ACTUAL_STATUS (expected: $EXPECTED_STATUS)"
    exit 1
fi
```

Make scripts executable:
```bash
chmod +x tests/e2e/setup-test-data.sh
chmod +x tests/e2e/teardown-test-data.sh
chmod +x tests/e2e/scripts/verify-entry-exists.sh
chmod +x tests/e2e/scripts/verify-entry-status.sh
```

---

## Testing

### Execute Test Scenario

1. **Setup test data:**
   ```bash
   ./tests/e2e/setup-test-data.sh
   ```

2. **Open Claude Code and run Scenario 01, Test Case 1.1:**
   ```
   "Log 8 hours of development on INTERNAL for today"
   ```

3. **Extract entry ID from Claude Code response**

4. **Verify in database:**
   ```bash
   ./tests/e2e/scripts/verify-entry-exists.sh <entry-id>
   ```

5. **Teardown:**
   ```bash
   ./tests/e2e/teardown-test-data.sh
   ```

### Validation Checklist

- [ ] Setup script creates test data successfully
- [ ] All 6 test cases in Scenario 01 documented clearly
- [ ] Verification scripts work correctly
- [ ] Teardown script cleans up test data
- [ ] Manual execution of at least 2 test cases successful
- [ ] Database state matches expected results

---

## Related Files

**Created:**
- `tests/e2e/README.md` - E2E test overview
- `tests/e2e/scenarios/01-log-time.md` - Log time scenarios
- `tests/e2e/setup-test-data.sh` - Setup script
- `tests/e2e/teardown-test-data.sh` - Teardown script
- `tests/e2e/scripts/verify-entry-exists.sh` - Verification helper
- `tests/e2e/scripts/verify-entry-status.sh` - Status verification helper

**To Be Created (Remaining Scenarios):**
- `tests/e2e/scenarios/02-query-entries.md`
- `tests/e2e/scenarios/03-update-entry.md`
- `tests/e2e/scenarios/04-move-task.md`
- `tests/e2e/scenarios/05-delete-entry.md`
- `tests/e2e/scenarios/06-get-projects.md`
- `tests/e2e/scenarios/07-submit-workflow.md`

**Note:** This task focuses on creating the infrastructure and Scenario 01. Remaining scenarios (02-07) should follow the same pattern and can be created as needed.

---

## Definition of Done

- [ ] E2E test infrastructure created (README, setup, teardown, helpers)
- [ ] Scenario 01 (log_time) fully documented with 6 test cases
- [ ] Setup and teardown scripts tested and working
- [ ] Verification helper scripts tested and working
- [ ] At least 2 test cases executed manually and verified
- [ ] Documentation clear and actionable

---

## Next Steps

After completing this task:
1. Proceed to **Task 11.3: Manual Workflow Test** to document the manual time logging workflow
2. Create remaining E2E scenarios (02-07) following the pattern established in Scenario 01
3. Consider automating E2E test execution (optional future enhancement)

---

## Resources

- [MCP Tools Specification](../../prd/mcp-tools.md)
- [API Specification](../../prd/api-specification.md)
- [Integration Test README](../integration/README.md)
