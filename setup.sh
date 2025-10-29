#!/bin/bash

# Time Reporting System - Automated Setup Script
# This script configures your local environment for development

set -e  # Exit on any error

echo "=========================================="
echo "Time Reporting System - Setup"
echo "=========================================="
echo ""

# Color codes for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if .env already exists
if [ -f ".env" ]; then
    echo -e "${YELLOW}⚠️  .env file already exists.${NC}"
    read -p "Do you want to regenerate it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Skipping .env generation. Using existing file.${NC}"
        SKIP_ENV=true
    fi
fi

# Generate secure bearer token
if [ "$SKIP_ENV" != "true" ]; then
    echo -e "${GREEN}✓${NC} Generating secure bearer token..."
    BEARER_TOKEN=$(openssl rand -base64 32)

    # Create .env file
    echo -e "${GREEN}✓${NC} Creating .env file..."
    cat > .env << EOF
# Environment variables for Time Reporting System
# DO NOT commit this file to version control!

#######################
# PostgreSQL Database
#######################
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=time_reporting

#######################
# GraphQL API
#######################
ASPNETCORE_ENVIRONMENT=Production
Authentication__BearerToken=${BEARER_TOKEN}

#######################
# MCP Server
#######################
GRAPHQL_API_URL=http://localhost:5001/graphql
BEARER_TOKEN=${BEARER_TOKEN}
EOF

    echo -e "${GREEN}✓${NC} .env file created successfully"
fi

# Load environment variables
if [ -f ".env" ]; then
    export $(cat .env | grep -v '^#' | grep -v '^$' | xargs)
fi

# Update .mcp.json with the bearer token
echo -e "${GREEN}✓${NC} Configuring .mcp.json with bearer token..."
if command -v envsubst &> /dev/null; then
    # Use envsubst if available
    envsubst < .mcp.json > .mcp.json.tmp && mv .mcp.json.tmp .mcp.json
else
    # Fallback to sed
    sed -i.bak "s/\${BEARER_TOKEN}/${BEARER_TOKEN}/g" .mcp.json && rm -f .mcp.json.bak
fi

# Update appsettings.json with the bearer token
echo -e "${GREEN}✓${NC} Configuring appsettings.json with bearer token..."
sed -i.bak "s/REPLACE_WITH_GENERATED_TOKEN/${BEARER_TOKEN}/g" TimeReportingApi/appsettings.json && rm -f TimeReportingApi/appsettings.json.bak

# Check if Docker/Podman is available
if command -v podman &> /dev/null; then
    CONTAINER_CMD="podman"
elif command -v docker &> /dev/null; then
    CONTAINER_CMD="docker"
else
    echo -e "${RED}✗ Error: Neither Docker nor Podman found. Please install one of them.${NC}"
    exit 1
fi

echo -e "${GREEN}✓${NC} Using ${CONTAINER_CMD} for containers"

# Start PostgreSQL database
echo -e "${GREEN}✓${NC} Starting PostgreSQL database..."
if [ -f ".claude/commands/db-start.md" ]; then
    # Use slash command if available (safer)
    # Note: This requires Claude Code environment
    echo "  Run: /db-start (or manually start with podman/docker compose)"
    ${CONTAINER_CMD} compose up -d postgres
else
    ${CONTAINER_CMD} compose up -d postgres
fi

# Wait for PostgreSQL to be ready
echo -e "${YELLOW}⏳${NC} Waiting for PostgreSQL to be ready..."
for i in {1..30}; do
    if ${CONTAINER_CMD} exec time-reporting-db psql -U postgres -d time_reporting -c "SELECT 1" &> /dev/null; then
        echo -e "${GREEN}✓${NC} PostgreSQL is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        echo -e "${RED}✗ Timeout waiting for PostgreSQL${NC}"
        exit 1
    fi
    sleep 1
done

# Apply Entity Framework migrations
echo -e "${GREEN}✓${NC} Applying database migrations..."
dotnet ef database update --project TimeReportingApi

# Seed the database
echo -e "${GREEN}✓${NC} Seeding database with initial data..."
dotnet run --project TimeReportingSeeder

# Build the solution
echo -e "${GREEN}✓${NC} Building the solution..."
dotnet build

echo ""
echo "=========================================="
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo "=========================================="
echo ""
echo "Your bearer token: ${BEARER_TOKEN}"
echo ""
echo "Next steps:"
echo "  1. Start the API: /run-api (or 'dotnet run --project TimeReportingApi')"
echo "  2. Configure Claude Code to use this MCP server (see docs/integration/CLAUDE-CODE-SETUP.md)"
echo "  3. Test the GraphQL API at http://localhost:5001/graphql"
echo ""
echo "Configuration files created:"
echo "  - .env (contains your bearer token)"
echo "  - .mcp.json (configured for Claude Code)"
echo ""
echo -e "${YELLOW}⚠️  Keep your bearer token secure! Don't commit .env to git.${NC}"
echo ""
