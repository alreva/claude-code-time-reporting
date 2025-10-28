# Task Index - Time Reporting System

**Total Tasks:** 61
**Completed:** 36
**In Progress:** 0
**Pending:** 25

---

## How to Use This Index

1. **Track Progress** - Check off tasks as you complete them
2. **Find Details** - Click task links to view detailed implementation guides
3. **Sequential Order** - Complete phases in order for dependencies
4. **Estimates** - Time estimates are for a single developer

---

## Phase 1: Database & Infrastructure (3 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 1.1 | PostgreSQL Schema | Create database schema with all tables, constraints, and indexes | 1-2 hrs | ☑ Completed | [View](./tasks/phase-01-database/task-1.1-postgresql-schema.md) |
| 1.2 | Seed Data | Create sample projects, tasks, and tags for development | 1 hr | ☑ Completed | [View](./tasks/phase-01-database/task-1.2-seed-data.md) |
| 1.3 | Docker Compose PostgreSQL | Set up PostgreSQL in Docker with persistent volume | 1 hr | ☑ Completed | [View](./tasks/phase-01-database/task-1.3-docker-compose-postgres.md) |

**Phase 1 Total:** 3-4 hours

---

## Phase 2: GraphQL API - Core Setup (6 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 2.1 | ASP.NET + HotChocolate Setup | Create ASP.NET Core project with HotChocolate GraphQL | 1 hr | ☑ Completed | [View](./tasks/phase-02-api-core/task-2.1-aspnet-hotchocolate-setup.md) |
| 2.2 | Entity Framework Core Config | Configure EF Core with PostgreSQL provider and connection | 1 hr | ☑ Completed | [View](./tasks/phase-02-api-core/task-2.2-ef-core-config.md) |
| 2.3 | Data Models | Implement C# entity models (TimeEntry, Project, ProjectTask, ProjectTag, TagValue) | 1-2 hrs | ☑ Completed | Create |
| 2.4 | Bearer Auth Middleware | Add Bearer token authentication middleware | 1 hr | ☑ Completed | Create |
| 2.5 | Health Check Endpoint | Create `/health` endpoint for Docker health checks | 30 min | ☑ Completed | Create |
| 2.6 | Test Project & Database Test Infrastructure | Create xUnit test project with DatabaseFixture and integrate SQL schema tests | 1.5-2 hrs | ☑ Completed | [View](./tasks/phase-02-api-core/task-2.6-test-infrastructure.md) |

**Phase 2 Total:** 6-7.5 hours

**Key Deliverables:**
- `TimeReportingApi/TimeReportingApi.csproj`
- `TimeReportingApi/Program.cs` - App startup
- `TimeReportingApi/Models/` - Entity models
- `TimeReportingApi/Data/TimeReportingDbContext.cs`
- `TimeReportingApi/Middleware/BearerAuthMiddleware.cs`
- `TimeReportingApi.Tests/TimeReportingApi.Tests.csproj`
- `TimeReportingApi.Tests/Fixtures/DatabaseFixture.cs`
- `TimeReportingApi.Tests/Integration/SqlSchemaValidationTests.cs`

---

## Phase 3: GraphQL API - Queries (5 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 3.1 | TimeEntries Query | Implement `timeEntries(filters)` query with filtering and pagination | 1-2 hrs | ☑ Completed | [View](./tasks/phase-03-queries/task-3.1-time-entries-query.md) |
| 3.2 | TimeEntry Query | Implement `timeEntry(id)` query to get single entry | 30 min | ☑ Completed | [View](./tasks/phase-03-queries/task-3.2-time-entry-query.md) |
| 3.3 | Projects Query | Implement `projects(activeOnly)` query | 30 min | ☑ Completed | [View](./tasks/phase-03-queries/task-3.3-projects-query.md) |
| 3.4 | Project Query | Implement `project(code)` query with tasks and tag configurations | 1 hr | ☑ Completed | [View](./tasks/phase-03-queries/task-3.4-project-query.md) |
| 3.5 | Query Tests | Write unit tests for all query resolvers | 1-2 hrs | ☑ Completed | [View](./tasks/phase-03-queries/task-3.5-query-tests.md) |

**Phase 3 Total:** 4-6 hours

**Key Deliverables:**
- `TimeReportingApi/GraphQL/Query.cs`
- `TimeReportingApi/GraphQL/Types/TimeEntryType.cs`
- `TimeReportingApi/GraphQL/Types/ProjectType.cs`
- `TimeReportingApi.Tests/GraphQL/QueryTests.cs`

---

## Phase 4: GraphQL API - Mutations Part 1 (5 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 4.1 | LogTime Mutation | Implement `logTime(input)` mutation with validation | 2 hrs | ☑ Completed | Create |
| 4.2 | UpdateTimeEntry Mutation | Implement `updateTimeEntry(id, input)` mutation | 1-2 hrs | ☑ Completed | Create |
| 4.3 | DeleteTimeEntry Mutation | Implement `deleteTimeEntry(id)` mutation with status check | 1 hr | ☑ Completed | Create |
| 4.4 | Validation Service | Create service to validate project codes, tasks, and tags | 2 hrs | ☑ Completed | Create |
| 4.5 | Mutation Tests | Write unit tests for create, update, delete mutations | 1-2 hrs | ☑ Completed | Create |

**Phase 4 Total:** 7-9 hours

**Key Deliverables:**
- `TimeReportingApi/GraphQL/Mutation.cs`
- `TimeReportingApi/GraphQL/Inputs/LogTimeInput.cs`
- `TimeReportingApi/GraphQL/Inputs/UpdateTimeEntryInput.cs`
- `TimeReportingApi/Services/ValidationService.cs`
- `TimeReportingApi.Tests/GraphQL/MutationTests.cs`

---

## Phase 5: GraphQL API - Mutations Part 2 (5 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 5.1 | MoveTaskToProject Mutation | Implement `moveTaskToProject` with revalidation | 1-2 hrs | ☑ Completed | Create |
| 5.2 | UpdateTags Mutation | Implement `updateTags` with tag validation | 1 hr | ☑ Completed | Create |
| 5.3 | SubmitTimeEntry Mutation | Implement `submitTimeEntry` for workflow | 30 min | ☑ Completed | Create |
| 5.4 | Approve/Decline Mutations | Implement `approveTimeEntry` and `declineTimeEntry` | 1 hr | ☑ Completed | Create |
| 5.5 | Workflow Tests | Write integration tests for complete approval workflow | 1-2 hrs | ☑ Completed | Create |

**Phase 5 Total:** 5-6.5 hours

**Key Deliverables:**
- `TimeReportingApi/GraphQL/Mutation.cs` - All Phase 5 mutations implemented
- `TimeReportingApi.Tests/GraphQL/MoveTaskToProjectMutationTests.cs` - 10 tests
- `TimeReportingApi.Tests/GraphQL/UpdateTagsMutationTests.cs` - 8 tests
- `TimeReportingApi.Tests/GraphQL/WorkflowMutationTests.cs` - 14 tests

---

## Phase 6: GraphQL API - Docker (4 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 6.1 | API Dockerfile | Create Dockerfile for ASP.NET Core API | 1 hr | ☑ Completed | [View](./tasks/phase-06-api-docker/task-6.1-api-dockerfile.md) |
| 6.2 | Update Docker Compose | Add GraphQL API service to docker-compose.yml | 30 min | ☑ Completed | [View](./tasks/phase-06-api-docker/task-6.2-update-docker-compose.md) |
| 6.3 | Environment Configuration | Set up environment variables for connection strings and secrets | 30 min | ☑ Completed | [View](./tasks/phase-06-api-docker/task-6.3-environment-configuration.md) |
| 6.4 | Integration Test | Test full stack (PostgreSQL + API) in Docker | 1 hr | ☑ Completed | [View](./tasks/phase-06-api-docker/task-6.4-integration-test.md) |

**Phase 6 Total:** 3 hours

**Key Deliverables:**
- `TimeReportingApi/Dockerfile`
- Updated `docker-compose.yml`
- `.env` with API configuration

---

## Phase 7: MCP Server - Setup (4 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 7.1 | MCP Project Init | Initialize C# .NET Console Application project with structure | 1 hr | ☑ Completed | [View](./tasks/phase-07-mcp-setup/task-7.1-mcp-project-init.md) |
| 7.2 | Dependencies | Install GraphQL.Client NuGet packages and configure GraphQL client | 30 min | ☑ Completed | [View](./tasks/phase-07-mcp-setup/task-7.2-dependencies.md) |
| 7.3 | JSON-RPC Models | Create JSON-RPC request/response models for MCP protocol | 30 min | ☑ Completed | [View](./tasks/phase-07-mcp-setup/task-7.3-json-rpc-models.md) |
| 7.4 | MCP Server Core | Implement stdio handler with tools/list and tools/call routing | 1 hr | ☑ Completed | [View](./tasks/phase-07-mcp-setup/task-7.4-mcp-server.md) |

**Phase 7 Total:** 3 hours

**Key Deliverables:**
- `TimeReportingMcp/TimeReportingMcp.csproj`
- `TimeReportingMcp/Program.cs` - Entry point
- `TimeReportingMcp/McpServer.cs` - Core server with stdio handling
- `TimeReportingMcp/Models/` - JSON-RPC models
- `TimeReportingMcp/Utils/` - Configuration and GraphQL client wrapper
- `TimeReportingMcp/Tools/` - Tool handlers (empty, filled in Phase 8)

---

## Phase 8: MCP Server - Tools Part 1 (4 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 8.1 | Log Time Tool | Implement `log_time` tool with GraphQL mutation | 1-2 hrs | ☑ Completed | [View](./tasks/phase-08-mcp-tools-part1/task-8.1-log-time-tool.md) |
| 8.2 | Query Entries Tool | Implement `query_time_entries` tool with filters | 1-2 hrs | ☑ Completed | [View](./tasks/phase-08-mcp-tools-part1/task-8.2-query-entries-tool.md) |
| 8.3 | Update Entry Tool | Implement `update_time_entry` tool | 1 hr | ☑ Completed | [View](./tasks/phase-08-mcp-tools-part1/task-8.3-update-entry-tool.md) |
| 8.4 | Error Handling | Add comprehensive error handling and validation | 1 hr | ☑ Completed | [View](./tasks/phase-08-mcp-tools-part1/task-8.4-error-handling.md) |

**Phase 8 Total:** 4-6 hours

**Key Deliverables:**
- `TimeReportingMcp/Tools/LogTimeTool.cs` - 8 tests passing
- `TimeReportingMcp/Tools/QueryEntriesTool.cs` - 7 tests passing
- `TimeReportingMcp/Tools/UpdateEntryTool.cs` - 7 tests passing
- `TimeReportingMcp/Utils/ErrorHandler.cs` - 10 tests passing
- `TimeReportingMcp.Tests/` - Complete test coverage (33 tests total)

---

## Phase 9: MCP Server - Tools Part 2 (4 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 9.1 | Move Task Tool | Implement `move_task_to_project` tool | 1 hr | ☐ Pending | Create |
| 9.2 | Delete Entry Tool | Implement `delete_time_entry` tool | 30 min | ☐ Pending | Create |
| 9.3 | Get Projects Tool | Implement `get_available_projects` tool | 1 hr | ☐ Pending | Create |
| 9.4 | Submit Entry Tool | Implement `submit_time_entry` tool | 30 min | ☐ Pending | Create |

**Phase 9 Total:** 3 hours

**Key Deliverables:**
- `mcp-server/src/tools/move-task.ts`
- `mcp-server/src/tools/delete-entry.ts`
- `mcp-server/src/tools/get-projects.ts`
- `mcp-server/src/tools/submit-entry.ts`

---

## Phase 10: MCP Server - Auto-tracking (4 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 10.1 | Session Context Manager | Create context manager for last project, task, timing | 1-2 hrs | ☐ Pending | Create |
| 10.2 | Detection Heuristics | Implement auto-tracking detection logic | 2 hrs | ☐ Pending | Create |
| 10.3 | Confirmation Prompts | Format suggestion prompts for Claude Code | 1 hr | ☐ Pending | Create |
| 10.4 | Context Persistence | Add in-memory persistence across sessions | 1 hr | ☐ Pending | Create |

**Phase 10 Total:** 5-6 hours

**Key Deliverables:**
- `mcp-server/src/context/session-manager.ts`
- `mcp-server/src/auto-tracking/detector.ts`
- `mcp-server/src/auto-tracking/suggester.ts`

---

## Phase 11: Integration & Testing (5 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 11.1 | Claude Code Configuration | Create MCP server config for Claude Code | 30 min | ☐ Pending | Create |
| 11.2 | E2E Test Scenarios | Write end-to-end test scenarios | 2 hrs | ☐ Pending | Create |
| 11.3 | Manual Workflow Test | Test manual time logging workflow | 1 hr | ☐ Pending | Create |
| 11.4 | Auto-tracking Test | Test auto-tracking with confirmation workflow | 1 hr | ☐ Pending | Create |
| 11.5 | Migration Workflow Test | Test project migration workflow | 1 hr | ☐ Pending | Create |

**Phase 11 Total:** 5.5 hours

**Key Deliverables:**
- `claude_desktop_config.json` (example)
- `tests/e2e/` - End-to-end test scripts
- `tests/scenarios/` - Test scenarios documentation

---

## Phase 12: Documentation & Deployment (5 tasks)

| # | Task | Description | Est. Time | Status | Details |
|---|------|-------------|-----------|--------|---------|
| 12.1 | API Documentation | Write GraphQL schema documentation | 1 hr | ☐ Pending | Create |
| 12.2 | MCP Setup Guide | Write MCP server setup and configuration guide | 1 hr | ☐ Pending | Create |
| 12.3 | User Guide | Create user guide for Claude Code commands and workflows | 1-2 hrs | ☐ Pending | Create |
| 12.4 | Architecture Diagram | Create system architecture diagram (visual) | 1 hr | ☐ Pending | Create |
| 12.5 | Deployment Guide | Write Docker Compose deployment guide | 1 hr | ☐ Pending | Create |

**Phase 12 Total:** 5-6 hours

**Key Deliverables:**
- `README.md` - Project overview
- `docs/SETUP.md` - Setup instructions
- `docs/USER_GUIDE.md` - Usage guide
- `docs/ARCHITECTURE.png` - Visual diagram
- `docs/DEPLOYMENT.md` - Deployment guide

---

## Summary

### Total Time Estimate

| Phase | Tasks | Estimated Hours |
|-------|-------|-----------------|
| Phase 1 | 3 | 3-4 |
| Phase 2 | 6 | 6-7.5 |
| Phase 3 | 5 | 4-6 |
| Phase 4 | 5 | 7-9 |
| Phase 5 | 5 | 5-6.5 |
| Phase 6 | 4 | 3 |
| Phase 7 | 4 | 3 |
| Phase 8 | 4 | 4-6 |
| Phase 9 | 4 | 3 |
| Phase 10 | 4 | 5-6 |
| Phase 11 | 5 | 5.5 |
| Phase 12 | 5 | 5-6 |
| **TOTAL** | **61** | **53.5-67 hours** |

**Estimated Project Duration:**
- **Full-time (8 hrs/day):** 6.5-8.5 working days
- **Part-time (4 hrs/day):** 13.5-17 working days
- **Side project (2 hrs/day):** 27-34 days

---

## Progress Tracking

### Milestones

- [x] **Milestone 1:** Database Ready (Phase 1 complete)
- [x] **Milestone 2:** API Core Ready (Phases 2-3 complete) ✅
- [x] **Milestone 2.5:** API Mutations Part 1 (Phase 4 complete) ✅
- [x] **Milestone 3:** API Mutations Part 2 (Phase 5 complete) ✅
- [x] **Milestone 4:** API Full Feature (Phase 6 complete) ✅
- [ ] **Milestone 5:** MCP Server Ready (Phases 7-9 complete)
- [ ] **Milestone 6:** Auto-tracking Ready (Phase 10 complete)
- [ ] **Milestone 7:** Production Ready (Phases 11-12 complete)

### Current Status

**Currently On:** Phase 9, Task 9.1 (MCP Tools Part 2)
**Last Updated:** 2025-10-29
**Overall Progress:** 59.0% (36/61 tasks)

---

## Task Creation Guidelines

When ready to start a task:

1. If detailed task file exists, follow it
2. If task file says "Create", use this template:

```markdown
# Task X.Y: [Task Name]

**Phase:** X - [Phase Name]
**Estimated Time:** [Time]
**Prerequisites:** [List]
**Status:** Pending

## Objective
[What needs to be done]

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

## Implementation
[Code/config to create]

## Testing
[How to verify it works]

## Related Files
[Files created/modified]

## Next Steps
[What comes after]
```

---

## Quick Reference

### Start Development

```bash
# 1. Clone repository
git clone <repo-url>
cd time-reporting-system

# 2. Start Phase 1
cd docs/tasks/phase-01-database
cat task-1.1-postgresql-schema.md

# 3. Start database
docker-compose up -d postgres

# 4. Continue with tasks sequentially
```

### Get Help

- **PRD:** `docs/prd/README.md`
- **Data Model:** `docs/prd/data-model.md`
- **API Spec:** `docs/prd/api-specification.md`
- **MCP Tools:** `docs/prd/mcp-tools.md`
- **Architecture:** `docs/prd/architecture.md`

---

**Ready to start? Begin with [Task 1.1: PostgreSQL Schema](./tasks/phase-01-database/task-1.1-postgresql-schema.md)!**
