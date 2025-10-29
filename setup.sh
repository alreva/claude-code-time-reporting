#!/bin/bash

# Time Reporting System - Setup Script
# Generates a secure bearer token and creates environment configuration files

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

# Check if env.sh already exists
if [ -f "env.sh" ]; then
    echo -e "${YELLOW}⚠️  env.sh already exists.${NC}"
    read -p "Do you want to regenerate it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Skipping generation. Using existing env.sh${NC}"
        echo ""
        echo "To load environment variables:"
        echo "  source env.sh"
        exit 0
    fi
fi

# Generate secure bearer token
echo -e "${GREEN}✓${NC} Generating secure bearer token..."
BEARER_TOKEN=$(openssl rand -base64 32)

# Create env.sh for shell environment variables
echo -e "${GREEN}✓${NC} Creating env.sh (source this to set environment variables)..."
cat > env.sh << EOF
#!/bin/bash
# Environment variables for Time Reporting System
# Source this file to set environment variables in your shell:
#   source env.sh
#
# All services (MCP Server, GraphQL API, Docker Compose) read from these environment variables

# PostgreSQL Database
export POSTGRES_USER=postgres
export POSTGRES_PASSWORD=postgres
export POSTGRES_DB=time_reporting

# GraphQL API & MCP Server (shared token)
export ASPNETCORE_ENVIRONMENT=Production
export Authentication__BearerToken=${BEARER_TOKEN}

# MCP Server
export GRAPHQL_API_URL=http://localhost:5001/graphql

echo "✅ Environment variables loaded"
echo "   Authentication__BearerToken: \${Authentication__BearerToken:0:10}..."
echo "   These variables are now available to:"
echo "     • MCP Server (run-mcp.sh)"
echo "     • GraphQL API (Docker Compose)"
echo "     • All slash commands"
EOF

chmod +x env.sh

echo -e "${GREEN}✓${NC} Configuration file created successfully"
echo ""
echo "=========================================="
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo "=========================================="
echo ""
echo "File created:"
echo "  📄 env.sh - Source this to set environment variables in your shell"
echo ""
echo "Next steps:"
echo ""
echo "  1. Load environment variables:"
echo "     ${GREEN}source env.sh${NC}"
echo ""
echo "  2. Deploy the stack:"
echo "     ${GREEN}/deploy${NC}"
echo ""
echo "Docker Compose and all services will read environment variables from your shell."
echo ""
echo -e "${YELLOW}⚠️  Remember to run 'source env.sh' in each new shell session!${NC}"
echo -e "${YELLOW}⚠️  env.sh is in .gitignore (your secret is safe).${NC}"
echo ""
