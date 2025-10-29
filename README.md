# Time Reporting System with Claude Code Integration

A time reporting system that integrates Claude Code with a custom GraphQL-based time tracker, enabling developers to track time spent on coding tasks automatically or manually through natural language commands.

---

## 🚀 Quick Start

**Get up and running in 3 commands:**

```bash
# 1. Clone the repository
git clone https://github.com/YOUR_USERNAME/time-reporting-system.git
cd time-reporting-system

# 2. Generate bearer token and load environment variables
./setup.sh
source env.sh

# 3. Deploy the full stack (database + API)
/deploy
```

**That's it!** All tracked files remain unchanged - your token lives ONLY in shell environment:

- ✅ `./setup.sh` generates a secure bearer token → creates `env.sh`
- ✅ `source env.sh` exports environment variables to your shell
- ✅ MCP server reads `$BEARER_TOKEN` from shell environment
- ✅ Docker Compose reads `$Authentication__BearerToken` from shell environment
- ✅ NO files contain actual tokens - pure environment variable approach
- ✅ No tracked files are ever modified

**100% environment variables - zero files with secrets!**

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

### Getting Started Guides

1. **[Implementation Summary](./docs/IMPLEMENTATION-SUMMARY.md)** - ⭐ **START HERE!** Quick overview of simplified approach
2. **[Setup Guide](./docs/integration/CLAUDE-CODE-SETUP.md)** - Configure Claude Code with MCP server
3. **[Deployment Guide](./docs/DEPLOYMENT.md)** - Deploy with Docker/Podman
4. **[User Guide](./docs/USER_GUIDE.md)** - Natural language time tracking commands
5. **[Podman Setup](./docs/PODMAN-SETUP.md)** - Using Podman instead of Docker Desktop

### Technical Documentation

- **[Architecture](./docs/ARCHITECTURE.md)** - System architecture with detailed diagrams
- **[API Documentation](./docs/API.md)** - Complete GraphQL API reference
- **[Data Model](./docs/prd/data-model.md)** - Database schema and entities
- **[MCP Tools](./docs/prd/mcp-tools.md)** - Tool definitions for Claude Code

### Product Specifications

- **[Product Requirements Document (PRD)](./docs/prd/README.md)** - Complete product specification
- **[Architecture Spec](./docs/prd/architecture.md)** - Original architecture document
- **[API Specification](./docs/prd/api-specification.md)** - GraphQL schema and examples
- **[ADR Index](./docs/adr/README.md)** - Architecture Decision Records

### Implementation Guides

- **[Task Index](./docs/TASK-INDEX.md)** - Master task list with 65 atomic tasks
- **[Task Guides](./docs/tasks/)** - Phase-specific implementation tasks

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
- **Phase 13:** StrawberryShake Migration (4 tasks)

**Total:** 65 tasks, ~58-72 hours

---

## 🚀 Quick Start

### Prerequisites

- **Docker or Podman** ([Podman setup guide](./docs/PODMAN-SETUP.md))
- **.NET 8 SDK** (for both API and MCP server)
- **Claude Code** installed
- **PostgreSQL client** (optional, for testing)

### Deploy the System

```bash
# 1. Clone the repository
git clone <repository-url>
cd time-reporting-system

# 2. Configure environment
# Generate Bearer token
openssl rand -base64 32

# Edit .env with your token
nano .env

# 3. Deploy services
/deploy

# 4. Seed database
/seed-db

# 5. Configure Claude Code
# Follow the setup guide
open docs/integration/CLAUDE-CODE-SETUP.md

# 6. Test the system
# In Claude Code: "Get available projects"
```

For detailed instructions, see the [Deployment Guide](./docs/DEPLOYMENT.md).

### Development Workflow

To contribute or extend the system:

```bash
# 1. Follow the Task Index
open docs/TASK-INDEX.md

# 2. Read task guides
cd docs/tasks/

# 3. Run tests
/test

# 4. Build and deploy
/build
/deploy
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
- **C# / .NET 10** - Everything (API + MCP Server)
- **HotChocolate 13+** - GraphQL server
- **Entity Framework Core 8** - ORM
- **StrawberryShake 15** - Strongly-typed GraphQL client with code generation
- **PostgreSQL 16** - Database

### Infrastructure
- **Docker or Podman** - Containerization
- **Docker Compose / Podman Compose** - Multi-container orchestration

### Code Generation
- **StrawberryShake** automatically generates C# client code from `.graphql` operation files
- Provides compile-time type safety and IntelliSense for all GraphQL operations
- Eliminates ~250 lines of manual type definitions and query strings

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

- **ProjectTag** - Metadata tags per project (with TagValue for allowed values)
  - Tag name, Allowed values

---

## 🔒 Security

- **Bearer Token Authentication** for API access
- **Input Validation** at multiple layers (MCP, GraphQL, Business Logic, Database)
- **Environment Variables** for sensitive configuration
- **Docker Secrets** for production deployment (future)

---

## 📈 Roadmap

### v1.0 (Complete! ✅)
- ✅ Complete PRD and task breakdown
- ✅ PostgreSQL database setup
- ✅ C# GraphQL API implementation (4 queries, 8 mutations)
- ✅ C# MCP server with 7 tools
- ✅ Auto-tracking with intelligent suggestions
- ✅ StrawberryShake typed GraphQL client migration
- ✅ Docker/Podman deployment
- ✅ Comprehensive documentation
- ✅ E2E testing and integration guides

**Status:** Production-ready! All 65 tasks completed (100%)

**Documentation:**
- User Guide for natural language commands
- API Documentation with examples
- Setup Guide for Claude Code integration
- Deployment Guide for Docker/Podman
- Architecture documentation with diagrams

### v2.0 (Future Enhancements)
- Multi-user support with authentication/authorization
- Web UI for admin configuration
- Real-time timer functionality
- JIRA/GitHub issue integration
- Advanced reporting and analytics
- Mobile app
- Approval notifications (email/Slack)
- Team dashboards

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
- **User Questions:** Check the [User Guide](./docs/USER_GUIDE.md)
- **Setup Help:** See [Setup Guide](./docs/integration/CLAUDE-CODE-SETUP.md)
- **Deployment Issues:** Review [Deployment Guide](./docs/DEPLOYMENT.md)
- **API Questions:** Reference [API Documentation](./docs/API.md)
- **Architecture:** Study [Architecture Docs](./docs/ARCHITECTURE.md)
- **Contributing:** Follow [Task Index](./docs/TASK-INDEX.md)

---

## 🎉 Ready to Use?

### For Users (Deploy and Use)

**👉 Start Here:** [Deployment Guide](./docs/DEPLOYMENT.md)

1. Deploy the system with Docker/Podman
2. Configure Claude Code ([Setup Guide](./docs/integration/CLAUDE-CODE-SETUP.md))
3. Start tracking time! ([User Guide](./docs/USER_GUIDE.md))

### For Developers (Contribute or Extend)

**👉 Start Here:** [Task Index](./docs/TASK-INDEX.md)

1. Review completed implementation
2. Understand architecture ([Architecture](./docs/ARCHITECTURE.md))
3. Follow TDD workflow for new features

---

## 🌟 What's New in v1.0

**Phase 12 Complete - Full Documentation Suite!**

- 📖 **[User Guide](./docs/USER_GUIDE.md)** - Natural language time tracking commands and workflows
- 🔧 **[API Documentation](./docs/API.md)** - Complete GraphQL schema reference with examples
- 🏗️ **[Architecture Docs](./docs/ARCHITECTURE.md)** - System architecture with detailed diagrams
- 🚀 **[Deployment Guide](./docs/DEPLOYMENT.md)** - Production-ready Docker/Podman deployment
- ✅ **All 61 tasks completed** - Production-ready v1.0!
