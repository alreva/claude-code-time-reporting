#!/bin/bash
# guard.sh — Blocks dotnet commands unless run from slash command context
# Usage: guard.sh "command" "context"
#   - command: The command to execute
#   - context: "slash" if coming from a slash command, empty otherwise

cmd="$1"
context="$2"  # expected "slash" if coming from a slash command

if [[ "$cmd" =~ ^dotnet ]] && [[ "$context" != "slash" ]]; then
  echo "❌ dotnet commands are only allowed via slash commands."
  echo "Use /build, /test, /run, or /ef-migration instead."
  exit 1
fi

# Execute normally if safe
eval "$cmd"
