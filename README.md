# Time Reporting System with Claude Code Integration

A time reporting system that integrates Claude Code with a custom GraphQL-based time tracker, enabling developers to track time spent on coding tasks automatically or manually through natural language commands.

---

## 🎯 Overview

This project allows you to:
- **Log time entries** directly from Claude Code using natural language
- **Manage time entries** (create, update, delete, move between projects)
- **Submit entries** for approval workflow
- **Query time data** with flexible filters
- **Single technology stack** - C# for everything!

---

## 🏗️ Architecture

```
Claude Code (Natural Language)
    ↓ stdio (JSON-RPC)
C# MCP Server (Console App ~200 lines!)
    ↓ HTTP/GraphQL
C# GraphQL API (ASP.NET Core + HotChocolate)
    ↓ Entity Framework
PostgreSQL Database
```

**Components:**
- **PostgreSQL 16** - Data persistence
- **ASP.NET Core 8 + HotChocolate** - GraphQL API
- **C# Console App** - MCP Server (simple!)
- **Docker/Podman** - Container orchestration

---

## 📚 Documentation

### Getting Started

1. **[Implementation Summary](./docs/IMPLEMENTATION-SUMMARY.md)** - ⭐ **START HERE!** Quick overview of simplified approach
2. **[Product Requirements Document (PRD)](./docs/prd/README.md)** - Complete product specification
3. **[Task Index](./docs/TASK-INDEX.md)** - Master task list with ~42 atomic tasks
4. **[Architecture](./docs/prd/architecture.md)** - System design and component details
5. **[Podman Setup](./docs/PODMAN-SETUP.md)** - Using Podman instead of Docker Desktop

### Technical Specifications

- **[Data Model](./docs/prd/data-model.md)** - Database schema and entities
- **[API Specification](./docs/prd/api-specification.md)** - GraphQL schema and examples
- **[MCP Tools](./docs/prd/mcp-tools.md)** - Tool definitions for Claude Code

### Implementation Tasks

- **Phase 1:** Database & Infrastructure (3 tasks)
- **Phase 2:** GraphQL API - Core Setup (5 tasks)
- **Phase 3:** GraphQL API - Queries (5 tasks)
- **Phase 4:** GraphQL API - Mutations Part 1 (5 tasks)
- **Phase 5:** GraphQL API - Mutations Part 2 (5 tasks)
- **Phase 6:** GraphQL API - Docker (4 tasks)
- **Phase 7:** MCP Server - Setup (4 tasks)
- **Phase 8:** MCP Server - Tools Part 1 (4 tasks)
- **Phase 9:** MCP Server - Tools Part 2 (4 tasks)
- **Phase 10:** MCP Server - Auto-tracking (4 tasks)
- **Phase 11:** Integration & Testing (5 tasks)
- **Phase 12:** Documentation & Deployment (5 tasks)

**Total:** 60 tasks, ~52-65 hours

---

## 🚀 Quick Start

### Prerequisites

- **Docker or Podman** ([Podman setup guide](./docs/PODMAN-SETUP.md))
- **.NET 8 SDK** (for both API and MCP server)
- **Claude Code** installed
- **PostgreSQL client** (optional, for testing)

### Start Development

```bash
# 1. Start with Phase 1 - Database Setup
cd docs/tasks/phase-01-database

# 2. Read Task 1.1
cat task-1.1-postgresql-schema.md

# 3. Follow the TASK-INDEX for sequential implementation
open docs/TASK-INDEX.md
```

---

## 💡 Usage Examples

Once implemented, you'll be able to:

```
User: "Log 8 hours of development work on INTERNAL project for today"
Claude Code: "Time entry created successfully! Entry ID: abc-123, Status: NOT_REPORTED"

User: "Show me all time entries for this week"
Claude Code: "Found 5 entries:
1. INTERNAL - Development - 8.0 hours (Oct 21)
2. CLIENT-A - Bug Fixing - 6.5 hours (Oct 22)
..."

User: "Move yesterday's entry from INTERNAL to CLIENT-A Feature Development"
Claude Code: "Entry moved to CLIENT-A - Feature Development"

User: "Submit all my time entries for approval"
Claude Code: "Submitted 5 time entries for approval"
```

---

## 🗂️ Project Structure

```
time-reporting-system/
├── docs/
│   ├── prd/
│   │   ├── README.md              # Main PRD
│   │   ├── data-model.md          # Database schema
│   │   ├── api-specification.md   # GraphQL API
│   │   ├── mcp-tools.md           # MCP tool specs
│   │   └── architecture.md        # System architecture
│   ├── tasks/                     # Task-specific implementation guides
│   │   ├── phase-01-database/
│   │   ├── phase-02-api-core/
│   │   └── ... (12 phases)
│   └── TASK-INDEX.md              # Master task list
├── db/
│   └── schema/
│       ├── 01-create-tables.sql   # DDL
│       └── 02-seed-data.sql       # Sample data
├── TimeReportingApi/              # C# GraphQL API (to be created)
├── TimeReportingMcp/              # C# MCP Server (to be created) ⭐
├── docker-compose.yml             # Container orchestration (Docker/Podman)
├── .env                           # Environment configuration
└── README.md                      # This file
```

---

## 🎓 Learning Path

1. **Start Here** - Read the [Implementation Summary](./docs/IMPLEMENTATION-SUMMARY.md) ⭐
2. **Understand the Product** - Read the [PRD](./docs/prd/README.md)
3. **Review Architecture** - Study the [Architecture](./docs/prd/architecture.md) (see C# MCP Server code!)
4. **Check Data Model** - Understand the [Data Model](./docs/prd/data-model.md)
5. **Start Implementation** - Follow [Task Index](./docs/TASK-INDEX.md) sequentially
6. **Using Podman?** - Read the [Podman Setup Guide](./docs/PODMAN-SETUP.md)

---

## 🔧 Technology Stack

### Single Language: C# 🎯
- **C# / .NET 8** - Everything (API + MCP Server)
- **HotChocolate 13+** - GraphQL server
- **Entity Framework Core 8** - ORM
- **GraphQL.Client** - GraphQL client (for MCP)
- **PostgreSQL 16** - Database

### Infrastructure
- **Docker or Podman** - Containerization
- **Docker Compose / Podman Compose** - Multi-container orchestration

**No Node.js, No TypeScript - Just C#!** 🎉

---

## 📊 Data Model Summary

### Core Entities

- **TimeEntry** - Individual time log records
  - Project, Task, Hours (standard/overtime)
  - Start/Completion dates
  - Status workflow, Tags, Description

- **Project** - Available projects
  - Code, Name, Active status
  - Available tasks, Tag configurations

- **ProjectTask** - Allowed tasks per project

- **TagConfiguration** - Metadata tags per project
  - Tag name, Allowed values

---

## 🔒 Security

- **Bearer Token Authentication** for API access
- **Input Validation** at multiple layers (MCP, GraphQL, Business Logic, Database)
- **Environment Variables** for sensitive configuration
- **Docker Secrets** for production deployment (future)

---

## 📈 Roadmap

### v1.0 (Current Scope - ~40-50 hours)
- ✅ Complete PRD and task breakdown
- ⏳ PostgreSQL database setup
- ⏳ C# GraphQL API implementation
- ⏳ C# MCP server (simple! ~4-5 hours)
- ⏳ Docker/Podman deployment

### v2.0 (Future Enhancements)
- **Auto-tracking** with smart suggestions
- Multi-user support with authentication
- Web UI for admin configuration
- Real-time timer functionality
- JIRA integration
- Reporting and analytics
- Mobile app

---

## 🤝 Contributing

Follow the task-based workflow:

1. Pick a task from [TASK-INDEX.md](./docs/TASK-INDEX.md)
2. Read the task's detailed implementation guide
3. Implement according to acceptance criteria
4. Test thoroughly
5. Check off the task in the index
6. Move to next task

---

## 📝 License

[Your License Here]

---

## 🆘 Support

For questions or issues:
- Review the [PRD](./docs/prd/README.md) for product details
- Check [TASK-INDEX.md](./docs/TASK-INDEX.md) for implementation guidance
- Refer to technical specs in `docs/prd/`

---

## 🎉 Ready to Start?

**👉 Begin with [Task 1.1: PostgreSQL Schema Setup](./docs/tasks/phase-01-database/task-1.1-postgresql-schema.md)**

Then follow the [Task Index](./docs/TASK-INDEX.md) for the complete implementation journey!
