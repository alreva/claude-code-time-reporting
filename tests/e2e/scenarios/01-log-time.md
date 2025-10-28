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
