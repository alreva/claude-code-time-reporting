#!/bin/bash

# Time Reporting System - Setup Script
# Creates environment configuration file

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

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI not found${NC}"
    echo ""
    echo "This project uses Azure Entra ID for authentication."
    echo "Please install Azure CLI:"
    echo "  macOS: brew install azure-cli"
    echo "  Linux: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux"
    echo "  Windows: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows"
    echo ""
    exit 1
fi

# Check if user is logged in to Azure
echo -e "${GREEN}✓${NC} Checking Azure CLI authentication..."
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}⚠️  Not logged in to Azure${NC}"
    echo ""
    echo "Please login to Azure:"
    echo "  ${GREEN}az login${NC}"
    echo ""
    exit 1
fi

# Get Azure account info
AZURE_USER=$(az account show --query user.name -o tsv)
AZURE_TENANT=$(az account show --query tenantId -o tsv)

echo -e "${GREEN}✓${NC} Authenticated as: ${AZURE_USER}"
echo -e "${GREEN}✓${NC} Tenant ID: ${AZURE_TENANT}"
echo ""

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

# Create env.sh for shell environment variables
echo -e "${GREEN}✓${NC} Creating env.sh (source this to set environment variables)..."
cat > env.sh << 'EOF'
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

# GraphQL API
export ASPNETCORE_ENVIRONMENT=Production

# MCP Server
export GRAPHQL_API_URL=http://localhost:5001/graphql

echo "✅ Environment variables loaded"
echo "   These variables are now available to:"
echo "     • MCP Server (uses Azure CLI authentication)"
echo "     • GraphQL API (Docker Compose)"
echo "     • All slash commands"
echo ""
echo "Note: MCP Server requires 'az login' for authentication"
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
echo "Authentication:"
echo "  ✅ You're already logged in to Azure (${AZURE_USER})"
echo "  🔑 MCP Server will use Azure CLI credentials automatically"
echo ""
echo -e "${YELLOW}⚠️  Remember to run 'source env.sh' in each new shell session!${NC}"
echo ""
