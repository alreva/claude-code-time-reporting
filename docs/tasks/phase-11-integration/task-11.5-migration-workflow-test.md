# Task 11.5: Migration Workflow Test

**Phase:** 11 - Integration & Testing
**Estimated Time:** 1 hour
**Prerequisites:** Task 11.3 (Manual workflow tested), `move_task_to_project` tool implemented
**Status:** Pending

---

## Objective

Test the project migration workflow that allows moving time entries between projects with automatic revalidation of tasks and tags to ensure data consistency.

---

## Acceptance Criteria

- [ ] Move entry between projects with same task name
- [ ] Move entry with task name change (task not available in target project)
- [ ] Move entry with automatic tag revalidation
- [ ] Move entry with tag removal (tags invalid for target project)
- [ ] Validate error handling for invalid target projects
- [ ] Verify database state after each migration
- [ ] Test batch migration scenarios (multiple entries)
- [ ] Document common migration patterns and edge cases

---

## Migration Feature Overview

**What is Migration?**

The migration feature (`move_task_to_project` tool) allows you to:
1. **Reassign** time entries to a different project
2. **Validate** that the task exists in the target project
3. **Revalidate** tags against the target project's tag configuration
4. **Preserve** entry history (hours, dates, description)
5. **Maintain** status workflow (only NOT_REPORTED entries can be moved)

**Common Use Cases:**
- Logged time to wrong project by mistake
- Project scope changed, work should be billed to different client
- Consolidating work from multiple projects
- Moving entries after project restructuring

**Validation Rules:**
- Target project must exist and be active
- Target task must be in target project's available tasks
- Tags must be valid for target project (or removed if incompatible)
- Only NOT_REPORTED entries can be moved
- Entry ID must exist

---

## Implementation Steps

### Step 1: Create Migration Test Guide (docs/workflows/MIGRATION-WORKFLOW-TEST.md)

```markdown
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
- [ ] Test projects seeded (INTERNAL, CLIENT-A, CLIENT-B)
- [ ] Sample time entries created for migration

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
/db-psql
SELECT id, project_code, task, standard_hours
FROM time_entries
WHERE id = '<entry-id-1>';
```

**Expected:**
```
id          | project_code | task        | standard_hours
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
"Log 3 hours of internal meetings on INTERNAL for today"
```

**Assume:** CLIENT-A doesn't have "Internal Meetings" task

**Migration Attempt:**
```
"Move entry <entry-id-2> to CLIENT-A project"
```

**Expected Response:**
```
⚠️  Task not available in target project

Entry ID: <entry-id-2>
Current: INTERNAL / Internal Meetings

Target project (CLIENT-A) does not have task "Internal Meetings".

Available tasks for CLIENT-A:
- Development
- Bug Fixing
- Code Review
- Testing

Please specify a new task for this entry:
```

**User Response:**
```
"Use 'Development' as the task"
```

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: <entry-id-2>
Migration Summary:
  Previous: INTERNAL / Internal Meetings
  Current:  CLIENT-A / Development
  Hours: 3.0
  Status: NOT_REPORTED

Task changed: Internal Meetings → Development (task not available in CLIENT-A)
```

**Verification:**
```bash
/db-psql
SELECT project_code, task FROM time_entries WHERE id = '<entry-id-2>';
```

**Expected:**
```
project_code | task
-------------|-------------
CLIENT-A     | Development
```

---

### Scenario 3: Migration with Tag Revalidation

**Goal:** Move entry with tags that need revalidation against target project's tag config.

**Setup:**
Create an entry with tags on CLIENT-A:
```
"Log 5 hours of bug fixing on CLIENT-A for today, tag it as Sprint-3, bug ticket"
```

**Entry created with tags:**
```json
{
  "sprint": "Sprint-3",
  "type": "Bug"
}
```

**Migration:**
```
"Move entry <entry-id-3> to CLIENT-B project, keep task as bug fixing"
```

**Scenario A: Tags Valid in Both Projects**

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: <entry-id-3>
Migration Summary:
  Previous: CLIENT-A / Bug Fixing
  Current:  CLIENT-B / Bug Fixing
  Hours: 5.0
  Tags: Sprint: Sprint-3, Type: Bug

Validation Results:
  ✅ Target project (CLIENT-B) exists
  ✅ Task (Bug Fixing) is available in CLIENT-B
  ✅ Tags revalidated: Both tags valid in CLIENT-B configuration
```

**Verification:**
```bash
/db-psql
SELECT project_code, tags FROM time_entries WHERE id = '<entry-id-3>';
```

**Expected:**
```
project_code | tags
-------------|------------------------------------------
CLIENT-B     | {"sprint": "Sprint-3", "type": "Bug"}
```

---

**Scenario B: Tags Invalid in Target Project**

**Assume:** CLIENT-B doesn't have "sprint" tag in its configuration

**Expected Response:**
```
⚠️  Tag validation warning

Entry ID: <entry-id-3>
Target project (CLIENT-B) does not support some of your tags:

Tags removed:
  - sprint: Sprint-3 (not configured for CLIENT-B)

Tags kept:
  - type: Bug (valid for CLIENT-B)

Proceed with migration?
```

**User Response:**
```
"Yes, proceed"
```

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: <entry-id-3>
Migration Summary:
  Previous: CLIENT-A / Bug Fixing
  Current:  CLIENT-B / Bug Fixing
  Tags: Type: Bug (sprint tag removed)
```

**Verification:**
```bash
/db-psql
SELECT tags FROM time_entries WHERE id = '<entry-id-3>';
```

**Expected:**
```
tags
------------------------
{"type": "Bug"}
```

---

### Scenario 4: Migration Error - Invalid Target Project

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
- CLIENT-B: Client B Project

Please specify a valid project code.
```

**Verification:**
```bash
/db-psql
SELECT project_code FROM time_entries WHERE id = '<entry-id-4>';
```

**Expected:**
Entry should still be on INTERNAL (migration failed, no change):
```
project_code
-------------
INTERNAL
```

---

### Scenario 5: Migration Error - Entry Already Submitted

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
/db-psql
SELECT project_code, status FROM time_entries WHERE id = '<entry-id-5>';
```

**Expected:**
Entry unchanged:
```
project_code | status
-------------|----------
INTERNAL     | SUBMITTED
```

---

### Scenario 6: Batch Migration - Multiple Entries

**Goal:** Test moving multiple entries at once.

**Setup:**
Create 3 entries on INTERNAL:
```
"Log 4 hours of development on INTERNAL for Monday"
"Log 4 hours of development on INTERNAL for Tuesday"
"Log 4 hours of development on INTERNAL for Wednesday"
```

**Entry IDs:** `<entry-id-6a>`, `<entry-id-6b>`, `<entry-id-6c>`

**Batch Migration:**
```
"Move entries <entry-id-6a>, <entry-id-6b>, and <entry-id-6c> to CLIENT-A project, keep task as development"
```

**Expected Response:**
```
✅ Batch migration successful!

Migrated 3 entries:

1. Entry <entry-id-6a>
   INTERNAL / Development → CLIENT-A / Development
   Hours: 4.0

2. Entry <entry-id-6b>
   INTERNAL / Development → CLIENT-A / Development
   Hours: 4.0

3. Entry <entry-id-6c>
   INTERNAL / Development → CLIENT-A / Development
   Hours: 4.0

Total hours migrated: 12.0
All entries now on CLIENT-A
```

**Verification:**
```bash
/db-psql
SELECT id, project_code, task, start_date
FROM time_entries
WHERE id IN ('<entry-id-6a>', '<entry-id-6b>', '<entry-id-6c>')
ORDER BY start_date;
```

**Expected:**
```
id            | project_code | task        | start_date
--------------|--------------|-------------|------------
<entry-id-6a> | CLIENT-A     | Development | 2025-10-27
<entry-id-6b> | CLIENT-A     | Development | 2025-10-28
<entry-id-6c> | CLIENT-A     | Development | 2025-10-29
```

---

### Scenario 7: Rollback - Undo Migration

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
/db-psql
SELECT project_code FROM time_entries WHERE id = '<entry-id-1>';
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

### Pattern 3: Tag Cleanup During Migration

**Scenario:** Move entries to a project with stricter tag requirements.

**Workflow:**
1. Move entry to target project
2. System warns about invalid tags
3. Accept tag removal or update tags
4. Complete migration

**Example:**
```
"Move entry <id> to CLIENT-B"

⚠️  Tag "custom-field" not valid for CLIENT-B
    Remove this tag?

"Yes, remove invalid tags"

✅ Entry moved, tags cleaned up
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

### Tag Validation
- [ ] Tags valid in target → keep tags
- [ ] Some tags invalid → prompt for removal
- [ ] All tags invalid → remove all tags
- [ ] No tags → migration succeeds

### Error Handling
- [ ] Invalid target project → error, no change
- [ ] Non-existent entry ID → error
- [ ] SUBMITTED entry → error, cannot move
- [ ] APPROVED entry → error, cannot move
- [ ] Invalid task specified → error

### Batch Operations
- [ ] Multiple entries migrated successfully
- [ ] Partial failure handled (some succeed, some fail)
- [ ] Total hours calculated correctly

### Rollback
- [ ] Entry can be moved back to original project
- [ ] Multiple rollbacks work correctly

---

## Verification Scripts

### Verify Migration

Create `tests/migration/verify-migration.sh`:

```bash
#!/bin/bash

ENTRY_ID=$1
EXPECTED_PROJECT=$2

if [[ -z "$ENTRY_ID" ]] || [[ -z "$EXPECTED_PROJECT" ]]; then
    echo "Usage: $0 <entry-id> <expected-project-code>"
    exit 1
fi

ACTUAL_PROJECT=$(docker exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT project_code FROM time_entries WHERE id = '$ENTRY_ID';
" | xargs)

if [[ "$ACTUAL_PROJECT" == "$EXPECTED_PROJECT" ]]; then
    echo "✅ Entry $ENTRY_ID is on project $EXPECTED_PROJECT (as expected)"
    exit 0
else
    echo "❌ Entry $ENTRY_ID is on project $ACTUAL_PROJECT (expected: $EXPECTED_PROJECT)"
    exit 1
fi
```

### Track Migration History

Create `tests/migration/migration-history.sh`:

```bash
#!/bin/bash

ENTRY_ID=$1

if [[ -z "$ENTRY_ID" ]]; then
    echo "Usage: $0 <entry-id>"
    exit 1
fi

echo "Migration History for Entry: $ENTRY_ID"
echo

docker exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT
    id,
    project_code,
    task,
    standard_hours,
    status,
    created_at,
    updated_at
FROM time_entries
WHERE id = '$ENTRY_ID';
"

echo
echo "Note: updated_at > created_at indicates entry was modified"
```

Make scripts executable:
```bash
chmod +x tests/migration/verify-migration.sh
chmod +x tests/migration/migration-history.sh
```

---

## Definition of Done

- [ ] All 7 test scenarios documented
- [ ] Common migration patterns documented
- [ ] Testing checklist complete
- [ ] Verification scripts created and tested
- [ ] At least 4 scenarios executed manually
- [ ] Database state verified after each migration
- [ ] Error handling validated
- [ ] Batch migration tested

---

## Next Steps

After completing Phase 11 (all integration tests):
1. Review all test results and document any issues
2. Create a comprehensive integration test report
3. Proceed to **Phase 12: Documentation & Deployment**
4. Consider creating automated migration test suite (future enhancement)

---

## Resources

- [Move Task to Project Tool](../../prd/mcp-tools.md#move-task-to-project)
- [API Specification - MoveTaskToProject](../../prd/api-specification.md#movetasktoproject)
- [Validation Service](../phase-04-mutations-part1/task-4.4-validation-service.md)
- [E2E Test Scenarios](../e2e/scenarios/04-move-task.md)
