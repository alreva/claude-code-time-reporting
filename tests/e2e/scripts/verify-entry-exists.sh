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

EXISTS=$($CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT EXISTS(SELECT 1 FROM time_entries WHERE id = '$ENTRY_ID');
" | xargs)

if [[ "$EXISTS" == "t" ]]; then
    echo "✅ Entry $ENTRY_ID exists"
    $CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "
    SELECT te.id, te.project_code, pt.task_name, te.standard_hours, te.status
    FROM time_entries te
    JOIN project_tasks pt ON te.project_task_id = pt.id
    WHERE te.id = '$ENTRY_ID';
    "
    exit 0
else
    echo "❌ Entry $ENTRY_ID does not exist"
    exit 1
fi
