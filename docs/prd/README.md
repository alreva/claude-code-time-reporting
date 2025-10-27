# Product Requirements Document (PRD)
# Time Reporting System with Claude Code Integration

**Version:** 1.0
**Last Updated:** 2025-10-24
**Status:** Draft

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Data Model](#3-data-model)
4. [GraphQL API Specification](#4-graphql-api-specification)
5. [MCP Server Tools](#5-mcp-server-tools)
6. [Hybrid Auto-Tracking Behavior](#6-hybrid-auto-tracking-behavior)
7. [Security & Authentication](#7-security--authentication)
8. [Docker Deployment](#8-docker-deployment)
9. [Success Metrics](#9-success-metrics)
10. [Future Enhancements](#10-future-enhancements)
11. [Implementation Roadmap](#11-implementation-roadmap)

---

## 1. Overview

### 1.1 Product Vision

A time reporting system that integrates Claude Code with a custom GraphQL-based time tracker, enabling developers to track time spent on coding tasks automatically or manually through natural language commands.

### 1.2 Goals

- ✅ Enable Claude Code to report time entries to a custom time tracker via MCP
- ✅ Provide flexible time entry management (CRUD operations)
- ✅ Move tasks between projects and update metadata
- ✅ Maintain approval workflow for time entries
- ✅ Single technology stack (C# for both API and MCP server)

### 1.3 Non-Goals (v1)

- ❌ User/admin management UI
- ❌ Real-time timer functionality (focus on logging completed time)
- ❌ Auto-tracking heuristics (can be added in v2)
- ❌ Session context persistence
- ❌ Integration with payroll systems
- ❌ Mobile app

### 1.4 Target Users

- **Primary:** Software developers using Claude Code for development tasks
- **Secondary:** Project managers reviewing/approving time entries (via GraphQL API)

---

## 2. Architecture

### 2.1 System Components

```
┌─────────────────┐
│   Claude Code   │
└────────┬────────┘
         │ stdio (JSON-RPC)
         ▼
┌─────────────────┐
│   MCP Server    │ (C# Console App)
│  - Tool Bridge  │
│  - Simple!      │
└────────┬────────┘
         │ HTTP + GraphQL
         │ Bearer Token Auth
         ▼
┌─────────────────┐
│  GraphQL API    │ (C# / HotChocolate)
│  - ASP.NET Core │
│  - EF Core      │
└────────┬────────┘
         │ Entity Framework
         ▼
┌─────────────────┐
│   PostgreSQL    │
│   - TimeEntries │
│   - Projects    │
│   - Tags Config │
└─────────────────┘
```

**PostgreSQL and GraphQL API run in Docker. MCP Server runs as a standalone .NET console app.**

For detailed architecture diagrams and component specifications, see [architecture.md](./architecture.md).

### 2.2 Data Flow

1. **User Request** → Claude Code receives natural language command
2. **Tool Invocation** → Claude Code calls appropriate MCP tool
3. **API Request** → MCP server sends GraphQL mutation/query with Bearer token
4. **Validation** → API validates project codes, tasks, tags against configuration
5. **Persistence** → Valid data stored in PostgreSQL
6. **Response** → Result flows back through layers to user

---

## 3. Data Model

The system uses a normalized relational model with the following entities:

### 3.1 Core Entities

| Entity | Description | Key Fields |
|--------|-------------|------------|
| **TimeEntry** | Individual time log record | ProjectCode, Task, Hours, Status, Tags |
| **Project** | Available projects per user | Code (PK), Tasks[], TagConfigurations[] |
| **TagConfiguration** | Metadata tags per project | Name, AllowedValues[] |
| **Tag** | Actual tag on TimeEntry | Name, Value (validated against config) |

### 3.2 Key Constraints

- **Project codes** are admin-configured, max 10 characters
- **Tasks** are per-project, max 100 characters, no custom values
- **Tags** must match project's tag configurations (name + allowed values)
- **Hours** must be non-negative decimals
- **StartDate** ≤ **CompletionDate**

For complete entity schemas, relationships, and database DDL, see [data-model.md](./data-model.md).

---

## 4. GraphQL API Specification

### 4.1 Queries

- `timeEntries(filters)` - Get time entries with filtering/pagination
- `timeEntry(id)` - Get single entry by ID
- `projects(activeOnly)` - List available projects
- `project(code)` - Get project with tasks and tag configurations

### 4.2 Mutations

- `logTime(input)` - Create new time entry
- `updateTimeEntry(id, input)` - Update existing entry
- `moveTaskToProject(entryId, newProjectCode, newTask)` - Move to different project
- `updateTags(entryId, tags)` - Update metadata tags
- `deleteTimeEntry(id)` - Delete entry
- `submitTimeEntry(id)` - Submit for approval
- `approveTimeEntry(id)` - Admin: approve entry
- `declineTimeEntry(id, comment)` - Admin: decline with reason

For complete GraphQL schema, input types, and examples, see [api-specification.md](./api-specification.md).

---

## 5. MCP Server Tools

The MCP server exposes the following tools to Claude Code:

| Tool Name | Purpose | Key Parameters |
|-----------|---------|----------------|
| `log_time` | Create time entry | projectCode, task, standardHours, dates |
| `query_time_entries` | Search entries | startDate, endDate, projectCode, status |
| `update_time_entry` | Modify entry | id, updates{} |
| `move_task_to_project` | Change project | entryId, newProjectCode, newTask |
| `delete_time_entry` | Remove entry | id |
| `get_available_projects` | List projects | activeOnly |
| `submit_time_entry` | Submit for approval | id |

For detailed tool specifications, parameters, return types, and usage examples, see [mcp-tools.md](./mcp-tools.md).

---

## 6. Usage Workflow

### 6.1 Manual Time Logging

Users explicitly ask Claude Code to log time:

**Examples:**
- "Log 8 hours of development on INTERNAL for today"
- "Create a time entry: CLIENT-A, Bug Fixing, 6.5 hours, yesterday"
- "Track 4 hours of code review for this week's sprint"

Claude Code calls the appropriate MCP tool and confirms success.

### 6.2 Natural Language Commands

All operations support natural language:

**Query:**
- "Show me my time entries for this week"
- "What did I log yesterday?"

**Update:**
- "Change yesterday's entry to 7.5 hours"
- "Move that entry to CLIENT-A project"

**Workflow:**
- "Submit all my entries for approval"
- "Delete that last entry I created"

### 6.3 Future: Auto-Tracking (v2)

Auto-tracking with smart suggestions can be added in v2:
- Detect coding sessions
- Suggest time entries
- Pre-fill based on context

---

## 7. Security & Authentication

### 7.1 Bearer Token Authentication

- All GraphQL API requests require `Authorization: Bearer <token>` header
- Tokens stored securely in MCP server configuration file
- Token validation middleware on all GraphQL requests
- Invalid/missing tokens return 401 Unauthorized

### 7.2 Validation Rules

The API enforces strict validation:

| Validation | Rule |
|------------|------|
| **Project Code** | Must exist in Projects table |
| **Task** | Must be in project's AvailableTasks array |
| **Tags** | Names must match project's TagConfigurations, values must be in AllowedValues |
| **Hours** | StandardHours ≥ 0, OvertimeHours ≥ 0 |
| **Dates** | StartDate ≤ CompletionDate |
| **Status Transitions** | NOT_REPORTED → SUBMITTED → APPROVED/DECLINED (one-way) |

### 7.3 Future Security Enhancements (v2)

- Multi-user authentication with user-specific projects
- Role-based access control (developer vs. admin)
- Audit logging for all mutations
- Rate limiting on API endpoints

---

## 8. Docker Deployment

### 8.1 Services

**docker-compose.yml** orchestrates three services:

1. **postgres** (PostgreSQL 16)
   - Port: 5432
   - Volume: `pgdata:/var/lib/postgresql/data`
   - Init scripts: `/docker-entrypoint-initdb.d/`

2. **graphql-api** (ASP.NET Core)
   - Port: 5000
   - Depends on: `postgres`
   - Health check: `/health`
   - Environment: Connection string, JWT secret

3. **mcp-server** (Node.js)
   - Stdio/socket communication with Claude Code
   - Environment: GraphQL API URL, Bearer token
   - Volume: Config files

### 8.2 Configuration Management

- **Environment variables** for non-sensitive config (URLs, ports)
- **Docker secrets** for sensitive data (tokens, passwords)
- **Volume mounts** for configuration files that may change

### 8.3 Deployment Commands

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild after code changes
docker-compose up -d --build
```

---

## 9. Success Metrics

### 9.1 Functional Requirements (Must-Have)

- [x] Claude Code can log time entries via MCP tools
- [x] Entries can be queried with filters (date, project, status)
- [x] Entries can be updated (all fields except ID)
- [x] Tasks can be moved between projects
- [x] Tags can be managed (add, update, validate)
- [x] Entries can be deleted
- [x] Approval workflow supported (submit, approve, decline)
- [x] Validation prevents invalid data (project codes, tasks, tags)

### 9.2 Non-Functional Requirements

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| API Response Time | 95th percentile < 500ms | Application Insights / logs |
| MCP Tool Execution | < 2 seconds end-to-end | Claude Code tool timing |
| Database Performance | Support 100k+ entries | Load testing with seed data |
| Container Startup | < 30 seconds | `docker-compose up` timing |
| API Availability | 99.9% uptime | Health check monitoring |

### 9.3 User Experience Goals

- Users can log time with a single natural language command
- Error messages clearly indicate what needs to be fixed
- Project/task discovery is easy (via `get_available_projects`)
- Simple, maintainable codebase with single technology stack

---

## 10. Future Enhancements (Not in v1)

### 10.1 UI & Visualization

- Web dashboard for viewing time reports
- Admin UI for configuring projects, tasks, tags
- Charts and analytics (time by project, trends)

### 10.2 Advanced Features

- **Auto-tracking** - Smart detection and suggestions (session context, heuristics)
- Real-time timer with start/stop (vs. logging completed time)
- JIRA integration (auto-populate issue IDs, sync status)
- Export to CSV, Excel, PDF
- Recurring tasks and templates

### 10.3 Multi-Tenancy

- Organization management
- User roles (developer, manager, admin)
- Per-organization project configurations

### 10.4 Integrations

- Slack notifications on approval/decline
- Calendar sync (block time on calendar)
- Payroll system export

---

## 11. Implementation Roadmap

The project is broken into **11 phases** with **~45 atomic tasks** (simplified from original 60). Each task is designed to be:

- **Atomic** - Completable in 1-2 hours
- **Testable** - Clear acceptance criteria
- **Reviewable** - Small enough for thorough code review
- **Independent** - Minimal dependencies on incomplete tasks

### Phase Overview

| Phase | Focus | Tasks | Estimated Time | Links |
|-------|-------|-------|----------------|-------|
| **Phase 1** | Database & Infrastructure | 3 tasks | 3-4 hrs | [Phase 1 Tasks](../tasks/phase-01-database/) |
| **Phase 2** | GraphQL API - Core Setup | 5 tasks | 4.5-5.5 hrs | [Phase 2 Tasks](../tasks/phase-02-api-core/) |
| **Phase 3** | GraphQL API - Queries | 5 tasks | 4-6 hrs | [Phase 3 Tasks](../tasks/phase-03-queries/) |
| **Phase 4** | GraphQL API - Mutations (Part 1) | 5 tasks | 7-9 hrs | [Phase 4 Tasks](../tasks/phase-04-mutations-part1/) |
| **Phase 5** | GraphQL API - Mutations (Part 2) | 5 tasks | 5-6.5 hrs | [Phase 5 Tasks](../tasks/phase-05-mutations-part2/) |
| **Phase 6** | GraphQL API - Docker | 4 tasks | 3 hrs | [Phase 6 Tasks](../tasks/phase-06-api-docker/) |
| **Phase 7** | MCP Server - C# Setup | 2 tasks | 1.5-2 hrs | [Phase 7 Tasks](../tasks/phase-07-mcp-setup/) |
| **Phase 8** | MCP Server - Core Tools | 3 tasks | 2-3 hrs | [Phase 8 Tasks](../tasks/phase-08-mcp-tools/) |
| **Phase 9** | MCP Server - Additional Tools | 2 tasks | 1.5 hrs | [Phase 9 Tasks](../tasks/phase-09-mcp-tools-extra/) |
| **Phase 10** | Integration & Testing | 4 tasks | 4 hrs | [Phase 10 Tasks](../tasks/phase-10-integration/) |
| **Phase 11** | Documentation & Deployment | 4 tasks | 4-5 hrs | [Phase 11 Tasks](../tasks/phase-11-documentation/) |
| **TOTAL** | | **~42 tasks** | **40-51 hrs** | |

### Quick Start

For a complete task checklist with progress tracking, see [**TASK-INDEX.md**](../TASK-INDEX.md).

To begin implementation:

1. **Start with Phase 1** - Database schema and Docker setup
2. **Follow sequential phases** - Each builds on previous work
3. **Track progress** - Check off tasks in TASK-INDEX.md
4. **Review task files** - Each task has detailed acceptance criteria

---

## Appendices

### Related Documents

- [Data Model Specification](./data-model.md) - Detailed entity schemas and relationships
- [API Specification](./api-specification.md) - Complete GraphQL schema with examples
- [MCP Tools Specification](./mcp-tools.md) - Tool parameters and usage patterns
- [Architecture Diagrams](./architecture.md) - System diagrams and component details
- [Task Index](../TASK-INDEX.md) - Master task checklist

### References

- [HotChocolate Documentation](https://chillicream.com/docs/hotchocolate)
- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [Claude Code Documentation](https://docs.claude.com/claude-code)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)

---

**Document Control**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-10-24 | Claude Code | Initial PRD creation |
