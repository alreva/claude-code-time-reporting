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

**Available Commands:** `/build`, `/test`, `/deploy`, `/db-start` (see full list below)
**Note:** Use `/deploy` by default for API deployment (containerized), not `/run-api`

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

#### Example Flow

When user proposes alternatives → Announce ADR → Discuss → Document in `docs/adr/` → Update index → Commit ADR → Then implement.

**Key lesson:** Phrases like "isn't this overkill?" are TRIGGERS. Stop immediately, announce the ADR moment, discuss trade-offs, then document as ADR before implementation.

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
2. `schema.graphql` defines the API schema for type generation
3. StrawberryShake generates C# typed client code at build time
4. Tools use `ITimeReportingClient` with full IntelliSense

**Benefits:**
- ✅ Compile-time type safety
- ✅ Zero manual type definitions
- ✅ Types always synchronized with API schema
- ✅ ~250 lines of code eliminated

**Schema Synchronization (Automated):**

The schema validation test executes the API's schema export command and compares it with the MCP schema:
1. **Schema Export Command**: `dotnet run --project TimeReportingApi -- export-schema` prints schema to stdout
2. **Schema Validation Test**: `SchemaValidationTests.cs` executes export command and compares with `TimeReportingMcp/schema.graphql`
3. **Test Failure**: If schemas don't match, test fails with helpful instructions

**When API schema changes and test fails:**
```bash
# 1. Export the current API schema to MCP project
dotnet run --project TimeReportingApi -- export-schema > TimeReportingMcp/schema.graphql

# 2. Rebuild MCP (triggers StrawberryShake code regeneration)
/build-mcp

# 3. Re-run tests to verify
/test
```

**Why this approach?**
- ✅ Prevents MCP schema drift from API schema
- ✅ StrawberryShake code generation always uses correct types
- ✅ Test-driven validation catches schema mismatches early
- ✅ No build dependencies - test executes export command directly
- ✅ Unix-friendly: schema export prints to stdout (can redirect anywhere)
- ✅ Works in any environment (CI/CD, local dev, not Claude Code-specific)

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
- **`/deploy`** - Build and deploy the full Docker stack (PostgreSQL + API) - **USE THIS BY DEFAULT**
- **`/run-api`** - Run the GraphQL API locally with hot reload (http://localhost:5001) - **ONLY when explicitly needed for debugging**
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

#### Deployment Commands
- **`/seed-db`** - Run database seeder to populate seed data

### Usage Pattern

**Default workflow (containerized deployment):**
After making code changes:
1. Run `/build` to compile
2. Run `/test` to verify tests pass
3. Run `/deploy` to deploy the full stack (PostgreSQL + API in containers)
4. Test your changes in GraphQL Playground (http://localhost:5001/graphql)

**Alternative workflow (local development with hot reload):**
Only use this when you need rapid iteration or debugging:
1. Run `/build` to compile
2. Run `/test` to verify tests pass
3. Run `/db-start` to start PostgreSQL container
4. Run `/run-api` to start API locally with hot reload
5. Test your changes in GraphQL Playground (http://localhost:5001/graphql)

**IMPORTANT:** By default, always use `/deploy` for a production-like containerized environment. Only use `/run-api` when explicitly requested or when you need hot reload for rapid development iterations.

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

### Database Schema

**Entities:** TimeEntry, Project, ProjectTask, ProjectTag, TagValue. Status workflow: NOT_REPORTED → SUBMITTED → APPROVED/DECLINED. EF Core with snake_case, DateOnly for dates, normalized relational design. See `docs/prd/data-model.md`.

### GraphQL API Layer

**Queries:** timeEntries, timeEntry, projects, project
**Mutations:** logTime, updateTimeEntry, moveTaskToProject, updateTags, deleteTimeEntry, submitTimeEntry, approveTimeEntry, declineTimeEntry
**Validation:** 3 layers (GraphQL schema, business logic, DB constraints)
See `docs/prd/api-specification.md` for details.

### MCP Server Architecture

**Simple design (~200 lines):** `Program.cs` (entry), `McpServer.cs` (JSON-RPC stdio), `Tools/` (7 tool handlers). Each tool: reads stdin → calls GraphQL → writes stdout. See `docs/prd/architecture.md` for implementation patterns.

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

## Implementation Guidelines

**Status Workflow:** NOT_REPORTED → SUBMITTED → APPROVED/DECLINED. Only NOT_REPORTED/DECLINED entries can be edited/deleted.

**Validation:** Project exists & active, task valid, tags valid, dates logical, hours ≥ 0, status transitions valid.

**EF Config:** DateOnly for dates, normalized relational (no JSONB), snake_case, cascade deletes, Precision(10,2) for hours.

**MCP Principles:** Simple (~200 lines), stateless tools, stdio + GraphQL, AzureCliCredential auth, structured error responses.

**Security:** Azure Entra ID (JWT validation), user identity tracking (oid/email/name), multi-layer validation, no secrets in config.

## Test-Driven Development (TDD) - MANDATORY

**⚠️ CRITICAL: Write tests BEFORE implementation. Follow Red-Green-Refactor cycle.**

**TDD Cycle:**
1. **RED** - Write failing test, run `/test` (FAIL ❌)
2. **GREEN** - Write minimal code to pass, run `/test` (PASS ✅)
3. **REFACTOR** - Improve code (optional), run `/test` after each change

**Test Coverage Required:**
- Happy path, edge cases, error cases, validation rules
- Test naming: `MethodName_Scenario_ExpectedBehavior`
- Tools: xUnit, FluentAssertions (optional), Moq, WebApplicationFactory

**Execution Requirements:**
- Run `/test` before committing - ALL tests must pass ✅
- Zero failing tests, zero warnings (treated as errors)
- Task NOT complete until all tests pass

See `docs/prd/` for detailed testing approach and scenarios.

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

### Benefits & Extensibility

**Benefits:** Safety, consistency, automation without confirmation, helpful errors, easy to extend.

**Adding Slash Commands:** Create `.claude/commands/your-command.md`, add to `.claude/settings.local.json` allow list, test guardrails.
