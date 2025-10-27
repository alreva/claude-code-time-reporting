---
description: Create and apply Entity Framework migrations
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Create a new Entity Framework migration or apply pending migrations.

### Usage

**Create a new migration:**
```bash
.claude/hooks/guard.sh "dotnet ef migrations add <MigrationName> --project TimeReportingApi" "slash"
```

**Apply migrations to database:**
```bash
.claude/hooks/guard.sh "dotnet ef database update --project TimeReportingApi" "slash"
```

**List migrations:**
```bash
.claude/hooks/guard.sh "dotnet ef migrations list --project TimeReportingApi" "slash"
```

### Notes

- Requires PostgreSQL to be running (/db-start)
- Migration files created in TimeReportingApi/Migrations/
- Always review generated migration before applying
