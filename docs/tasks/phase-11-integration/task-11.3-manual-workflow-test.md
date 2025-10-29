# Task 11.3: Manual Workflow Test

**Phase:** 11 - Integration & Testing
**Estimated Time:** 1 hour
**Prerequisites:** Task 11.1 (Claude Code configured), Task 11.2 (E2E scenarios), GraphQL API deployed
**Status:** Pending

---

## Objective

Test the complete manual time logging workflow end-to-end, simulating a developer's typical day of tracking time spent on coding tasks through natural language commands in Claude Code.

---

## Acceptance Criteria

- [ ] Complete workflow documented from start to finish
- [ ] All core operations tested: create, query, update, delete, submit
- [ ] Natural language variations tested (different phrasing for same operation)
- [ ] Database state verified at each step
- [ ] Workflow guide can be followed by new users
- [ ] Common patterns and best practices documented
- [ ] Edge cases and error scenarios included

---

## Manual Workflow Overview

**Scenario:** A developer logs time for a full workday using Claude Code

**Timeline:**
- **9:00 AM** - Start development work on INTERNAL project
- **12:00 PM** - Query morning's work
- **1:00 PM** - Correct an error in morning's entry
- **5:00 PM** - Log afternoon work on CLIENT-A
- **5:30 PM** - Move a task to different project
- **6:00 PM** - Review and submit all entries for approval

---

## Implementation Steps

### Step 1: Create Workflow Test Guide (docs/workflows/MANUAL-TIME-LOGGING.md)

```markdown
# Manual Time Logging Workflow Guide

## Overview

This guide demonstrates a complete manual time logging workflow using Claude Code and the Time Reporting System.

**Use Case:** A developer tracking a full workday of coding activities

---

## Prerequisites

**Before starting:**
- [ ] GraphQL API running (`/deploy` or `docker-compose up -d`)
- [ ] Claude Code configured with time-reporting MCP server
- [ ] Projects seeded in database (`/seed-db`)
- [ ] You have a valid bearer token configured

**Verify setup:**
```bash
./tests/integration/verify-mcp-connection.sh
```

---

## Workflow Steps

### Step 1: Morning - Log Development Work (9:00 AM)

**Context:** You spent the morning (8:00 AM - 12:00 PM, 4 hours) working on development for the INTERNAL project.

**User Prompt (Natural Language):**
```
"Log 4 hours of development on INTERNAL for this morning"
```

**Alternative Phrasings:**
- "Track 4 hours of development work on INTERNAL today"
- "I spent 4 hours on INTERNAL development this morning, log it"
- "Create time entry: 4 hours, INTERNAL, development, today"

**Expected Response:**
```
✅ Time entry created successfully!

Entry ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
Project: INTERNAL
Task: Development
Hours: 4.0
Date: 2025-10-29
Status: NOT_REPORTED
```

**Save the Entry ID** for later steps!

**Verification:**
```bash
/db-psql
SELECT id, project_code, task, standard_hours, start_date, status
FROM time_entries
ORDER BY created_at DESC LIMIT 1;
```

**Expected:**
```
id                                   | project_code | task        | standard_hours | start_date | status
-------------------------------------|--------------|-------------|----------------|------------|-------------
a1b2c3d4-e5f6-7890-abcd-ef1234567890 | INTERNAL     | Development | 4.00           | 2025-10-29 | NOT_REPORTED
```

---

### Step 2: Midday - Query Today's Entries (12:00 PM)

**Context:** You want to review what you've logged so far today.

**User Prompt:**
```
"Show me my time entries for today"
```

**Alternative Phrasings:**
- "What did I log today?"
- "Query my time entries for today"
- "List all my time entries from today"

**Expected Response:**
```
📋 Time Entries for 2025-10-29:

1. Entry ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
   Project: INTERNAL
   Task: Development
   Hours: 4.0
   Status: NOT_REPORTED
   Created: 09:05 AM

Total Hours: 4.0
Total Entries: 1
```

**Verification:**
```bash
/db-psql
SELECT COUNT(*) FROM time_entries WHERE start_date = CURRENT_DATE;
```

**Expected:** `1`

---

### Step 3: Afternoon - Correct Morning Entry (1:00 PM)

**Context:** You realize you actually worked 4.5 hours, not 4 hours. Update the entry.

**User Prompt:**
```
"Update entry a1b2c3d4-e5f6-7890-abcd-ef1234567890 to 4.5 hours"
```

**Alternative Phrasings:**
- "Change entry a1b2... to 4.5 hours"
- "Update the hours on entry a1b2... to 4.5"
- "Fix entry a1b2..., it should be 4.5 hours not 4"

**Expected Response:**
```
✅ Time entry updated successfully!

Entry ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
Project: INTERNAL
Task: Development
Hours: 4.5 (updated from 4.0)
Status: NOT_REPORTED
```

**Verification:**
```bash
/db-psql
SELECT standard_hours, updated_at > created_at AS was_updated
FROM time_entries
WHERE id = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';
```

**Expected:**
```
standard_hours | was_updated
---------------|-------------
4.50           | t
```

---

### Step 4: Late Afternoon - Log Client Work (5:00 PM)

**Context:** You spent the afternoon (1:00 PM - 5:00 PM, 4 hours) on bug fixing for CLIENT-A.

**User Prompt:**
```
"Log 4 hours of bug fixing on CLIENT-A for this afternoon"
```

**Expected Response:**
```
✅ Time entry created successfully!

Entry ID: b2c3d4e5-f6g7-8901-bcde-fg2345678901
Project: CLIENT-A
Task: Bug Fixing
Hours: 4.0
Date: 2025-10-29
Status: NOT_REPORTED
```

**Verification:**
```bash
/db-psql
SELECT COUNT(*) FROM time_entries WHERE start_date = CURRENT_DATE;
```

**Expected:** `2`

---

### Step 5: Evening - Move Task to Different Project (5:30 PM)

**Context:** You realize the bug fixing work should have been logged under CLIENT-B, not CLIENT-A.

**User Prompt:**
```
"Move entry b2c3d4e5-f6g7-8901-bcde-fg2345678901 to CLIENT-B project, keep the task as bug fixing"
```

**Alternative Phrasings:**
- "Change entry b2c3... from CLIENT-A to CLIENT-B"
- "Move entry b2c3... to CLIENT-B"
- "Reassign entry b2c3... to CLIENT-B project"

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: b2c3d4e5-f6g7-8901-bcde-fg2345678901
Previous: CLIENT-A / Bug Fixing
Current: CLIENT-B / Bug Fixing
Hours: 4.0
Status: NOT_REPORTED
```

**Verification:**
```bash
/db-psql
SELECT project_code, task
FROM time_entries
WHERE id = 'b2c3d4e5-f6g7-8901-bcde-fg2345678901';
```

**Expected:**
```
project_code | task
-------------|----------
CLIENT-B     | Bug Fixing
```

---

### Step 6: End of Day - Review and Submit (6:00 PM)

**Context:** Review all today's entries and submit them for approval.

**Step 6.1: Review Today's Entries**

**User Prompt:**
```
"Show me all my time entries for today"
```

**Expected Response:**
```
📋 Time Entries for 2025-10-29:

1. Entry ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
   Project: INTERNAL
   Task: Development
   Hours: 4.5
   Status: NOT_REPORTED

2. Entry ID: b2c3d4e5-f6g7-8901-bcde-fg2345678901
   Project: CLIENT-B
   Task: Bug Fixing
   Hours: 4.0
   Status: NOT_REPORTED

Total Hours: 8.5
Total Entries: 2
```

**Step 6.2: Submit Entries for Approval**

**User Prompt:**
```
"Submit entry a1b2c3d4-e5f6-7890-abcd-ef1234567890 for approval"
```

**Expected Response:**
```
✅ Time entry submitted for approval!

Entry ID: a1b2c3d4-e5f6-7890-abcd-ef1234567890
Status: NOT_REPORTED → SUBMITTED
```

**Repeat for second entry:**
```
"Submit entry b2c3d4e5-f6g7-8901-bcde-fg2345678901 for approval"
```

**Verification:**
```bash
/db-psql
SELECT id, project_code, status
FROM time_entries
WHERE start_date = CURRENT_DATE
ORDER BY created_at;
```

**Expected:**
```
id                                   | project_code | status
-------------------------------------|--------------|----------
a1b2c3d4-e5f6-7890-abcd-ef1234567890 | INTERNAL     | SUBMITTED
b2c3d4e5-f6g7-8901-bcde-fg2345678901 | CLIENT-B     | SUBMITTED
```

---

## Workflow Summary

**Total Operations Performed:**
1. ✅ Created time entry (INTERNAL, Development, 4h)
2. ✅ Queried entries for today
3. ✅ Updated entry hours (4h → 4.5h)
4. ✅ Created second entry (CLIENT-A, Bug Fixing, 4h)
5. ✅ Moved entry to different project (CLIENT-A → CLIENT-B)
6. ✅ Reviewed all entries
7. ✅ Submitted both entries for approval

**Final State:**
- 2 time entries created
- Total hours: 8.5
- Both entries: SUBMITTED status
- Ready for manager approval

---

## Common Patterns

### Natural Language Variations

Claude Code understands various phrasings for the same operation:

**Creating Entries:**
- "Log 8 hours of X on Y for Z"
- "Track 8 hours, project Y, task X, date Z"
- "Create time entry: 8h, Y, X, Z"
- "I worked 8 hours on Y doing X today"

**Querying:**
- "Show me entries for today"
- "What did I log this week?"
- "List my time entries for October"
- "Query entries for project INTERNAL"

**Updating:**
- "Update entry <id> to 8 hours"
- "Change entry <id> hours to 8"
- "Fix entry <id>, should be 8 hours"
- "Modify entry <id>: 8 hours"

**Deleting:**
- "Delete entry <id>"
- "Remove entry <id>"
- "Cancel entry <id>"

### Best Practices

1. **Save Entry IDs:** After creating entries, copy the ID for future updates/deletes
2. **Review Before Submitting:** Query today's entries before submitting
3. **Use Natural Language:** Don't worry about exact phrasing - Claude Code understands variations
4. **Verify Important Changes:** Check database after critical updates (moving projects, etc.)
5. **Submit Daily:** Submit entries at end of day for timely approval

---

## Edge Cases and Errors

### Error: Invalid Project Code

**User Prompt:**
```
"Log 8 hours on INVALID-PROJECT for today"
```

**Expected Response:**
```
❌ Failed to create time entry.

Error: Invalid project code "INVALID-PROJECT"

Available projects:
- INTERNAL: Internal Development
- CLIENT-A: Client A Project
- CLIENT-B: Client B Project
```

**Resolution:** Use a valid project code from the list

---

### Error: Invalid Task for Project

**User Prompt:**
```
"Log 8 hours of invalid-task on INTERNAL for today"
```

**Expected Response:**
```
❌ Failed to create time entry.

Error: Invalid task "invalid-task" for project INTERNAL

Available tasks for INTERNAL:
- Development
- Bug Fixing
- Code Review
- Documentation
- Meetings
```

**Resolution:** Use a valid task from the project's available tasks

---

### Error: Cannot Update Submitted Entry

**User Prompt:**
```
"Update entry <id-of-submitted-entry> to 10 hours"
```

**Expected Response:**
```
❌ Cannot update time entry.

Error: Entry is in SUBMITTED status and cannot be modified.

Status workflow:
- NOT_REPORTED → Can edit/delete
- SUBMITTED → Read-only (waiting for approval)
- APPROVED → Immutable
- DECLINED → Can edit and resubmit

This entry must be declined by a manager before you can edit it.
```

---

### Error: Cannot Delete Submitted Entry

**User Prompt:**
```
"Delete entry <id-of-submitted-entry>"
```

**Expected Response:**
```
❌ Cannot delete time entry.

Error: Only entries with NOT_REPORTED status can be deleted.
This entry has status: SUBMITTED

To remove this entry, ask a manager to decline it first.
```

---

## Troubleshooting

### Tools Not Responding

**Check MCP Connection:**
```bash
./tests/integration/verify-mcp-connection.sh
```

**Check API Health:**
```bash
curl http://localhost:5001/health
```

### Authentication Errors

**Verify Token Matches:**
1. Check `.env` file: `BEARER_TOKEN=...`
2. Check Claude Code config: `~/.config/claude-code/config.json`
3. Restart API: `/deploy`
4. Restart Claude Code

### Database State Issues

**Reset Test Data:**
```bash
./tests/e2e/teardown-test-data.sh
./tests/e2e/setup-test-data.sh
```

**Manual Cleanup:**
```bash
/db-psql
DELETE FROM time_entries WHERE user_id = 'test-user';
```

---

## Next Steps

After completing this workflow:
- Try the **Auto-tracking Workflow** (Task 11.4) for intelligent time suggestions
- Test the **Migration Workflow** (Task 11.5) for moving tasks between projects
- Review **E2E Test Scenarios** (Task 11.2) for comprehensive testing

---
```

### Step 2: Create Workflow Validation Checklist

Create `docs/workflows/WORKFLOW-VALIDATION-CHECKLIST.md`:

```markdown
# Manual Workflow Validation Checklist

## Pre-Flight Checks

- [ ] GraphQL API running and healthy
- [ ] Claude Code configured with MCP server
- [ ] Database seeded with test projects
- [ ] Bearer token configured correctly
- [ ] MCP connection verified

## Workflow Execution

### Create Operations
- [ ] Create basic time entry (4 hours, INTERNAL, Development)
- [ ] Create entry with overtime (6.5 + 1.5 hours)
- [ ] Create entry with date range (Monday to Friday)
- [ ] Create entry with tags

### Query Operations
- [ ] Query today's entries
- [ ] Query this week's entries
- [ ] Query by project (INTERNAL)
- [ ] Query by status (NOT_REPORTED)
- [ ] Query single entry by ID

### Update Operations
- [ ] Update hours (4h → 4.5h)
- [ ] Update task within same project
- [ ] Update date range
- [ ] Update tags
- [ ] Attempt update on SUBMITTED entry (should fail)

### Move Operations
- [ ] Move entry to different project (CLIENT-A → CLIENT-B)
- [ ] Move and change task
- [ ] Move with tag revalidation

### Delete Operations
- [ ] Delete NOT_REPORTED entry
- [ ] Attempt delete SUBMITTED entry (should fail)

### Submit Operations
- [ ] Submit NOT_REPORTED entry
- [ ] Verify status changed to SUBMITTED
- [ ] Attempt submit already-SUBMITTED entry (should be idempotent)

### Error Handling
- [ ] Invalid project code error
- [ ] Invalid task error
- [ ] Invalid tags error
- [ ] Update submitted entry error
- [ ] Delete submitted entry error

## Database Verification

### After Each Operation
- [ ] Entry exists in database
- [ ] Fields match expected values
- [ ] Status is correct
- [ ] Timestamps updated (created_at, updated_at)

### Final State
- [ ] All entries accounted for
- [ ] No orphaned entries
- [ ] Status workflow followed correctly

## Natural Language Variations

- [ ] "Log X hours on Y" works
- [ ] "Track X hours, Y project" works
- [ ] "Create time entry: X, Y, Z" works
- [ ] "Show my entries for today" works
- [ ] "What did I log this week?" works
- [ ] "Update entry <id> to X hours" works
- [ ] "Move entry <id> to project Y" works

## Cleanup

- [ ] Test entries cleaned up
- [ ] Database in known state
- [ ] No test data pollution

---

**Pass Criteria:** All checkboxes ticked ✅

**Time to Complete:** ~45-60 minutes

**Next:** Run auto-tracking workflow test
```

---

## Testing

### Execute Manual Workflow Test

1. **Start the stack:**
   ```bash
   /deploy
   ```

2. **Verify setup:**
   ```bash
   ./tests/integration/verify-mcp-connection.sh
   ```

3. **Follow the workflow guide:**
   - Open `docs/workflows/MANUAL-TIME-LOGGING.md`
   - Execute each step in Claude Code
   - Verify database state after each step
   - Check off items in validation checklist

4. **Expected Duration:** 45-60 minutes

5. **Success Criteria:**
   - All 7 workflow steps completed successfully
   - All database verifications pass
   - All edge case errors handled correctly
   - Validation checklist 100% complete

---

## Related Files

**Created:**
- `docs/workflows/MANUAL-TIME-LOGGING.md` - Complete workflow guide
- `docs/workflows/WORKFLOW-VALIDATION-CHECKLIST.md` - Validation checklist

**Referenced:**
- `tests/integration/verify-mcp-connection.sh` - Connection verification
- `tests/e2e/setup-test-data.sh` - Test data setup
- `tests/e2e/teardown-test-data.sh` - Test data cleanup

---

## Definition of Done

- [ ] Workflow guide created with all 6 steps documented
- [ ] Natural language variations documented
- [ ] Edge cases and error scenarios included
- [ ] Validation checklist created
- [ ] Workflow executed manually end-to-end at least once
- [ ] All database verifications pass
- [ ] Common patterns and best practices documented
- [ ] Troubleshooting section complete

---

## Next Steps

After completing this task:
1. Proceed to **Task 11.4: Auto-tracking Test** to test intelligent time tracking suggestions
2. Proceed to **Task 11.5: Migration Workflow Test** to test project migration scenarios
3. Document any additional patterns discovered during testing

---

## Resources

- [MCP Tools Specification](../../prd/mcp-tools.md)
- [API Specification](../../prd/api-specification.md)
- [E2E Test Scenarios](../e2e/scenarios/)
- [Data Model](../../prd/data-model.md)
