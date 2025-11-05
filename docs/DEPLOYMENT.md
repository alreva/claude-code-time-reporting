# Deployment Guide

**Time Reporting System Docker/Podman Deployment**

Version: 1.0

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Quick Start](#quick-start)
4. [Configuration](#configuration)
5. [Deployment Steps](#deployment-steps)
6. [Verification](#verification)
7. [Management](#management)
8. [Troubleshooting](#troubleshooting)
9. [Production Deployment](#production-deployment)

---

## Overview

The Time Reporting System is deployed using Docker Compose (or Podman Compose) with two containerized services:

1. **PostgreSQL Database** - Data persistence
2. **GraphQL API** - ASP.NET Core application

The MCP Server runs on the host machine (not containerized) and connects to the GraphQL API.

### Architecture

```
┌─────────────────────────────────────────────────────┐
│                    HOST MACHINE                      │
│                                                       │
│  ┌────────────────┐                                  │
│  │ Claude Code    │                                  │
│  └────────┬───────┘                                  │
│           │ stdio                                     │
│  ┌────────▼───────┐                                  │
│  │ MCP Server     │                                  │
│  │ (Host Process) │                                  │
│  └────────┬───────┘                                  │
│           │ HTTP :5001                               │
│  ┌────────▼───────────────────────────────────────┐ │
│  │        DOCKER/PODMAN NETWORK                   │ │
│  │                                                 │ │
│  │  ┌─────────────────┐  ┌─────────────────────┐ │ │
│  │  │ PostgreSQL      │  │ GraphQL API         │ │ │
│  │  │ Container       │◄─┤ Container           │ │ │
│  │  │ :5432           │  │ :5001               │ │ │
│  │  └─────────────────┘  └─────────────────────┘ │ │
│  │                                                 │ │
│  └─────────────────────────────────────────────────┘ │
│                                                       │
└───────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Required Software

- **Docker OR Podman** (container runtime)
  - Docker Desktop (macOS/Windows)
  - Docker Engine (Linux)
  - Podman (all platforms, Docker Desktop alternative)
- **.NET 10 SDK** (for MCP server)
- **Claude Code** (for using the system)

### System Requirements

- **CPU:** 2+ cores recommended
- **RAM:** 4GB minimum, 8GB recommended
- **Disk:** 2GB free space (including container images)
- **OS:** macOS, Linux, or Windows

### Installation Guides

**Docker:**
- macOS: https://docs.docker.com/desktop/install/mac-install/
- Windows: https://docs.docker.com/desktop/install/windows-install/
- Linux: https://docs.docker.com/engine/install/

**Podman:**
- See [PODMAN-SETUP.md](./PODMAN-SETUP.md)

**.NET 10 SDK:**
- All platforms: https://dotnet.microsoft.com/download/dotnet/10.0

---

## Quick Start

### 1. Clone Repository

```bash
git clone <repository-url>
cd time-reporting-system
```

### 2. Authenticate with Azure

Authenticate using Azure CLI before starting the system:

```bash
az login
```

This provides your Azure Entra ID identity which is used by the MCP Server to authenticate with the GraphQL API.

**Note:** The system uses Azure Entra ID for authentication. No Azure AD tokens or environment variables are needed for authentication.

### 3. Deploy Stack

Using Docker:
```bash
docker compose up -d
```

Using Podman:
```bash
podman compose up -d
```

Or use the slash command:
```bash
/deploy
```

### 4. Seed Database

```bash
/seed-db
```

### 5. Verify Deployment

```bash
# Check containers are running
docker ps
# or
podman ps

# Test API health
curl http://localhost:5001/health
```

Expected response:
```json
{"status":"healthy"}
```

---

## Configuration

### Environment Variables

All configuration is managed through the `.env` file at the project root.

#### PostgreSQL Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `POSTGRES_USER` | PostgreSQL username | `postgres` | Yes |
| `POSTGRES_PASSWORD` | PostgreSQL password | `postgres` | Yes |
| `POSTGRES_DB` | Database name | `time_reporting` | Yes |

**Security Note:** Change the default password in production!

#### API Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Production` | Yes |
| `AzureAd__TenantId` | Azure Entra ID tenant ID | (none) | Yes |
| `AzureAd__ClientId` | API application ID from Azure AD | (none) | Yes |

**Authentication Note:**
- System uses Azure Entra ID for authentication
- API validates JWT tokens issued by Microsoft Entra ID
- User identity (oid, email, name) is extracted from validated tokens
- Run `az login` before using the system

---

### Docker Compose Configuration

The `docker-compose.yml` file defines two services:

#### PostgreSQL Service

```yaml
postgres:
  image: postgres:16-alpine
  container_name: time-reporting-db
  ports:
    - "5432:5432"
  volumes:
    - postgres_data:/var/lib/postgresql/data
  healthcheck:
    test: ["CMD-SHELL", "pg_isready -U postgres"]
    interval: 5s
  restart: unless-stopped
```

**Key Features:**
- Persistent data volume (`postgres_data`)
- Health check for startup dependencies
- Auto-restart on failure

#### GraphQL API Service

```yaml
api:
  build:
    context: .
    dockerfile: TimeReportingApi/Dockerfile
  container_name: time-reporting-api
  ports:
    - "5001:5001"
  depends_on:
    postgres:
      condition: service_healthy
  healthcheck:
    test: ["CMD-SHELL", "curl --fail http://localhost:5001/health || exit 1"]
    interval: 10s
  restart: unless-stopped
```

**Key Features:**
- Waits for PostgreSQL to be healthy before starting
- Built from source (Dockerfile)
- Health check endpoint
- Auto-restart on failure

---

## Deployment Steps

### Step-by-Step Deployment

#### 1. Prepare Configuration

```bash
# Copy example .env if not exists
cp .env.example .env

# Edit .env with your values
nano .env
```

Configure Azure AD settings:

```bash
# Set Azure AD configuration in .env
# Get these values from your Azure AD App Registration
```

Update `.env`:
```env
POSTGRES_PASSWORD=<your-postgres-password>
AzureAd__TenantId=<your-tenant-id>
AzureAd__ClientId=<your-api-app-id>
```

**Note:** See `docs/AZURE-AD-SETUP.md` for detailed Azure Entra ID configuration instructions.

#### 2. Build Images

Build the GraphQL API Docker image:

```bash
# Docker
docker compose build

# Podman
podman compose build

# Or use slash command
/build-api
```

This compiles the C# application and creates a container image.

#### 3. Start Services

Start the entire stack:

```bash
# Docker
docker compose up -d

# Podman
podman compose up -d

# Or use slash command
/deploy
```

The `-d` flag runs containers in detached mode (background).

**Startup Sequence:**
1. PostgreSQL container starts
2. Health check waits for PostgreSQL to be ready
3. GraphQL API container starts (after PostgreSQL is healthy)
4. API connects to database and applies migrations
5. Health check confirms API is responding

#### 4. Verify Services

Check container status:

```bash
# Docker
docker ps

# Podman
podman ps

# Expected output:
# time-reporting-api   Up (healthy)   5001/tcp
# time-reporting-db    Up (healthy)   5432/tcp
```

Test API connectivity:

```bash
curl http://localhost:5001/health
# Expected: {"status":"healthy"}
```

Test GraphQL endpoint (requires Azure AD token):

```bash
# Get Azure AD token
TOKEN=$(az account get-access-token --resource api://<your-api-app-id> --query accessToken -o tsv)

# Test API
curl -X POST http://localhost:5001/graphql \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"query":"query { projects { code name } }"}'
```

#### 5. Initialize Database

Apply database schema and seed data:

```bash
# Run database seeder
/seed-db

# Or manually:
dotnet run --project TimeReportingSeeder
```

This creates:
- 3 sample projects (INTERNAL, CLIENT-A, CLIENT-B)
- Tasks for each project
- Tag configurations
- No time entries (you'll create those)

Verify seeding:

```bash
# Connect to database
/db-psql

# Check projects
SELECT code, name FROM projects;

# Exit psql
\q
```

#### 6. Configure MCP Server

Update Claude Code configuration:

**macOS/Linux:**
```bash
nano ~/.config/claude-code/config.json
```

**Windows:**
```powershell
notepad %APPDATA%\claude-code\config.json
```

Configuration:
```json
{
  "mcpServers": {
    "time-reporting": {
      "command": "/absolute/path/to/time-reporting-system/run-mcp.sh"
    }
  }
}
```

**Important:**
- Make sure you've run `az login` before starting Claude Code
- The MCP Server uses `AzureCliCredential` to acquire tokens from your Azure CLI session
- Tokens are automatically refreshed when they expire

#### 7. Restart Claude Code

Close and reopen Claude Code to load the new MCP server configuration.

#### 8. Test End-to-End

In Claude Code, test the integration:

```
User: "Get available projects"
```

Expected response should list the 3 seeded projects.

```
User: "Log 8 hours of development on INTERNAL for today"
```

Expected response should confirm time entry creation.

---

## Verification

### Health Checks

#### PostgreSQL Health

```bash
# Using Docker/Podman healthcheck
docker ps  # Check status column shows "(healthy)"

# Direct database connection
/db-psql
SELECT 1;
\q
```

#### API Health

```bash
# HTTP health endpoint
curl http://localhost:5001/health

# GraphQL introspection query
curl -X POST http://localhost:5001/graphql \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"query":"{ __schema { queryType { name } } }"}'
```

#### MCP Server Connection

In Claude Code:
```
"What tools do you have access to?"
```

Should list 7 time reporting tools.

### Logs

View logs for debugging:

```bash
# All services
docker compose logs

# Specific service
docker compose logs api
docker compose logs postgres

# Follow logs in real-time
docker compose logs -f api

# Or use slash commands
/db-logs        # PostgreSQL logs
podman compose logs api  # API logs
```

### Database Verification

Connect to database and verify schema:

```bash
/db-psql

-- List tables
\dt

-- Expected tables:
-- projects
-- project_tasks
-- project_tags
-- tag_values
-- time_entries
-- time_entry_tags

-- Check seed data
SELECT * FROM projects;
SELECT * FROM project_tasks LIMIT 5;

\q
```

---

## Management

### Starting and Stopping

#### Start All Services

```bash
docker compose up -d
# or
podman compose up -d
# or
/deploy
```

#### Stop All Services

```bash
docker compose down
# or
podman compose down
```

**Note:** This stops containers but preserves data (volumes).

#### Stop and Remove Data

```bash
docker compose down -v
# or
podman compose down -v
```

**Warning:** The `-v` flag deletes all data! Use only when you want a clean slate.

#### Restart Services

```bash
docker compose restart
# or
podman compose restart

# Restart specific service
docker compose restart api
```

### Updating

#### Update API Code

After making changes to the API code:

```bash
# 1. Rebuild image
docker compose build api

# 2. Restart service
docker compose up -d api
```

#### Update Database Schema

After changing entity models or adding migrations:

```bash
# 1. Create migration (if needed)
/ef-migration

# 2. Restart API (applies migrations automatically on startup)
docker compose restart api
```

#### Update MCP Server

After changing MCP server code:

```bash
# Build MCP project
/build-mcp

# Restart Claude Code (MCP server will use new build)
```

### Monitoring

#### Container Resource Usage

```bash
# Docker
docker stats

# Podman
podman stats
```

Shows CPU, memory, network I/O for each container.

#### API Request Logs

```bash
docker compose logs -f api | grep "HTTP"
```

#### Database Connection Count

```bash
/db-psql
SELECT count(*) FROM pg_stat_activity;
\q
```

### Backup and Restore

#### Backup Database

```bash
# Create backup
docker exec time-reporting-db pg_dump -U postgres time_reporting > backup.sql
# or
podman exec time-reporting-db pg_dump -U postgres time_reporting > backup.sql

# Compress backup
gzip backup.sql
```

#### Restore Database

```bash
# Extract backup (if compressed)
gunzip backup.sql.gz

# Restore from backup
docker exec -i time-reporting-db psql -U postgres -d time_reporting < backup.sql
# or
podman exec -i time-reporting-db psql -U postgres -d time_reporting < backup.sql
```

#### Automated Backups

Create a cron job (Linux/macOS):

```bash
# Edit crontab
crontab -e

# Add daily backup at 2 AM
0 2 * * * /path/to/backup-script.sh
```

Example `backup-script.sh`:
```bash
#!/bin/bash
BACKUP_DIR="/path/to/backups"
DATE=$(date +%Y%m%d_%H%M%S)
docker exec time-reporting-db pg_dump -U postgres time_reporting | gzip > "$BACKUP_DIR/backup_$DATE.sql.gz"

# Keep only last 7 days
find "$BACKUP_DIR" -name "backup_*.sql.gz" -mtime +7 -delete
```

---

## Troubleshooting

### Container Won't Start

**Problem:** `docker compose up` fails or containers exit immediately

**Solution:**

1. Check logs:
   ```bash
   docker compose logs api
   docker compose logs postgres
   ```

2. Common causes:
   - Port already in use (5001 or 5432)
   - Invalid .env configuration
   - Database migration failed

3. Check port conflicts:
   ```bash
   # macOS/Linux
   lsof -i :5001
   lsof -i :5432

   # Windows
   netstat -ano | findstr :5001
   netstat -ano | findstr :5432
   ```

4. If port in use, stop conflicting service or change port in docker-compose.yml

### Database Connection Failed

**Problem:** API logs show "Connection refused" or database errors

**Solution:**

1. Verify PostgreSQL is healthy:
   ```bash
   docker ps
   # Status should show "(healthy)"
   ```

2. Test database connectivity:
   ```bash
   /db-psql
   ```

3. Check connection string in `.env`:
   ```env
   # WRONG (localhost doesn't work inside container)
   ConnectionStrings__TimeReportingDb=Host=localhost;...

   # CORRECT (use service name)
   ConnectionStrings__TimeReportingDb=Host=postgres;...
   ```

4. Restart API after fixing:
   ```bash
   docker compose restart api
   ```

### Authentication Errors (401 Unauthorized)

**Problem:** MCP tools or API requests return 401 Unauthorized

**Solution:**

1. Verify Azure CLI authentication:
   ```bash
   # Check if logged in
   az account show

   # If not logged in, authenticate
   az login
   ```

2. Verify Azure AD configuration in API:
   ```bash
   # Check API .env
   cat .env | grep AzureAd

   # Should see:
   # AzureAd__TenantId=<your-tenant-id>
   # AzureAd__ClientId=<your-api-app-id>
   ```

3. Test token acquisition:
   ```bash
   # Try to get a token for the API
   az account get-access-token --resource api://<your-api-app-id>
   ```

4. Restart services after configuration change:
   ```bash
   docker compose restart api
   # Close and reopen Claude Code
   ```

### MCP Server Can't Connect to API

**Problem:** MCP tools fail with "Connection refused"

**Solution:**

1. Verify API is accessible from host:
   ```bash
   curl http://localhost:5001/health
   ```

2. Check API URL in Claude Code config:
   ```json
   "GRAPHQL_API_URL": "http://localhost:5001/graphql"
   ```

   Must be `localhost`, not `postgres` (MCP runs on host, not in container network)

3. Check firewall isn't blocking port 5001

### Database Data Lost

**Problem:** Database is empty after restart

**Cause:** Docker volume was deleted

**Solution:**

1. Verify volume exists:
   ```bash
   docker volume ls | grep postgres_data
   ```

2. If missing, data was deleted. Restore from backup:
   ```bash
   docker exec -i time-reporting-db psql -U postgres -d time_reporting < backup.sql
   ```

3. Prevent future data loss:
   - Never use `docker compose down -v` in production
   - Implement regular backups
   - Consider using external volumes

---

## Production Deployment

### Pre-Production Checklist

- [ ] Configure Azure Entra ID App Registration
- [ ] Set up managed identity for production (replace AzureCliCredential)
- [ ] Change PostgreSQL default password
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable HTTPS/SSL
- [ ] Configure firewall rules
- [ ] Set up automated backups
- [ ] Configure log aggregation
- [ ] Set up monitoring/alerting
- [ ] Document rollback procedures
- [ ] Test disaster recovery

### Security Hardening

#### 1. Azure AD Configuration

```bash
# Configure Azure AD App Registration
# See docs/AZURE-AD-SETUP.md for detailed instructions

# For production, use Managed Identity instead of AzureCliCredential
# Configure in appsettings.Production.json
```

#### 2. SSL/TLS Configuration

Add reverse proxy (Nginx) for SSL termination:

```nginx
server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

#### 3. Firewall Rules

```bash
# Allow only necessary ports
sudo ufw allow 443/tcp    # HTTPS
sudo ufw deny 5001/tcp    # Block direct API access
sudo ufw deny 5432/tcp    # Block direct DB access
```

#### 4. Environment Isolation

Use separate `.env` files for each environment:

```bash
.env.development
.env.staging
.env.production  # Never commit this!
```

Load appropriate file:
```bash
docker compose --env-file .env.production up -d
```

### High Availability

#### Load Balancing

Use multiple API instances behind a load balancer:

```yaml
api:
  deploy:
    replicas: 3

```

#### Database Replication

Set up PostgreSQL primary-replica:

```yaml
postgres-replica:
  image: postgres:16-alpine
  environment:
    POSTGRES_PRIMARY_HOST: postgres
  volumes:
    - postgres_replica_data:/var/lib/postgresql/data
```

### Monitoring

#### Health Check Endpoints

- API: `http://localhost:5001/health`
- Database: Use `pg_isready` command

#### Monitoring Tools

- **Prometheus** + **Grafana** for metrics
- **ELK Stack** for logs
- **Uptime monitoring** (UptimeRobot, Pingdom)

### Backup Strategy

1. **Automated Daily Backups**
   - Retain 7 days of daily backups
   - Retain 4 weeks of weekly backups
   - Retain 12 months of monthly backups

2. **Test Restore Process Monthly**

3. **Off-site Backup Storage**
   - Cloud storage (S3, Azure Blob)
   - Geographic redundancy

---

## Additional Resources

- [Setup Guide](./integration/CLAUDE-CODE-SETUP.md)
- [User Guide](./USER_GUIDE.md)
- [API Documentation](./API.md)
- [Architecture](./ARCHITECTURE.md)
- [Podman Setup](./PODMAN-SETUP.md)

---

## Support

For issues or questions:

1. Check [Troubleshooting](#troubleshooting) section
2. Review logs: `docker compose logs`
3. Verify configuration: `.env` and `docker-compose.yml`
4. Check prerequisites are met

---

**Last Updated:** 2025-10-29
