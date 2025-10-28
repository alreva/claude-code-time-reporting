# Project Migration Workflow Test Guide

## Overview

This guide tests the project migration feature that allows moving time entries between projects with automatic validation.

**Capabilities Tested:**
- ✅ Move entry between projects (same task)
- ✅ Move entry with task change
- ✅ Move entry with tag revalidation
- ✅ Move entry with tag removal
- ✅ Error handling for invalid migrations
- ✅ Batch migration scenarios

---

## Prerequisites

- [ ] GraphQL API running
- [ ] Claude Code configured with MCP server
- [ ] Test projects seeded (INTERNAL, CLIENT-A)
- [ ] Sample time entries created for migration

**Verify setup:**
```bash
./tests/integration/verify-mcp-connection.sh
./tests/e2e/setup-test-data.sh
```

---

## Test Scenarios

### Scenario 1: Simple Migration - Same Task Name

**Goal:** Move entry from one project to another where both have the same task.

**Setup:**
Create an entry on INTERNAL:
```
"Log 4 hours of development on INTERNAL for today"
```

**Expected:**
Entry created with ID `<entry-id-1>`

**Migration:**
```
"Move entry <entry-id-1> to CLIENT-A project, keep the task as development"
```

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: <entry-id-1>
Migration Summary:
  Previous: INTERNAL / Development
  Current:  CLIENT-A / Development
  Hours: 4.0
  Status: NOT_REPORTED (unchanged)

Validation Results:
  ✅ Target project (CLIENT-A) exists and is active
  ✅ Task (Development) is available in CLIENT-A
  ✅ No tags to revalidate
```

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT te.id, te.project_code, pt.task_name, te.standard_hours
FROM time_entries te
JOIN project_tasks pt ON te.project_task_id = pt.id
WHERE te.id = '<entry-id-1>';"
```

**Expected:**
```
id          | project_code | task_name   | standard_hours
------------|--------------|-------------|----------------
<entry-id-1>| CLIENT-A     | Development | 4.00
```

**Key Points:**
- Project changed: INTERNAL → CLIENT-A
- Task unchanged: Development (exists in both projects)
- Hours preserved: 4.0
- Status preserved: NOT_REPORTED

---

### Scenario 2: Migration with Task Change

**Goal:** Move entry to a project where the original task doesn't exist.

**Setup:**
Create an entry with a task unique to INTERNAL:
```
"Log 3 hours of architecture on INTERNAL for today"
```

**Assume:** CLIENT-A doesn't have "Architecture" task

**Migration Attempt:**
```
"Move entry <entry-id-2> to CLIENT-A project"
```

**Expected Response:**
```
⚠️  Task not available in target project

Entry ID: <entry-id-2>
Current: INTERNAL / Architecture

Target project (CLIENT-A) does not have task "Architecture".

Available tasks for CLIENT-A:
- Feature Development
- Bug Fixing
- Maintenance
- Support
- Code Review

Please specify a new task for this entry:
```

**User Response:**
```
"Use 'Feature Development' as the task"
```

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: <entry-id-2>
Migration Summary:
  Previous: INTERNAL / Architecture
  Current:  CLIENT-A / Feature Development
  Hours: 3.0
  Status: NOT_REPORTED

Task changed: Architecture → Feature Development (task not available in CLIENT-A)
```

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT te.project_code, pt.task_name
FROM time_entries te
JOIN project_tasks pt ON te.project_task_id = pt.id
WHERE te.id = '<entry-id-2>';"
```

**Expected:**
```
project_code | task_name
-------------|-------------------
CLIENT-A     | Feature Development
```

---

### Scenario 3: Migration Error - Invalid Target Project

**Goal:** Test error handling when target project doesn't exist.

**Setup:**
Create a test entry:
```
"Log 2 hours of code review on INTERNAL for today"
```

**Migration Attempt:**
```
"Move entry <entry-id-4> to INVALID-PROJECT"
```

**Expected Response:**
```
❌ Migration failed

Entry ID: <entry-id-4>
Error: Target project "INVALID-PROJECT" does not exist

Available projects:
- INTERNAL: Internal Development
- CLIENT-A: Client A Project

Please specify a valid project code.
```

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT project_code FROM time_entries WHERE id = '<entry-id-4>';"
```

**Expected:**
Entry should still be on INTERNAL (migration failed, no change):
```
project_code
-------------
INTERNAL
```

---

### Scenario 4: Migration Error - Entry Already Submitted

**Goal:** Verify that submitted entries cannot be moved.

**Setup:**
Create and submit an entry:
```
"Log 6 hours of development on INTERNAL for today"
"Submit entry <entry-id-5> for approval"
```

**Migration Attempt:**
```
"Move entry <entry-id-5> to CLIENT-A"
```

**Expected Response:**
```
❌ Migration not allowed

Entry ID: <entry-id-5>
Status: SUBMITTED

Only entries with NOT_REPORTED status can be moved.

Status workflow:
- NOT_REPORTED → Can move ✅
- SUBMITTED → Cannot move (read-only)
- APPROVED → Cannot move (immutable)
- DECLINED → Can move after decline ✅

This entry must be declined by a manager before you can move it.
```

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT project_code, status FROM time_entries WHERE id = '<entry-id-5>';"
```

**Expected:**
Entry unchanged:
```
project_code | status
-------------|----------
INTERNAL     | SUBMITTED
```

---

### Scenario 5: Rollback - Undo Migration

**Goal:** Test moving an entry back to its original project.

**Setup:**
From Scenario 1, entry `<entry-id-1>` was moved from INTERNAL to CLIENT-A.

**Rollback:**
```
"Actually, move entry <entry-id-1> back to INTERNAL"
```

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: <entry-id-1>
Migration Summary:
  Previous: CLIENT-A / Development
  Current:  INTERNAL / Development
  Hours: 4.0

Entry returned to original project.
```

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT project_code FROM time_entries WHERE id = '<entry-id-1>';"
```

**Expected:**
```
project_code
-------------
INTERNAL
```

---

## Common Migration Patterns

### Pattern 1: Wrong Project at Creation

**Scenario:** You logged time to the wrong project and immediately catch it.

**Workflow:**
1. Create entry on wrong project
2. Immediately move to correct project
3. Verify and submit

**Example:**
```
"Log 8 hours of development on INTERNAL for today"
// Oops, should be CLIENT-A

"Move that entry to CLIENT-A"
✅ Moved

"Submit the entry"
✅ Submitted
```

---

### Pattern 2: Bulk Correction After Project Restructuring

**Scenario:** Client asks to rebill last week's work from INTERNAL to CLIENT-A.

**Workflow:**
1. Query last week's INTERNAL entries
2. Identify entries to migrate
3. Batch move to CLIENT-A
4. Verify total hours
5. Submit for approval

**Example:**
```
"Show me all INTERNAL entries from last week"
// Review list, extract IDs

"Move entries <id1>, <id2>, <id3>, <id4> to CLIENT-A, keep tasks unchanged"
✅ Batch migration successful (20 hours total)

"Submit all CLIENT-A entries from last week for approval"
✅ 4 entries submitted
```

---

## Testing Checklist

### Basic Migration
- [ ] Move entry to valid project (same task)
- [ ] Move entry to valid project (different task)
- [ ] Move entry preserves hours
- [ ] Move entry preserves dates
- [ ] Move entry preserves description

### Task Validation
- [ ] Task exists in both projects → keep task
- [ ] Task missing in target → prompt for new task
- [ ] Invalid task specified → error message
- [ ] Task list displayed when needed

### Error Handling
- [ ] Invalid target project → error, no change
- [ ] Non-existent entry ID → error
- [ ] SUBMITTED entry → error, cannot move
- [ ] APPROVED entry → error, cannot move
- [ ] Invalid task specified → error

### Rollback
- [ ] Entry can be moved back to original project
- [ ] Multiple rollbacks work correctly

---

## Troubleshooting

### Migration Fails with "Entry not found"

**Check entry exists:**
```bash
./tests/e2e/scripts/verify-entry-exists.sh <entry-id>
```

### Migration Fails with "Project not found"

**List available projects:**
```
"Get available projects"
```

**Check database:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT code, name, is_active FROM projects;"
```

### Migration Succeeds but Wrong Task

**Verify task mapping:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT pt.id, pt.project_code, pt.task_name
FROM project_tasks pt
WHERE pt.project_code = 'CLIENT-A'
ORDER BY pt.task_name;"
```

---

## Definition of Done

- [ ] All 5 test scenarios documented
- [ ] Common migration patterns documented
- [ ] Testing checklist complete
- [ ] At least 3 scenarios executed manually
- [ ] Database state verified after each migration
- [ ] Error handling validated
- [ ] Rollback scenario tested

---

## Next Steps

After completing Phase 11 (all integration tests):
1. Review all test results and document any issues
2. Create a comprehensive integration test report
3. Proceed to **Phase 12: Documentation & Deployment**
4. Update TASK-INDEX.md to mark Phase 11 complete

---

## Resources

- [Move Task to Project Tool](../../docs/prd/mcp-tools.md#move-task-to-project)
- [API Specification - MoveTaskToProject](../../docs/prd/api-specification.md#movetasktoproject)
- [E2E Test Scenario 04](../../tests/e2e/scenarios/04-move-task.md)
- [Manual Workflow Test](./MANUAL-TIME-LOGGING.md)
