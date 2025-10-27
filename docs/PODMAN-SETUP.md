# Podman Setup Guide

**For users with Podman instead of Docker Desktop**

---

## Prerequisites

- Podman installed (version 4.0+ recommended)
- Podman machine running (macOS/Windows)

---

## Quick Start

### Check Podman Version

```bash
podman --version
# Should be 4.0 or higher for best compatibility
```

### Start Podman Machine (macOS/Windows)

```bash
# Start the Podman VM
podman machine start

# Verify it's running
podman machine list
```

### Linux Users

```bash
# Start Podman socket (if needed)
systemctl --user start podman.socket

# Enable it to start on boot
systemctl --user enable podman.socket
```

---

## Using Docker Compose with Podman

You have **three options**. Pick the one that works best for you:

### Option 1: Native `podman compose` (Recommended for Podman 4.0+)

```bash
# Check if available
podman compose version

# Use it exactly like docker-compose
podman compose up -d
podman compose ps
podman compose logs -f
podman compose down

# Full workflow for this project
podman compose up -d postgres
podman compose up -d graphql-api
podman compose ps
```

**Pros:**
- Native support, no extra setup
- Best performance
- Official Podman feature

**Cons:**
- Requires Podman 4.0+

---

### Option 2: `docker-compose` with Podman Socket

Use the regular `docker-compose` CLI by pointing it to Podman:

```bash
# macOS: Set DOCKER_HOST to Podman socket
export DOCKER_HOST=unix://$HOME/.local/share/containers/podman/machine/podman.sock

# Linux: Set DOCKER_HOST to Podman socket
export DOCKER_HOST=unix:///run/user/$UID/podman/podman.sock

# Now use docker-compose normally
docker-compose up -d
docker-compose ps
docker-compose down
```

**Tip:** Add the `export DOCKER_HOST=...` line to your `~/.bashrc` or `~/.zshrc` so it's always set.

**Pros:**
- Works with existing docker-compose
- No new commands to learn

**Cons:**
- Need to set environment variable
- Requires docker-compose CLI installed

---

### Option 3: `podman-compose`

Install and use `podman-compose`:

```bash
# Install via pip
pip install podman-compose

# Or on Fedora/RHEL
sudo dnf install podman-compose

# Use it like docker-compose
podman-compose up -d
podman-compose ps
podman-compose down
```

**Pros:**
- Drop-in replacement for docker-compose
- Works on older Podman versions

**Cons:**
- Extra dependency to install
- Community project (not official Podman)

---

## Project-Specific Setup

### 1. No Changes Needed to docker-compose.yml!

The `docker-compose.yml` file works as-is with Podman. No modifications needed.

### 2. Start the Services

Using **Option 1** (native `podman compose`):

```bash
# Start PostgreSQL
podman compose up -d postgres

# Check logs
podman compose logs postgres

# Verify it's healthy
podman compose ps

# Once healthy, start the API (when implemented)
podman compose up -d graphql-api
```

Using **Option 2** (`docker-compose` with Podman):

```bash
# Set environment variable first
export DOCKER_HOST=unix://$HOME/.local/share/containers/podman/machine/podman.sock

# Now use docker-compose
docker-compose up -d postgres
docker-compose logs postgres
docker-compose ps
```

### 3. Connect to PostgreSQL

```bash
# Using podman exec
podman exec -it time-reporting-postgres psql -U postgres -d time_reporting

# Or using podman compose exec
podman compose exec postgres psql -U postgres -d time_reporting
```

### 4. Stop Services

```bash
# Stop all services
podman compose down

# Stop and remove volumes (delete all data)
podman compose down -v
```

---

## Common Issues & Solutions

### Issue 1: "podman compose not found"

**Solution:** Your Podman version is < 4.0. Use Option 2 or 3 instead.

```bash
# Check version
podman --version

# Upgrade Podman or use docker-compose with DOCKER_HOST
```

---

### Issue 2: "Cannot connect to Podman socket"

**macOS/Windows:**
```bash
# Make sure Podman machine is running
podman machine list
podman machine start

# Get the socket path
podman machine inspect --format '{{.ConnectionInfo.PodmanSocket.Path}}'
```

**Linux:**
```bash
# Start the socket
systemctl --user start podman.socket

# Check status
systemctl --user status podman.socket
```

---

### Issue 3: Port conflicts

```bash
# Check what's using the port
podman ps -a

# Or
lsof -i :5432
lsof -i :5000

# Stop conflicting containers
podman stop <container-name>
```

---

### Issue 4: Permission denied on volumes

**macOS/Windows:** Usually not an issue with Podman machine

**Linux:**
```bash
# Podman runs rootless by default
# Make sure volume directories are writable
chmod 755 ./db/schema

# Or run with user namespace
podman compose up -d --userns=keep-id
```

---

## Podman vs Docker Differences

### What's the Same:
- ✅ docker-compose.yml syntax
- ✅ Container images (uses Docker Hub)
- ✅ Networking
- ✅ Volumes

### What's Different:
- Podman runs **rootless** by default (more secure!)
- Podman uses **pods** (groups of containers)
- No daemon required (lighter weight)
- `podman ps` vs `docker ps` (but commands are compatible)

---

## Recommended Workflow for This Project

### Daily Development

```bash
# 1. Start Podman machine (macOS/Windows only)
podman machine start

# 2. Start database
podman compose up -d postgres

# 3. Wait for healthy status
podman compose ps

# 4. Run migrations (once API is built)
cd TimeReportingApi
dotnet ef database update

# 5. Start API in Docker
podman compose up -d graphql-api

# 6. Run MCP server locally (not in Docker)
cd ../TimeReportingMcp
dotnet run

# 7. When done, stop services
podman compose down
```

### Check Logs

```bash
# All services
podman compose logs -f

# Specific service
podman compose logs -f postgres
podman compose logs -f graphql-api
```

### Clean Up

```bash
# Stop and remove containers
podman compose down

# Remove volumes (delete all data)
podman compose down -v

# Remove images
podman rmi postgres:16
podman rmi time-reporting-api
```

---

## Performance Tips

### macOS/Windows:

```bash
# Allocate more resources to Podman machine
podman machine stop
podman machine set --cpus 4 --memory 4096
podman machine start
```

### Linux:

```bash
# Podman runs natively - no VM needed!
# Already optimal performance
```

---

## Alias Setup (Optional)

If you want to use `docker` and `docker-compose` commands with Podman:

### Add to `~/.bashrc` or `~/.zshrc`:

```bash
# Podman aliases
alias docker='podman'
alias docker-compose='podman compose'

# Set DOCKER_HOST for compatibility (if needed)
export DOCKER_HOST=unix://$HOME/.local/share/containers/podman/machine/podman.sock
```

Then:
```bash
source ~/.bashrc  # or ~/.zshrc

# Now you can use
docker ps
docker-compose up -d
```

---

## Summary

**For this project, I recommend:**

1. ✅ Use `podman compose` (native, Podman 4.0+)
2. ✅ Keep the existing `docker-compose.yml` unchanged
3. ✅ Run MCP server on host (not in container)
4. ✅ Run PostgreSQL + API in containers

**Everything in the main documentation works with Podman!** Just replace:
- `docker-compose` → `podman compose`
- `docker exec` → `podman exec`
- `docker ps` → `podman ps`

---

**Ready to start?** Go back to [Task 1.3: Docker Compose PostgreSQL Setup](./tasks/phase-01-database/task-1.3-docker-compose-postgres.md) and use `podman compose` instead of `docker-compose`!
