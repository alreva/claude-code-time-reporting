---
description: Stop PostgreSQL database
allowed-tools: Bash(docker-compose:*), Bash(podman-compose:*)
---

Stop the PostgreSQL database container.

### Execution

**Using Docker:**
```bash
docker-compose stop postgres
```

**Using Podman:**
```bash
podman-compose stop postgres
```

### Expected Output

- PostgreSQL container stops gracefully
- Port 5432 is freed

### Notes

- Data persists in volume (not lost)
- Use /db-start to restart
- Use docker-compose down to remove container and network
