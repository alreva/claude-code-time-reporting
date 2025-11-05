#!/bin/bash

# Time Reporting MCP Server - Wrapper Script
# This script is mainly for manual testing.
# Claude Code automatically starts the MCP server via .mcp.json configuration.

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Verify GRAPHQL_API_URL is set
if [ -z "$GRAPHQL_API_URL" ]; then
    echo "❌ Error: GRAPHQL_API_URL environment variable not set!" >&2
    echo "" >&2
    echo "Please run:" >&2
    echo "  1. ./setup.sh             (if not done yet)" >&2
    echo "  2. source env.sh          (to load environment variables)" >&2
    echo "" >&2
    exit 1
fi

# Check if Azure CLI is authenticated
if ! az account show &> /dev/null 2>&1; then
    echo "❌ Error: Not logged in to Azure CLI!" >&2
    echo "" >&2
    echo "MCP Server uses Azure Entra ID authentication." >&2
    echo "Please login first:" >&2
    echo "  az login" >&2
    echo "" >&2
    exit 1
fi

# Run the MCP server (it will use AzureCliCredential for authentication)
exec dotnet run --project "$SCRIPT_DIR/TimeReportingMcp/TimeReportingMcp.csproj"
