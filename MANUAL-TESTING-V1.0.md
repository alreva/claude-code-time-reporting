# Manual Testing Guide - v1.0 Complete System

**Version:** 1.0 Production Ready
**Last Updated:** October 29, 2025
**Status:** All 61 tasks complete ✅

This guide covers manual testing of the complete Time Reporting System including all GraphQL queries, mutations, and workflows.

---

## 🚀 Quick Start

### Option 1: Docker (Production-like)

```bash
# Start full stack
/deploy

# Use production token
Bearer YOUR_Authentication__BearerToken_HERE
```

**GraphQL Playground:** http://localhost:5001/graphql

### Option 2: Local Development

```bash
# Start database only
/db-start

# Start API locally (in another terminal)
/run-api

# Use development token
Bearer dev-token-12345-for-local-testing
```

**GraphQL Playground:** http://localhost:5001/graphql

---

## 📋 Environment Tokens

| Environment | Token | Source |
|-------------|-------|--------|
| **Development** | `dev-token-12345-for-local-testing` | `appsettings.Development.json` |
| **Production (Docker)** | `YOUR_Authentication__BearerToken_HERE` | `.env` file |

---

## 🧪 Test Scenarios

### 1. Health Check (No Auth Required)

```bash
curl http://localhost:5001/health
```

**Expected:** `Healthy`

---

### 2. Authentication Tests

#### Test 2.1: No Token (Should Fail)

```graphql
query {
  projects {
    code
    name
  }
}
```

**Expected:** `401 Unauthorized - Missing Authorization header`

#### Test 2.2: Invalid Token (Should Fail)

Add header: `Authorization: Bearer invalid-token-123`

**Expected:** `401 Unauthorized - Invalid or missing bearer token`

#### Test 2.3: Valid Token (Should Work)

Add header: `Authorization: Bearer dev-token-12345-for-local-testing`
(or production token if using Docker)

**Expected:** Query executes successfully

---

### 3. Query Tests

#### Test 3.1: List All Projects

```graphql
query {
  projects {
    code
    name
    isActive
    projectTasks {
      taskName
      isActive
    }
    projectTags {
      tagName
      tagValues {
        value
      }
    }
  }
}
```

**Expected:**
- Returns INTERNAL, CLIENT-A, CLIENT-B projects
- Each with their tasks and tag configurations
- Should see seed data from Phase 1

#### Test 3.2: Get Single Project by Code

```graphql
query {
  project(code: "INTERNAL") {
    code
    name
    projectTasks {
      taskName
    }
    projectTags {
      tagName
      tagValues {
        value
      }
    }
  }
}
```

**Expected:**
- Returns INTERNAL project details
- Shows Development, Bug Fixing, Code Review tasks
- Shows Environment and Priority tags with values

#### Test 3.3: List Time Entries (Empty Initially)

```graphql
query {
  timeEntries {
    id
    project {
      code
      name
    }
    projectTask {
      taskName
    }
    description
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    tags {
      tagValue {
        projectTag {
          tagName
        }
        value
      }
    }
  }
}
```

**Expected:**
- Returns empty array initially (no time entries yet)
- After creating entries, will return them with full details

#### Test 3.4: Filter Projects (Active Only)

```graphql
query {
  projects(where: { isActive: { eq: true } }) {
    code
    name
    isActive
  }
}
```

**Expected:** Returns only active projects

---

### 4. Mutation Tests - Create Time Entry

#### Test 4.1: Create Simple Time Entry

```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    projectTaskId: "TASK_ID_HERE"  # Get from Test 3.2
    description: "Implemented new feature"
    standardHours: 8.0
    overtimeHours: 0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
  }) {
    id
    project { code }
    projectTask { taskName }
    description
    standardHours
    status
    createdAt
  }
}
```

**Expected:**
- Returns created time entry
- Status is `NOT_REPORTED`
- All fields populated correctly

**Note:** You need to get a valid `projectTaskId` first. Run this query:

```graphql
query {
  project(code: "INTERNAL") {
    projectTasks {
      id
      taskName
    }
  }
}
```

Copy one of the task IDs and use it in the `logTime` mutation.

#### Test 4.2: Create Entry with Tags

```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    projectTaskId: "TASK_ID_HERE"
    description: "Fixed production bug"
    standardHours: 6.0
    overtimeHours: 2.0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
    tags: [
      {
        projectTagId: "TAG_ID_ENV"     # Get from Test 3.2
        tagValueId: "VALUE_ID_PROD"    # Get from Test 3.2
      },
      {
        projectTagId: "TAG_ID_PRIORITY"
        tagValueId: "VALUE_ID_HIGH"
      }
    ]
  }) {
    id
    description
    tags {
      tagValue {
        projectTag { tagName }
        value
      }
    }
  }
}
```

**Expected:**
- Entry created with 6 standard + 2 overtime hours
- Tags properly associated (Environment: Production, Priority: High)

**To get tag/value IDs:**

```graphql
query {
  project(code: "INTERNAL") {
    projectTags {
      id
      tagName
      tagValues {
        id
        value
      }
    }
  }
}
```

#### Test 4.3: Create Entry - Validation Errors

**Test invalid project:**

```graphql
mutation {
  logTime(input: {
    projectCode: "INVALID"
    projectTaskId: "some-id"
    description: "Test"
    standardHours: 8.0
    overtimeHours: 0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
  }) {
    id
  }
}
```

**Expected:** Error - Project not found or inactive

**Test negative hours:**

```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    projectTaskId: "TASK_ID_HERE"
    description: "Test"
    standardHours: -5.0  # Invalid!
    overtimeHours: 0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
  }) {
    id
  }
}
```

**Expected:** Validation error - Hours must be non-negative

**Test invalid date range:**

```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    projectTaskId: "TASK_ID_HERE"
    description: "Test"
    standardHours: 8.0
    overtimeHours: 0
    startDate: "2025-10-30"
    completionDate: "2025-10-29"  # Before start date!
  }) {
    id
  }
}
```

**Expected:** Validation error - Completion date must be after start date

---

### 5. Mutation Tests - Update Time Entry

#### Test 5.1: Update Description and Hours

First, create an entry and get its ID. Then:

```graphql
mutation {
  updateTimeEntry(
    id: "ENTRY_ID_HERE"
    input: {
      description: "Updated description with more details"
      standardHours: 7.5
      overtimeHours: 0.5
    }
  ) {
    id
    description
    standardHours
    overtimeHours
    updatedAt
  }
}
```

**Expected:**
- Entry updated with new values
- `updatedAt` timestamp changed

#### Test 5.2: Update Tags Only

```graphql
mutation {
  updateTags(
    timeEntryId: "ENTRY_ID_HERE"
    tags: [
      {
        projectTagId: "TAG_ID_ENV"
        tagValueId: "VALUE_ID_DEV"  # Change from Production to Development
      }
    ]
  ) {
    id
    tags {
      tagValue {
        projectTag { tagName }
        value
      }
    }
  }
}
```

**Expected:** Tags updated to new values

#### Test 5.3: Move Task to Different Project

```graphql
mutation {
  moveTaskToProject(
    timeEntryId: "ENTRY_ID_HERE"
    newProjectCode: "CLIENT-A"
    newProjectTaskId: "NEW_TASK_ID"  # Must be from CLIENT-A project
  ) {
    id
    project { code name }
    projectTask { taskName }
    tags {
      tagValue {
        projectTag { tagName }
        value
      }
    }
  }
}
```

**Expected:**
- Entry moved to CLIENT-A project
- Task updated to valid CLIENT-A task
- Tags revalidated against CLIENT-A's tag configuration

#### Test 5.4: Update Submitted Entry (Should Fail)

First, submit an entry:

```graphql
mutation {
  submitTimeEntry(id: "ENTRY_ID_HERE") {
    id
    status
  }
}
```

Then try to update it:

```graphql
mutation {
  updateTimeEntry(
    id: "ENTRY_ID_HERE"
    input: {
      description: "Try to modify submitted entry"
    }
  ) {
    id
  }
}
```

**Expected:** Error - Cannot modify entry in SUBMITTED status

---

### 6. Mutation Tests - Status Workflow

#### Test 6.1: Submit Time Entry

```graphql
mutation {
  submitTimeEntry(id: "ENTRY_ID_HERE") {
    id
    status
    updatedAt
  }
}
```

**Expected:**
- Status changes from `NOT_REPORTED` to `SUBMITTED`
- Entry becomes read-only

#### Test 6.2: Approve Time Entry

```graphql
mutation {
  approveTimeEntry(id: "ENTRY_ID_HERE") {
    id
    status
    updatedAt
  }
}
```

**Expected:**
- Status changes from `SUBMITTED` to `APPROVED`
- Entry becomes permanently read-only

#### Test 6.3: Decline Time Entry

```graphql
mutation {
  declineTimeEntry(
    id: "ENTRY_ID_HERE"
    comment: "Please add more details to the description"
  ) {
    id
    status
    updatedAt
  }
}
```

**Expected:**
- Status changes from `SUBMITTED` to `DECLINED`
- Entry can now be edited and resubmitted

#### Test 6.4: Invalid Status Transitions

**Try to approve a NOT_REPORTED entry:**

```graphql
mutation {
  approveTimeEntry(id: "NOT_SUBMITTED_ENTRY_ID") {
    id
  }
}
```

**Expected:** Error - Can only approve SUBMITTED entries

---

### 7. Mutation Tests - Delete Time Entry

#### Test 7.1: Delete NOT_REPORTED Entry

```graphql
mutation {
  deleteTimeEntry(id: "ENTRY_ID_HERE") {
    success
    message
  }
}
```

**Expected:**
- `success: true`
- Entry deleted from database

#### Test 7.2: Delete SUBMITTED Entry (Should Fail)

```graphql
mutation {
  deleteTimeEntry(id: "SUBMITTED_ENTRY_ID") {
    success
  }
}
```

**Expected:** Error - Can only delete NOT_REPORTED or DECLINED entries

#### Test 7.3: Delete APPROVED Entry (Should Fail)

```graphql
mutation {
  deleteTimeEntry(id: "APPROVED_ENTRY_ID") {
    success
  }
}
```

**Expected:** Error - Cannot delete approved entries

---

### 8. Complex Query Tests

#### Test 8.1: Filter Time Entries by Date Range

```graphql
query {
  timeEntries(where: {
    and: [
      { startDate: { gte: "2025-10-01" } }
      { startDate: { lte: "2025-10-31" } }
    ]
  }) {
    id
    description
    startDate
    completionDate
  }
}
```

**Expected:** Returns entries within October 2025

#### Test 8.2: Filter by Project and Status

```graphql
query {
  timeEntries(where: {
    and: [
      { project: { code: { eq: "INTERNAL" } } }
      { status: { eq: SUBMITTED } }
    ]
  }) {
    id
    project { code }
    status
    description
  }
}
```

**Expected:** Returns only INTERNAL project entries with SUBMITTED status

#### Test 8.3: Get Single Entry with Full Details

```graphql
query {
  timeEntry(id: "ENTRY_ID_HERE") {
    id
    project {
      code
      name
    }
    projectTask {
      id
      taskName
    }
    description
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    tags {
      id
      tagValue {
        id
        value
        projectTag {
          id
          tagName
        }
      }
    }
    createdAt
    updatedAt
  }
}
```

**Expected:** Returns complete entry with all nested relationships

---

## 🔄 Complete Workflow Test

Test the full lifecycle of a time entry:

### Step 1: Create Entry

```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    projectTaskId: "TASK_ID"
    description: "Complete workflow test"
    standardHours: 8.0
    overtimeHours: 0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
  }) {
    id
    status
  }
}
```

Save the returned `id` for next steps.

### Step 2: Update Entry

```graphql
mutation {
  updateTimeEntry(
    id: "SAVED_ID"
    input: {
      description: "Complete workflow test - updated"
      standardHours: 7.0
    }
  ) {
    id
    description
    standardHours
    status
  }
}
```

### Step 3: Add Tags

```graphql
mutation {
  updateTags(
    timeEntryId: "SAVED_ID"
    tags: [{
      projectTagId: "TAG_ID"
      tagValueId: "VALUE_ID"
    }]
  ) {
    id
    tags {
      tagValue {
        projectTag { tagName }
        value
      }
    }
  }
}
```

### Step 4: Submit for Approval

```graphql
mutation {
  submitTimeEntry(id: "SAVED_ID") {
    id
    status
  }
}
```

### Step 5: Try to Modify (Should Fail)

```graphql
mutation {
  updateTimeEntry(
    id: "SAVED_ID"
    input: { description: "Try to modify" }
  ) {
    id
  }
}
```

### Step 6: Approve Entry

```graphql
mutation {
  approveTimeEntry(id: "SAVED_ID") {
    id
    status
  }
}
```

### Step 7: Query Final State

```graphql
query {
  timeEntry(id: "SAVED_ID") {
    id
    description
    standardHours
    status
    tags {
      tagValue {
        projectTag { tagName }
        value
      }
    }
    createdAt
    updatedAt
  }
}
```

**Expected Final State:**
- Status: `APPROVED`
- All fields preserved
- Entry is read-only
- Cannot be modified or deleted

---

## 🐛 Error Handling Tests

### Test: Invalid GraphQL Syntax

```graphql
query {
  projects {
    code
    invalid_field  # This field doesn't exist
  }
}
```

**Expected:** GraphQL schema validation error

### Test: Missing Required Fields

```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    # Missing projectTaskId - required!
    description: "Test"
    standardHours: 8.0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
  }) {
    id
  }
}
```

**Expected:** Validation error - Required field missing

### Test: Invalid Enum Value

```graphql
query {
  timeEntries(where: {
    status: { eq: INVALID_STATUS }
  }) {
    id
  }
}
```

**Expected:** GraphQL validation error - Invalid enum value

---

## 🔍 Database Verification

While API is running, open database:

```bash
/db-psql
```

### Verify Data Integrity

```sql
-- Check time entries exist
SELECT count(*) FROM time_entries;

-- Verify foreign key constraints
SELECT te.id, te.project_code, p.name as project_name
FROM time_entries te
JOIN projects p ON te.project_code = p.code;

-- Verify tag relationships
SELECT te.id, te.description, tv.value, pt.tag_name
FROM time_entries te
JOIN time_entry_tags tet ON te.id = tet.time_entry_id
JOIN tag_values tv ON tet.tag_value_id = tv.id
JOIN project_tags pt ON tv.project_tag_id = pt.id;

-- Verify status workflow
SELECT status, count(*)
FROM time_entries
GROUP BY status
ORDER BY status;
```

### Test Database Constraints

```sql
-- Try to insert invalid data (should fail)
INSERT INTO time_entries (
  id, project_code, project_task_id,
  standard_hours, overtime_hours,
  start_date, completion_date,
  status, created_at, updated_at
)
VALUES (
  gen_random_uuid(), 'INTERNAL', 'some-task-id',
  -5, 0,  -- Negative hours (should fail)
  CURRENT_DATE, CURRENT_DATE,
  'NOT_REPORTED', NOW(), NOW()
);
-- Expected: ERROR: violates check constraint
```

Exit psql: `\q`

---

## 🛑 Cleanup

```bash
# Stop API
/stop-api

# Stop database
/db-stop

# Or stop entire stack
podman compose down
```

---

## 📊 Test Coverage Summary

| Category | Features | Status |
|----------|----------|--------|
| **Authentication** | Bearer token auth, health check | ✅ Complete |
| **Queries** | projects, project, timeEntries, timeEntry | ✅ Complete |
| **Mutations - Create** | logTime with validation | ✅ Complete |
| **Mutations - Update** | updateTimeEntry, updateTags, moveTaskToProject | ✅ Complete |
| **Mutations - Delete** | deleteTimeEntry with status check | ✅ Complete |
| **Mutations - Workflow** | submitTimeEntry, approveTimeEntry, declineTimeEntry | ✅ Complete |
| **Validation** | Project, task, tag, date, hours validation | ✅ Complete |
| **Status Workflow** | NOT_REPORTED → SUBMITTED → APPROVED/DECLINED | ✅ Complete |
| **Database** | Constraints, foreign keys, indexes | ✅ Complete |
| **Error Handling** | GraphQL errors, validation errors | ✅ Complete |

---

## 📝 Notes

- **All 61 tasks completed** - Full v1.0 feature set
- **TDD approach** - All features have passing unit/integration tests
- **Production ready** - Docker deployment with health checks
- **Security** - Bearer token authentication on all endpoints
- **Data integrity** - Multi-layer validation (GraphQL, business logic, database)

---

## 🚀 What's Next?

**v2.0 Features (Future):**
- Phase 10: Auto-tracking based on user activity
- Advanced reporting and analytics
- User management and permissions
- Bulk operations
- Export to Excel/PDF

---

**Happy Testing! 🎉**
