#!/bin/bash

ENTRY_ID=$1
EXPECTED_PROJECT=$2

if [[ -z "$ENTRY_ID" ]] || [[ -z "$EXPECTED_PROJECT" ]]; then
    echo "Usage: $0 <entry-id> <expected-project-code>"
    exit 1
fi

# Detect container command
if command -v podman &> /dev/null; then
    CONTAINER_CMD="podman"
elif command -v docker &> /dev/null; then
    CONTAINER_CMD="docker"
else
    echo "❌ FAIL: Neither podman nor docker found"
    exit 1
fi

ACTUAL_PROJECT=$($CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT project_code FROM time_entries WHERE id = '$ENTRY_ID';
" | xargs)

if [[ "$ACTUAL_PROJECT" == "$EXPECTED_PROJECT" ]]; then
    echo "✅ Entry $ENTRY_ID is on project $EXPECTED_PROJECT (as expected)"
    exit 0
else
    echo "❌ Entry $ENTRY_ID is on project $ACTUAL_PROJECT (expected: $EXPECTED_PROJECT)"
    exit 1
fi
