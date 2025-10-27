#!/bin/bash
# pre_bash.sh — intercepts every Bash tool call before execution
# First line of defense against direct dotnet command execution

cmd="$1"

# Block direct dotnet execution
if [[ "$cmd" =~ ^dotnet[[:space:]] ]]; then
  echo "❌ Direct dotnet execution is prohibited. Use /build, /test, /run, or /ef-migration instead."
  exit 1
fi

# Block direct docker-compose for database operations
if [[ "$cmd" =~ ^docker-compose[[:space:]]+(up|down|restart)[[:space:]] ]]; then
  echo "❌ Direct docker-compose execution is prohibited. Use /db-start, /db-stop, or /db-restart instead."
  exit 1
fi

# Block direct podman-compose for database operations
if [[ "$cmd" =~ ^podman-compose[[:space:]]+(up|down|restart)[[:space:]] ]]; then
  echo "❌ Direct podman-compose execution is prohibited. Use /db-start, /db-stop, or /db-restart instead."
  exit 1
fi

exit 0
