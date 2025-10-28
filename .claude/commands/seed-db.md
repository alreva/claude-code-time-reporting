---
description: Run database seeder to populate seed data
allowed-tools: Bash(.claude/hooks/guard.sh:*)
---

Runs the TimeReportingSeeder console application to populate the database with seed data.

**Features:**
- Idempotent (safe to run multiple times)
- Upsert behavior - updates existing data, creates new data
- Connects to local PostgreSQL database
- Shows summary of seeded data

**Prerequisites:**
- PostgreSQL database must be running (`/db-start`)
- Database migrations must be applied (happens automatically when API starts)

### Execution

```bash
.claude/hooks/guard.sh "dotnet run --project TimeReportingSeeder/TimeReportingSeeder.csproj" "slash"
```
