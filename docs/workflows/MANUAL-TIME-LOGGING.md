# Manual Time Logging Workflow Guide

## Overview

This guide demonstrates a complete manual time logging workflow using Claude Code and the Time Reporting System.

**Use Case:** A developer tracking a full workday of coding activities

---

## Prerequisites

**Before starting:**
- [ ] GraphQL API running (`/deploy` or `podman compose up -d`)
- [ ] Claude Code configured with time-reporting MCP server
- [ ] Projects seeded in database (`/seed-db`)
- [ ] You have a valid Azure AD token configured

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
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT te.id, te.project_code, pt.task_name, te.standard_hours, te.start_date, te.status
FROM time_entries te
JOIN project_tasks pt ON te.project_task_id = pt.id
ORDER BY te.created_at DESC LIMIT 1;"
```

**Expected:**
```
id                                   | project_code | task_name   | standard_hours | start_date | status
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
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT COUNT(*) FROM time_entries WHERE start_date = CURRENT_DATE;"
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
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT standard_hours, updated_at > created_at AS was_updated
FROM time_entries
WHERE id = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';"
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
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT COUNT(*) FROM time_entries WHERE start_date = CURRENT_DATE;"
```

**Expected:** `2`

---

### Step 5: Evening - Move Task to Different Project (5:30 PM)

**Context:** You realize the bug fixing work should have been logged under INTERNAL, not CLIENT-A.

**User Prompt:**
```
"Move entry b2c3d4e5-f6g7-8901-bcde-fg2345678901 to INTERNAL project, keep the task as bug fixing"
```

**Alternative Phrasings:**
- "Change entry b2c3... from CLIENT-A to INTERNAL"
- "Move entry b2c3... to INTERNAL"
- "Reassign entry b2c3... to INTERNAL project"

**Expected Response:**
```
✅ Time entry moved successfully!

Entry ID: b2c3d4e5-f6g7-8901-bcde-fg2345678901
Previous: CLIENT-A / Bug Fixing
Current: INTERNAL / Bug Fixing
Hours: 4.0
Status: NOT_REPORTED
```

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT te.project_code, pt.task_name
FROM time_entries te
JOIN project_tasks pt ON te.project_task_id = pt.id
WHERE te.id = 'b2c3d4e5-f6g7-8901-bcde-fg2345678901';"
```

**Expected:**
```
project_code | task_name
-------------|----------
INTERNAL     | Bug Fixing
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
   Project: INTERNAL
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
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT id, project_code, status
FROM time_entries
WHERE start_date = CURRENT_DATE
ORDER BY created_at;"
```

**Expected:**
```
id                                   | project_code | status
-------------------------------------|--------------|----------
a1b2c3d4-e5f6-7890-abcd-ef1234567890 | INTERNAL     | SUBMITTED
b2c3d4e5-f6g7-8901-bcde-fg2345678901 | INTERNAL     | SUBMITTED
```

---

## Workflow Summary

**Total Operations Performed:**
1. ✅ Created time entry (INTERNAL, Development, 4h)
2. ✅ Queried entries for today
3. ✅ Updated entry hours (4h → 4.5h)
4. ✅ Created second entry (CLIENT-A, Bug Fixing, 4h)
5. ✅ Moved entry to different project (CLIENT-A → INTERNAL)
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
- Architecture
- Development
- Code Review
- Testing
- Documentation
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
1. Check `env.sh` file: `Azure AD authentication (az login)=...`
2. Ensure you sourced env.sh: `source env.sh`
3. Check Claude Code config: `~/.config/claude-code/config.json`
4. Restart API: `/deploy`
5. Restart Claude Code

### Database State Issues

**Reset Test Data:**
```bash
./tests/e2e/teardown-test-data.sh
./tests/e2e/setup-test-data.sh
```

**Manual Cleanup:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE user_id = 'test-user';"
```

---

## Next Steps

After completing this workflow:
- Try the **Auto-tracking Workflow** (AUTO-TRACKING-TEST.md) for intelligent time suggestions
- Test the **Migration Workflow** (MIGRATION-WORKFLOW-TEST.md) for moving tasks between projects
- Review **E2E Test Scenarios** (../../tests/e2e/README.md) for comprehensive testing

---

**Congratulations!** You've completed a full day of manual time logging through Claude Code!
