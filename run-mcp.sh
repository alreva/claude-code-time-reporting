#!/bin/bash

# MCP Server Wrapper Script
# Checks that environment variables are set and runs the MCP server

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Verify required environment variables are set
if [ -z "$BEARER_TOKEN" ]; then
    echo "❌ Error: BEARER_TOKEN environment variable not set!" >&2
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
