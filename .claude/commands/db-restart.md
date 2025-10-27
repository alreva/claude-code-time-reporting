---
description: Restart PostgreSQL database
allowed-tools: Bash(docker-compose:*), Bash(podman-compose:*)
---

Restart the PostgreSQL database container.

### Execution

**Using Docker:**
```bash
docker-compose restart postgres
```

**Using Podman:**
```bash
podman-compose restart postgres
```

### Expected Output

- PostgreSQL container restarts
- Database reconnects and becomes available

### Notes

- Useful for configuration changes
- Data persists across restarts
