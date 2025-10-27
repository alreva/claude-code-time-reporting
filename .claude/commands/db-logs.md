---
description: View PostgreSQL logs
allowed-tools: Bash(docker-compose:*), Bash(podman-compose:*)
---

View logs from the PostgreSQL database container.

### Execution

**Using Docker:**
```bash
docker-compose logs -f postgres
```

**Using Podman:**
```bash
podman-compose logs -f postgres
```

### Expected Output

- Streams PostgreSQL logs in real-time
- Shows connection attempts, queries, errors
- Press Ctrl+C to stop following

### Notes

- Use -f flag to follow logs in real-time
- Omit -f to see last logs and exit
- Useful for debugging connection issues
