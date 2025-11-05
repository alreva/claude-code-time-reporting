# GraphQL API Specification

**Version:** 1.0
**Last Updated:** 2025-10-24

---

## Overview

This document provides the complete GraphQL schema, queries, mutations, and usage examples for the Time Reporting API built with HotChocolate and ASP.NET Core.

**Base URL:** `http://localhost:5001/graphql`
**Authentication:** Azure AD JWT token required in `Authorization` header

---

## Table of Contents

1. [Schema Overview](#1-schema-overview)
2. [Type Definitions](#2-type-definitions)
3. [Queries](#3-queries)
4. [Mutations](#4-mutations)
5. [Input Types](#5-input-types)
6. [Enums](#6-enums)
7. [Usage Examples](#7-usage-examples)
8. [Error Handling](#8-error-handling)

---

## 1. Schema Overview

```graphql
schema {
  query: Query
  mutation: Mutation
}
```

### 1.1 Quick Reference

**Queries:**
- `timeEntries` - List time entries with filters
- `timeEntry` - Get single entry by ID
- `projects` - List available projects
- `project` - Get project details with tasks and tags

**Mutations:**
- `logTime` - Create new time entry
- `updateTimeEntry` - Update existing entry
- `moveTaskToProject` - Move entry to different project
- `updateTags` - Update entry tags
- `deleteTimeEntry` - Delete entry
- `submitTimeEntry` - Submit for approval
- `approveTimeEntry` - Approve entry (admin)
- `declineTimeEntry` - Decline entry with comment (admin)

---

## 2. Type Definitions

### 2.1 TimeEntry

```graphql
type TimeEntry {
  id: ID!
  projectCode: String!
  task: String!
  issueId: String
  standardHours: Decimal!
  overtimeHours: Decimal!
  description: String
  startDate: Date!
  completionDate: Date!
  status: TimeEntryStatus!
  declineComment: String
  tags: [Tag!]!
  createdAt: DateTime!
  updatedAt: DateTime!
  userId: String

  # Navigation
  project: Project!
}
```

### 2.2 Project

```graphql
type Project {
  code: String!
  name: String!
  isActive: Boolean!
  createdAt: DateTime!
  updatedAt: DateTime!

  # Navigation
  availableTasks: [ProjectTask!]!
  tags: [ProjectTag!]!
  timeEntries: [TimeEntry!]!
}
```

### 2.3 ProjectTask

```graphql
type ProjectTask {
  id: Int!
  projectCode: String!
  taskName: String!
  isActive: Boolean!

  # Navigation
  project: Project!
}
```

### 2.4 ProjectTag

```graphql
type ProjectTag {
  id: Int!
  projectCode: String!
  tagName: String!
  isActive: Boolean!

  # Navigation
  project: Project!
  allowedValues: [TagValue!]!
}
```

### 2.5 TagValue

```graphql
type TagValue {
  id: Int!
  projectTagId: Int!
  value: String!

  # Navigation
  projectTag: ProjectTag!
}
```

### 2.6 Tag

```graphql
type Tag {
  name: String!
  value: String!
}
```

---

## 3. Queries

### 3.1 timeEntries

Get a list of time entries with optional filtering and pagination.

```graphql
type Query {
  timeEntries(
    startDate: Date
    endDate: Date
    projectCode: String
    status: TimeEntryStatus
    limit: Int = 50
    offset: Int = 0
  ): [TimeEntry!]!
}
```

**Parameters:**
- `startDate` - Filter entries where `StartDate >= startDate`
- `endDate` - Filter entries where `CompletionDate <= endDate`
- `projectCode` - Filter by specific project
- `status` - Filter by workflow status
- `limit` - Max results to return (default: 50, max: 200)
- `offset` - Skip N results for pagination (default: 0)

**Example:**

```graphql
query GetRecentEntries {
  timeEntries(
    startDate: "2025-10-01"
    endDate: "2025-10-24"
    status: SUBMITTED
    limit: 10
  ) {
    id
    projectCode
    task
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    tags {
      name
      value
    }
  }
}
```

### 3.2 timeEntry

Get a single time entry by ID.

```graphql
type Query {
  timeEntry(id: ID!): TimeEntry
}
```

**Parameters:**
- `id` - UUID of the time entry

**Returns:** TimeEntry or `null` if not found

**Example:**

```graphql
query GetEntry {
  timeEntry(id: "123e4567-e89b-12d3-a456-426614174000") {
    id
    projectCode
    task
    issueId
    standardHours
    overtimeHours
    description
    startDate
    completionDate
    status
    declineComment
    tags {
      name
      value
    }
    project {
      name
    }
  }
}
```

### 3.3 projects

List all available projects.

```graphql
type Query {
  projects(activeOnly: Boolean = true): [Project!]!
}
```

**Parameters:**
- `activeOnly` - If true, only return active projects (default: true)

**Example:**

```graphql
query GetProjects {
  projects(activeOnly: true) {
    code
    name
    isActive
  }
}
```

### 3.4 project

Get detailed information about a specific project, including tasks and tag configurations.

```graphql
type Query {
  project(code: String!): Project
}
```

**Parameters:**
- `code` - Project code (max 10 chars)

**Returns:** Project or `null` if not found

**Example:**

```graphql
query GetProjectDetails {
  project(code: "INTERNAL") {
    code
    name
    isActive
    availableTasks {
      id
      taskName
      isActive
    }
    tagConfigurations {
      id
      tagName
      allowedValues
      isActive
    }
  }
}
```

---

## 4. Mutations

### 4.1 logTime

Create a new time entry.

```graphql
type Mutation {
  logTime(input: LogTimeInput!): TimeEntry!
}
```

**Example:**

```graphql
mutation CreateEntry {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    issueId: "DEV-123"
    standardHours: 6.5
    overtimeHours: 1.5
    description: "Implemented user authentication feature"
    startDate: "2025-10-24"
    completionDate: "2025-10-24"
    tags: [
      { name: "Environment", value: "Production" }
      { name: "Billable", value: "Yes" }
    ]
  }) {
    id
    projectCode
    task
    standardHours
    overtimeHours
    status
    createdAt
  }
}
```

### 4.2 updateTimeEntry

Update an existing time entry (only allowed in NOT_REPORTED or DECLINED status).

```graphql
type Mutation {
  updateTimeEntry(
    id: ID!
    input: UpdateTimeEntryInput!
  ): TimeEntry!
}
```

**Example:**

```graphql
mutation UpdateEntry {
  updateTimeEntry(
    id: "123e4567-e89b-12d3-a456-426614174000"
    input: {
      standardHours: 7.0
      description: "Updated description with more details"
      tags: [
        { name: "Environment", value: "Staging" }
      ]
    }
  ) {
    id
    standardHours
    description
    tags {
      name
      value
    }
    updatedAt
  }
}
```

### 4.3 moveTaskToProject

Move a time entry to a different project and task.

```graphql
type Mutation {
  moveTaskToProject(
    entryId: ID!
    newProjectCode: String!
    newTask: String!
  ): TimeEntry!
}
```

**Example:**

```graphql
mutation MoveTask {
  moveTaskToProject(
    entryId: "123e4567-e89b-12d3-a456-426614174000"
    newProjectCode: "CLIENT-A"
    newTask: "Bug Fixing"
  ) {
    id
    projectCode
    task
    project {
      name
    }
    updatedAt
  }
}
```

### 4.4 updateTags

Update tags on a time entry.

```graphql
type Mutation {
  updateTags(
    entryId: ID!
    tags: [TagInput!]!
  ): TimeEntry!
}
```

**Example:**

```graphql
mutation UpdateTags {
  updateTags(
    entryId: "123e4567-e89b-12d3-a456-426614174000"
    tags: [
      { name: "Priority", value: "High" }
      { name: "Sprint", value: "Sprint-3" }
    ]
  ) {
    id
    tags {
      name
      value
    }
    updatedAt
  }
}
```

### 4.5 deleteTimeEntry

Delete a time entry (only allowed in NOT_REPORTED status).

```graphql
type Mutation {
  deleteTimeEntry(id: ID!): Boolean!
}
```

**Returns:** `true` if deleted, error if not found or not allowed

**Example:**

```graphql
mutation DeleteEntry {
  deleteTimeEntry(id: "123e4567-e89b-12d3-a456-426614174000")
}
```

### 4.6 submitTimeEntry

Submit a time entry for approval (changes status from NOT_REPORTED to SUBMITTED).

```graphql
type Mutation {
  submitTimeEntry(id: ID!): TimeEntry!
}
```

**Example:**

```graphql
mutation SubmitEntry {
  submitTimeEntry(id: "123e4567-e89b-12d3-a456-426614174000") {
    id
    status
    updatedAt
  }
}
```

### 4.7 approveTimeEntry

Approve a submitted time entry (admin only).

```graphql
type Mutation {
  approveTimeEntry(id: ID!): TimeEntry!
}
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

### 4.8 declineTimeEntry

Decline a submitted time entry with a comment (admin only).

```graphql
type Mutation {
  declineTimeEntry(
    id: ID!
    comment: String!
  ): TimeEntry!
}
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

---

## 5. Input Types

### 5.1 LogTimeInput

```graphql
input LogTimeInput {
  projectCode: String!
  task: String!
  issueId: String
  standardHours: Decimal!
  overtimeHours: Decimal = 0.0
  description: String
  startDate: Date!
  completionDate: Date!
  tags: [TagInput!] = []
}
```

**Validation:**
- `projectCode` - Must exist and be active
- `task` - Must be in project's available tasks
- `standardHours` - Must be >= 0
- `overtimeHours` - Must be >= 0
- `startDate` - Must be <= completionDate
- `tags` - Must match project's tag configurations

### 5.2 UpdateTimeEntryInput

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

**Notes:**
- All fields are optional - only provided fields are updated
- Cannot update `projectCode` - use `moveTaskToProject` instead
- Cannot update `status` - use workflow mutations instead

### 5.3 TagInput

```graphql
input TagInput {
  name: String!
  value: String!
}
```

**Validation:**
- `name` - Must exist in project's tag configurations
- `value` - Must be in allowed values for that tag name

---

## 6. Enums

### 6.1 TimeEntryStatus

```graphql
enum TimeEntryStatus {
  NOT_REPORTED
  SUBMITTED
  APPROVED
  DECLINED
}
```

**Workflow:**
```
NOT_REPORTED ──submit──> SUBMITTED ──approve──> APPROVED
                               │
                               └──decline──> DECLINED ──submit──> SUBMITTED
```

---

## 7. Usage Examples

### 7.1 Complete Workflow Example

```graphql
# 1. Get available projects
query {
  projects {
    code
    name
    availableTasks {
      taskName
    }
  }
}

# 2. Create a time entry
mutation {
  logTime(input: {
    projectCode: "INTERNAL"
    task: "Development"
    standardHours: 8.0
    startDate: "2025-10-24"
    completionDate: "2025-10-24"
  }) {
    id
    status
  }
}

# 3. Update the entry with more details
mutation {
  updateTimeEntry(
    id: "<entry-id>"
    input: {
      description: "Worked on authentication module"
      tags: [
        { name: "Environment", value: "Production" }
      ]
    }
  ) {
    id
    description
    tags { name value }
  }
}

# 4. Submit for approval
mutation {
  submitTimeEntry(id: "<entry-id>") {
    id
    status
  }
}

# 5. Admin approves
mutation {
  approveTimeEntry(id: "<entry-id>") {
    id
    status
  }
}
```

### 7.2 Querying with Complex Filters

```graphql
query GetMonthlyReport {
  timeEntries(
    startDate: "2025-10-01"
    endDate: "2025-10-31"
    status: APPROVED
  ) {
    projectCode
    task
    standardHours
    overtimeHours
    startDate
    project {
      name
    }
  }
}
```

### 7.3 Moving Tasks Between Projects

```graphql
# First, get available projects and tasks
query {
  project(code: "CLIENT-A") {
    availableTasks {
      taskName
    }
  }
}

# Then move the entry
mutation {
  moveTaskToProject(
    entryId: "<entry-id>"
    newProjectCode: "CLIENT-A"
    newTask: "Feature Development"
  ) {
    id
    projectCode
    task
  }
}
```

---

## 8. Error Handling

### 8.1 Error Response Format

```json
{
  "errors": [
    {
      "message": "Task 'InvalidTask' is not available for project 'INTERNAL'",
      "path": ["logTime"],
      "extensions": {
        "code": "VALIDATION_ERROR",
        "field": "task"
      }
    }
  ]
}
```

### 8.2 Common Error Codes

| Code | Description | Example |
|------|-------------|---------|
| `VALIDATION_ERROR` | Input validation failed | Invalid project code, task not in project |
| `NOT_FOUND` | Resource not found | Entry ID doesn't exist |
| `FORBIDDEN` | Action not allowed | Cannot update submitted entry |
| `AUTHENTICATION_ERROR` | Missing or invalid token | No Bearer token provided |
| `BUSINESS_RULE_VIOLATION` | Business logic error | Cannot approve already approved entry |

### 8.3 Validation Error Examples

**Invalid project:**
```json
{
  "errors": [
    {
      "message": "Project 'INVALID' does not exist or is inactive",
      "extensions": { "code": "VALIDATION_ERROR", "field": "projectCode" }
    }
  ]
}
```

**Invalid tag:**
```json
{
  "errors": [
    {
      "message": "Value 'Unknown' is not allowed for tag 'Environment'. Allowed values: Production, Staging, Development",
      "extensions": { "code": "VALIDATION_ERROR", "field": "tags[0].value" }
    }
  ]
}
```

**Date range error:**
```json
{
  "errors": [
    {
      "message": "StartDate must be less than or equal to CompletionDate",
      "extensions": { "code": "VALIDATION_ERROR", "field": "startDate" }
    }
  ]
}
```

### 8.4 Authentication

All requests must include Azure AD JWT token:

```http
POST /graphql HTTP/1.1
Host: localhost:5001
Content-Type: application/json
Authorization: Bearer <azure-ad-jwt-token>

{
  "query": "query { projects { code name } }"
}
```

**Acquiring token:**
```bash
# Authenticate with Azure CLI
az login

# Get access token for the API
az account get-access-token --resource api://<your-api-app-id> --query accessToken -o tsv
```

**Missing or invalid token:**
```json
{
  "errors": [
    {
      "message": "Unauthorized: Missing or invalid Azure AD token",
      "extensions": { "code": "AUTHENTICATION_ERROR" }
    }
  ]
}
```

---

## 9. GraphQL Introspection

### 9.1 Schema Introspection

GraphQL introspection is enabled for development. You can explore the schema using GraphQL Playground or similar tools.

**Endpoint:** `http://localhost:5001/graphql`

### 9.2 Useful Introspection Queries

**Get all types:**
```graphql
query {
  __schema {
    types {
      name
      kind
    }
  }
}
```

**Get type details:**
```graphql
query {
  __type(name: "TimeEntry") {
    name
    fields {
      name
      type {
        name
        kind
      }
    }
  }
}
```

---

## 10. Performance Considerations

### 10.1 DataLoader Pattern

The API uses DataLoader (via HotChocolate) to prevent N+1 query problems:

- `Project` lookups are batched when loading multiple time entries
- `ProjectTask` and `ProjectTag` are eagerly loaded with projects

### 10.2 Pagination

Always use `limit` parameter for large result sets:

```graphql
query {
  timeEntries(limit: 50, offset: 0) {
    id
    # ...
  }
}
```

**Maximum limit:** 200 entries per request

### 10.3 Field Selection

Request only the fields you need to minimize response size:

```graphql
# Good - minimal fields
query {
  timeEntries {
    id
    projectCode
    standardHours
  }
}

# Avoid - unnecessary nested data
query {
  timeEntries {
    id
    projectCode
    standardHours
    project {
      availableTasks {
        taskName
      }
      tagConfigurations {
        allowedValues
      }
    }
  }
}
```

---

**Related Documents:**
- [Data Model](./data-model.md) - Entity definitions and relationships
- [MCP Tools](./mcp-tools.md) - Tools that consume this API
- [PRD Main](./README.md) - Product requirements overview
