# System Architecture Specification (PRD)

**Version:** 1.0
**Last Updated:** 2025-10-24

> **📋 About this document:** This is the detailed architecture specification from the Product Requirements Document (PRD). It contains implementation-focused details, code examples, and technical diagrams for developers. For high-level architecture overview, see [Main Architecture Documentation](../ARCHITECTURE.md).

---

## Overview

This document provides detailed architectural diagrams and component specifications for the Time Reporting System with Claude Code integration.

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Component Details](#2-component-details)
3. [Data Flow](#3-data-flow)
4. [Deployment Architecture](#4-deployment-architecture)
5. [Security Architecture](#5-security-architecture)
6. [Technology Stack](#6-technology-stack)

---

## 1. System Architecture

### 1.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         User's Machine                          │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │                      Claude Code                          │ │
│  │  - Natural language interface                             │ │
│  │  - Task execution                                         │ │
│  │  - Code generation                                        │ │
│  └────────────────────────┬──────────────────────────────────┘ │
│                           │ stdio/socket                        │
│                           │ (MCP Protocol)                      │
│  ┌────────────────────────▼──────────────────────────────────┐ │
│  │         MCP Server (C# Console App)                      │ │
│  │  - JSON-RPC stdio handler                                 │ │
│  │  - 7 tool implementations                                 │ │
│  │  - GraphQL client (GraphQL.Client)                        │ │
│  │  - Simple & lightweight!                                  │ │
│  └────────────────────────┬──────────────────────────────────┘ │
│                           │                                     │
└───────────────────────────┼─────────────────────────────────────┘
                            │ HTTP/HTTPS
                            │ GraphQL over POST
                            │ Bearer Token Auth
┌───────────────────────────▼─────────────────────────────────────┐
│                      Docker Host                                │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │              GraphQL API Container                        │ │
│  │  - ASP.NET Core 10.0                                       │ │
│  │  - HotChocolate GraphQL                                   │ │
│  │  - Entity Framework Core                                  │ │
│  │  - Bearer token validation                                │ │
│  │  - Business logic & validation                            │ │
│  └────────────────────────┬──────────────────────────────────┘ │
│                           │ TCP/IP (Port 5432)                  │
│                           │ PostgreSQL Protocol                 │
│  ┌────────────────────────▼──────────────────────────────────┐ │
│  │            PostgreSQL Container                           │ │
│  │  - PostgreSQL 16                                          │ │
│  │  - Persistent volume                                      │ │
│  │  - Database: time_reporting                               │ │
│  │  - Tables: time_entries, projects, project_tasks,         │ │
│  │            tag_configurations                             │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Layer Responsibilities

| Layer | Responsibility | Technologies |
|-------|---------------|--------------|
| **Presentation** | Natural language interface, user interaction | Claude Code |
| **Integration** | Tool bridge, JSON-RPC protocol handler | MCP Server (C# Console App) |
| **Application** | Business logic, validation, GraphQL API | ASP.NET Core, HotChocolate |
| **Data Access** | ORM, database operations | Entity Framework Core |
| **Persistence** | Data storage | PostgreSQL |

---

## 2. Component Details

### 2.1 Claude Code

**Purpose:** AI-powered coding assistant that users interact with

**Capabilities:**
- Natural language understanding
- Context awareness
- Tool invocation via MCP
- Response formatting

**Integration:**
- Communicates with MCP server via stdio
- Receives tool definitions and schemas
- Invokes tools with structured parameters
- Receives structured responses

**Configuration:**
```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/TimeReportingMcp/TimeReportingMcp.csproj"],
      "env": {
        "GRAPHQL_API_URL": "http://localhost:5001/graphql",
        "Authentication__BearerToken": "your-token-here"
      }
    }
  }
}
```

---

### 2.2 MCP Server (C# Console App)

**Purpose:** Simple bridge between Claude Code and GraphQL API

**Technology:**
- **Framework:** .NET 10 Console Application
- **Language:** C#
- **GraphQL Client:** GraphQL.Client (NuGet package)
- **JSON:** System.Text.Json for JSON-RPC

**Architecture:**

```
┌─────────────────────────────────────────────────────────┐
│          MCP Server (C# Console App)                    │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌────────────────────────────────────────────────┐   │
│  │     JSON-RPC Stdio Handler                     │   │
│  │  - Read from Console.In                        │   │
│  │  - Parse JSON-RPC requests                     │   │
│  │  - Route to tool handlers                      │   │
│  │  - Write responses to Console.Out              │   │
│  └─────────────────┬──────────────────────────────┘   │
│                    │                                    │
│  ┌─────────────────▼──────────────────────────────┐   │
│  │         Tool Handlers (7 tools)                │   │
│  │  - log_time                                    │   │
│  │  - query_time_entries                          │   │
│  │  - update_time_entry                           │   │
│  │  - move_task_to_project                        │   │
│  │  - delete_time_entry                           │   │
│  │  - get_available_projects                      │   │
│  │  - submit_time_entry                           │   │
│  └─────────────────┬──────────────────────────────┘   │
│                    │                                    │
│  ┌─────────────────▼──────────────────────────────┐   │
│  │       GraphQL Client (GraphQL.Client)          │   │
│  │  - HttpClient with Bearer token                │   │
│  │  - Query/Mutation builder                      │   │
│  │  - Response deserialization                    │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Key Classes:**

```csharp
// Main program
class Program
{
    static async Task Main(string[] args)
    {
        var server = new McpServer();
        await server.RunAsync();
    }
}

// MCP Server
class McpServer
{
    private readonly GraphQLHttpClient _graphqlClient;

    public McpServer(IConfiguration configuration)
    {
        var apiUrl = configuration["GRAPHQL_API_URL"];
        var bearerToken = configuration["Authentication:BearerToken"];

        _graphqlClient = new GraphQLHttpClient(apiUrl, new SystemTextJsonSerializer());
        _graphqlClient.HttpClient.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {bearerToken}");
    }

    public async Task RunAsync()
    {
        while (true)
        {
            var line = await Console.In.ReadLineAsync();
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
            var response = await HandleRequest(request);
            Console.WriteLine(JsonSerializer.Serialize(response));
        }
    }

    private async Task<JsonRpcResponse> HandleRequest(JsonRpcRequest request)
    {
        if (request.Method == "tools/call")
        {
            return await HandleToolCall(request.Params);
        }
        // Handle other MCP methods (tools/list, etc.)
    }

    private async Task<JsonRpcResponse> HandleToolCall(ToolCallParams params)
    {
        return params.Name switch
        {
            "log_time" => await LogTime(params.Arguments),
            "query_time_entries" => await QueryEntries(params.Arguments),
            "update_time_entry" => await UpdateEntry(params.Arguments),
            "move_task_to_project" => await MoveTask(params.Arguments),
            "delete_time_entry" => await DeleteEntry(params.Arguments),
            "get_available_projects" => await GetProjects(params.Arguments),
            "submit_time_entry" => await SubmitEntry(params.Arguments),
            _ => throw new Exception($"Unknown tool: {params.Name}")
        };
    }
}

// Tool handler example
private async Task<JsonRpcResponse> LogTime(Dictionary<string, JsonElement> args)
{
    var mutation = new GraphQLRequest
    {
        Query = @"
            mutation LogTime($input: LogTimeInput!) {
                logTime(input: $input) {
                    id
                    status
                    createdAt
                }
            }",
        Variables = new { input = args }
    };

    var response = await _graphqlClient.SendMutationAsync<LogTimeResponse>(mutation);

    return new JsonRpcResponse
    {
        Result = new
        {
            content = new[]
            {
                new { type = "text", text = $"Created entry {response.Data.LogTime.Id}" }
            }
        }
    };
}
```

**Configuration:**
- `GRAPHQL_API_URL` - GraphQL endpoint (e.g., http://localhost:5001/graphql)
- `Authentication__BearerToken` - Authentication token

**That's it!** ~200 lines of C# for the entire MCP server.

---

### 2.3 GraphQL API

**Purpose:** Business logic and data access layer

**Technology:**
- **Framework:** ASP.NET Core 10.0
- **GraphQL:** HotChocolate 13+
- **ORM:** Entity Framework Core 10
- **Database Provider:** Npgsql (PostgreSQL)

**Architecture:**

```
┌─────────────────────────────────────────────────────────┐
│                 GraphQL API (ASP.NET Core)              │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌────────────────────────────────────────────────┐   │
│  │          HTTP Pipeline                         │   │
│  │  - Bearer token authentication middleware      │   │
│  │  - Exception handler middleware                │   │
│  │  - Request logging                             │   │
│  └─────────────────┬──────────────────────────────┘   │
│                    │                                    │
│  ┌─────────────────▼──────────────────────────────┐   │
│  │         GraphQL Layer (HotChocolate)           │   │
│  │  ┌──────────────────────────────────────────┐ │   │
│  │  │  Query Type                              │ │   │
│  │  │  - timeEntries(filters)                  │ │   │
│  │  │  - timeEntry(id)                         │ │   │
│  │  │  - projects(activeOnly)                  │ │   │
│  │  │  - project(code)                         │ │   │
│  │  └──────────────────────────────────────────┘ │   │
│  │  ┌──────────────────────────────────────────┐ │   │
│  │  │  Mutation Type                           │ │   │
│  │  │  - logTime(input)                        │ │   │
│  │  │  - updateTimeEntry(id, input)            │ │   │
│  │  │  - moveTaskToProject(...)                │ │   │
│  │  │  - updateTags(...)                       │ │   │
│  │  │  - deleteTimeEntry(id)                   │ │   │
│  │  │  - submitTimeEntry(id)                   │ │   │
│  │  │  - approveTimeEntry(id)                  │ │   │
│  │  │  - declineTimeEntry(id, comment)         │ │   │
│  │  └──────────────────────────────────────────┘ │   │
│  │  ┌──────────────────────────────────────────┐ │   │
│  │  │  DataLoaders                             │ │   │
│  │  │  - ProjectByCodeDataLoader               │ │   │
│  │  │  - TasksByProjectDataLoader              │ │   │
│  │  └──────────────────────────────────────────┘ │   │
│  └─────────────────┬──────────────────────────────┘   │
│                    │                                    │
│  ┌─────────────────▼──────────────────────────────┐   │
│  │         Business Logic Layer                   │   │
│  │  - TimeEntryService                            │   │
│  │  - ProjectService                              │   │
│  │  - ValidationService                           │   │
│  │  - WorkflowService                             │   │
│  └─────────────────┬──────────────────────────────┘   │
│                    │                                    │
│  ┌─────────────────▼──────────────────────────────┐   │
│  │         Data Access Layer (EF Core)            │   │
│  │  - TimeReportingDbContext                      │   │
│  │  - DbSet<TimeEntry>                            │   │
│  │  - DbSet<Project>                              │   │
│  │  - DbSet<ProjectTask>                          │   │
│  │  - DbSet<TagConfiguration>                     │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Key Services:**

```csharp
// Time entry operations
public interface ITimeEntryService
{
    Task<TimeEntry> CreateAsync(LogTimeInput input);
    Task<TimeEntry> UpdateAsync(Guid id, UpdateTimeEntryInput input);
    Task<TimeEntry> MoveToProjectAsync(Guid id, string projectCode, string task);
    Task<bool> DeleteAsync(Guid id);
    Task<TimeEntry> SubmitAsync(Guid id);
}

// Validation
public interface IValidationService
{
    Task<ValidationResult> ValidateTimeEntryAsync(TimeEntry entry);
    Task<bool> IsProjectValidAsync(string code);
    Task<bool> IsTaskValidAsync(string projectCode, string task);
    Task<bool> AreTagsValidAsync(string projectCode, List<Tag> tags);
}

// Workflow
public interface IWorkflowService
{
    Task<TimeEntry> ApproveAsync(Guid id);
    Task<TimeEntry> DeclineAsync(Guid id, string comment);
    bool CanTransition(TimeEntryStatus from, TimeEntryStatus to);
}
```

**Configuration (appsettings.json):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=time_reporting;Username=postgres;Password=<password>"
  },
  "Authentication": {
    "BearerToken": "<your-token>"
  },
  "GraphQL": {
    "EnableIntrospection": true,
    "EnablePlayground": true
  }
}
```

---

### 2.4 PostgreSQL Database

**Purpose:** Persistent data storage

**Technology:**
- **Version:** PostgreSQL 16
- **Storage:** Persistent Docker volume

**Schema:**

```
time_reporting (database)
├── time_entries (table)
│   ├── id (uuid, PK)
│   ├── project_code (varchar(10), FK → projects.code)
│   ├── task (varchar(100))
│   ├── issue_id (varchar(30), nullable)
│   ├── standard_hours (decimal(10,2))
│   ├── overtime_hours (decimal(10,2))
│   ├── description (text, nullable)
│   ├── start_date (date)
│   ├── completion_date (date)
│   ├── status (varchar(20))
│   ├── decline_comment (text, nullable)
│   ├── tags (jsonb)
│   ├── created_at (timestamp)
│   ├── updated_at (timestamp)
│   └── user_id (varchar(100), nullable)
│
├── projects (table)
│   ├── code (varchar(10), PK)
│   ├── name (varchar(200))
│   ├── is_active (boolean)
│   ├── created_at (timestamp)
│   └── updated_at (timestamp)
│
├── project_tasks (table)
│   ├── id (serial, PK)
│   ├── project_code (varchar(10), FK → projects.code)
│   ├── task_name (varchar(100))
│   └── is_active (boolean)
│
└── tag_configurations (table)
    ├── id (serial, PK)
    ├── project_code (varchar(10), FK → projects.code)
    ├── tag_name (varchar(20))
    ├── allowed_values (jsonb)
    └── is_active (boolean)
```

**Indexes:**
- `idx_time_entries_project_date` on `(project_code, start_date DESC)`
- `idx_time_entries_status` on `(status)`
- `idx_time_entries_user` on `(user_id, start_date DESC)`
- `idx_project_tasks_project` on `(project_code)`
- `idx_tag_configurations_project` on `(project_code)`

---

## 3. Data Flow

### 3.1 Create Time Entry Flow

```
┌─────────┐
│  User   │ "Log 8 hours of development on INTERNAL for today"
└────┬────┘
     │
     ▼
┌─────────────────┐
│  Claude Code    │
│  1. Parse NL    │
│  2. Identify    │
│     tool: log_time
└────┬────────────┘
     │ MCP tool call: log_time({
     │   projectCode: "INTERNAL",
     │   task: "Development",
     │   standardHours: 8.0,
     │   startDate: "2025-10-24",
     │   completionDate: "2025-10-24"
     │ })
     ▼
┌─────────────────┐
│   MCP Server    │
│  1. Validate    │
│     params      │
│  2. Build       │
│     GraphQL     │
│     mutation    │
└────┬────────────┘
     │ POST /graphql
     │ Authorization: Bearer <token>
     │ {
     │   "query": "mutation LogTime($input: LogTimeInput!) { ... }",
     │   "variables": { "input": {...} }
     │ }
     ▼
┌─────────────────┐
│  GraphQL API    │
│  1. Auth check  │
│  2. Validate    │
│     business    │
│     rules       │
└────┬────────────┘
     │ EF Core query:
     │ - Verify project exists
     │ - Verify task in project
     │ - Validate tags
     ▼
┌─────────────────┐
│   PostgreSQL    │
│  INSERT INTO    │
│  time_entries   │
│  VALUES (...)   │
└────┬────────────┘
     │ RETURNING id, ...
     ▼
┌─────────────────┐
│  GraphQL API    │
│  Return         │
│  TimeEntry      │
└────┬────────────┘
     │ JSON response
     ▼
┌─────────────────┐
│   MCP Server    │
│  1. Parse       │
│     response    │
│  2. Update      │
│     context     │
└────┬────────────┘
     │ Tool result
     ▼
┌─────────────────┐
│  Claude Code    │
│  Format         │
│  user message   │
└────┬────────────┘
     │
     ▼
┌─────────┐
│  User   │ "Time entry created successfully (ID: abc-123)"
└─────────┘
```

### 3.2 Query Time Entries Flow

```
User → Claude Code → MCP Server → GraphQL API → PostgreSQL
                                      ↓
                                 Apply filters
                                 (WHERE project_code = 'X'
                                  AND start_date >= 'Y'
                                  AND status = 'Z')
                                      ↓
                                 EF Core LINQ query
                                      ↓
                                 PostgreSQL SELECT
                                      ↓
                                 Results → API → MCP → Claude → User
```

### 3.3 Validation Flow

```
┌─────────────────────────────────────────────────────────┐
│                  Validation Layers                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Layer 1: MCP Server (Client-side)                     │
│  ┌────────────────────────────────────────────────┐   │
│  │ - Parameter type checking                      │   │
│  │ - Required field validation                    │   │
│  │ - Basic format validation                      │   │
│  └────────────────────────────────────────────────┘   │
│                      │                                  │
│                      ▼                                  │
│  Layer 2: GraphQL Schema (API)                         │
│  ┌────────────────────────────────────────────────┐   │
│  │ - Input type validation                        │   │
│  │ - Non-null enforcement                         │   │
│  │ - Enum value checking                          │   │
│  └────────────────────────────────────────────────┘   │
│                      │                                  │
│                      ▼                                  │
│  Layer 3: Business Logic (Service)                     │
│  ┌────────────────────────────────────────────────┐   │
│  │ - Project exists and is active                 │   │
│  │ - Task is in project's available tasks         │   │
│  │ - Tags match project configuration             │   │
│  │ - Date range valid (start <= completion)       │   │
│  │ - Hours are positive                           │   │
│  │ - Status transitions are allowed               │   │
│  └────────────────────────────────────────────────┘   │
│                      │                                  │
│                      ▼                                  │
│  Layer 4: Database Constraints                         │
│  ┌────────────────────────────────────────────────┐   │
│  │ - Foreign key constraints                      │   │
│  │ - Check constraints                            │   │
│  │ - Unique constraints                           │   │
│  │ - NOT NULL constraints                         │   │
│  └────────────────────────────────────────────────┘   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 4. Deployment Architecture

### 4.1 Docker Compose Setup

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: time_reporting
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
      - ./db/init.sql:/docker-entrypoint-initdb.d/init.sql
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  graphql-api:
    build:
      context: ./api
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=time_reporting;Username=postgres;Password=${DB_PASSWORD}"
      Authentication__BearerToken: ${Authentication__BearerToken}
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  pgdata:
```

### 4.2 Network Architecture

```
┌──────────────────────────────────────────────────────────┐
│                   Docker Network (bridge)                │
│                                                          │
│  ┌─────────────────┐        ┌─────────────────┐        │
│  │  graphql-api    │        │    postgres     │        │
│  │  Container      │        │    Container    │        │
│  │                 │  TCP   │                 │        │
│  │  Port 8080      │◄──────►│  Port 5432      │        │
│  │  (Internal)     │  5432  │  (Internal)     │        │
│  └────────┬────────┘        └─────────────────┘        │
│           │                                              │
│           │ Port mapping                                 │
│           │ 5000:8080                                    │
└───────────┼──────────────────────────────────────────────┘
            │
            │ HTTP
            ▼
    ┌───────────────┐
    │  Host Machine │
    │  Port 5001    │
    └───────┬───────┘
            │
            │ HTTP (GraphQL)
            ▼
    ┌───────────────────────┐
    │  MCP Server           │
    │  (C# Console App)     │
    │  Runs on host machine │
    └───────┬───────────────┘
            │
            │ stdio (JSON-RPC)
            ▼
    ┌───────────────┐
    │  Claude Code  │
    └───────────────┘
```

---

## 5. Security Architecture

### 5.1 Authentication Flow

```
┌─────────────┐
│ MCP Server  │
│ (has token) │
└──────┬──────┘
       │
       │ HTTP POST /graphql
       │ Authorization: Bearer <token>
       │
       ▼
┌──────────────────────────────────────────┐
│        GraphQL API                       │
│  ┌────────────────────────────────────┐ │
│  │  Auth Middleware                   │ │
│  │  1. Extract token from header      │ │
│  │  2. Validate token format          │ │
│  │  3. Compare with configured token  │ │
│  │  4. Reject if invalid (401)        │ │
│  │  5. Continue if valid              │ │
│  └─────────────┬──────────────────────┘ │
│                ▼                         │
│  ┌────────────────────────────────────┐ │
│  │  GraphQL Executor                  │ │
│  │  (processes request)               │ │
│  └────────────────────────────────────┘ │
└──────────────────────────────────────────┘
```

### 5.2 Token Management

**Generation:**
```bash
# Generate secure random token
openssl rand -base64 32
```

**Storage:**
- MCP Server: Environment variable `Authentication__BearerToken`
- API: Configuration file or environment variable
- **Never** commit tokens to version control

**Rotation:**
1. Generate new token
2. Update API configuration
3. Update MCP server configuration
4. Restart services
5. Invalidate old token

### 5.3 Data Protection

| Layer | Protection | Implementation |
|-------|-----------|----------------|
| **Transport** | TLS/HTTPS | Docker network encryption (future) |
| **API** | Bearer token auth | Middleware validation |
| **Database** | Connection encryption | PostgreSQL SSL (future) |
| **Storage** | Encrypted volume | Docker volume encryption (future) |

---

## 6. Technology Stack

### 6.1 Development Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Claude Code** | AI Assistant | Latest |
| **MCP Server** | .NET Console App | 8.0 |
| **Language** | C# | 12 |
| **GraphQL Client (MCP)** | GraphQL.Client | Latest |
| **API Framework** | ASP.NET Core | 8.0 |
| **GraphQL Server** | HotChocolate | 13+ |
| **ORM** | Entity Framework Core | 8.0 |
| **Database** | PostgreSQL | 16 |
| **Containerization** | Docker | 24+ |
| **Orchestration** | Docker Compose | 2.0+ |

### 6.2 Dependencies

**MCP Server (.csproj):**
```xml
<ItemGroup>
  <PackageReference Include="GraphQL.Client" Version="6.0.0" />
  <PackageReference Include="GraphQL.Client.Serializer.SystemTextJson" Version="6.0.0" />
</ItemGroup>
```

**GraphQL API (.csproj):**
```xml
<ItemGroup>
  <PackageReference Include="HotChocolate.AspNetCore" Version="13.9.0" />
  <PackageReference Include="HotChocolate.Data.EntityFramework" Version="13.9.0" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
</ItemGroup>
```

### 6.3 Build & Deploy

**MCP Server:**
```bash
cd TimeReportingMcp
dotnet restore
dotnet build
dotnet run
```

**GraphQL API:**
```bash
cd TimeReportingApi
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

**Docker:**
```bash
# Start PostgreSQL and API
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

---

**Related Documents:**
- [Data Model](./data-model.md) - Database schema details
- [API Specification](./api-specification.md) - GraphQL API
- [MCP Tools](./mcp-tools.md) - Tool specifications
- [PRD Main](./README.md) - Product requirements overview
