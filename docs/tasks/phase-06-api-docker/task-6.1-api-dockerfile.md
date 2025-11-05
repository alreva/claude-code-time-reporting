# Task 6.1: API Dockerfile

**Phase:** 6 - GraphQL API - Docker
**Estimated Time:** 1 hour
**Prerequisites:** Phase 5 complete (all mutations implemented and tested)
**Status:** Pending

## Objective

Create a production-ready multi-stage Dockerfile for the ASP.NET Core GraphQL API that:
- Uses multi-stage builds to minimize final image size
- Builds the application in a build stage
- Runs the application in a minimal runtime stage
- Follows .NET Docker best practices
- Supports both development and production scenarios

## Acceptance Criteria

- [ ] Dockerfile created at `TimeReportingApi/Dockerfile`
- [ ] `.dockerignore` created at `TimeReportingApi/.dockerignore`
- [ ] Uses multi-stage build pattern (build stage + runtime stage)
- [ ] Build stage uses `mcr.microsoft.com/dotnet/sdk:10.0-preview` base image
- [ ] Runtime stage uses `mcr.microsoft.com/dotnet/aspnet:10.0-preview` base image
- [ ] Application listens on port 5001 (to avoid macOS AirPlay conflict on port 5001)
- [ ] Non-root user configured for security
- [ ] All dependencies properly restored and built
- [ ] Image builds successfully with `docker build` or `podman build`
- [ ] Image size optimized (runtime image should be ~250-280MB for .NET 10 preview)
- [ ] Health check endpoint `/health` accessible
- [ ] **Slash command `/deploy` created** for building and deploying the Docker stack
- [ ] `/deploy` command registered in `.claude/settings.local.json` permissions

## Implementation

### Step 1: Create Dockerfile

Create `TimeReportingApi/Dockerfile` with the following structure:

```dockerfile
# Multi-stage Dockerfile for Time Reporting GraphQL API

#######################
# Stage 1: Build Stage
#######################
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files for dependency restoration
# Copy in order of least to most frequently changed for better layer caching
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["TimeReportingAnalyzers/TimeReportingAnalyzers.csproj", "TimeReportingAnalyzers/"]
COPY ["TimeReportingApi/TimeReportingApi.csproj", "TimeReportingApi/"]

# Restore dependencies (this layer will be cached unless project files change)
RUN dotnet restore "TimeReportingApi/TimeReportingApi.csproj"

# Copy remaining source code
COPY ["TimeReportingAnalyzers/", "TimeReportingAnalyzers/"]
COPY ["TimeReportingApi/", "TimeReportingApi/"]

# Build the application in Release configuration
WORKDIR /src/TimeReportingApi
RUN dotnet build "TimeReportingApi.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "TimeReportingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

########################
# Stage 2: Runtime Stage
########################
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create a non-root user for running the application
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application from build stage
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port 5001 (avoid macOS AirPlay conflict on port 5001)
EXPOSE 5001

# Health check using the /health endpoint
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:5001/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "TimeReportingApi.dll"]
```

### Step 2: Create .dockerignore

Create `TimeReportingApi/.dockerignore` to exclude unnecessary files from the Docker build context:

```
# Exclude build outputs
bin/
obj/
publish/
out/

# Exclude IDE files
.vs/
.vscode/
.idea/
*.user
*.suo

# Exclude test results
TestResults/
*.trx

# Exclude local config
appsettings.Development.json
appsettings.*.json
!appsettings.json

# Exclude documentation
*.md
!README.md

# Exclude git
.git/
.gitignore
.gitattributes

# Exclude Docker files
Dockerfile
.dockerignore
```

### Step 3: Update appsettings for Docker

The current `appsettings.json` has hardcoded localhost connections. For Docker, we need to support environment variable overrides.

The connection string and Azure AD token should be overridable via environment variables:
- `ConnectionStrings__TimeReportingDb` - Connection string
- `Azure AD via AzureCliCredential` - Bearer token

ASP.NET Core's configuration system automatically reads environment variables with double underscore notation.

### Step 4: Create /deploy Slash Command

Create a convenient slash command for building and deploying the Docker stack.

Create `.claude/commands/deploy.md`:

```markdown
---
description: Build and deploy the full Docker stack (PostgreSQL + API)
allowed-tools: Bash(podman:*)
---

# Deploy Command

Builds the Docker image and deploys the full stack using Podman Compose.

## What This Does

1. Builds the TimeReportingApi Docker image
2. Starts the PostgreSQL database service
3. Starts the GraphQL API service
4. Waits for services to become healthy
5. Shows service status

## Usage

```bash
/deploy
```

## Options

**Rebuild:**
```bash
# Force rebuild of images (ignore cache)
/deploy --rebuild
```

**Clean start:**
```bash
# Stop existing stack, remove containers, and deploy fresh
/deploy --clean
```

## Execution

```bash
#!/usr/bin/env bash
set -euo pipefail

# Parse arguments
REBUILD=false
CLEAN=false

for arg in "$@"; do
    case $arg in
        --rebuild)
            REBUILD=true
            shift
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
    esac
done

echo "🚀 Deploying Time Reporting Stack..."
echo ""

# Clean start if requested
if [ "$CLEAN" = true ]; then
    echo "🧹 Cleaning existing stack..."
    podman compose down
    echo ""
fi

# Build image
if [ "$REBUILD" = true ]; then
    echo "🔨 Building Docker image (forced rebuild)..."
    podman compose build --no-cache api
else
    echo "🔨 Building Docker image..."
    podman compose build api
fi

echo ""
echo "🚀 Starting services..."
podman compose up -d

echo ""
echo "⏳ Waiting for services to become healthy..."
sleep 5

# Wait for services (max 60 seconds)
MAX_WAIT=60
ELAPSED=0

while [ $ELAPSED -lt $MAX_WAIT ]; do
    POSTGRES_STATUS=$(podman compose ps postgres --format "{{.Status}}" 2>/dev/null || echo "")
    API_STATUS=$(podman compose ps api --format "{{.Status}}" 2>/dev/null || echo "")

    POSTGRES_HEALTHY=$(echo "$POSTGRES_STATUS" | grep -q "healthy" && echo "true" || echo "false")
    API_HEALTHY=$(echo "$API_STATUS" | grep -q "healthy" && echo "true" || echo "false")

    if [ "$POSTGRES_HEALTHY" = "true" ] && [ "$API_HEALTHY" = "true" ]; then
        echo "✅ All services are healthy!"
        break
    fi

    echo "  Waiting... (${ELAPSED}s)"
    sleep 3
    ((ELAPSED+=3))
done

if [ $ELAPSED -ge $MAX_WAIT ]; then
    echo "⚠️  Services did not become healthy within ${MAX_WAIT}s"
    echo ""
    echo "Check service status with: podman compose ps"
    echo "Check logs with: podman compose logs"
    exit 1
fi

echo ""
echo "📊 Service Status:"
podman compose ps

echo ""
echo "✅ Stack deployed successfully!"
echo ""
echo "🌐 GraphQL Playground: http://localhost:5001/graphql"
echo "💚 Health Check: http://localhost:5001/health"
echo ""
echo "📋 Useful commands:"
echo "  View logs:        podman compose logs -f"
echo "  View API logs:    podman compose logs -f api"
echo "  View DB logs:     podman compose logs -f postgres"
echo "  Stop stack:       podman compose down"
echo "  Service status:   podman compose ps"
```
```

### Step 5: Register /deploy in Permissions

Add the `/deploy` command to `.claude/settings.local.json` in the permissions allow list:

```json
{
  "permissions": {
    "allow": [
      "SlashCommand(/deploy)",
      // ... other existing permissions
    ]
  }
}
```

### Step 6: Build and Test Locally

Test the Dockerfile locally using Podman (since this environment uses Podman):

```bash
# Option 1: Use the new /deploy command (recommended)
/deploy

# Option 2: Manual build and run
# Build the Docker image
podman build -t time-reporting-api:latest -f TimeReportingApi/Dockerfile .

# Check image size
podman images time-reporting-api:latest

# Run the container (connected to host network for database access)
podman run --rm -p 5001:5001 \
  -e ConnectionStrings__TimeReportingDb="Host=host.containers.internal;Port=5432;Database=time_reporting;Username=postgres;Password=postgres" \
  -e Azure AD via AzureCliCredential="YOUR_Azure AD via AzureCliCredential_HERE" \
  time-reporting-api:latest

# In another terminal, test health endpoint
curl http://localhost:5001/health

# Test GraphQL endpoint with Azure AD token
curl -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_Azure AD via AzureCliCredential_HERE" \
  -d '{"query":"{ projects { code name } }"}'
```

## Testing

### Test Cases

#### 1. Image Build Success
```bash
podman build -t time-reporting-api:test -f TimeReportingApi/Dockerfile .
```
**Expected:** Build completes successfully with no errors

#### 2. Image Size Verification
```bash
podman images time-reporting-api:test --format "{{.Size}}"
```
**Expected:** Runtime image size is approximately 220-250MB

#### 3. Container Starts Successfully
```bash
podman run --rm -d --name api-test \
  -p 5001:5001 \
  -e ConnectionStrings__TimeReportingDb="Host=host.containers.internal;Port=5432;Database=time_reporting;Username=postgres;Password=postgres" \
  -e Azure AD via AzureCliCredential="YOUR_Azure AD via AzureCliCredential_HERE" \
  time-reporting-api:test

# Wait a few seconds for startup
sleep 5

# Check logs
podman logs api-test
```
**Expected:** Application starts without errors, logs show "Now listening on: http://[::]:5001"

#### 4. Health Check Passes
```bash
curl -f http://localhost:5001/health
echo $?
```
**Expected:** Returns HTTP 200, exit code 0

#### 5. GraphQL Endpoint Works
```bash
curl -X POST http://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_Azure AD via AzureCliCredential_HERE" \
  -d '{"query":"{ projects { code name } }"}' | jq
```
**Expected:** Returns valid GraphQL response with projects data

#### 6. Non-Root User Verification
```bash
podman exec api-test whoami
```
**Expected:** Returns "appuser" (not "root")

#### 7. Cleanup
```bash
podman stop api-test
podman rmi time-reporting-api:test
```

### TDD Approach for Dockerfile

While Dockerfiles don't have traditional unit tests, follow this verification workflow:

1. **Write Dockerfile** (implementation)
2. **Build image** (compile/verify syntax)
3. **Run container** (integration test)
4. **Test endpoints** (functional test)
5. **Verify security** (non-root user, image size)
6. **Clean up** (remove test artifacts)

## Docker Best Practices Applied

### Multi-Stage Builds
- Separates build dependencies from runtime dependencies
- Reduces final image size by ~60% (sdk:10.0-preview is ~800MB, aspnet:10.0-preview is ~250MB)

### Layer Caching Optimization
- Copy project files first for dependency restoration
- Source code copied last (changes most frequently)
- Maximizes Docker layer cache hits

### Security
- Non-root user (`appuser`) for running the application
- Minimal runtime image (aspnet:10.0-preview vs sdk:10.0-preview)
- No unnecessary tools or packages in runtime image

### Health Checks
- Built-in health check using `/health` endpoint
- Docker/Podman can automatically restart unhealthy containers
- Load balancers can route traffic away from unhealthy instances

### Configuration
- Environment variables for connection strings and secrets
- No hardcoded credentials in the image
- Follows 12-factor app methodology

## Related Files

**Created:**
- `TimeReportingApi/Dockerfile`
- `TimeReportingApi/.dockerignore`
- `.claude/commands/deploy.md` - New slash command for deployment

**Modified:**
- `.claude/settings.local.json` - Added `/deploy` to permissions allow list

**Referenced:**
- `TimeReportingApi/TimeReportingApi.csproj`
- `Directory.Build.props`
- `Directory.Packages.props`
- `TimeReportingAnalyzers/TimeReportingAnalyzers.csproj`

## Common Issues and Solutions

### Issue: curl not found in health check
**Solution:** Install curl in the runtime stage:
```dockerfile
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
```

### Issue: Database connection fails with "host.containers.internal"
**Solution for Podman:** Use `--network=host` instead of port mapping, or use the actual database container name when both are in the same Docker Compose network.

### Issue: Build fails on COPY step
**Solution:** Ensure Docker build context is the repository root, not the TimeReportingApi directory:
```bash
# Wrong (from TimeReportingApi directory)
podman build -t api -f Dockerfile .

# Correct (from repository root)
podman build -t api -f TimeReportingApi/Dockerfile .
```

### Issue: Image size is too large (>300MB)
**Solution:** Verify you're using the runtime stage as final stage, not the build stage. Check `FROM ... AS runtime` line is last stage.

## Next Steps

After completing this task:
1. Proceed to **Task 6.2: Update Docker Compose** to add the API service
2. Configure networking between PostgreSQL and API services
3. Set up environment variable management

## References

- [.NET Docker Official Images](https://hub.docker.com/_/microsoft-dotnet)
- [ASP.NET Core Docker Documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- [Docker Multi-Stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [Dockerfile Best Practices](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)

---

**Time Estimate Breakdown:**
- Write Dockerfile: 20 min
- Create .dockerignore: 5 min
- Create /deploy slash command: 10 min
- Register /deploy in permissions: 2 min
- Build and test locally: 15 min
- Troubleshooting and optimization: 8 min
- **Total: 1 hour**
