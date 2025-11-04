# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 🚨 CRITICAL RULES - READ FIRST

**Before implementing ANY feature, you MUST:**

1. ✅ **Write tests FIRST** (Test-Driven Development is mandatory)
2. ✅ **Use slash commands** for build/test/run operations (direct dotnet commands are blocked)
3. ✅ **Run /test** and ensure ALL tests pass before committing
4. ✅ **Follow the Red-Green-Refactor cycle** for every feature

**Quick TDD Workflow:**
```
Write Test → Run /test (FAIL ❌) → Write Code → Run /test (PASS ✅) → Commit
```

**Available Commands:** `/build`, `/test`, `/run-api`, `/db-start` (see full list below)

---

## 🤖 AUTONOMOUS DEVELOPMENT WORKFLOW

**YOU ARE TRUSTED TO WORK AUTONOMOUSLY. Follow these rules:**

### Rule 1: Automatic Commits After Task Completion
**AFTER completing ANY task and ALL tests pass:**
1. ✅ Run `/test` to verify all tests pass
2. ✅ IMMEDIATELY commit changes with `git add` + `git commit`
3. ✅ Use descriptive commit message: `"Complete Task X.Y: [Description] - All tests passing"`
4. ✅ DO NOT ask for permission to commit - just do it

**Example commit message:**
```
Complete Task 2.3: Implement C# Entity Models with TDD

- Created TimeEntry, Project, ProjectTask, ProjectTag, TagValue entities
- Configured Entity Framework with snake_case mapping
- Added comprehensive unit tests for all models
- All tests passing ✅
```

### Rule 2: Phase Planning Before Execution
**When assigned a PHASE (e.g., "implement Phase 2"), FIRST do planning:**

1. ✅ Read ALL tasks in the phase from `docs/TASK-INDEX.md` and task guides
2. ✅ Identify tasks requiring user decisions:
   - Authentication/IdP integration choices
   - External service configurations
   - Architecture decisions with multiple valid approaches
   - Environment-specific settings
3. ✅ Ask ALL questions upfront in a single message
4. ✅ Document decisions in the relevant task files or create a decisions log
5. ✅ THEN execute the entire phase autonomously

**Planning template:**
```
📋 Phase X Planning Review

Tasks in this phase:
- Task X.1: [Description] - Ready to implement ✅
- Task X.2: [Description] - Ready to implement ✅
- Task X.3: [Description] - **NEEDS DECISION** ⚠️
  Question: Which IdP provider? (Azure AD, Auth0, Custom?)
- Task X.4: [Description] - Ready to implement ✅

Questions for you:
1. Task X.3 - IdP Integration: Which provider should we use?
2. Task X.3 - Token format: JWT or opaque tokens?

Once you answer, I'll document the decisions and execute all tasks autonomously.
```

### Rule 3: Autonomous Phase Execution (After Planning)
**After planning and getting your decisions:**
1. ✅ Execute ALL tasks in the phase sequentially
2. ✅ DO NOT stop between tasks to ask for permission
3. ✅ Complete the ENTIRE phase from start to finish
4. ✅ Commit after EACH task when tests pass
5. ✅ Only stop if you encounter an error you cannot resolve

**Phase execution pattern:**
```
Phase 2 planning → User answers questions → Document decisions
    ↓
Task 2.1: TDD → Tests pass → Commit → Move to Task 2.2
    ↓
Task 2.2: TDD → Tests pass → Commit → Move to Task 2.3
    ↓
Task 2.3: TDD → Tests pass → Commit → Move to Task 2.4
    ↓
... continue until all phase tasks complete
    ↓
Report: "Phase 2 complete. All 5 tasks finished, all tests passing."
```

### Rule 4: What to Report During Execution
**During autonomous execution:**
- ✅ Show progress updates (currently on Task X.Y)
- ✅ Show test results for each task
- ✅ Show commit messages
- ❌ DO NOT ask "shall I proceed to the next task?"
- ❌ DO NOT wait for confirmation between tasks

### Rule 5: Error Handling
**If a task fails:**
1. Attempt to fix the issue (up to 2-3 attempts)
2. If still failing, report the error with details
3. Ask for guidance or clarification
4. DO NOT commit broken code

**Remember:**
- Plan phases FIRST, ask all questions upfront
- Then execute autonomously with auto-commits
- You are trusted to complete entire phases once decisions are made

---

## 📋 ARCHITECTURE DECISION RECORDS (ADRs)

**When system design discussions lead to architectural decisions, you MUST create an ADR.**

### What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences.

**An ADR documents:**
- **Context**: What problem or situation led to this decision?
- **Decision**: What did we decide to do?
- **Rationale**: Why did we choose this approach?
- **Consequences**: What are the trade-offs? (benefits and costs)
- **Implementation**: How do we apply this decision?

### When to Create an ADR

**✅ Create an ADR when making decisions that:**
- Affect the system's structure, patterns, or design principles
- Have long-term impact on the codebase
- Involve trade-offs between competing concerns
- Future developers need to understand the "why" behind the "what"
- Change a previous architectural approach

**Examples of ADR-worthy decisions:**
- ✅ Choosing shadow properties over explicit FK properties
- ✅ Using C# for both API and MCP server (mono-stack)
- ✅ Normalized relational schema vs JSONB
- ✅ Entity naming conventions (ProjectTag vs TagConfiguration)
- ❌ Renaming a variable (too small)
- ❌ Fixing a bug (not architectural)
- ❌ Adding a utility function (not a design decision)

### ADR Recognition Pattern - CRITICAL

**During conversations, you MUST recognize "ADR moments" and immediately announce them:**

#### 🚨 ADR TRIGGER PHRASES - STOP AND ANNOUNCE

**When the user says ANY of these phrases, IMMEDIATELY stop and announce an ADR moment:**

| User Says | ADR Trigger | What to Announce |
|-----------|-------------|------------------|
| "isn't this overkill?" | ✅ YES | Questioning documented approach = alternative being proposed |
| "can't we just use [X]?" | ✅ YES | Simpler alternative = architectural choice |
| "wouldn't it be better to [X]?" | ✅ YES | Design alternative = trade-off discussion |
| "I think we should use [X] instead" | ✅ YES | Alternative proposal = architectural decision |
| "there's a possibility to [X]" | ✅ YES | Different approach = design choice |
| "what about [alternative]?" | ✅ YES | Exploring alternatives = ADR needed |
| "why not [different approach]?" | ✅ YES | Questioning decision = trade-offs |

**CRITICAL: When you see these phrases, your FIRST response must be:**
```
"This feels like an ADR - we're making an architectural decision about [topic].
Let me explore this before updating any implementation files."
```

**Then:**
1. Discuss the alternatives and trade-offs
2. Document the decision as an ADR FIRST
3. ONLY THEN update task files or implementation

#### Pattern Recognition

When the conversation involves:
- **Design alternatives** with trade-offs being discussed
- **"Should we use X or Y?"** questions about architecture
- **Pattern/principle establishment** for the project
- **Refactoring decisions** that change architectural approach
- **Technology choices** between competing options
- **User questioning documented approach** (e.g., "isn't this overkill?")
- **User suggesting simpler/alternative approach** (e.g., "can't we just use X?")

**YOU MUST:**
1. ✅ **Immediately announce**: "This feels like an ADR - we're making an architectural decision about [X]"
2. ✅ **Continue the discussion** to reach a decision
3. ✅ **Document the decision** as an ADR in `docs/adr/` BEFORE changing implementation
4. ✅ **Update the index** in `docs/adr/README.md`
5. ✅ **Commit the ADR** separately from implementation code

#### Example Dialogue Flow - CORRECT ✅

```
User: "I think we should use shadow properties for FKs to prevent conflicts"
Claude: "This feels like an ADR - we're making an architectural decision about
         foreign key property management. Let me explore this design decision..."

[Discussion continues, decision is made]

Claude: "I'm going to document this as ADR 0001: Shadow Foreign Keys"
        [Creates docs/adr/0001-shadow-foreign-keys.md]
        [Updates docs/adr/README.md index]
        [Commits ADR]
```

#### Real Example - What NOT to Do ❌

**What happened (WRONG):**
```
User: "isn't this overkill: there is a possibility to expose DbContext via
       HotChocolate register context feature"

Claude: [WRONG - Immediately updated task files without announcing ADR]
        "You're right! Let me update the task files to use HotChocolate conventions..."
        [Updates all 5 task files]
        [User has to point out: "isn't this an ADR?"]
```

**What SHOULD have happened (CORRECT):**
```
User: "isn't this overkill: there is a possibility to expose DbContext via
       HotChocolate register context feature"

Claude: "This feels like an ADR - we're making an architectural decision about
         GraphQL query implementation: custom resolvers vs HotChocolate conventions.
         Let me explore this before updating task files."

[Discussion about trade-offs: code volume, flexibility, control]

Claude: "I'm going to document this as ADR 0006: HotChocolate Conventions Over Custom Resolvers"
        [Creates docs/adr/0006-hotchocolate-conventions-over-resolvers.md]
        [Updates docs/adr/README.md index]
        [Commits ADR separately]

        "Now let me update the task files to reflect this decision..."
        [Updates task files]
        [Commits implementation changes separately]
```

**Key lesson:** The phrase "isn't this overkill?" is a TRIGGER. Stop immediately and announce the ADR moment.

#### What NOT to Document as ADRs

❌ **Bug fixes** - Not architectural decisions
❌ **Trivial choices** - Obvious decisions without trade-offs
❌ **Implementation details** - "How" without "why"
❌ **Temporary workarounds** - Not permanent decisions

### ADR Creation Process

**When you recognize an ADR moment:**

1. **Announce the ADR moment**
   ```
   "This feels like an ADR - we're making an architectural decision about [topic]"
   ```

2. **Facilitate the decision**
   - Ask clarifying questions if needed
   - Explore trade-offs
   - Discuss alternatives

3. **Create the ADR**
   - Use the template in `docs/adr/TEMPLATE.md`
   - Number it sequentially (e.g., `0006-next-decision.md`)
   - Follow the structure: Context → Decision → Rationale → Consequences → Implementation → Alternatives

4. **Update the index**
   - Add entry to the table in `docs/adr/README.md`

5. **Commit separately**
   - Commit message: `"ADR 00XX: [Title]"`
   - Keep ADR commits separate from implementation

### ADR Directory Structure

```
docs/adr/
├── README.md                        # Index and process documentation
├── TEMPLATE.md                      # Template for new ADRs
├── 0001-shadow-foreign-keys.md     # Existing ADRs
├── 0002-csharp-mono-stack.md
├── 0003-naming-consistency.md
├── 0004-normalized-schema.md
├── 0005-relational-over-jsonb.md
└── XXXX-new-decision.md             # Future ADRs
```

### Quick Reference: ADR vs Implementation

| Aspect | ADR | Implementation |
|--------|-----|----------------|
| **What** | Why we chose approach X | How to implement approach X |
| **When** | During design discussions | During coding tasks |
| **Content** | Context, decision, trade-offs | Code, tests, documentation |
| **Commit** | Separate ADR commit | Task completion commit |

**Remember:** If you're discussing trade-offs and "should we use X or Y?" - it's probably an ADR moment!

---

## 🔧 Environment-Specific Commands - CRITICAL

**This environment uses Podman, NOT Docker Desktop.**

### Container Commands

**✅ ALWAYS USE:**
- `podman compose up -d` (with space, not hyphen)
- `podman compose down`
- `podman compose ps`
- `podman compose logs postgres`
- `podman exec time-reporting-db <command>`

**❌ NEVER USE:**
- `docker-compose` (Docker daemon not running)
- `podman-compose` (not installed)
- `docker` commands (will fail)

### Database Access

**✅ ALWAYS USE:**
```bash
# Run SQL queries
podman exec time-reporting-db psql -U postgres -d time_reporting -c "SELECT 1;"

# Run SQL files
podman exec -i time-reporting-db psql -U postgres -d time_reporting < file.sql

# Interactive session (via slash command)
/db-psql
```

**❌ NEVER USE:**
- `psql` directly on host (not installed)
- Assume PostgreSQL client is available locally

### Database Container Info
- **Container name:** `time-reporting-db`
- **Image:** `postgres:16-alpine`
- **Port:** `5432:5432`
- **Database:** `time_reporting`
- **User:** `postgres`

### Preferred Pattern

**ALWAYS prefer slash commands when available:**
- `/db-start` - Starts PostgreSQL via guardrails
- `/db-stop` - Stops PostgreSQL
- `/db-logs` - Views logs
- `/db-psql` - Interactive psql session
- `/db-restart` - Restarts database

Slash commands handle the Podman/Docker abstraction for you.

---

## Project Overview

A time reporting system that integrates Claude Code with a GraphQL-based time tracker. The system allows developers to track time spent on coding tasks through natural language commands via Claude Code.

**Technology Stack:** Single language (C#) for everything
- **Database:** PostgreSQL 16
- **API:** ASP.NET Core 10 + HotChocolate GraphQL + Entity Framework Core
- **MCP Server:** C# Console Application (~200 lines!)
- **Container:** Docker/Podman

## Architecture

```
Claude Code (Natural Language)
    ↓ stdio (JSON-RPC)
C# MCP Server (Console App)
    ↓ HTTP/GraphQL
C# GraphQL API (ASP.NET Core + HotChocolate)
    ↓ Entity Framework
PostgreSQL Database
```

### Key Components

1. **PostgreSQL Database** - Core data storage with 4 tables: time_entries, projects, project_tasks, tag_configurations
2. **GraphQL API** - ASP.NET Core with HotChocolate providing 4 queries and 8 mutations
3. **MCP Server** - Lightweight C# console app that bridges Claude Code to the GraphQL API (just reads JSON from stdin, calls GraphQL, writes JSON to stdout)
4. **Claude Code Integration** - MCP server provides 7 tools for time tracking operations

### Database Schema & Seeding

**Schema Management:** EF Core Code-First Migrations
- **Location**: `TimeReportingApi/Migrations/`
- **Apply migrations**: Automatically applied on API startup OR use `/ef-migration` slash command
- **Create new migration**: `dotnet ef migrations add <MigrationName> --project TimeReportingApi`

**Seed Data:** TimeReportingSeeder Console Application
- **Location**: `TimeReportingSeeder/`
- **Purpose**: Populate database with sample projects, tasks, tags, and time entries
- **Run seeder**: Use `/seed-db` slash command OR `dotnet run --project TimeReportingSeeder`
- **Data seeded**: Projects (INTERNAL, CLIENT-A, MAINT), tasks, tag configurations, sample time entries with user information

**IMPORTANT**: Do NOT use SQL files - all schema changes are managed via EF Core migrations, and seed data is managed via the TimeReportingSeeder project.

### StrawberryShake Typed GraphQL Client

The MCP server uses **StrawberryShake 15** for strongly-typed GraphQL client code generation:

**How it works:**
1. `.graphql` operation files define all queries and mutations
2. StrawberryShake generates C# typed client code at build time
3. Tools use `ITimeReportingClient` with full IntelliSense

**Benefits:**
- ✅ Compile-time type safety
- ✅ Zero manual type definitions
- ✅ Types always synchronized with API schema
- ✅ ~250 lines of code eliminated

**When API schema changes:**
```bash
# Download latest schema
curl -s "http://localhost:5001/graphql?sdl" > TimeReportingMcp/schema.graphql

# Rebuild (triggers code regeneration)
/build-mcp
```

See [ADR 0009](docs/adr/0009-strawberryshake-typed-graphql-client.md) for architectural decision details.

## Development Commands - IMPORTANT GUARDRAILS

**⚠️ CRITICAL: DO NOT use direct dotnet or docker-compose commands. Instead, use custom slash commands.**

This project uses a guardrails system with 3 layers of defense to ensure safe command execution:
1. **Pre-Bash Hook** - First-line intercept for all Bash commands
2. **Permission Checks** - Explicit allow/deny lists in settings
3. **PreToolUse Hook** - Detailed enforcement with helpful error messages

### Available Slash Commands

Custom slash commands are defined in `.claude/commands/`. **YOU HAVE PERMISSION** to run these commands automatically without asking after making code changes.

#### Build Commands
- **`/build`** - Build the entire solution (API + MCP Server)
- **`/build-api`** - Build only the GraphQL API project
- **`/build-mcp`** - Build only the MCP Server project

#### Test Commands
- **`/test`** - Run all tests (API + MCP Server)
- **`/test-api`** - Run API tests only
- **`/test-mcp`** - Run MCP Server tests only

#### Run Commands
- **`/run-api`** - Run the GraphQL API with hot reload (http://localhost:5001)
- **`/run-mcp`** - Run the MCP Server (normally started by Claude Code automatically)
- **`/stop-api`** - Stop the running GraphQL API
- **`/stop-mcp`** - Stop the running MCP Server

#### Database Commands
- **`/db-start`** - Start PostgreSQL database (Docker/Podman)
- **`/db-stop`** - Stop PostgreSQL database
- **`/db-restart`** - Restart PostgreSQL database
- **`/db-logs`** - View PostgreSQL logs
- **`/db-psql`** - Connect to PostgreSQL database with psql client

#### Entity Framework Commands
- **`/ef-migration`** - Create and apply Entity Framework migrations

### Usage Pattern

After making code changes:
1. Run `/build` to compile
2. Run `/test` to verify tests pass
3. Run `/run-api` to start the server
4. Test your changes in GraphQL Playground (http://localhost:5001/graphql)

### Why Slash Commands?

Direct execution of `dotnet build`, `dotnet test`, `docker-compose up`, etc. is **blocked by hooks** to:
- Prevent accidental execution of destructive commands
- Ensure consistent command patterns across development
- Provide centralized error handling and messaging
- Enable safe automation without user confirmation

### Allowed Direct Commands

The following commands ARE allowed and can be run directly:
- `git add`, `git commit`, `git status`, `git log`, `git diff`
- `psql` (after using /db-psql)
- `ls`, `cat`, `pwd`, `mkdir`, `chmod`

All other operations should use slash commands.

## Code Architecture

### Database Schema (docs/prd/data-model.md)

**TimeEntry** - Core entity for time log entries
- Project, Task, Hours (standard/overtime)
- Start/Completion dates
- Status workflow: NOT_REPORTED → SUBMITTED → APPROVED/DECLINED
- JSONB tags for metadata
- Validation: project must exist, task must be in project's available tasks, tags must match project configuration

**Project** - Available projects with tasks and tag configurations
- Code (PK, VARCHAR 10), Name, IsActive
- One-to-many: ProjectTask, ProjectTag

**ProjectTask** - Allowed tasks per project (e.g., "Development", "Bug Fixing")

**ProjectTag** - Metadata tags per project (renamed from TagConfiguration for naming consistency)
- One-to-many: TagValue

**TagValue** - Allowed values for each tag (normalized from JSONB)

### GraphQL API Layer (docs/prd/api-specification.md)

**Queries:**
- `timeEntries(filters)` - Filter by project, date range, status, user
- `timeEntry(id)` - Single entry by ID
- `projects(activeOnly)` - List all projects
- `project(code)` - Single project with tasks and tags

**Mutations:**
- `logTime(input)` - Create time entry with validation
- `updateTimeEntry(id, input)` - Update editable entry
- `moveTaskToProject(entryId, newProjectCode, newTask)` - Move entry between projects with revalidation
- `updateTags(entryId, tags)` - Update metadata tags
- `deleteTimeEntry(id)` - Delete if NOT_REPORTED status
- `submitTimeEntry(id)` - Submit for approval workflow
- `approveTimeEntry(id)` - Approve submitted entry
- `declineTimeEntry(id, comment)` - Decline with comment

**Authentication:** Bearer token middleware validates Authorization header

**Validation Layers:**
1. GraphQL schema (input types, non-null enforcement)
2. Business logic service (project exists, task valid, tags valid, status transitions)
3. Database constraints (foreign keys, check constraints)

### MCP Server Architecture (docs/prd/architecture.md)

The MCP server is intentionally simple (~200 lines total):

**Structure:**
- `Program.cs` - Main entry point
- `McpServer.cs` - JSON-RPC stdio handler that routes to tool handlers
- `Tools/` - 7 tool handlers (log_time, query_time_entries, update_time_entry, move_task_to_project, delete_time_entry, get_available_projects, submit_time_entry)
- Each tool: reads stdin → calls GraphQL → writes stdout

**Implementation Pattern (10-20 lines per tool):**
```csharp
private async Task<JsonRpcResponse> LogTime(Dictionary<string, JsonElement> args)
{
    var mutation = new GraphQLRequest
    {
        Query = @"mutation LogTime($input: LogTimeInput!) { ... }",
        Variables = new { input = args }
    };

    var response = await _graphqlClient.SendMutationAsync<LogTimeResponse>(mutation);

    return new JsonRpcResponse
    {
        Result = new { content = new[] { new { type = "text", text = $"Created entry {response.Data.LogTime.Id}" } } }
    };
}
```

## Task-Based Development Workflow

This project follows a task-based implementation approach documented in `docs/TASK-INDEX.md`.

**12 Phases, 60 Tasks, ~52-65 hours total:**
1. **Phase 1:** Database & Infrastructure (3 tasks, 3-4 hrs)
2. **Phase 2:** GraphQL API - Core Setup (5 tasks, 4.5-5.5 hrs)
3. **Phase 3:** GraphQL API - Queries (5 tasks, 4-6 hrs)
4. **Phase 4:** GraphQL API - Mutations Part 1 (5 tasks, 7-9 hrs)
5. **Phase 5:** GraphQL API - Mutations Part 2 (5 tasks, 5-6.5 hrs)
6. **Phase 6:** GraphQL API - Docker (4 tasks, 3 hrs)
7. **Phase 7:** MCP Server - Setup (4 tasks, 3 hrs)
8. **Phase 8:** MCP Server - Tools Part 1 (4 tasks, 4-6 hrs)
9. **Phase 9:** MCP Server - Tools Part 2 (4 tasks, 3 hrs)
10. **Phase 10:** MCP Server - Auto-tracking (4 tasks, 5-6 hrs) - **Moved to v2**
11. **Phase 11:** Integration & Testing (5 tasks, 5.5 hrs)
12. **Phase 12:** Documentation & Deployment (5 tasks, 5-6 hrs)

**Current Status:** Phase 1, Task 1.1 - No code implementation has begun yet.

**Task Workflow (with TDD):**
1. Check `docs/TASK-INDEX.md` for task list
2. Find detailed guide in `docs/tasks/phase-XX-name/task-X.Y-name.md`
3. **Write tests FIRST** that verify acceptance criteria (RED phase)
4. Run `/test` to verify tests fail ❌
5. Implement minimum code to pass tests (GREEN phase)
6. Run `/test` to verify tests pass ✅
7. Refactor if needed, running `/test` after each change
8. Run `/test` one final time - ALL tests must pass ✅
9. Check off in index and proceed to next task

**Remember:** A task is NOT complete until all tests pass. Never skip the TDD workflow.

**Implementation Note:** The v1 implementation includes auto-tracking features (Phase 10) to provide intelligent, proactive time logging suggestions based on user activity patterns.

## Key Documentation

**Start Here:**
- `docs/IMPLEMENTATION-SUMMARY.md` - Quick overview of simplified C# approach
- `README.md` - Project overview and getting started

**Technical Specifications:**
- `docs/prd/README.md` - Complete product requirements
- `docs/prd/architecture.md` - System design with MCP server code examples
- `docs/prd/data-model.md` - Database schema and Entity Framework config
- `docs/prd/api-specification.md` - GraphQL queries and mutations with examples
- `docs/prd/mcp-tools.md` - MCP tool definitions for Claude Code

**Implementation:**
- `docs/TASK-INDEX.md` - Master task list with links to detailed guides
- `docs/tasks/` - Phase-specific implementation tasks
- `docs/PODMAN-SETUP.md` - Podman alternative to Docker Desktop

## Important Implementation Guidelines

### Status Workflow

Time entries follow a strict state machine:
- **NOT_REPORTED** → SUBMITTED (only this transition allowed from initial state)
- **SUBMITTED** → APPROVED or DECLINED (read-only until approval)
- **DECLINED** → SUBMITTED (can resubmit after decline)
- **APPROVED** → terminal state (immutable)

Only entries in NOT_REPORTED or DECLINED status can be edited or deleted.

### Validation Requirements

Multi-layer validation ensures data integrity:

1. **Project validation:** Must exist and be active
2. **Task validation:** Must be in project's available tasks and active
3. **Tag validation:** Tag name must exist in project's tag configurations, tag value must be in allowed values
4. **Date validation:** start_date <= completion_date
5. **Hours validation:** standard_hours >= 0, overtime_hours >= 0
6. **Status transition validation:** Only allowed transitions per workflow

### Entity Framework Configuration

- Use `DateOnly` for dates (not DateTime)
- Normalized relational tables (no JSONB): ProjectTag, TagValue, TimeEntryTag
- Snake_case table/column names via mapping
- Cascade delete for ProjectTask and ProjectTag when Project deleted
- Precision(10,2) for decimal hours

### MCP Server Principles

- **Keep it simple:** ~200 lines total, just stdio + GraphQL calls
- No session management, no auto-tracking (v2 feature)
- Each tool is stateless: read params → call GraphQL → return result
- Configuration via .NET Configuration system (GRAPHQL_API_URL, Authentication__BearerToken)
- Error handling returns structured MCP error responses

## Security Notes

- Bearer token authentication for API access
- Tokens stored in environment variables (never commit to version control)
- Input validation at GraphQL schema, business logic, and database constraint levels
- Use `openssl rand -base64 32` to generate secure tokens

## Test-Driven Development (TDD) Workflow - MANDATORY

**⚠️ CRITICAL: Always write tests BEFORE implementing features. This is non-negotiable.**

This project follows strict Test-Driven Development (TDD) practices. When implementing any new feature or fixing a bug, you MUST follow the Red-Green-Refactor cycle:

### The Red-Green-Refactor Cycle

#### 1. RED - Write a Failing Test First
**Before writing ANY feature code:**
1. Create or update the test file for the component you're about to implement
2. Write a test that describes the desired behavior
3. Run `/test` (or `/test-api`, `/test-mcp`) to verify the test FAILS
4. The test should fail for the right reason (e.g., method doesn't exist, returns wrong value)

**Example:**
```csharp
[Fact]
public async Task LogTime_WithValidInput_CreatesTimeEntry()
{
    // Arrange
    var input = new LogTimeInput
    {
        ProjectCode = "INTERNAL",
        Task = "Development",
        StandardHours = 8.0m,
        StartDate = DateOnly.FromDateTime(DateTime.Today),
        CompletionDate = DateOnly.FromDateTime(DateTime.Today)
    };

    // Act
    var result = await _mutation.LogTime(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("INTERNAL", result.ProjectCode);
    Assert.Equal(TimeEntryStatus.NotReported, result.Status);
}
```

Run `/test-api` → Test should FAIL ❌ (method doesn't exist yet)

#### 2. GREEN - Write Minimal Code to Pass
**After the test fails:**
1. Implement the MINIMUM code necessary to make the test pass
2. Don't add extra features or "nice-to-haves"
3. Focus only on satisfying the test requirements
4. Run `/test` again to verify the test now PASSES

**Example:**
```csharp
public async Task<TimeEntry> LogTime(LogTimeInput input)
{
    var entry = new TimeEntry
    {
        Id = Guid.NewGuid(),
        ProjectCode = input.ProjectCode,
        Task = input.Task,
        StandardHours = input.StandardHours,
        StartDate = input.StartDate,
        CompletionDate = input.CompletionDate,
        Status = TimeEntryStatus.NotReported,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    await _dbContext.TimeEntries.AddAsync(entry);
    await _dbContext.SaveChangesAsync();

    return entry;
}
```

Run `/test-api` → Test should PASS ✅

#### 3. REFACTOR - Improve Code Quality (Optional)
**After tests pass:**
1. Refactor code to improve design, readability, or performance
2. Run `/test` after each refactoring step to ensure tests still pass
3. Never refactor without passing tests

### TDD Workflow for Feature Implementation

When implementing a new feature from the TASK-INDEX:

```
Step 1: READ the task requirements thoroughly
    ↓
Step 2: WRITE test(s) that verify acceptance criteria
    ↓
Step 3: RUN /test → Verify tests FAIL ❌
    ↓
Step 4: IMPLEMENT minimum code to pass tests
    ↓
Step 5: RUN /test → Verify tests PASS ✅
    ↓
Step 6: REFACTOR if needed (run /test after each change)
    ↓
Step 7: COMMIT with message describing feature + "All tests passing"
```

### Mandatory Test Coverage

**Every feature MUST have tests for:**

1. **Happy Path:** Normal, expected usage
2. **Edge Cases:** Boundary conditions, empty inputs, null values
3. **Error Cases:** Invalid inputs, business rule violations
4. **Validation:** All validation rules are enforced

**Example test suite for LogTime mutation:**
```csharp
// Happy path
[Fact] LogTime_WithValidInput_CreatesTimeEntry()

// Edge cases
[Fact] LogTime_WithZeroHours_CreatesTimeEntry()
[Fact] LogTime_WithSameDateRange_CreatesTimeEntry()

// Error cases
[Fact] LogTime_WithInvalidProject_ThrowsValidationException()
[Fact] LogTime_WithInvalidTask_ThrowsValidationException()
[Fact] LogTime_WithNegativeHours_ThrowsValidationException()
[Fact] LogTime_WithEndDateBeforeStartDate_ThrowsValidationException()

// Validation
[Fact] LogTime_WithInactivProject_ThrowsValidationException()
[Fact] LogTime_WithInvalidTags_ThrowsValidationException()
```

### Test Execution Requirements

**BEFORE committing ANY code:**
1. Run `/test` to execute all tests
2. ALL tests MUST pass ✅
3. Zero failing tests is mandatory
4. Zero warnings is mandatory (warnings treated as errors)

**If any test fails:**
- DO NOT commit the code
- Fix the implementation or test
- Re-run `/test` until all pass

### Test Organization

```
TimeReportingApi.Tests/
├── GraphQL/
│   ├── Mutations/
│   │   ├── LogTimeMutationTests.cs
│   │   ├── UpdateTimeEntryMutationTests.cs
│   │   └── ...
│   └── Queries/
│       ├── TimeEntriesQueryTests.cs
│       └── ...
├── Services/
│   ├── ValidationServiceTests.cs
│   ├── WorkflowServiceTests.cs
│   └── ...
└── Integration/
    ├── TimeEntryWorkflowTests.cs
    └── ...
```

### Test Naming Convention

Use the pattern: `MethodName_Scenario_ExpectedBehavior`

**Good examples:**
- `LogTime_WithValidInput_CreatesTimeEntry`
- `UpdateTimeEntry_WhenApproved_ThrowsInvalidOperationException`
- `SubmitTimeEntry_WithNotReportedStatus_ChangesStatusToSubmitted`

**Bad examples:**
- `TestLogTime` (not descriptive)
- `Test1` (meaningless)
- `ItWorks` (not specific)

### Testing Tools and Frameworks

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library (optional but recommended)
- **Moq** - Mocking framework for dependencies
- **WebApplicationFactory** - Integration testing for API

### Example TDD Session

```
User: "Implement the UpdateTimeEntry mutation"

Claude Code:
1. ✅ Reading task requirements from docs/tasks/...
2. ✅ Creating TimeReportingApi.Tests/GraphQL/Mutations/UpdateTimeEntryMutationTests.cs
3. ✅ Writing test: UpdateTimeEntry_WithValidInput_UpdatesEntry
4. ✅ Running /test-api → Test FAILS (expected - mutation doesn't exist)
5. ✅ Implementing UpdateTimeEntry mutation in TimeReportingApi/GraphQL/Mutation.cs
6. ✅ Running /test-api → Test PASSES
7. ✅ Writing test: UpdateTimeEntry_WhenApproved_ThrowsException
8. ✅ Running /test-api → Test FAILS (not handling approved status)
9. ✅ Adding status check to UpdateTimeEntry
10. ✅ Running /test-api → Test PASSES
11. ✅ Running /test → ALL tests PASS
12. ✅ Committing: "Implement UpdateTimeEntry mutation - All tests passing"
```

### Accountability

- Every task completion MUST include passing tests
- Task is NOT complete until `/test` shows all green ✅
- Pull requests without tests will be rejected
- Broken tests in main branch are unacceptable

### TDD Benefits for This Project

1. **Confidence:** Know that your code works before deployment
2. **Documentation:** Tests serve as living documentation
3. **Regression Prevention:** Catch bugs before they reach production
4. **Design Improvement:** Writing tests first leads to better API design
5. **Faster Debugging:** Tests pinpoint exactly where failures occur

## Testing Approach

**Test Types:**
- **Unit Tests:** GraphQL resolvers, business logic services (ValidationService, WorkflowService)
- **Integration Tests:** Docker stack (PostgreSQL + API), database operations
- **End-to-End Tests:** MCP server tools via Claude Code
- **Test Scenarios:** Documented in `tests/scenarios/`

**All tests must follow TDD workflow described above.**

## Project Structure

```
time-reporting-system/
├── .claude/                    # Claude Code configuration
│   ├── config.json             # Hook registration
│   ├── settings.local.json     # Permissions and hooks
│   ├── commands/               # Slash command definitions (16 commands)
│   │   ├── build*.md           # Build commands
│   │   ├── test*.md            # Test commands
│   │   ├── run*.md             # Run/stop commands
│   │   ├── db-*.md             # Database commands
│   │   └── ef-migration.md     # EF migration command
│   └── hooks/                  # Guardrail hook scripts
│       ├── guard.sh            # Safe wrapper for dotnet commands
│       ├── pre_bash.sh         # First-line defense intercept
│       └── check-dotnet-commands.sh  # PreToolUse hook
├── docs/                       # All documentation
│   ├── prd/                    # Product requirements and specs
│   ├── tasks/                  # Phase-specific implementation guides
│   ├── TASK-INDEX.md           # Master task list
│   ├── IMPLEMENTATION-SUMMARY.md
│   └── PODMAN-SETUP.md
├── db/
│   └── schema/                 # SQL DDL and seed data
├── TimeReportingApi/           # C# GraphQL API (to be created)
│   ├── Models/
│   ├── GraphQL/
│   ├── Services/
│   └── Data/
├── TimeReportingMcp/           # C# MCP Server (to be created)
│   ├── Program.cs
│   ├── McpServer.cs
│   ├── Tools/
│   └── Models/
├── docker-compose.yml
├── .env
├── README.md
└── CLAUDE.md                   # This file
```

## Guardrails System Architecture

This project implements a comprehensive guardrails system to ensure safe command execution and prevent accidental destructive operations.

### Three Layers of Defense

#### Layer 1: Pre-Bash Hook (`.claude/hooks/pre_bash.sh`)
- **Trigger:** Every Bash tool call (registered in `.claude/config.json`)
- **Purpose:** First-line intercept before command execution
- **Blocks:**
  - Direct `dotnet` commands (build, test, run, watch, ef)
  - Direct `docker-compose` and `podman-compose` up/down/restart commands
- **Error Message:** Suggests appropriate slash commands

**Example:**
```bash
# Attempt: Bash("dotnet build")
# Result: ❌ Direct dotnet execution is prohibited. Use /build, /test, /run, or /ef-migration instead.
```

#### Layer 2: Permission Checks (`.claude/settings.local.json`)
- **Trigger:** Continuous evaluation by Claude Code
- **Purpose:** Explicit allow/deny lists for tools and commands
- **Allowed:**
  - All slash commands (/build, /test, /run-api, /db-start, etc.)
  - Git operations (add, commit, status, log, diff)
  - Safe utilities (psql, ls, cat, pwd, mkdir, chmod)
  - Web access (GitHub, Microsoft, PostgreSQL, StackOverflow)
- **Denied:** Anything not explicitly allowed (implicit deny)

#### Layer 3: PreToolUse Hook (`.claude/hooks/check-dotnet-commands.sh`)
- **Trigger:** Every Bash command matching pattern `Bash.*`
- **Purpose:** Detailed enforcement with helpful error messages
- **Detects:**
  - `dotnet build|test|run|watch|ef` patterns
  - `docker-compose` and `podman-compose` patterns
- **Returns:** JSON response with `deny` decision and user-friendly suggestions

**Example JSON Response:**
```json
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "deny",
    "permissionDecisionReason": "❌ Direct dotnet commands are not allowed..."
  }
}
```

### How Slash Commands Bypass Guardrails

All slash commands use the `guard.sh` wrapper with a special "slash" context parameter:

```bash
.claude/hooks/guard.sh "dotnet build" "slash"
                                       ^^^^^^
                                   Context flag
```

The `guard.sh` script checks:
1. Is command a `dotnet` command? If yes, check context
2. Is context "slash"? If yes, **allow execution**
3. Otherwise, block with error

This ensures dotnet commands can ONLY be executed through approved slash commands.

### Guardrails Flow Diagram

```
User attempts: Bash("dotnet build")
    ↓
Layer 1: pre_bash.sh intercepts
    - Pattern match: "^dotnet "
    - BLOCK ✅ (exit code 1)
    ↓
Layer 2: Permission check (if hook bypassed)
    - "Bash(dotnet:*)" NOT in allow list
    - DENY ✅
    ↓
Layer 3: PreToolUse hook (final check)
    - Regex: "^dotnet[[:space:]]+(build|test|run|watch|ef)"
    - JSON deny response with helpful message ✅
    ↓
Result: Command blocked, user sees helpful error
```

```
User invokes: /build
    ↓
Slash command executes: guard.sh "dotnet build" "slash"
    ↓
guard.sh checks:
    - Command is "dotnet build" ✓
    - Context is "slash" ✓
    - ALLOW execution ✅
    ↓
Command runs successfully
```

### Benefits of Guardrails System

1. **Safety:** Prevents accidental execution of destructive commands
2. **Consistency:** Enforces standard command patterns across development
3. **Automation:** Allows safe automatic execution without user confirmation
4. **Learning:** Provides helpful error messages with correct alternatives
5. **Flexibility:** Easy to extend with new slash commands and rules

### Adding New Slash Commands

To add a new slash command:

1. Create `.claude/commands/your-command.md`:
```markdown
---
description: Your command description
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Your command documentation.

### Execution
```bash
.claude/hooks/guard.sh "dotnet your-command" "slash"
```
```

2. Add to `.claude/settings.local.json`:
```json
{
  "permissions": {
    "allow": [
      "SlashCommand(/your-command)"
    ]
  }
}
```

3. Test the command and verify guardrails work correctly
