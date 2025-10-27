#!/bin/bash
# db-guard.sh - Safe wrapper for database operations
# Usage: db-guard.sh <operation>
# Operations: start, stop, restart, logs, psql

set -e

OPERATION=$1

# Detect if docker or podman is available
# Priority: podman compose (native 5.0+) > docker-compose > podman-compose
if command -v podman &> /dev/null && podman compose version &> /dev/null; then
    COMPOSE_CMD="podman compose"
elif command -v docker-compose &> /dev/null; then
    COMPOSE_CMD="docker-compose"
elif command -v podman-compose &> /dev/null; then
    COMPOSE_CMD="podman-compose"
else
    echo "Error: No compose tool found (tried: podman compose, docker-compose, podman-compose)"
    exit 1
fi

case "$OPERATION" in
    start)
        echo "Starting PostgreSQL database..."
        $COMPOSE_CMD up -d postgres
        echo "✅ PostgreSQL is starting. Use /db-logs to view startup logs."
        ;;
    stop)
        echo "Stopping PostgreSQL database..."
        $COMPOSE_CMD stop postgres
        echo "✅ PostgreSQL stopped."
        ;;
    restart)
        echo "Restarting PostgreSQL database..."
        $COMPOSE_CMD restart postgres
        echo "✅ PostgreSQL restarted."
        ;;
    logs)
        echo "Showing PostgreSQL logs (Ctrl+C to exit)..."
        $COMPOSE_CMD logs -f postgres
        ;;
    psql)
        echo "Connecting to PostgreSQL..."
        $COMPOSE_CMD exec postgres psql -U postgres -d time_reporting
        ;;
    *)
        echo "Error: Invalid operation '$OPERATION'"
        echo "Valid operations: start, stop, restart, logs, psql"
        exit 1
        ;;
esac
