# GraphQL API Documentation

**Time Reporting System API**
Version: 1.0

---

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Queries](#queries)
4. [Mutations](#mutations)
5. [Types](#types)
6. [Error Handling](#error-handling)
7. [Examples](#examples)

---

## Overview

The Time Reporting API is a GraphQL API built with ASP.NET Core and HotChocolate. It provides comprehensive time tracking functionality with project management, tag configuration, and approval workflows.

**Base URL:** `http://localhost:5000/graphql`
**GraphQL Playground:** `http://localhost:5000/graphql` (when running in development mode)

### Key Features

- Time entry CRUD operations with validation
- Project and task management
- Flexible tag system for metadata
- Approval workflow (NOT_REPORTED → SUBMITTED → APPROVED/DECLINED)
- Advanced filtering, sorting, and pagination
- Bearer token authentication

---

## Authentication

All API requests require Bearer token authentication.

### Request Header

```http
Authorization: Bearer <your-token-here>
```

### Configuration

Set the `BEARER_TOKEN` environment variable in your `.env` file:

```bash
BEARER_TOKEN=your-secure-token-here
```

Generate a secure token:

```bash
openssl rand -base64 32
```

### Unauthorized Response

If authentication fails, you'll receive a `401 Unauthorized` response.

---

## Queries

### timeEntries

Get time entries with filtering, sorting, and pagination.

**Signature:**
```graphql
timeEntries(
  where: TimeEntryFilterInput
  order: [TimeEntrySortInput!]
  first: Int
  after: String
): TimeEntriesConnection
```

**Features:**
- Pagination (default: 50 items, max: 200)
- Filtering by any field (project, status, dates, user, etc.)
- Sorting by any field
- Field projection (request only the fields you need)

**Example:**
```graphql
query GetMyTimeEntries {
  timeEntries(
    where: {
      status: { eq: NOT_REPORTED }
      startDate: { gte: "2025-10-01" }
    }
    order: { startDate: DESC }
    first: 10
  ) {
    nodes {
      id
      project {
        code
        name
      }
      projectTask {
        taskName
      }
      standardHours
      startDate
      status
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

**Filter Options:**
- `projectCode`: Filter by project (eq, neq, in, nin, contains, startsWith, endsWith)
- `status`: Filter by status (eq, neq, in, nin)
- `startDate`: Filter by start date (eq, neq, gt, gte, lt, lte)
- `completionDate`: Filter by completion date (eq, neq, gt, gte, lt, lte)
- `standardHours`: Filter by hours (eq, neq, gt, gte, lt, lte)
- `issueId`: Filter by issue ID (eq, neq, in, nin, contains, startsWith, endsWith)
- Combine with `and`, `or`, `not` operators

---

### timeEntry

Get a single time entry by ID.

**Signature:**
```graphql
timeEntry(id: UUID!): TimeEntry
```

**Example:**
```graphql
query GetTimeEntry {
  timeEntry(id: "123e4567-e89b-12d3-a456-426614174000") {
    id
    project {
      code
      name
    }
    projectTask {
      taskName
    }
    standardHours
    overtimeHours
    description
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
    createdAt
    updatedAt
  }
}
```

**Returns:** `TimeEntry` object or `null` if not found.

---

### projects

Get all projects with filtering, sorting, and projection capabilities.

**Signature:**
```graphql
projects(
  where: ProjectFilterInput
  order: [ProjectSortInput!]
): [Project!]!
```

**Example:**
```graphql
query GetActiveProjects {
  projects(
    where: { isActive: { eq: true } }
    order: { code: ASC }
  ) {
    code
    name
    isActive
  }
}
```

**Filter Options:**
- `code`: Filter by project code (eq, neq, in, nin, contains, startsWith, endsWith)
- `name`: Filter by project name (eq, neq, in, nin, contains, startsWith, endsWith)
- `isActive`: Filter by active status (eq)

---

### project

Get a single project by code with all navigation properties (tasks and tags).

**Signature:**
```graphql
project(code: String!): Project
```

**Example:**
```graphql
query GetProjectDetails {
  project(code: "INTERNAL") {
    code
    name
    isActive
    availableTasks {
      taskName
      isActive
    }
    tags {
      tagName
      isRequired
      allowedValues {
        value
      }
    }
  }
}
```

**Returns:** `Project` object with all related data or `null` if not found.

---

## Mutations

### logTime

Create a new time entry with validation.

**Signature:**
```graphql
logTime(input: LogTimeInput!): TimeEntry!
```

**Input:**
```graphql
input LogTimeInput {
  projectCode: String!
  task: String!
  issueId: String
  standardHours: Decimal!
  overtimeHours: Decimal
  description: String
  startDate: Date!
  completionDate: Date!
  tags: [TagInput!]
}

input TagInput {
  name: String!
  value: String!
}
```

**Example:**
```graphql
mutation CreateTimeEntry {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 8.0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
    description: "Implemented Phase 12 documentation"
    tags: [
      { name: "Type", value: "Backend" }
      { name: "Priority", value: "High" }
    ]
  }) {
    id
    status
    createdAt
  }
}
```

**Validation:**
- Project must exist and be active
- Task must be in project's available tasks and active
- Tags must match project's tag configurations
- `startDate` ≤ `completionDate`
- `standardHours` ≥ 0, `overtimeHours` ≥ 0
- All tag values must be in allowed values list

**Returns:** Newly created `TimeEntry` with status `NOT_REPORTED`.

---

### updateTimeEntry

Update an existing time entry (only allowed for NOT_REPORTED or DECLINED status).

**Signature:**
```graphql
updateTimeEntry(
  id: UUID!
  input: UpdateTimeEntryInput!
): TimeEntry!
```

**Input:**
```graphql
input UpdateTimeEntryInput {
  task: String
  issueId: String
  standardHours: Decimal
  overtimeHours: Decimal
  description: String
  startDate: Date
  completionDate: Date
  tags: [TagInput!]
}
```

**Example:**
```graphql
mutation UpdateEntry {
  updateTimeEntry(
    id: "123e4567-e89b-12d3-a456-426614174000"
    input: {
      standardHours: 9.5
      description: "Updated with additional work"
    }
  ) {
    id
    standardHours
    description
    updatedAt
  }
}
```

**Restrictions:**
- Only `NOT_REPORTED` or `DECLINED` entries can be updated
- `SUBMITTED` entries are read-only until approved/declined
- `APPROVED` entries are immutable

---

### deleteTimeEntry

Delete a time entry (only allowed for NOT_REPORTED or DECLINED status).

**Signature:**
```graphql
deleteTimeEntry(id: UUID!): Boolean!
```

**Example:**
```graphql
mutation DeleteEntry {
  deleteTimeEntry(id: "123e4567-e89b-12d3-a456-426614174000")
}
```

**Returns:** `true` if deleted successfully.

**Restrictions:**
- Only `NOT_REPORTED` or `DECLINED` entries can be deleted
- `SUBMITTED` and `APPROVED` entries cannot be deleted

---

### moveTaskToProject

Move a time entry to a different project and/or task. Clears tags if moving between projects (tags are project-specific).

**Signature:**
```graphql
moveTaskToProject(
  entryId: UUID!
  newProjectCode: String!
  newTask: String!
): TimeEntry!
```

**Example:**
```graphql
mutation MoveEntry {
  moveTaskToProject(
    entryId: "123e4567-e89b-12d3-a456-426614174000"
    newProjectCode: "CLIENT-A"
    newTask: "Feature Development"
  ) {
    id
    project {
      code
      name
    }
    projectTask {
      taskName
    }
    tags {
      tagValue {
        value
      }
    }
  }
}
```

**Behavior:**
- Validates new project and task
- If moving to different project, **clears all tags** (they won't be valid in new project)
- If moving to different task in same project, **keeps tags**

**Restrictions:**
- Only `NOT_REPORTED` or `DECLINED` entries can be moved

---

### updateTags

Update tags on a time entry (convenience mutation for tag-only updates).

**Signature:**
```graphql
updateTags(
  entryId: UUID!
  tags: [TagInput!]!
): TimeEntry!
```

**Example:**
```graphql
mutation UpdateEntryTags {
  updateTags(
    entryId: "123e4567-e89b-12d3-a456-426614174000"
    tags: [
      { name: "Type", value: "Frontend" }
      { name: "Priority", value: "Medium" }
    ]
  ) {
    id
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

**Behavior:**
- Clears existing tags
- Adds new tags
- Validates all tags against project's tag configurations

**Restrictions:**
- Only `NOT_REPORTED` or `DECLINED` entries can have tags updated

---

### submitTimeEntry

Submit a time entry for approval. Transitions from NOT_REPORTED or DECLINED to SUBMITTED.

**Signature:**
```graphql
submitTimeEntry(id: UUID!): TimeEntry!
```

**Example:**
```graphql
mutation SubmitForApproval {
  submitTimeEntry(id: "123e4567-e89b-12d3-a456-426614174000") {
    id
    status
    updatedAt
  }
}
```

**Status Transitions:**
- `NOT_REPORTED` → `SUBMITTED` ✓
- `DECLINED` → `SUBMITTED` ✓
- `SUBMITTED` → (already submitted, error)
- `APPROVED` → (cannot resubmit, error)

---

### approveTimeEntry

Approve a submitted time entry. Transitions from SUBMITTED to APPROVED.

**Signature:**
```graphql
approveTimeEntry(id: UUID!): TimeEntry!
```

**Example:**
```graphql
mutation ApproveEntry {
  approveTimeEntry(id: "123e4567-e89b-12d3-a456-426614174000") {
    id
    status
    updatedAt
  }
}
```

**Status Transitions:**
- `SUBMITTED` → `APPROVED` ✓
- Others → (error, must be SUBMITTED)

**Note:** Once approved, entries become **immutable** (cannot edit, delete, or move).

---

### declineTimeEntry

Decline a submitted time entry with a comment. Transitions from SUBMITTED to DECLINED.

**Signature:**
```graphql
declineTimeEntry(
  id: UUID!
  comment: String!
): TimeEntry!
```

**Example:**
```graphql
mutation DeclineEntry {
  declineTimeEntry(
    id: "123e4567-e89b-12d3-a456-426614174000"
    comment: "Please add more details about the work performed"
  ) {
    id
    status
    declineComment
    updatedAt
  }
}
```

**Status Transitions:**
- `SUBMITTED` → `DECLINED` ✓
- Others → (error, must be SUBMITTED)

**Note:** Declined entries can be edited and resubmitted.

---

## Types

### TimeEntry

```graphql
type TimeEntry {
  id: UUID!
  project: Project!
  projectTask: ProjectTask!
  issueId: String
  standardHours: Decimal!
  overtimeHours: Decimal!
  description: String
  startDate: Date!
  completionDate: Date!
  status: TimeEntryStatus!
  declineComment: String
  tags: [TimeEntryTag!]!
  createdAt: DateTime!
  updatedAt: DateTime!
}
```

### TimeEntryStatus

```graphql
enum TimeEntryStatus {
  NOT_REPORTED
  SUBMITTED
  APPROVED
  DECLINED
}
```

### Project

```graphql
type Project {
  code: String!
  name: String!
  isActive: Boolean!
  availableTasks: [ProjectTask!]!
  tags: [ProjectTag!]!
}
```

### ProjectTask

```graphql
type ProjectTask {
  taskName: String!
  isActive: Boolean!
}
```

### ProjectTag

```graphql
type ProjectTag {
  tagName: String!
  isRequired: Boolean!
  allowedValues: [TagValue!]!
}
```

### TagValue

```graphql
type TagValue {
  value: String!
  projectTag: ProjectTag!
}
```

### TimeEntryTag

```graphql
type TimeEntryTag {
  timeEntry: TimeEntry!
  tagValue: TagValue!
}
```

---

## Error Handling

### Error Types

The API returns structured errors in GraphQL format:

```json
{
  "errors": [
    {
      "message": "Error description",
      "extensions": {
        "code": "ERROR_CODE",
        "field": "fieldName"
      }
    }
  ]
}
```

### Common Error Codes

| Code | Description | Example |
|------|-------------|---------|
| `VALIDATION_ERROR` | Input validation failed | Invalid project code, negative hours |
| `BUSINESS_RULE_ERROR` | Business rule violation | Cannot update approved entry |
| `NOT_FOUND` | Resource not found | Time entry ID doesn't exist |
| `UNAUTHORIZED` | Authentication failed | Missing or invalid Bearer token |

### Example Errors

**Validation Error:**
```json
{
  "errors": [
    {
      "message": "Project 'INVALID' not found",
      "extensions": {
        "code": "VALIDATION_ERROR",
        "field": "projectCode"
      }
    }
  ]
}
```

**Business Rule Error:**
```json
{
  "errors": [
    {
      "message": "Cannot update time entry in APPROVED status. Approved entries are immutable.",
      "extensions": {
        "code": "BUSINESS_RULE_ERROR"
      }
    }
  ]
}
```

---

## Examples

### Complete Workflow Example

```graphql
# 1. Get available projects
query {
  projects(where: { isActive: { eq: true } }) {
    code
    name
    availableTasks {
      taskName
    }
  }
}

# 2. Get project details with tags
query {
  project(code: "INTERNAL") {
    tags {
      tagName
      isRequired
      allowedValues {
        value
      }
    }
  }
}

# 3. Create time entry
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 8.0
    startDate: "2025-10-29"
    completionDate: "2025-10-29"
    tags: [
      { name: "Type", value: "Backend" }
    ]
  }) {
    id
    status
  }
}

# 4. Update entry (if needed)
mutation {
  updateTimeEntry(
    id: "entry-id"
    input: { standardHours: 8.5 }
  ) {
    id
    standardHours
  }
}

# 5. Submit for approval
mutation {
  submitTimeEntry(id: "entry-id") {
    id
    status
  }
}

# 6. Approve (manager action)
mutation {
  approveTimeEntry(id: "entry-id") {
    id
    status
  }
}
```

### Filtering Examples

**Get this week's entries:**
```graphql
query {
  timeEntries(
    where: {
      startDate: { gte: "2025-10-23" }
      completionDate: { lte: "2025-10-29" }
    }
  ) {
    nodes {
      id
      startDate
      standardHours
    }
  }
}
```

**Get entries by project:**
```graphql
query {
  timeEntries(
    where: {
      project: {
        code: { in: ["INTERNAL", "CLIENT-A"] }
      }
    }
  ) {
    nodes {
      id
      project {
        code
      }
    }
  }
}
```

**Get entries pending submission:**
```graphql
query {
  timeEntries(
    where: {
      status: { eq: NOT_REPORTED }
    }
    order: { completionDate: DESC }
  ) {
    nodes {
      id
      completionDate
      standardHours
    }
  }
}
```

---

## Additional Resources

- [Architecture Documentation](./ARCHITECTURE.md)
- [Data Model](./prd/data-model.md)
- [MCP Tools Specification](./prd/mcp-tools.md)
- [Deployment Guide](./DEPLOYMENT.md)
- [Setup Guide](./integration/CLAUDE-CODE-SETUP.md)

---

**Last Updated:** 2025-10-29
