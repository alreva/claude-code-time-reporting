#!/bin/bash

set -e

echo "=== Tearing Down E2E Test Data ==="
echo

# Detect container command
if command -v podman &> /dev/null; then
    CONTAINER_CMD="podman"
elif command -v docker &> /dev/null; then
    CONTAINER_CMD="docker"
else
    echo "❌ FAIL: Neither podman nor docker found"
    exit 1
fi

# Step 1: Delete test entries
echo "1. Deleting test entries..."
$CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE user_id = 'test-user';
" > /dev/null
echo "✅ Test entries deleted"
echo

# Step 2: Delete entries created during scenarios (by ID pattern)
echo "2. Deleting scenario entries..."
$CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -c "
DELETE FROM time_entries WHERE id IN (
  '00000000-0000-0000-0000-000000000001',
  '00000000-0000-0000-0000-000000000002',
  '00000000-0000-0000-0000-000000000003'
);
" > /dev/null
echo "✅ Scenario entries deleted"
echo

# Step 3: Verify cleanup
REMAINING_COUNT=$($CONTAINER_CMD exec time-reporting-db psql -U postgres -d time_reporting -t -c "
SELECT COUNT(*) FROM time_entries WHERE user_id = 'test-user';
" | xargs)

if [[ "$REMAINING_COUNT" -eq 0 ]]; then
    echo "✅ All test entries removed"
else
    echo "⚠️  Warning: $REMAINING_COUNT test entries still remain"
fi
echo

echo "=== Teardown Complete ==="
