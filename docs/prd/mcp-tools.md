# MCP Tools Specification

**Version:** 1.0
**Last Updated:** 2025-10-24

---

## Overview

This document specifies the MCP (Model Context Protocol) tools that Claude Code uses to interact with the Time Reporting GraphQL API.

**MCP Server Name:** `time-reporting`
**Communication:** stdio (standard input/output)
**Authentication:** Azure Entra ID via AzureCliCredential (requires `az login`)

---

## Table of Contents

1. [Tool List](#1-tool-list)
2. [Tool Specifications](#2-tool-specifications)
3. [Session Context](#3-session-context)
4. [Error Handling](#4-error-handling)
5. [Usage Patterns](#5-usage-patterns)

---

## 1. Tool List

| Tool Name | Purpose | Category |
|-----------|---------|----------|
| `log_time` | Create new time entry | Create |
| `query_time_entries` | Search time entries | Read |
| `update_time_entry` | Modify existing entry | Update |
| `move_task_to_project` | Change project/task | Update |
| `delete_time_entry` | Remove entry | Delete |
| `get_available_projects` | List projects with tasks/tags | Read |
| `submit_time_entry` | Submit for approval | Workflow |

---

## 2. Tool Specifications

### 2.1 log_time

**Description:** Create a new time entry and log it to the time tracking system.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "projectCode": {
      "type": "string",
      "description": "Project code (max 10 chars)",
      "maxLength": 10
    },
    "task": {
      "type": "string",
      "description": "Task name from project's available tasks",
      "maxLength": 100
    },
    "issueId": {
      "type": "string",
      "description": "External issue ID (e.g., JIRA ticket)",
      "maxLength": 30
    },
    "standardHours": {
      "type": "number",
      "description": "Regular hours worked",
      "minimum": 0
    },
    "overtimeHours": {
      "type": "number",
      "description": "Overtime hours worked (optional, defaults to 0)",
      "minimum": 0,
      "default": 0
    },
    "description": {
      "type": "string",
      "description": "Description of work performed"
    },
    "startDate": {
      "type": "string",
      "format": "date",
      "description": "Work start date (ISO 8601: YYYY-MM-DD)"
    },
    "completionDate": {
      "type": "string",
      "format": "date",
      "description": "Work completion date (ISO 8601: YYYY-MM-DD)"
    },
    "tags": {
      "type": "array",
      "description": "Metadata tags for the entry",
      "items": {
        "type": "object",
        "properties": {
          "name": {"type": "string", "maxLength": 20},
          "value": {"type": "string", "maxLength": 100}
        },
        "required": ["name", "value"]
      }
    }
  },
  "required": ["projectCode", "task", "standardHours", "startDate", "completionDate"]
}
```

**Output:**
```json
{
  "id": "uuid",
  "projectCode": "INTERNAL",
  "task": "Development",
  "standardHours": 8.0,
  "overtimeHours": 0.0,
  "status": "NOT_REPORTED",
  "createdAt": "2025-10-24T10:30:00Z"
}
```

**Example Claude Code Usage:**
```
User: "Log 8 hours of development work on INTERNAL project for today"

Claude: I'll log that time entry for you.
[Calls log_time with:
  projectCode: "INTERNAL"
  task: "Development"
  standardHours: 8.0
  startDate: "2025-10-24"
  completionDate: "2025-10-24"
]

Response: "Time entry created successfully with ID abc-123. Status: NOT_REPORTED"
```

---

### 2.2 query_time_entries

**Description:** Query time entries with optional filters.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "startDate": {
      "type": "string",
      "format": "date",
      "description": "Filter entries starting from this date"
    },
    "endDate": {
      "type": "string",
      "format": "date",
      "description": "Filter entries up to this date"
    },
    "projectCode": {
      "type": "string",
      "description": "Filter by specific project"
    },
    "status": {
      "type": "string",
      "enum": ["NOT_REPORTED", "SUBMITTED", "APPROVED", "DECLINED"],
      "description": "Filter by entry status"
    },
    "limit": {
      "type": "integer",
      "description": "Maximum number of results",
      "default": 50,
      "maximum": 200
    },
    "offset": {
      "type": "integer",
      "description": "Number of results to skip (for pagination)",
      "default": 0
    }
  }
}
```

**Output:**
```json
{
  "entries": [
    {
      "id": "uuid",
      "projectCode": "INTERNAL",
      "task": "Development",
      "standardHours": 8.0,
      "overtimeHours": 0.0,
      "startDate": "2025-10-24",
      "completionDate": "2025-10-24",
      "status": "NOT_REPORTED",
      "tags": [{"name": "Environment", "value": "Production"}]
    }
  ],
  "count": 1
}
```

**Example Claude Code Usage:**
```
User: "Show me time entries for this week"

Claude: I'll query your time entries for this week.
[Calls query_time_entries with:
  startDate: "2025-10-21"
  endDate: "2025-10-24"
  limit: 50
]

Response: "Found 5 time entries:
1. INTERNAL - Development - 8.0 hours (Oct 21)
2. CLIENT-A - Bug Fixing - 6.5 hours (Oct 22)
..."
```

---

### 2.3 update_time_entry

**Description:** Update fields on an existing time entry (only allowed for NOT_REPORTED or DECLINED entries).

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "UUID of the time entry to update"
    },
    "updates": {
      "type": "object",
      "description": "Fields to update (only provided fields will be changed)",
      "properties": {
        "task": {"type": "string"},
        "issueId": {"type": "string"},
        "standardHours": {"type": "number", "minimum": 0},
        "overtimeHours": {"type": "number", "minimum": 0},
        "description": {"type": "string"},
        "startDate": {"type": "string", "format": "date"},
        "completionDate": {"type": "string", "format": "date"},
        "tags": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "name": {"type": "string"},
              "value": {"type": "string"}
            }
          }
        }
      }
    }
  },
  "required": ["id", "updates"]
}
```

**Output:** Updated TimeEntry object

**Example Claude Code Usage:**
```
User: "Update yesterday's entry to 7.5 hours"

Claude: I'll update that entry.
[First queries to find yesterday's entry, then calls update_time_entry with:
  id: "found-entry-id"
  updates: { standardHours: 7.5 }
]

Response: "Updated entry to 7.5 standard hours"
```

---

### 2.4 move_task_to_project

**Description:** Move a time entry to a different project and task (useful when entry was logged to wrong project).

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "entryId": {
      "type": "string",
      "description": "UUID of the time entry to move"
    },
    "newProjectCode": {
      "type": "string",
      "description": "Target project code",
      "maxLength": 10
    },
    "newTask": {
      "type": "string",
      "description": "Task name in the target project",
      "maxLength": 100
    }
  },
  "required": ["entryId", "newProjectCode", "newTask"]
}
```

**Output:** Updated TimeEntry object

**Example Claude Code Usage:**
```
User: "Move today's development entry to CLIENT-A project under Feature Development"

Claude: I'll move that entry to the CLIENT-A project.
[Calls move_task_to_project with:
  entryId: "found-entry-id"
  newProjectCode: "CLIENT-A"
  newTask: "Feature Development"
]

Response: "Entry moved to CLIENT-A - Feature Development"
```

---

### 2.5 delete_time_entry

**Description:** Delete a time entry (only allowed for NOT_REPORTED entries).

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "UUID of the time entry to delete"
    }
  },
  "required": ["id"]
}
```

**Output:**
```json
{
  "success": true,
  "message": "Time entry deleted successfully"
}
```

**Example Claude Code Usage:**
```
User: "Delete that last entry I just created"

Claude: I'll delete that entry.
[Calls delete_time_entry with:
  id: "recent-entry-id"
]

Response: "Time entry deleted successfully"
```

---

### 2.6 get_available_projects

**Description:** Get list of available projects with their tasks and tag configurations.

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "activeOnly": {
      "type": "boolean",
      "description": "Only return active projects",
      "default": true
    }
  }
}
```

**Output:**
```json
{
  "projects": [
    {
      "code": "INTERNAL",
      "name": "Internal Development",
      "tasks": ["Development", "Code Review", "Testing", "Documentation"],
      "tags": [
        {
          "name": "Environment",
          "allowedValues": ["Production", "Staging", "Development"]
        },
        {
          "name": "Billable",
          "allowedValues": ["Yes", "No"]
        }
      ]
    }
  ]
}
```

**Example Claude Code Usage:**
```
User: "What projects can I log time to?"

Claude: I'll get the available projects for you.
[Calls get_available_projects with: activeOnly: true]

Response: "Available projects:
1. INTERNAL (Internal Development)
   - Tasks: Development, Code Review, Testing, Documentation
   - Tags: Environment (Production/Staging/Development), Billable (Yes/No)

2. CLIENT-A (Client A Project)
   - Tasks: Feature Development, Bug Fixing, Maintenance, Support
   - Tags: Priority (High/Medium/Low), Sprint (Sprint-1/Sprint-2/...)
"
```

---

### 2.7 submit_time_entry

**Description:** Submit a time entry for approval (changes status from NOT_REPORTED to SUBMITTED).

**Input Schema:**
```json
{
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "UUID of the time entry to submit"
    }
  },
  "required": ["id"]
}
```

**Output:** Updated TimeEntry object with status SUBMITTED

**Example Claude Code Usage:**
```
User: "Submit all my time entries for this week for approval"

Claude: I'll submit your time entries.
[First queries entries, then calls submit_time_entry for each:
  id: "entry-1-id"
  id: "entry-2-id"
  ...
]

Response: "Submitted 5 time entries for approval"
```

---

## 3. Session Context

The MCP server maintains session context to improve user experience and enable auto-tracking.

### 3.1 Context Data

```typescript
interface SessionContext {
  lastProjectCode: string | null;     // Last used project
  lastTask: string | null;            // Last used task
  sessionStartTime: Date | null;      // When current session started
  accumulatedDescription: string[];   // Descriptions of work done
  recentEntries: string[];            // IDs of recently created entries
}
```

### 3.2 Context Usage

**Default Project:**
- When user doesn't specify project, use `lastProjectCode`
- Prompt user if no context available

**Session Duration:**
- Track time between session start and log command
- Suggest hours based on duration

**Accumulated Descriptions:**
- Build comprehensive descriptions from multiple interactions
- Example: "Implemented auth, fixed bugs, wrote tests"

### 3.3 Context Persistence

Context is stored in-memory per Claude Code session. Future enhancement: persist to file for cross-session continuity.

---

## 4. Error Handling

### 4.1 Error Response Format

All tools return errors in this format:

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Task 'InvalidTask' is not available for project 'INTERNAL'",
    "field": "task",
    "allowedValues": ["Development", "Code Review", "Testing", "Documentation"]
  }
}
```

### 4.2 Common Error Codes

| Code | Description | User Action |
|------|-------------|-------------|
| `VALIDATION_ERROR` | Input validation failed | Check project code, task name, tag values |
| `NOT_FOUND` | Entry not found | Verify entry ID |
| `FORBIDDEN` | Action not allowed | Cannot update submitted entries |
| `AUTHENTICATION_ERROR` | Token invalid/missing | Check MCP server configuration |
| `NETWORK_ERROR` | API unreachable | Check if GraphQL API is running |

### 4.3 Error Handling in Tools

**Validation Errors:**
- Extract specific field errors from GraphQL response
- Provide helpful suggestions (e.g., list allowed values)

**Not Found Errors:**
- Re-query if entry might have changed
- Suggest alternative actions

**Forbidden Errors:**
- Explain why action isn't allowed
- Suggest correct workflow (e.g., "Entry is already submitted, cannot update")

---

## 5. Usage Patterns

### 5.1 Quick Log Pattern

**User:** "Log 8 hours for today on INTERNAL project"

**Claude Code:**
1. Calls `get_available_projects` to verify INTERNAL exists
2. Uses default task "Development" from context or prompts
3. Calls `log_time` with today's date
4. Updates session context

### 5.2 Interactive Correction Pattern

**User:** "Actually, that should be 7.5 hours"

**Claude Code:**
1. Uses `recentEntries` from context to identify entry
2. Calls `update_time_entry` with new hours
3. Confirms update

### 5.3 Bulk Query Pattern

**User:** "Show me all time logged to CLIENT-A this month"

**Claude Code:**
1. Calls `query_time_entries` with filters:
   - startDate: first day of month
   - endDate: today
   - projectCode: "CLIENT-A"
2. Formats results as table or summary

### 5.4 Migration Pattern

**User:** "Move yesterday's entry from INTERNAL to CLIENT-A Feature Development"

**Claude Code:**
1. Calls `query_time_entries` for yesterday
2. Calls `move_task_to_project` with new project/task
3. Verifies tags are still valid (or removes invalid ones)

### 5.5 Auto-Tracking Pattern

**Scenario:** User spent 2 hours coding with Claude Code

**Claude Code:**
1. Detects significant coding session (>15 min)
2. Suggests time entry:
   ```
   I notice you've been working for about 2 hours. Would you like to log this time?

   Suggested entry:
   - Project: INTERNAL (last used)
   - Task: Development
   - Hours: 2.0
   - Date: Today
   - Description: Worked on authentication feature implementation

   [Approve] [Modify] [Skip]
   ```
3. If approved, calls `log_time`
4. If modified, prompts for changes then calls `log_time`

---

## 6. Tool Implementation (TypeScript)

### 6.1 Example Tool Implementation

```typescript
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { GraphQLClient } from 'graphql-request';

const server = new Server({
  name: "time-reporting",
  version: "1.0.0"
});

// Azure AD token acquired via AzureCliCredential
const tokenService = new TokenService();
const token = await tokenService.GetTokenAsync();

const graphqlClient = new GraphQLClient('http://localhost:5001/graphql', {
  headers: {
    Authorization: `Bearer ${token}`  // Azure AD JWT token
  }
});

server.setRequestHandler("tools/call", async (request) => {
  const { name, arguments: args } = request.params;

  switch (name) {
    case "log_time": {
      const mutation = `
        mutation LogTime($input: LogTimeInput!) {
          logTime(input: $input) {
            id
            projectCode
            task
            standardHours
            overtimeHours
            status
            createdAt
          }
        }
      `;

      try {
        const result = await graphqlClient.request(mutation, {
          input: {
            projectCode: args.projectCode,
            task: args.task,
            issueId: args.issueId,
            standardHours: args.standardHours,
            overtimeHours: args.overtimeHours || 0,
            description: args.description,
            startDate: args.startDate,
            completionDate: args.completionDate,
            tags: args.tags || []
          }
        });

        // Update session context
        sessionContext.lastProjectCode = args.projectCode;
        sessionContext.lastTask = args.task;
        sessionContext.recentEntries.unshift(result.logTime.id);

        return {
          content: [{
            type: "text",
            text: JSON.stringify({
              success: true,
              entry: result.logTime
            })
          }]
        };
      } catch (error) {
        return {
          content: [{
            type: "text",
            text: JSON.stringify({
              success: false,
              error: parseGraphQLError(error)
            })
          }]
        };
      }
    }
    // ... other tools
  }
});
```

---

**Related Documents:**
- [API Specification](./api-specification.md) - GraphQL API these tools consume
- [Architecture](./architecture.md) - System architecture
- [PRD Main](./README.md) - Product requirements overview
