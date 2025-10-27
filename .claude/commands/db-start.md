---
description: Start PostgreSQL database
allowed-tools: Bash(docker-compose:*), Bash(podman-compose:*)
---

Start the PostgreSQL database container.

### Execution

**Using Docker:**
```bash
docker-compose up -d postgres
```

**Using Podman:**
```bash
podman-compose up -d postgres
```

### Expected Output

- PostgreSQL container starts
- Database available on localhost:5432
- Health check passes

### Notes

- Use /db-logs to view startup logs
- Use /db-psql to connect to database
- Database data persists in Docker volume
