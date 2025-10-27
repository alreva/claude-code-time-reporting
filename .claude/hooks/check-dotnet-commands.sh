#!/bin/bash
# check-dotnet-commands.sh - PreToolUse hook for detailed enforcement
# Warns about direct dotnet commands and suggests slash commands instead

tool_use=$(cat)

if echo "$tool_use" | grep -q '"tool_name":"Bash"'; then
  command=$(echo "$tool_use" | grep -o '"command":"[^"]*"' | head -1 | sed 's/"command":"//' | sed 's/"$//')

  # Check if it's a dotnet build, test, run, watch, or ef command
  if echo "$command" | grep -qE '^dotnet[[:space:]]+(build|test|run|watch|ef)'; then
    # Deny the command (blocking)
    cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "deny",
    "permissionDecisionReason": "❌ Direct dotnet commands are not allowed. Please use slash commands instead:\n\n- /build (instead of 'dotnet build')\n- /test (instead of 'dotnet test')\n- /run-api (instead of 'dotnet run' for GraphQL API)\n- /run-mcp (instead of 'dotnet run' for MCP Server)\n- /ef-migration (for Entity Framework migrations)\n\nFor database operations:\n- /db-start (start PostgreSQL)\n- /db-stop (stop PostgreSQL)\n- /db-restart (restart PostgreSQL)\n- /db-logs (view PostgreSQL logs)\n- /db-psql (connect to PostgreSQL)"
  }
}
EOF
    exit 0
  fi

  # Check if it's a docker-compose or podman-compose database command
  if echo "$command" | grep -qE '^(docker-compose|podman-compose)[[:space:]]+(up|down|restart)'; then
    cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "deny",
    "permissionDecisionReason": "❌ Direct docker-compose/podman-compose commands are not allowed. Please use slash commands instead:\n\n- /db-start (start PostgreSQL)\n- /db-stop (stop PostgreSQL)\n- /db-restart (restart PostgreSQL)\n- /db-logs (view logs)\n- /db-psql (connect to database)"
  }
}
EOF
    exit 0
  fi
fi

# Allow the tool use
cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "allow"
  }
}
EOF
exit 0
