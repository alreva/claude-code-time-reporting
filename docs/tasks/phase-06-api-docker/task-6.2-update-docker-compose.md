# Task 6.2: Update Docker Compose

**Phase:** 6 - GraphQL API - Docker
**Estimated Time:** 30 minutes
**Prerequisites:** Task 6.1 complete (API Dockerfile created)
**Status:** Pending

## Objective

Update the existing `docker-compose.yml` to add the GraphQL API service alongside the PostgreSQL database, with:
- Proper service dependencies (API depends on database)
- Network configuration for inter-service communication
- Environment variable configuration
- Health checks for both services
- Automatic restart policies

## Acceptance Criteria

- [ ] `docker-compose.yml` updated with `api` service definition
- [ ] API service depends on PostgreSQL service (`depends_on` with health condition)
- [ ] API service uses Dockerfile from Task 6.1
- [ ] API service exposes port 5001 to host
- [ ] API connects to PostgreSQL via service name (`postgres`) not `localhost`
- [ ] Both services on same Docker network
- [ ] Environment variables properly configured for API service
- [ ] Health checks configured for API service
- [ ] Restart policies configured (`restart: unless-stopped`)
- [ ] Stack starts successfully with `podman compose up -d`
- [ ] Both services healthy and communicating

## Implementation

### Step 1: Update docker-compose.yml

Update the root `docker-compose.yml` file to include the API service:

```yaml
services:
  # PostgreSQL Database Service
  postgres:
    image: postgres:16-alpine
    container_name: time-reporting-db
    environment:
      POSTGRES_USER: ${POSTGRES_USER:-postgres}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
      POSTGRES_DB: ${POSTGRES_DB:-time_reporting}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./db/schema:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5
      start_period: 10s
    restart: unless-stopped
    networks:
      - time-reporting-network

  # GraphQL API Service
  api:
    build:
      context: .
      dockerfile: TimeReportingApi/Dockerfile
    container_name: time-reporting-api
    environment:
      # ASP.NET Core Configuration
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
      ASPNETCORE_URLS: http://+:5001

      # Database Connection
      # IMPORTANT: Use 'postgres' service name, not 'localhost'
      ConnectionStrings__TimeReportingDb: "Host=postgres;Port=5432;Database=${POSTGRES_DB:-time_reporting};Username=${POSTGRES_USER:-postgres};Password=${POSTGRES_PASSWORD:-postgres}"

      # Authentication
      Authentication__BearerToken: ${BEARER_TOKEN}
    ports:
      - "5001:5001"
    depends_on:
      postgres:
        condition: service_healthy  # Wait for database to be healthy
    healthcheck:
      test: ["CMD-SHELL", "curl --fail http://localhost:5001/health || exit 1"]
      interval: 10s
      timeout: 3s
      retries: 5
      start_period: 30s  # Give API more time to start up
    restart: unless-stopped
    networks:
      - time-reporting-network

volumes:
  postgres_data:
    driver: local

networks:
  time-reporting-network:
    driver: bridge
```

### Key Changes Explained

#### 1. Service Dependency with Health Check
```yaml
depends_on:
  postgres:
    condition: service_healthy
```
- API waits for PostgreSQL to be **healthy** (not just started)
- Prevents API startup failures due to database unavailability
- Uses PostgreSQL's health check to determine readiness

#### 2. Database Connection via Service Name
```yaml
ConnectionStrings__TimeReportingDb: "Host=postgres;Port=5432;..."
```
- Use `postgres` (service name) instead of `localhost`
- Docker Compose creates DNS entries for service names
- Services in same network can resolve each other by name

#### 3. Restart Policy
```yaml
restart: unless-stopped
```
- Automatically restart on failure
- Won't restart if manually stopped
- Production-ready resilience

#### 4. Named Network
```yaml
networks:
  time-reporting-network:
    driver: bridge
```
- Explicit network for better isolation
- All services on same network can communicate
- Better than default bridge network for service discovery

#### 5. Health Checks
- PostgreSQL: `pg_isready` command
- API: HTTP GET to `/health` endpoint
- Both used by Docker/Podman for orchestration

### Step 2: Update .env File (if needed)

The existing `.env` file should already have:
```bash
# PostgreSQL Configuration
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=time_reporting

# API Configuration
BEARER_TOKEN=C5ZoARiAp+pso1oTQKvL3jRFvxToo//Pc/6ZLbRIsE4=
```

Optionally add:
```bash
# ASP.NET Core Environment (Development or Production)
ASPNETCORE_ENVIRONMENT=Production
```

**Note:** The `.env` file is loaded automatically by Docker Compose.

### Step 3: Install curl in API Docker Image

The health check uses `curl`, so we need to install it in the runtime image. Update the Dockerfile's runtime stage:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for running the application
RUN groupadd -r appuser && useradd -r -g appuser appuser

# ... rest of Dockerfile
```

This was mentioned in Task 6.1's "Common Issues" but should be implemented here.

### Step 4: Start the Stack

Use the `/db-start` slash command or `podman compose` directly:

```bash
# Start all services
podman compose up -d

# Check service status
podman compose ps

# View logs
podman compose logs -f api
podman compose logs -f postgres

# Check health status
podman compose ps --format "table {{.Name}}\t{{.Status}}"
```

Expected output:
```
NAME                  STATUS
time-reporting-db     Up (healthy)
time-reporting-api    Up (healthy)
```

## Testing

### Test Cases

#### 1. Services Start Successfully
```bash
podman compose up -d
```
**Expected:** Both services start without errors

#### 2. Health Checks Pass
```bash
podman compose ps
```
**Expected:** Both services show "healthy" status within 30 seconds

#### 3. Database Connectivity from API
```bash
# Check API logs for successful database connection
podman compose logs api | grep -i "database\|postgres\|connection"
```
**Expected:** No connection errors in logs

#### 4. GraphQL Endpoint Accessible
```bash
curl -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer C5ZoARiAp+pso1oTQKvL3jRFvxToo//Pc/6ZLbRIsE4=" \
  -d '{"query":"{ projects { code name } }"}' | jq
```
**Expected:** Returns GraphQL response with projects data

#### 5. Service Dependency Order
```bash
# Stop database while API is running
podman compose stop postgres

# Check API status (should restart when trying to connect)
podman compose logs api --tail=20

# Restart database
podman compose start postgres

# API should automatically reconnect
sleep 10
podman compose logs api --tail=20
```
**Expected:** API handles database downtime gracefully

#### 6. Automatic Restart on Failure
```bash
# Kill API process
podman compose kill api

# Wait a few seconds
sleep 5

# Check if API restarted
podman compose ps api
```
**Expected:** API automatically restarts due to `restart: unless-stopped` policy

#### 7. Network Isolation
```bash
# Verify API can ping postgres by service name
podman compose exec api ping -c 3 postgres
```
**Expected:** Ping succeeds (or times out, but name resolves)

#### 8. Stop and Clean Up
```bash
# Stop all services
podman compose down

# Verify services stopped
podman compose ps
```
**Expected:** No running services

### TDD Workflow for Docker Compose

```
1. Write/update docker-compose.yml (implementation)
2. Run `podman compose up -d` (integration test - services start)
3. Run `podman compose ps` (verify health checks pass)
4. Test API endpoint with curl (functional test)
5. Test service dependencies (stop/start tests)
6. Run `podman compose down` (cleanup)
```

## Docker Compose Best Practices Applied

### Health-Based Dependencies
```yaml
depends_on:
  postgres:
    condition: service_healthy
```
- Better than simple `depends_on` without condition
- Ensures database is **ready**, not just started
- Prevents API connection failures on startup

### Environment Variable Precedence
1. Shell environment variables
2. `.env` file
3. Default values in `docker-compose.yml` (e.g., `${POSTGRES_USER:-postgres}`)

### Service Naming
- Use descriptive service names: `postgres`, `api`
- Container names for easier debugging: `time-reporting-db`, `time-reporting-api`
- Network name describes purpose: `time-reporting-network`

### Volume Naming
- Named volume `postgres_data` for persistence
- Data survives `docker-compose down`
- Only removed with `docker-compose down -v` (intentional data deletion)

### Restart Policies
- `unless-stopped`: Survives Docker daemon restarts
- Production-ready resilience
- Won't restart if manually stopped with `docker-compose stop`

## Common Issues and Solutions

### Issue: API can't connect to database
**Symptom:** `Could not establish connection to postgres`
**Solution:** Ensure API uses `Host=postgres` (service name) not `Host=localhost` in connection string

### Issue: Health checks always failing
**Symptom:** Service stuck in "starting" status
**Solution:** Check health check command syntax and timing:
- Increase `start_period` for slow startup
- Increase `interval` for slow health checks
- Verify health check command works: `podman compose exec api curl http://localhost:5001/health`

### Issue: Services start in wrong order
**Symptom:** API starts before database is ready
**Solution:** Use `condition: service_healthy` in `depends_on` (already implemented above)

### Issue: Environment variables not loading
**Symptom:** API uses default values instead of `.env` values
**Solution:**
- Ensure `.env` is in same directory as `docker-compose.yml`
- Verify `.env` file has no spaces around `=` (use `KEY=value` not `KEY = value`)
- Check `.env` is not in `.gitignore` (it should be!)

### Issue: Port 5001 already in use
**Symptom:** `Error: address already in use`
**Solution:**
```bash
# Find process using port 5001
lsof -i :5001

# Kill the process or change port mapping in docker-compose.yml
# Change: "5002:5001" to expose on different host port
```

## Related Files

**Modified:**
- `docker-compose.yml` - Added API service, network, health checks
- `TimeReportingApi/Dockerfile` - Added curl installation (from Task 6.1)

**Referenced:**
- `.env` - Environment variables
- `TimeReportingApi/Dockerfile` - API image build instructions
- `db/schema/*.sql` - Database initialization scripts

**Created:**
- None (all files already exist)

## Useful Commands

```bash
# Start services in foreground (see logs)
podman compose up

# Start services in background (detached)
podman compose up -d

# View logs (follow mode)
podman compose logs -f
podman compose logs -f api
podman compose logs -f postgres

# Check service status with health
podman compose ps

# Restart specific service
podman compose restart api

# Rebuild and restart API after code changes
podman compose up -d --build api

# Stop all services
podman compose stop

# Stop and remove containers (data persists in volumes)
podman compose down

# Stop, remove containers, and delete volumes (DESTRUCTIVE)
podman compose down -v

# View resource usage
podman compose stats

# Execute command in running container
podman compose exec api /bin/bash
podman compose exec postgres psql -U postgres -d time_reporting
```

## Next Steps

After completing this task:
1. Proceed to **Task 6.3: Environment Configuration** to improve secret management
2. Add production-specific environment configurations
3. Document environment variable requirements

## References

- [Docker Compose File Reference](https://docs.docker.com/compose/compose-file/)
- [Docker Compose Networking](https://docs.docker.com/compose/networking/)
- [Docker Compose Health Checks](https://docs.docker.com/compose/compose-file/05-services/#healthcheck)
- [Podman Compose Documentation](https://docs.podman.io/en/latest/markdown/podman-compose.1.html)

---

**Time Estimate Breakdown:**
- Update docker-compose.yml: 10 min
- Update Dockerfile for curl: 5 min
- Start and test stack: 10 min
- Troubleshooting: 5 min
- **Total: 30 minutes**
