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
