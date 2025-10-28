# Integration Tests

Integration tests for the Time Reporting System Docker stack.

## Overview

The integration test suite verifies:
- Docker Compose stack orchestration
- Service health checks and dependencies
- Database schema and connectivity
- GraphQL API functionality (queries and mutations)
- Authentication and authorization
- Error handling and validation
- Service resilience and recovery

## Running Tests

### Prerequisites

1. Podman or Docker Compose installed
2. `.env` file with valid `BEARER_TOKEN`
3. Port 5001 available (API)
4. Port 5432 available (PostgreSQL)

### Execute Tests

```bash
# From repository root
./tests/integration/docker-stack-test.sh
```

### Options

```bash
# Skip cleanup (leave stack running after tests)
CLEANUP=false ./tests/integration/docker-stack-test.sh

# Run with custom timeout
MAX_WAIT=120 ./tests/integration/docker-stack-test.sh
```

## Test Cases

### Test 1: Stack Starts Successfully
Verifies `podman compose up -d` starts without errors.

### Test 2: Services Become Healthy
Waits for both PostgreSQL and API services to report healthy status.

### Test 3: Database Schema Loaded
Checks that all 4 tables exist: `time_entries`, `projects`, `project_tasks`, `project_tags`.

### Test 4: Health Endpoint Responds
Verifies `/health` endpoint returns HTTP 200.

### Test 5: Authentication Required
Confirms requests without Bearer token are rejected with 401 Unauthorized.

### Test 6: Query Projects
Tests authenticated GraphQL query returns project data.

### Test 7: Query Project with Tasks
Tests complex query with nested data (project → tasks → tags).

### Test 8: LogTime Mutation
Creates a new time entry and verifies response.

### Test 9: Query Time Entries
Retrieves time entries with filters.

### Test 10: UpdateTimeEntry Mutation
Modifies existing time entry (from Test 8).

### Test 11: Complete Workflow
Executes full workflow: submit entry → approve entry.

### Test 12: Error Handling
Tests API rejects invalid project code with proper error message.

### Test 13: Service Resilience
Restarts database and verifies API reconnects successfully.

### Test 14: Log Analysis
Checks API logs for critical errors or exceptions.

## Expected Output

```
========================================
Time Reporting Stack Integration Tests
========================================

[INFO] Pre-flight checks...
[INFO] Starting integration tests...

[INFO] Test 1: Docker Compose stack starts successfully
✓ PASSED: Stack started successfully

[INFO] Test 2: Both services become healthy
[INFO] Waiting for postgres to be healthy (max 60s)...
[INFO] postgres is healthy (4s)
[INFO] Waiting for api to be healthy (max 60s)...
[INFO] api is healthy (12s)
✓ PASSED: Both services are healthy

... (tests 3-14) ...

========================================
Test Results Summary
========================================
Tests run:    14
Tests passed: 14
Tests failed: 0

✓ ALL TESTS PASSED
```

## Troubleshooting

### All Tests Fail Immediately

**Cause:** Services not starting
**Solution:**
```bash
# Check service status
podman compose ps

# View logs
podman compose logs

# Restart stack
podman compose down
podman compose up -d
```

### Test 3 Fails (Database Schema)

**Cause:** Schema initialization scripts not loaded
**Solution:**
```bash
# Verify schema files exist
ls -l db/schema/

# Recreate database with fresh schema
podman compose down -v
podman compose up -d
```

### Test 5 Fails (Auth Required)

**Cause:** API not enforcing authentication
**Solution:** Check `BearerAuthMiddleware` is registered in `Program.cs`

### Test 8-11 Fail (Mutations)

**Cause:** Business logic errors or validation failures
**Solution:**
```bash
# Check API logs for errors
podman compose logs api | grep -i error

# Test mutation directly
curl -X POST http://localhost:5001/graphql \
  -H "Authorization: Bearer $BEARER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"query":"mutation { logTime(...) {...} }"}'
```

### Test 13 Fails (Resilience)

**Cause:** API doesn't reconnect after database restart
**Solution:** Check EF Core connection resilience settings

## CI/CD Integration

### GitHub Actions

```yaml
# .github/workflows/integration-test.yml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Set up environment
        run: |
          cp .env.example .env
          echo "BEARER_TOKEN=$(openssl rand -base64 32)" >> .env

      - name: Run integration tests
        run: ./tests/integration/docker-stack-test.sh
```

## References

- Docker Compose health checks: https://docs.docker.com/compose/compose-file/05-services/#healthcheck
- Podman Compose: https://docs.podman.io/en/latest/markdown/podman-compose.1.html
- GraphQL testing: https://graphql.org/learn/serving-over-http/
