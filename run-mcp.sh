#!/bin/bash

# MCP Server Wrapper Script
# Loads environment variables from .env and runs the MCP server

set -e

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Load environment variables from .env if it exists
if [ -f "$SCRIPT_DIR/.env" ]; then
    # Export all variables from .env (excluding comments and empty lines)
    set -a
    source "$SCRIPT_DIR/.env"
    set +a
else
    echo "❌ Error: .env file not found!" >&2
    echo "Please run ./setup.sh first to generate your bearer token and .env file." >&2
    exit 1
fi

# Verify required environment variables are set
if [ -z "$BEARER_TOKEN" ]; then
    echo "❌ Error: BEARER_TOKEN not set in .env file!" >&2
    exit 1
fi

if [ -z "$GRAPHQL_API_URL" ]; then
    echo "❌ Error: GRAPHQL_API_URL not set in .env file!" >&2
    exit 1
fi

# Run the MCP server with environment variables loaded
exec dotnet run --project "$SCRIPT_DIR/TimeReportingMcp/TimeReportingMcp.csproj"
