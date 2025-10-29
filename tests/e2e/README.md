# End-to-End Test Scenarios

## Overview

This directory contains end-to-end (E2E) test scenarios for the Time Reporting System. These tests validate the complete integration:

```
Claude Code → MCP Server → GraphQL API → PostgreSQL
```

## Prerequisites

**Before running E2E tests:**

1. ✅ GraphQL API running (`/deploy` or `podman compose up -d`)
2. ✅ Claude Code configured with MCP server (see `docs/integration/CLAUDE-CODE-SETUP.md`)
3. ✅ Test database seeded with projects (`/seed-db`)
4. ✅ Bearer token configured in both API and Claude Code

**Verify setup:**
```bash
./tests/integration/verify-mcp-connection.sh
```

## Test Execution

### Manual Test Execution

Each scenario file (`scenarios/*.md`) contains:
- **User prompt** - What to say to Claude Code
- **Expected behavior** - What should happen
- **Verification** - How to confirm success

**Example workflow:**
1. Open Claude Code
2. Read scenario from `scenarios/01-log-time.md`
3. Execute user prompt in Claude Code
4. Verify expected behavior
5. Run verification script to check database

### Automated Test Execution

Use the test runner script:

```bash
# Run all E2E tests
./tests/e2e/run-all-tests.sh

# Run specific scenario
./tests/e2e/run-test.sh scenarios/01-log-time.md
```

## Test Data Management

### Setup Test Data

Create fresh test data before running scenarios:

```bash
./tests/e2e/setup-test-data.sh
```

This script:
- Clears existing test entries
- Seeds projects (INTERNAL, CLIENT-A, CLIENT-B)
- Creates sample time entries for testing updates/deletes

### Teardown Test Data

Clean up test data after scenarios:

```bash
./tests/e2e/teardown-test-data.sh
```

This script:
- Removes test time entries created during scenarios
- Preserves project configuration

## Test Scenarios

| Scenario | Tools Tested | Duration |
|----------|-------------|----------|
| [01-log-time.md](scenarios/01-log-time.md) | `log_time` | 10 min |
| [02-query-entries.md](scenarios/02-query-entries.md) | `query_time_entries` | 10 min |
| [03-update-entry.md](scenarios/03-update-entry.md) | `update_time_entry` | 10 min |
| [04-move-task.md](scenarios/04-move-task.md) | `move_task_to_project` | 10 min |
| [05-delete-entry.md](scenarios/05-delete-entry.md) | `delete_time_entry` | 5 min |
| [06-get-projects.md](scenarios/06-get-projects.md) | `get_available_projects` | 5 min |
| [07-submit-workflow.md](scenarios/07-submit-workflow.md) | `submit_time_entry` | 15 min |

**Total estimated time:** ~65 minutes for full E2E test suite

## Verification Helpers

Helper scripts in `scripts/` directory:

- `verify-entry-exists.sh <entry-id>` - Check if entry exists in database
- `verify-entry-status.sh <entry-id> <expected-status>` - Verify entry status
- `cleanup-test-entries.sh` - Remove all test entries

## Troubleshooting

### Tests Failing?

**Check connectivity:**
```bash
./tests/integration/verify-mcp-connection.sh
```

**Check API logs:**
```bash
podman compose logs -f graphql-api
```

**Check MCP server output:**
- Claude Code logs (location varies by platform)

**Verify database state:**
```bash
/db-psql
SELECT * FROM time_entries ORDER BY created_at DESC LIMIT 10;
```

### Common Issues

1. **Authentication failures** - Verify BEARER_TOKEN matches in API and Claude Code config
2. **Connection refused** - Ensure GraphQL API is running on port 5001
3. **Invalid project codes** - Run `/seed-db` to populate test projects
4. **Stale test data** - Run `./teardown-test-data.sh` then `./setup-test-data.sh`

---

## Next Steps

After completing E2E scenarios:
1. Review [Manual Workflow Test](../../docs/workflows/MANUAL-TIME-LOGGING.md)
2. Test [Auto-tracking Workflow](../../docs/workflows/AUTO-TRACKING-TEST.md)
3. Explore [Migration Workflow](../../docs/workflows/MIGRATION-WORKFLOW-TEST.md)
