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

# Check if files already exist
if [ -f "env.sh" ] || [ -f ".env" ]; then
    echo -e "${YELLOW}⚠️  Configuration files already exist.${NC}"
    read -p "Do you want to regenerate them? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Skipping generation. Using existing files.${NC}"
        echo ""
        echo "To use existing configuration:"
        echo "  source env.sh"
        exit 0
    fi
fi

# Generate secure bearer token
echo -e "${GREEN}✓${NC} Generating secure bearer token..."
BEARER_TOKEN=$(openssl rand -base64 32)

# Create env.sh for shell environment variables
echo -e "${GREEN}✓${NC} Creating env.sh (source this in your shell)..."
cat > env.sh << EOF
#!/bin/bash
# Environment variables for Time Reporting System
# Source this file to set environment variables in your shell:
#   source env.sh

# PostgreSQL Database
export POSTGRES_USER=postgres
export POSTGRES_PASSWORD=postgres
export POSTGRES_DB=time_reporting

# GraphQL API
export ASPNETCORE_ENVIRONMENT=Production
export Authentication__BearerToken=${BEARER_TOKEN}

# MCP Server
export GRAPHQL_API_URL=http://localhost:5001/graphql
export BEARER_TOKEN=${BEARER_TOKEN}

echo "✅ Environment variables loaded"
echo "   BEARER_TOKEN: \${BEARER_TOKEN:0:10}..."
EOF

chmod +x env.sh

# Create .env file for Docker Compose
echo -e "${GREEN}✓${NC} Creating .env file (for Docker Compose)..."
cat > .env << EOF
# Environment variables for Time Reporting System
# Used by docker-compose.yml
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

echo -e "${GREEN}✓${NC} Configuration files created successfully"
echo ""
echo "=========================================="
echo -e "${GREEN}✅ Setup Complete!${NC}"
echo "=========================================="
echo ""
echo "Two files created:"
echo "  📄 env.sh  - Source this to set shell environment variables"
echo "  📄 .env    - Used by docker-compose automatically"
echo ""
echo "Next steps:"
echo ""
echo "  1. Load environment variables:"
echo "     ${GREEN}source env.sh${NC}"
echo ""
echo "  2. Deploy the stack:"
echo "     ${GREEN}/deploy${NC}"
echo ""
echo "Your bearer token will be used by:"
echo "  ✓ MCP Server (reads from shell environment)"
echo "  ✓ GraphQL API (docker-compose reads .env)"
echo "  ✓ All slash commands (use shell environment)"
echo ""
echo -e "${YELLOW}⚠️  Remember to run 'source env.sh' in each new shell session!${NC}"
echo -e "${YELLOW}⚠️  Both env.sh and .env are in .gitignore (secrets are safe).${NC}"
echo ""
