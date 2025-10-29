# System Architecture Documentation

**Time Reporting System with Claude Code Integration**

Version: 1.0

> **📖 About this document:** This is the main architecture documentation for users and contributors. For detailed implementation specifications and code examples, see [PRD Architecture Specification](./prd/architecture.md).

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture Diagrams](#architecture-diagrams)
3. [Component Details](#component-details)
4. [Data Flow](#data-flow)
5. [Technology Stack](#technology-stack)
6. [Design Decisions](#design-decisions)
7. [Security Architecture](#security-architecture)
8. [Deployment Architecture](#deployment-architecture)

---

## Overview

The Time Reporting System is a modern, cloud-native application that enables developers to track time naturally through Claude Code using the Model Context Protocol (MCP). The system follows a clean layered architecture with clear separation of concerns.

### Key Characteristics

- **Single Language:** C# throughout (API + MCP Server)
- **Protocol-Based:** MCP for Claude Code integration
- **API-First:** GraphQL API with comprehensive schema
- **Containerized:** Docker/Podman deployment
- **Database:** PostgreSQL with Entity Framework Core

---

## Architecture Diagrams

### High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         USER (Developer)                         │
│                    Using Claude Code CLI/IDE                     │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ Natural Language Commands
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                         CLAUDE CODE                              │
│                    (Anthropic's AI Assistant)                    │
│                                                                   │
│  - Interprets natural language                                   │
│  - Invokes MCP tools                                             │
│  - Formats responses                                             │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ JSON-RPC over stdio
                               │ (Model Context Protocol)
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                     MCP SERVER (C#)                              │
│              .NET Console Application (~300 lines)               │
│                                                                   │
│  Components:                                                      │
│  ├─ stdio Handler (JSON-RPC)                                     │
│  ├─ Tool Router (7 tools)                                        │
│  ├─ GraphQL Client Wrapper                                       │
│  ├─ Error Handler                                                │
│  └─ Auto-Tracking Engine (context + heuristics)                  │
│                                                                   │
│  Configuration:                                                   │
│  - GRAPHQL_API_URL                                               │
│  - Authentication__BearerToken                                                  │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ HTTP/HTTPS
                               │ GraphQL Queries/Mutations
                               │ Bearer Token Authentication
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                   GRAPHQL API (C#)                               │
│             ASP.NET Core 10 + HotChocolate 13                     │
│                                                                   │
│  Layer Structure:                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  GraphQL Layer (Query + Mutation)                        │   │
│  │  - 4 Queries, 8 Mutations                                │   │
│  │  - Input validation (schema-level)                       │   │
│  │  - Type definitions                                       │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                           │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │  Business Logic Layer                                     │   │
│  │  - ValidationService (project, task, tag validation)     │   │
│  │  - WorkflowService (status transitions)                  │   │
│  │  - Business rules enforcement                             │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                           │
│  ┌────────────────────▼────────────────────────────────────┐   │
│  │  Data Access Layer                                        │   │
│  │  - Entity Framework Core 10                                │   │
│  │  - TimeReportingDbContext                                 │   │
│  │  - Entity Models (TimeEntry, Project, etc.)              │   │
│  │  - Repository Pattern (via DbContext)                     │   │
│  └────────────────────┬────────────────────────────────────┘   │
│                       │                                           │
│  Middleware:                                                      │
│  - Bearer Token Authentication                                    │
│  - Exception Handling                                             │
│  - Request Logging                                                │
│                                                                   │
│  Health Check: /health                                            │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                               │ Npgsql Driver
                               │ Entity Framework Core
                               │ Connection String
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│                    POSTGRESQL 16                                 │
│                      (Database)                                   │
│                                                                   │
│  Schema:                                                          │
│  ├─ time_entries (core time log data)                            │
│  ├─ projects (project configurations)                            │
│  ├─ project_tasks (available tasks per project)                  │
│  ├─ project_tags (tag configurations per project)                │
│  ├─ tag_values (allowed values for tags)                         │
│  └─ time_entry_tags (many-to-many join table)                    │
│                                                                   │
│  Features:                                                        │
│  - JSONB for flexible metadata                                    │
│  - Foreign key constraints                                        │
│  - Check constraints (validation)                                 │
│  - Indexes for performance                                        │
│  - Cascade delete rules                                           │
└───────────────────────────────────────────────────────────────────┘
```

---

### Component Interaction Flow

```
┌─────────────┐
│    User     │
└──────┬──────┘
       │
       │ "Log 8 hours of development on INTERNAL for today"
       ▼
┌─────────────┐
│ Claude Code │
└──────┬──────┘
       │
       │ 1. Parse natural language
       │ 2. Identify: log_time tool needed
       │ 3. Extract parameters:
       │    - projectCode: "INTERNAL"
       │    - task: "Development"
       │    - standardHours: 8.0
       │    - startDate: 2025-10-29
       │    - completionDate: 2025-10-29
       ▼
┌─────────────┐
│ MCP Server  │◄─── JSON-RPC Request via stdio
└──────┬──────┘
       │
       │ 4. Route to LogTimeTool handler
       │ 5. Build GraphQL mutation:
       │    mutation {
       │      logTime(input: { ... }) {
       │        id, status, createdAt
       │      }
       │    }
       │ 6. Add Bearer token to request
       ▼
┌─────────────┐
│ GraphQL API │◄─── HTTP POST to /graphql
└──────┬──────┘     Authorization: Bearer <token>
       │
       │ 7. Authenticate request (Bearer token)
       │ 8. Parse GraphQL request
       │ 9. Route to Mutation.LogTime resolver
       │ 10. Call ValidationService:
       │     - ValidateProjectAsync(INTERNAL)
       │     - ValidateTaskAsync(INTERNAL, Development)
       │     - ValidateDateRange(2025-10-29, 2025-10-29)
       │     - ValidateHours(8.0, 0.0)
       │ 11. Load Project and ProjectTask entities
       │ 12. Create TimeEntry with navigation properties
       │ 13. Add to DbContext
       ▼
┌─────────────┐
│ PostgreSQL  │◄─── DbContext.SaveChangesAsync()
└──────┬──────┘
       │
       │ 14. Insert into time_entries table
       │ 15. Return generated ID and timestamps
       │ 16. Commit transaction
       ▼
┌─────────────┐
│ GraphQL API │───► Response:
└──────┬──────┘     {
       │              "data": {
       │                "logTime": {
       ▼                  "id": "a1b2c3d4-...",
┌─────────────┐             "status": "NOT_REPORTED",
│ MCP Server  │             "createdAt": "2025-10-29T..."
└──────┬──────┘           }
       │              }
       │            }
       │ 17. Format MCP response:
       ▼     {
┌─────────────┐       "content": [{
│ Claude Code │         "type": "text",
└──────┬──────┘         "text": "Time entry created..."
       │              }]
       │            }
       ▼
┌─────────────┐
│    User     │◄─── "Time entry created successfully!
└─────────────┘      Entry ID: a1b2c3d4-...
                     Status: NOT_REPORTED"
```

---

## Component Details

### 1. Claude Code (User Interface Layer)

**Role:** Natural language interface for developers

**Capabilities:**
- Interprets developer intent from natural language
- Invokes appropriate MCP tools with extracted parameters
- Formats and presents responses in user-friendly way
- Maintains conversation context

**Technology:**
- Anthropic's Claude AI
- MCP Client (built into Claude Code)

**Configuration:**
- `~/.config/claude-code/config.json` (macOS/Linux)
- `%APPDATA%\claude-code\config.json` (Windows)

---

### 2. MCP Server (Integration Layer)

**Role:** Bridge between Claude Code and GraphQL API

**Responsibilities:**
- Accept JSON-RPC requests over stdio
- Route tool calls to appropriate handlers
- Execute GraphQL queries/mutations
- Handle authentication (Bearer token)
- Format responses for Claude Code
- Manage auto-tracking state

**Architecture:**

```
TimeReportingMcp/
├── Program.cs                      # Entry point, stdio setup, DI
├── McpServer.cs                    # Core server, tool routing
├── GraphQL/                        # GraphQL operations (StrawberryShake)
│   ├── LogTime.graphql            # Create time entry mutation
│   ├── QueryTimeEntries.graphql   # Query entries
│   ├── UpdateTimeEntry.graphql    # Update entry mutation
│   ├── DeleteTimeEntry.graphql    # Delete entry mutation
│   ├── MoveTaskToProject.graphql  # Move entry mutation
│   ├── GetProjects.graphql        # Get projects query
│   └── SubmitTimeEntry.graphql    # Submit entry mutation
├── Tools/
│   ├── LogTimeTool.cs             # Create time entries
│   ├── QueryEntriesTool.cs        # Search/filter entries
│   ├── UpdateEntryTool.cs         # Update entries
│   ├── MoveTaskTool.cs            # Move between projects
│   ├── DeleteEntryTool.cs         # Delete entries
│   ├── GetProjectsTool.cs         # List projects/tasks
│   └── SubmitEntryTool.cs         # Submit for approval
├── AutoTracking/
│   ├── SessionContext.cs          # Session state management
│   ├── DetectionHeuristics.cs     # Work detection logic
│   ├── SuggestionFormatter.cs     # Format suggestions
│   └── ContextPersistence.cs      # Persist state to disk
├── Models/
│   ├── JsonRpcRequest.cs          # MCP protocol models
│   ├── JsonRpcResponse.cs
│   └── ToolDefinition.cs
├── .graphqlrc.json                # StrawberryShake config
├── schema.graphql                 # Downloaded GraphQL schema
└── obj/Debug/net10.0/berry/       # Generated typed client (gitignored)
    └── TimeReportingClient.cs     # ITimeReportingClient + types
```

**Key Features:**
- Stateless tool execution
- Strongly-typed GraphQL client (StrawberryShake)
- Compile-time type safety for all GraphQL operations
- Comprehensive error handling
- Auto-tracking suggestions (Phase 10)

**Technology:**
- .NET 10 Console Application
- StrawberryShake 15 - Typed GraphQL client with code generation
- System.Text.Json for JSON-RPC
- Microsoft.Extensions.DependencyInjection for DI

---

### 3. GraphQL API (Application Layer)

**Role:** Business logic and data access

**Responsibilities:**
- Expose GraphQL schema (queries + mutations)
- Validate all inputs (schema + business logic)
- Enforce business rules (status workflow, permissions)
- Manage database transactions
- Handle authentication/authorization

**Architecture:**

```
TimeReportingApi/
├── Program.cs                     # App startup, DI configuration
├── GraphQL/
│   ├── Query.cs                   # 4 queries
│   ├── Mutation.cs                # 8 mutations
│   ├── Types/
│   │   ├── TimeEntryType.cs      # GraphQL type definitions
│   │   ├── ProjectType.cs
│   │   └── ...
│   └── Inputs/
│       ├── LogTimeInput.cs       # Input types
│       ├── UpdateTimeEntryInput.cs
│       └── ...
├── Models/
│   ├── TimeEntry.cs              # Entity models
│   ├── Project.cs
│   ├── ProjectTask.cs
│   ├── ProjectTag.cs
│   ├── TagValue.cs
│   └── TimeEntryTag.cs
├── Data/
│   ├── TimeReportingDbContext.cs  # EF Core DbContext
│   └── Configurations/
│       ├── TimeEntryConfiguration.cs  # EF Core mappings
│       └── ...
├── Services/
│   ├── ValidationService.cs       # Business validation
│   └── WorkflowService.cs         # Status transitions
├── Middleware/
│   ├── BearerAuthMiddleware.cs    # Authentication
│   └── ExceptionMiddleware.cs     # Global error handling
└── Exceptions/
    ├── ValidationException.cs
    └── BusinessRuleException.cs
```

**API Endpoints:**
- `POST /graphql` - GraphQL endpoint
- `GET /health` - Health check

**Technology:**
- ASP.NET Core 10
- HotChocolate 13 (GraphQL)
- Entity Framework Core 10
- Npgsql (PostgreSQL driver)

---

### 4. PostgreSQL Database (Persistence Layer)

**Role:** Data persistence and integrity

**Schema Design:**

```sql
-- Core entity: time entries
time_entries (
  id UUID PK,
  project_code VARCHAR(10) FK → projects.code,
  project_task_id UUID FK → project_tasks.id,
  issue_id VARCHAR(30),
  standard_hours NUMERIC(10,2),
  overtime_hours NUMERIC(10,2),
  description TEXT,
  start_date DATE,
  completion_date DATE,
  status VARCHAR(20),  -- NOT_REPORTED, SUBMITTED, APPROVED, DECLINED
  decline_comment TEXT,
  created_at TIMESTAMP,
  updated_at TIMESTAMP
)

-- Project configurations
projects (
  code VARCHAR(10) PK,
  name VARCHAR(200),
  is_active BOOLEAN
)

-- Available tasks per project
project_tasks (
  id UUID PK,
  project_code VARCHAR(10) FK → projects.code,
  task_name VARCHAR(100),
  is_active BOOLEAN
)

-- Tag configurations per project
project_tags (
  id UUID PK,
  project_code VARCHAR(10) FK → projects.code,
  tag_name VARCHAR(20),
  is_required BOOLEAN
)

-- Allowed values for tags
tag_values (
  id UUID PK,
  project_tag_id UUID FK → project_tags.id,
  value VARCHAR(100)
)

-- Many-to-many: time entries ↔ tags
time_entry_tags (
  time_entry_id UUID FK → time_entries.id,
  tag_value_id UUID FK → tag_values.id,
  PRIMARY KEY (time_entry_id, tag_value_id)
)
```

**Key Constraints:**
- Foreign keys with cascade delete
- Check constraints (hours ≥ 0, dates valid)
- Unique constraints (project code, task names per project)
- Indexes on frequently queried fields

**Data Integrity:**
- Referential integrity via foreign keys
- Business rules enforced at application layer
- Transaction management via EF Core

---

## Data Flow

### Create Time Entry Flow

```
1. User Input
   "Log 8 hours of development on INTERNAL for today"

2. Claude Code Processing
   ├─ Parse: log_time tool needed
   ├─ Extract: projectCode=INTERNAL, task=Development, hours=8.0
   └─ Invoke: MCP tool with parameters

3. MCP Server
   ├─ Receive: JSON-RPC request via stdin
   ├─ Route: LogTimeTool.Execute()
   ├─ Build: GraphQL mutation
   ├─ Attach: Bearer token
   └─ Send: HTTP POST to GraphQL API

4. GraphQL API
   ├─ Authenticate: Verify Bearer token
   ├─ Parse: GraphQL mutation
   ├─ Validate:
   │   ├─ Project exists and active
   │   ├─ Task valid for project
   │   ├─ Date range valid
   │   └─ Hours non-negative
   ├─ Load: Project and ProjectTask entities
   ├─ Create: TimeEntry entity
   ├─ Add: Tags if provided
   └─ Save: DbContext.SaveChangesAsync()

5. PostgreSQL
   ├─ Insert: time_entries record
   ├─ Insert: time_entry_tags records (if tags)
   ├─ Generate: UUID, timestamps
   └─ Commit: Transaction

6. Response Flow
   GraphQL API
   ├─ Return: TimeEntry with ID
   ↓
   MCP Server
   ├─ Format: MCP response
   ├─ Write: JSON to stdout
   ↓
   Claude Code
   ├─ Parse: MCP response
   ├─ Format: User-friendly message
   ↓
   User
   └─ Display: "Time entry created successfully!"
```

---

### Query Time Entries Flow

```
1. User: "Show my time entries for this week"

2. Claude Code
   ├─ Parse: query_time_entries tool
   ├─ Calculate: startDate=2025-10-23, endDate=2025-10-29
   └─ Invoke: MCP tool

3. MCP Server
   ├─ Build: GraphQL query with filters
   ├─ Execute: HTTP POST

4. GraphQL API
   ├─ Authenticate
   ├─ Parse: Query.GetTimeEntries
   ├─ Apply: Filters (date range, status, etc.)
   ├─ Execute: EF Core LINQ query
   └─ Return: List of TimeEntry objects

5. PostgreSQL
   ├─ Execute: SQL SELECT with WHERE clause
   └─ Return: Rows

6. Response
   GraphQL API → MCP Server → Claude Code → User
   Formatted list of time entries with details
```

---

### Approval Workflow Flow

```
1. Create Entry (Status: NOT_REPORTED)
   ↓
2. Edit/Update (allowed while NOT_REPORTED)
   ↓
3. Submit (Status: NOT_REPORTED → SUBMITTED)
   ↓
4. Manager Review
   ├─ Approve → Status: APPROVED (immutable)
   └─ Decline → Status: DECLINED (can edit and resubmit)
       ↓
       5. Edit (allowed while DECLINED)
       ↓
       6. Resubmit (Status: DECLINED → SUBMITTED)
       ↓
       Back to step 4
```

---

## Technology Stack

### Backend

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| API Framework | ASP.NET Core | 8.0 | Web API foundation |
| GraphQL Server | HotChocolate | 13.x | GraphQL schema & execution |
| ORM | Entity Framework Core | 8.0 | Database access |
| Database | PostgreSQL | 16 | Data persistence |
| Driver | Npgsql | Latest | PostgreSQL driver |
| MCP Server | .NET Console App | 8.0 | MCP protocol bridge |
| GraphQL Client | GraphQL.Client | Latest | MCP→API communication |

### Infrastructure

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Container Runtime | Docker/Podman | Containerization |
| Orchestration | Docker Compose | Multi-container management |
| Reverse Proxy | (Future: Nginx) | SSL termination, load balancing |
| Logging | (Future: Serilog) | Structured logging |

### Development

| Tool | Purpose |
|------|---------|
| .NET SDK 8 | Development & build |
| Visual Studio Code | IDE |
| PostgreSQL Client | Database management |
| Postman/Insomnia | API testing |
| xUnit | Unit & integration testing |

---

## Design Decisions

### ADR 0001: Shadow Foreign Keys

**Decision:** Use Entity Framework shadow properties for foreign keys instead of explicit FK properties.

**Rationale:**
- Cleaner domain models (no FK pollution)
- EF Core manages FKs automatically
- Prevents accidental FK/navigation property conflicts

**Implementation:**
```csharp
// Set navigation property only
entry.Project = project;  // EF fills shadow FK automatically
```

### ADR 0002: C# Mono-Stack

**Decision:** Use C# for both API and MCP server (not TypeScript/Node.js).

**Rationale:**
- Single language, consistent patterns
- Simpler development & deployment
- MCP server is just ~300 lines of C# console app
- No need for Node.js runtime

### ADR 0003: Normalized Schema Over JSONB

**Decision:** Use normalized relational tables for tags instead of JSONB columns.

**Rationale:**
- Better type safety & validation
- Referential integrity via FKs
- Easier to query and enforce constraints
- PostgreSQL JSONB remains available for truly flexible data

### ADR 0004: HotChocolate Conventions Over Custom Resolvers

**Decision:** Use HotChocolate's built-in conventions instead of custom repository resolvers.

**Rationale:**
- Less boilerplate code
- Automatic filtering/sorting/pagination
- Better performance (automatic query optimization)
- Simpler codebase

### ADR 0005: Bearer Token Authentication

**Decision:** Use simple Bearer token auth instead of OAuth/JWT.

**Rationale:**
- Sufficient for v1 (single-user, local development)
- Simple to configure & maintain
- Can upgrade to OAuth/JWT in v2 if needed

See [ADR Documentation](./adr/README.md) for complete list.

---

## Security Architecture

### Authentication Flow

```
┌─────────────┐
│ MCP Server  │
└──────┬──────┘
       │
       │ 1. Read Authentication__BearerToken from environment
       │
       ▼
┌─────────────────────────────────────┐
│ HTTP Request to GraphQL API         │
│ Authorization: Bearer <token>       │
└──────┬──────────────────────────────┘
       │
       ▼
┌─────────────┐
│ API         │
│ Middleware  │
└──────┬──────┘
       │
       │ 2. Extract token from Authorization header
       │ 3. Compare with Authentication__BearerToken from configuration
       │    (loaded from environment variable via IConfiguration)
       │
       ├─ Match? → Allow request ✓
       └─ No match? → Return 401 Unauthorized ✗
```

### Security Best Practices

1. **Token Storage**
   - Never commit tokens to version control
   - Use environment variables only
   - Generate unique tokens per environment

2. **Token Generation**
   ```bash
   openssl rand -base64 32
   ```

3. **Token Rotation**
   - Rotate tokens periodically (quarterly/annually)
   - Update both API .env and Claude Code config
   - Restart both services

4. **Input Validation**
   - GraphQL schema validation (type, required fields)
   - Business logic validation (project exists, task valid, etc.)
   - Database constraints (check constraints, FKs)

5. **SQL Injection Prevention**
   - Entity Framework parameterized queries
   - No raw SQL with user input

6. **Future Enhancements** (v2)
   - OAuth 2.0 / OpenID Connect
   - JWT tokens with expiration
   - Role-based access control (RBAC)
   - Audit logging

---

## Deployment Architecture

### Development Environment

```
┌─────────────┐      ┌─────────────┐
│ Claude Code │◄────►│ MCP Server  │
│  (Host)     │stdio │  (Host)     │
└─────────────┘      └──────┬──────┘
                            │ HTTP
                            ▼
                     ┌──────────────┐
                     │ PostgreSQL   │
                     │ (Container)  │
                     │ :5432        │
                     └──────┬───────┘
                            │
                     ┌──────▼───────┐
                     │ GraphQL API  │
                     │ (Container)  │
                     │ :5000        │
                     └──────────────┘
```

### Production Environment (Future)

```
                    ┌─────────────┐
                    │ Load        │
                    │ Balancer    │
                    └──────┬──────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
        ┌─────▼──────┐ ┌──▼──────┐ ┌──▼──────┐
        │ API        │ │ API     │ │ API     │
        │ Instance 1 │ │ Inst 2  │ │ Inst 3  │
        └─────┬──────┘ └──┬──────┘ └──┬──────┘
              │           │            │
              └───────────┼────────────┘
                          │
                   ┌──────▼────────┐
                   │ PostgreSQL    │
                   │ (Primary)     │
                   └──────┬────────┘
                          │
                   ┌──────▼────────┐
                   │ PostgreSQL    │
                   │ (Replica)     │
                   └───────────────┘
```

---

## Additional Resources

- [API Documentation](./API.md)
- [Data Model](./prd/data-model.md)
- [Setup Guide](./integration/CLAUDE-CODE-SETUP.md)
- [Deployment Guide](./DEPLOYMENT.md)
- [ADR Documentation](./adr/README.md)

---

**Last Updated:** 2025-10-29
