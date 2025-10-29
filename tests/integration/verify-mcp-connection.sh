#!/bin/bash

set -e

echo "=== MCP Connection Verification ==="
echo

# Step 1: Check .NET SDK
echo "1. Checking .NET SDK..."
if ! command -v dotnet &> /dev/null; then
    echo "❌ FAIL: dotnet command not found"
    echo "   Install .NET 8 SDK:"
    echo "   - macOS: brew install dotnet-sdk"
    echo "   - Windows: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo "   - Linux: Follow distribution-specific instructions"
    exit 1
fi
DOTNET_VERSION=$(dotnet --version)
echo "✅ PASS: .NET SDK version $DOTNET_VERSION installed"
echo

# Step 2: Check GraphQL API
echo "2. Checking GraphQL API..."
if ! curl -s http://localhost:5001/health > /dev/null 2>&1; then
    echo "❌ FAIL: GraphQL API not responding at http://localhost:5001/health"
    echo "   Start the API with: /deploy"
    exit 1
fi
API_RESPONSE=$(curl -s http://localhost:5001/health)
echo "✅ PASS: GraphQL API is running"
echo "   Response: $API_RESPONSE"
echo

# Step 3: Check MCP Server builds
echo "3. Checking MCP Server build..."
if [[ ! -d "TimeReportingMcp" ]]; then
    echo "❌ FAIL: TimeReportingMcp directory not found"
    echo "   Run this script from the project root directory"
    exit 1
fi

cd TimeReportingMcp
if ! dotnet build --verbosity quiet > /dev/null 2>&1; then
    echo "❌ FAIL: MCP Server build failed"
    echo "   Run: /build-mcp"
    echo "   Check build errors and fix them"
    cd ..
    exit 1
fi
echo "✅ PASS: MCP Server builds successfully"
cd ..
echo

# Step 4: Check configuration file exists
echo "4. Checking Claude Code configuration..."
CONFIG_PATH=""
if [[ "$OSTYPE" == "darwin"* ]] || [[ "$OSTYPE" == "linux-gnu"* ]]; then
    CONFIG_PATH="$HOME/.config/claude-code/config.json"
elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
    CONFIG_PATH="$APPDATA/claude-code/config.json"
fi

if [[ -f "$CONFIG_PATH" ]]; then
    echo "✅ PASS: Configuration file exists at $CONFIG_PATH"

    # Check if file contains time-reporting server config
    if command -v jq &> /dev/null; then
        if jq -e '.mcpServers."time-reporting"' "$CONFIG_PATH" > /dev/null 2>&1; then
            echo "   ✅ time-reporting server configured"

            # Check GRAPHQL_API_URL
            API_URL=$(jq -r '.mcpServers."time-reporting".env.GRAPHQL_API_URL' "$CONFIG_PATH")
            echo "   GraphQL API URL: $API_URL"

            # Check BEARER_TOKEN is set (don't display actual value)
            if jq -e '.mcpServers."time-reporting".env.BEARER_TOKEN' "$CONFIG_PATH" > /dev/null 2>&1; then
                echo "   ✅ BEARER_TOKEN is configured"
            else
                echo "   ⚠️  WARN: BEARER_TOKEN not found in config"
            fi
        else
            echo "   ⚠️  WARN: time-reporting server not found in config"
            echo "          See: docs/integration/CLAUDE-CODE-SETUP.md"
        fi
    fi
else
    echo "⚠️  WARN: Configuration file not found at $CONFIG_PATH"
    echo "         You need to create it before using Claude Code"
    echo "         See: docs/integration/CLAUDE-CODE-SETUP.md"
    echo "         Example: docs/integration/claude_desktop_config.json.example"
fi
echo

# Step 5: Check bearer token is set in API
echo "5. Checking bearer token configuration..."
if [[ -f ".env" ]]; then
    if grep -q "BEARER_TOKEN" .env 2>/dev/null; then
        echo "✅ PASS: BEARER_TOKEN found in .env file"

        # Extract tokens and compare (without displaying them)
        if [[ -f "$CONFIG_PATH" ]] && command -v jq &> /dev/null; then
            API_TOKEN=$(grep BEARER_TOKEN .env | cut -d'=' -f2 | tr -d '"' | tr -d ' ')
            CLAUDE_TOKEN=$(jq -r '.mcpServers."time-reporting".env.BEARER_TOKEN' "$CONFIG_PATH" 2>/dev/null || echo "")

            if [[ "$API_TOKEN" == "$CLAUDE_TOKEN" ]] && [[ -n "$API_TOKEN" ]]; then
                echo "   ✅ Tokens match in API and Claude Code config"
            elif [[ -n "$CLAUDE_TOKEN" ]] && [[ "$CLAUDE_TOKEN" != "null" ]]; then
                echo "   ⚠️  WARN: Tokens may not match - verify manually"
            fi
        fi
    else
        echo "⚠️  WARN: BEARER_TOKEN not found in .env file"
        echo "         Generate one with: openssl rand -base64 32"
        echo "         Add to .env: BEARER_TOKEN=<your-token>"
        echo "         Restart API: /deploy"
    fi
else
    echo "⚠️  WARN: .env file not found"
    echo "         Create .env file with BEARER_TOKEN"
fi
echo

# Step 6: Check database connection
echo "6. Checking database connection..."
if command -v podman &> /dev/null; then
    CONTAINER_CMD="podman"
elif command -v docker &> /dev/null; then
    CONTAINER_CMD="docker"
else
    echo "⚠️  WARN: Neither podman nor docker found - skipping database check"
    CONTAINER_CMD=""
fi

if [[ -n "$CONTAINER_CMD" ]]; then
    if $CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "SELECT 1;" > /dev/null 2>&1; then
        echo "✅ PASS: Database connection OK"

        # Check if projects are seeded
        PROJECT_COUNT=$($CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -t -c "SELECT COUNT(*) FROM projects;" | xargs)
        echo "   Projects in database: $PROJECT_COUNT"

        if [[ "$PROJECT_COUNT" -lt 1 ]]; then
            echo "   ⚠️  WARN: No projects found - run: /seed-db"
        fi
    else
        echo "⚠️  WARN: Cannot connect to database"
        echo "         Start database: /db-start"
    fi
fi
echo

echo "=== Verification Complete ==="
echo

# Summary
echo "Summary:"
echo "--------"
echo "✅ .NET SDK: Installed"
echo "✅ GraphQL API: Running"
echo "✅ MCP Server: Builds successfully"

if [[ -f "$CONFIG_PATH" ]]; then
    echo "✅ Claude Code Config: Exists"
else
    echo "⚠️  Claude Code Config: Not found (needs setup)"
fi

if grep -q "BEARER_TOKEN" .env 2>/dev/null; then
    echo "✅ Bearer Token: Configured"
else
    echo "⚠️  Bearer Token: Not configured"
fi
echo

echo "Next steps:"
if [[ ! -f "$CONFIG_PATH" ]]; then
    echo "1. Configure Claude Code (see docs/integration/CLAUDE-CODE-SETUP.md)"
    echo "2. Restart Claude Code"
    echo "3. Test with: 'Get available projects'"
else
    echo "1. Restart Claude Code (if you just updated config)"
    echo "2. Test with: 'Get available projects'"
    echo "3. Follow E2E test scenarios: tests/e2e/README.md"
fi
