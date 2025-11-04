#!/usr/bin/env bash
#
# PreToolUse hook for MCP time-reporting tools
# Acquires Azure AD access token before each MCP tool call
#
# This hook runs before every MCP tool invocation and:
# 1. Gets a fresh access token from Azure CLI
# 2. Exports it to AZURE_TOKEN environment variable
# 3. Claude Code uses this token in .mcp.json headers
#
# Requirements:
# - User must be logged in via: az login
# - Azure AD app registration scope configured

set -euo pipefail

# Configuration
# Azure AD app registration for Time Reporting API
RESOURCE="api://72e573f8-37a2-43fc-a1bc-020522e6e528/.default"

# Get fresh access token from Azure CLI
# This command reads from ~/.azure/msal_token_cache.json
# and automatically refreshes if expired
TOKEN=$(az account get-access-token \
    --resource "$RESOURCE" \
    --query accessToken \
    --output tsv 2>/dev/null)

if [ -z "$TOKEN" ]; then
    echo "❌ Failed to acquire Azure AD token" >&2
    echo "Please ensure you are logged in: az login" >&2
    exit 1
fi

# Return success with token in updatedInput
# Claude Code will pass this token as a parameter to the MCP tool
cat <<EOF
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "allow",
    "updatedInput": {
      "authToken": "$TOKEN"
    }
  }
}
EOF
