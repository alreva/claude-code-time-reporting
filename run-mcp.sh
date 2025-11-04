#!/bin/bash

# ⚠️  DEPRECATED: This stdio-based MCP server does not work properly with Azure Entra ID tokens
#
# Use the WebSocket MCP server instead:
#   - The server runs in Docker on port 5002
#   - Start with: podman compose up -d
#   - Configure Claude Code to connect to: ws://localhost:5002/mcp
#   - See .mcp.json for WebSocket configuration
#
# This script is kept for reference only and should not be used.

echo "⚠️  DEPRECATED: This stdio-based MCP server is deprecated"
echo ""
echo "The stdio MCP server does not work properly with Azure Entra ID token acquisition."
echo ""
echo "Please use the WebSocket MCP server instead:"
echo "  1. Start the full stack: podman compose up -d"
echo "  2. MCP server will be available at: ws://localhost:5002/mcp"
echo "  3. Claude Code is configured via .mcp.json to connect automatically"
echo ""
exit 1

# OLD CODE BELOW - DO NOT USE
# ============================================================

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Verify required environment variables are set
if [ -z "$Authentication__BearerToken" ]; then
    echo "❌ Error: Authentication__BearerToken environment variable not set!" >&2
    echo "" >&2
    echo "Please run:" >&2
    echo "  1. ./setup.sh             (if not done yet)" >&2
    echo "  2. source env.sh          (to load environment variables)" >&2
    echo "" >&2
    exit 1
fi

if [ -z "$GRAPHQL_API_URL" ]; then
    echo "❌ Error: GRAPHQL_API_URL environment variable not set!" >&2
    echo "" >&2
    echo "Please run:" >&2
    echo "  1. ./setup.sh             (if not done yet)" >&2
    echo "  2. source env.sh          (to load environment variables)" >&2
    echo "" >&2
    exit 1
fi

# Run the MCP server with environment variables from shell
exec dotnet run --project "$SCRIPT_DIR/TimeReportingMcp/TimeReportingMcp.csproj"
