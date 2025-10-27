---
description: Start PostgreSQL database
allowed-tools: Bash(.claude/hooks/db-guard.sh:*)
---

Start the PostgreSQL database container.

### Execution

```bash
.claude/hooks/db-guard.sh start
```

### Expected Output

- PostgreSQL container starts
- Database available on localhost:5432
- Health check passes

### Notes

- Use /db-logs to view startup logs
- Use /db-psql to connect to database
- Database data persists in Docker volume
