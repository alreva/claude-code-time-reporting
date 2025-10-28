#!/bin/bash

ENTRY_ID=$1

if [[ -z "$ENTRY_ID" ]]; then
    echo "Usage: $0 <entry-id>"
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

echo "Migration History for Entry: $ENTRY_ID"
echo

$CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT
    te.id,
    te.project_code,
    pt.task_name,
    te.standard_hours,
    te.status,
    te.created_at,
    te.updated_at
FROM time_entries te
JOIN project_tasks pt ON te.project_task_id = pt.id
WHERE te.id = '$ENTRY_ID';"

echo
echo "Note: updated_at > created_at indicates entry was modified"
