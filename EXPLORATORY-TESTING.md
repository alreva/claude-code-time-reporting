# Exploratory Testing Guide

Your Time Reporting System is now running and ready for testing!

## 🚀 Quick Access

- **GraphQL Playground:** http://localhost:5001/graphql
- **Health Check:** http://localhost:5001/health
- **Bearer Token:** `YOUR_Authentication__BearerToken_HERE`

## 📋 Pre-Seeded Test Data

The database is already seeded with test data:

### Projects
- **INTERNAL** - Internal Development
- **CLIENT-A** - Client A Project
- **MAINT** - Maintenance & Support

### Tasks per Project
Each project has:
- Development
- Bug Fixing
- Code Review
- Testing
- Documentation

### Tag Configurations
- **priority**: Required, values: Low, Medium, High, Critical
- **sprint**: Optional, values: Sprint-1 through Sprint-10
- **feature**: Optional, values: Auth, Reports, Dashboard, Settings, API

## 🧪 Testing Scenarios

### 1. Query Available Projects

**GraphQL Query:**
```graphql
{
  projects {
    code
    name
    isActive
  }
}
```

**cURL:**
```bash
curl -X POST http://localhost:5001/graphql \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_Authentication__BearerToken_HERE' \
  -d '{"query":"{ projects { code name isActive } }"}'
```

### 2. Get Project Details with Tasks and Tags

**GraphQL Query:**
```graphql
{
  project(code: "INTERNAL") {
    code
    name
    isActive
    tasks {
      id
      name
      isActive
    }
    tagConfigurations {
      id
      name
      isRequired
      allowedValues {
        id
        value
      }
    }
  }
}
```

**cURL:**
```bash
curl -X POST http://localhost:5001/graphql \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_Authentication__BearerToken_HERE' \
  -d '{"query":"{ project(code: \"INTERNAL\") { code name tasks { name } tagConfigurations { name isRequired allowedValues { value } } } }"}'
```

### 3. Log Time Entry

**GraphQL Mutation:**
```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 8
    overtimeHours: 0
    startDate: "2024-01-15"
    completionDate: "2024-01-15"
    tags: [
      { name: "priority", value: "High" }
      { name: "sprint", value: "Sprint-1" }
      { name: "feature", value: "API" }
    ]
    comment: "Implemented GraphQL mutations"
  }) {
    id
    projectCode
    task
    standardHours
    overtimeHours
    status
    startDate
    completionDate
    tags {
      name
      value
    }
    comment
  }
}
```

**cURL:**
```bash
curl -X POST http://localhost:5001/graphql \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_Authentication__BearerToken_HERE' \
  -d '{"query":"mutation { logTime(input: { projectCode: \"INTERNAL\", task: \"Development\", standardHours: 8, startDate: \"2024-01-15\", completionDate: \"2024-01-15\", tags: [{ name: \"priority\", value: \"High\" }] }) { id projectCode task standardHours status } }"}'
```

### 4. Query Time Entries with Filters

**GraphQL Query:**
```graphql
{
  timeEntries(
    projectCode: "INTERNAL"
    status: NOT_REPORTED
    limit: 10
  ) {
    id
    projectCode
    task
    standardHours
    overtimeHours
    status
    startDate
    completionDate
    tags {
      name
      value
    }
    comment
  }
}
```

**cURL:**
```bash
curl -X POST http://localhost:5001/graphql \
  -H 'Content-Type: application/json' \
  -H 'Authorization: Bearer YOUR_Authentication__BearerToken_HERE' \
  -d '{"query":"{ timeEntries(projectCode: \"INTERNAL\", limit: 10) { id projectCode task standardHours status } }"}'
```

### 5. Update Time Entry

**GraphQL Mutation:**
```graphql
mutation {
  updateTimeEntry(
    id: "YOUR_ENTRY_ID_HERE"
    input: {
      standardHours: 6
      overtimeHours: 2
      comment: "Updated hours breakdown"
    }
  ) {
    id
    standardHours
    overtimeHours
    comment
  }
}
```

### 6. Move Task to Different Project

**GraphQL Mutation:**
```graphql
mutation {
  moveTaskToProject(
    entryId: "YOUR_ENTRY_ID_HERE"
    newProjectCode: "CLIENT-A"
    newTask: "Bug Fixing"
  ) {
    id
    projectCode
    task
    status
  }
}
```

### 7. Update Tags

**GraphQL Mutation:**
```graphql
mutation {
  updateTags(
    entryId: "YOUR_ENTRY_ID_HERE"
    tags: [
      { name: "priority", value: "Critical" }
      { name: "sprint", value: "Sprint-2" }
    ]
  ) {
    id
    tags {
      name
      value
    }
  }
}
```

### 8. Complete Approval Workflow

**Step 1: Submit for Approval**
```graphql
mutation {
  submitTimeEntry(id: "YOUR_ENTRY_ID_HERE") {
    id
    status
  }
}
```

**Step 2: Approve (or Decline)**
```graphql
mutation {
  approveTimeEntry(id: "YOUR_ENTRY_ID_HERE") {
    id
    status
  }
}
```

**Or Decline:**
```graphql
mutation {
  declineTimeEntry(
    id: "YOUR_ENTRY_ID_HERE"
    comment: "Missing required tags"
  ) {
    id
    status
    declineComment
  }
}
```

### 9. Delete Time Entry

**GraphQL Mutation:**
```graphql
mutation {
  deleteTimeEntry(id: "YOUR_ENTRY_ID_HERE") {
    id
    status
  }
}
```

## 🔍 Testing Tips

### Using GraphQL Playground (Recommended)

1. Open http://localhost:5001/graphql in your browser
2. Click the "HTTP HEADERS" tab at bottom
3. Add authentication header:
   ```json
   {
     "Authorization": "Bearer YOUR_Authentication__BearerToken_HERE"
   }
   ```
4. Use the left panel to write queries/mutations
5. Click the "Play" button to execute
6. Use the right panel's "Docs" or "Schema" tabs to explore the API

### Testing Authentication

**Without Bearer Token (should fail):**
```bash
curl -X POST http://localhost:5001/graphql \
  -H 'Content-Type: application/json' \
  -d '{"query":"{ projects { code } }"}'
```

Expected: `Invalid Authorization header format` or `401 Unauthorized`

### Testing Validation

**Invalid Project Code:**
```graphql
mutation {
  logTime(input: {
    projectCode: "INVALID"
    task: "Development"
    standardHours: 8
    startDate: "2024-01-15"
    completionDate: "2024-01-15"
  }) {
    id
  }
}
```

Expected: Error message about invalid project

**Invalid Task:**
```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "InvalidTask"
    standardHours: 8
    startDate: "2024-01-15"
    completionDate: "2024-01-15"
  }) {
    id
  }
}
```

Expected: Error message about invalid task for project

**Invalid Tag:**
```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 8
    startDate: "2024-01-15"
    completionDate: "2024-01-15"
    tags: [
      { name: "priority", value: "InvalidValue" }
    ]
  }) {
    id
  }
}
```

Expected: Error message about invalid tag value

**Missing Required Tag:**
```graphql
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 8
    startDate: "2024-01-15"
    completionDate: "2024-01-15"
  }) {
    id
  }
}
```

Expected: Error about missing required "priority" tag

### Testing Status Workflow

**Try to update approved entry (should fail):**
1. Create entry
2. Submit it
3. Approve it
4. Try to update it → Should fail with error

**Try to delete submitted entry (should fail):**
1. Create entry
2. Submit it
3. Try to delete it → Should fail with error

## 🛠️ Useful Commands

**View API Logs:**
```bash
podman compose logs -f api
```

**View Database Logs:**
```bash
podman compose logs -f postgres
```

**Check Service Status:**
```bash
podman compose ps
```

**Restart Services:**
```bash
podman compose restart api
podman compose restart postgres
```

**Connect to Database:**
```bash
podman exec -it time-reporting-db psql -U postgres -d time_reporting
```

**View Tables:**
```sql
\dt
```

**Query Time Entries:**
```sql
SELECT id, project_code, task, standard_hours, status, created_at
FROM time_entries
ORDER BY created_at DESC
LIMIT 10;
```

**View All Projects:**
```sql
SELECT code, name, is_active FROM projects;
```

## 📊 Expected Behavior

### Status Transitions
- **NOT_REPORTED** → Can update, delete, submit
- **SUBMITTED** → Can only approve or decline (read-only)
- **APPROVED** → Cannot modify (terminal state)
- **DECLINED** → Can update and resubmit

### Validation Rules
- Project must exist and be active
- Task must be in project's available tasks and be active
- Tags must match project's tag configurations
- Tag values must be in allowed values list
- Required tags must be present
- Standard hours >= 0, Overtime hours >= 0
- Start date <= Completion date

## 🐛 Known Issues / Notes

- Health check detection in integration test script has minor issues with Podman format
- All functional tests pass when run manually
- GraphQL Playground is the easiest way to explore the API

## 📚 Additional Resources

- **PRD:** `docs/prd/README.md`
- **API Specification:** `docs/prd/api-specification.md`
- **Data Model:** `docs/prd/data-model.md`
- **Environment Config:** `docs/ENVIRONMENT.md`

---

Happy Testing! 🎉
