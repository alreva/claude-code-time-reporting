#!/bin/bash

ENTRY_ID=$1
EXPECTED_STATUS=$2

if [[ -z "$ENTRY_ID" ]] || [[ -z "$EXPECTED_STATUS" ]]; then
    echo "Usage: $0 <entry-id> <expected-status>"
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

ACTUAL_STATUS=$($CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT status FROM time_entries WHERE id = '$ENTRY_ID';
" | xargs)

if [[ "$ACTUAL_STATUS" == "$EXPECTED_STATUS" ]]; then
    echo "✅ Entry $ENTRY_ID status is $EXPECTED_STATUS (as expected)"
    exit 0
else
    echo "❌ Entry $ENTRY_ID status is $ACTUAL_STATUS (expected: $EXPECTED_STATUS)"
    exit 1
fi
