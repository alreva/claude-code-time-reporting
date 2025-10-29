#!/bin/bash

# Time Reporting System - Setup Script
# Generates a secure bearer token and creates .env file

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
        echo ""
        echo "Your existing .env file will be used."
        echo "Run this script again with 'y' to regenerate the token."
        exit 0
    fi
fi

# Generate secure bearer token
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
echo ""
echo "=========================================="
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo "=========================================="
echo ""
echo "Your bearer token has been generated and saved to .env"
echo ""
echo "Next steps:"
echo ""
echo "  1. Start database:    /db-start"
echo "  2. Run migrations:    /ef-migration"
echo "  3. Seed data:         /seed-db"
echo "  4. Build:             /build"
echo "  5. Deploy stack:      /deploy"
echo ""
echo "OR for quick deployment:"
echo "  /deploy"
echo ""
echo "The bearer token from .env will be automatically used by:"
echo "  ✓ MCP Server (via run-mcp.sh wrapper)"
echo "  ✓ GraphQL API (via docker-compose.yml)"
echo "  ✓ All slash commands"
echo ""
echo -e "${YELLOW}⚠️  Keep your .env file secure! It's already in .gitignore.${NC}"
echo ""
