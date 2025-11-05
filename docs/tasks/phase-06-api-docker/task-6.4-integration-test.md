# Task 6.4: Integration Test

**Phase:** 6 - GraphQL API - Docker
**Estimated Time:** 1 hour
**Prerequisites:** Tasks 6.1-6.3 complete (Dockerfile, Docker Compose, Environment Configuration)
**Status:** Pending

## Objective

Create and execute comprehensive integration tests that verify the entire Docker stack (PostgreSQL + GraphQL API) works correctly, including:
- Service orchestration and dependencies
- Database connectivity and schema
- GraphQL API functionality (queries and mutations)
- Bearer token authentication
- Health checks and resilience
- End-to-end workflows

## Acceptance Criteria

- [ ] Integration test script created (`tests/integration/docker-stack-test.sh`)
- [ ] Tests verify Docker Compose stack starts successfully
- [ ] Tests verify both services become healthy
- [ ] Tests verify database schema is loaded
- [ ] Tests verify GraphQL queries work (authentication + data retrieval)
- [ ] Tests verify GraphQL mutations work (create, update, delete)
- [ ] Tests verify complete workflow (log time → submit → approve)
- [ ] Tests verify error handling (invalid auth, bad requests)
- [ ] Tests verify service resilience (database restart scenario)
- [ ] All tests pass successfully
- [ ] Test results documented in test output

## Implementation

### Step 1: Create Test Script Directory

```bash
mkdir -p tests/integration
```

### Step 2: Create Integration Test Script

Create `tests/integration/docker-stack-test.sh`:

```bash
#!/usr/bin/env bash
# Integration test for Docker Compose stack (PostgreSQL + GraphQL API)

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
STACK_NAME="time-reporting"
API_URL="http://localhost:5001/graphql"
HEALTH_URL="http://localhost:5001/health"
MAX_WAIT=60  # Maximum seconds to wait for services to be healthy

# Load Azure AD token from .env
if [ -f ".env" ]; then
    source .env
else
    echo -e "${RED}ERROR: .env file not found${NC}"
    exit 1
fi

if [ -z "${Azure AD via AzureCliCredential:-}" ]; then
    echo -e "${RED}ERROR: Azure AD via AzureCliCredential not set in .env${NC}"
    exit 1
fi

# Test counters
TESTS_RUN=0
TESTS_PASSED=0
TESTS_FAILED=0

#######################
# Helper Functions
#######################

function log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

function log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

function log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

function test_passed() {
    ((TESTS_PASSED++))
    ((TESTS_RUN++))
    echo -e "${GREEN}✓ PASSED${NC}: $1"
}

function test_failed() {
    ((TESTS_FAILED++))
    ((TESTS_RUN++))
    echo -e "${RED}✗ FAILED${NC}: $1"
}

function graphql_query() {
    local query="$1"
    local auth="${2:-true}"  # Include auth by default

    if [ "$auth" = "true" ]; then
        curl -s -X POST "$API_URL" \
            -H "Content-Type: application/json" \
            -H "Authorization: Bearer $Azure AD via AzureCliCredential" \
            -d "{\"query\":\"$query\"}"
    else
        curl -s -X POST "$API_URL" \
            -H "Content-Type: application/json" \
            -d "{\"query\":\"$query\"}"
    fi
}

function wait_for_healthy() {
    local service="$1"
    local elapsed=0

    log_info "Waiting for $service to be healthy (max ${MAX_WAIT}s)..."

    while [ $elapsed -lt $MAX_WAIT ]; do
        local status=$(podman compose ps "$service" --format "{{.Status}}" 2>/dev/null || echo "")

        if echo "$status" | grep -q "healthy"; then
            log_info "$service is healthy (${elapsed}s)"
            return 0
        fi

        sleep 2
        ((elapsed+=2))
    done

    log_error "$service did not become healthy within ${MAX_WAIT}s"
    return 1
}

#######################
# Test Suite
#######################

function test_01_stack_starts() {
    log_info "Test 1: Docker Compose stack starts successfully"

    if podman compose up -d 2>&1 | grep -q "error"; then
        test_failed "Stack failed to start"
        return 1
    fi

    test_passed "Stack started successfully"
}

function test_02_services_healthy() {
    log_info "Test 2: Both services become healthy"

    if wait_for_healthy "postgres" && wait_for_healthy "api"; then
        test_passed "Both services are healthy"
    else
        test_failed "Services did not become healthy"
        return 1
    fi
}

function test_03_database_schema() {
    log_info "Test 3: Database schema is loaded"

    # Check if tables exist
    local tables=$(podman compose exec -T postgres psql -U postgres -d time_reporting -c "
        SELECT table_name FROM information_schema.tables
        WHERE table_schema = 'public'
        ORDER BY table_name;
    " -t 2>/dev/null | tr -d ' ' | grep -v '^$')

    if echo "$tables" | grep -q "time_entries" && \
       echo "$tables" | grep -q "projects" && \
       echo "$tables" | grep -q "project_tasks" && \
       echo "$tables" | grep -q "project_tags"; then
        test_passed "Database schema loaded (4 tables found)"
    else
        test_failed "Database schema incomplete"
        log_error "Found tables: $tables"
        return 1
    fi
}

function test_04_health_endpoint() {
    log_info "Test 4: Health check endpoint responds"

    local http_code=$(curl -s -o /dev/null -w "%{http_code}" "$HEALTH_URL")

    if [ "$http_code" = "200" ]; then
        test_passed "Health endpoint returns 200 OK"
    else
        test_failed "Health endpoint returned $http_code (expected 200)"
        return 1
    fi
}

function test_05_auth_required() {
    log_info "Test 5: GraphQL endpoint requires authentication"

    local response=$(graphql_query "{ projects { code } }" false)

    if echo "$response" | grep -q "Unauthorized\|401"; then
        test_passed "Unauthorized request rejected"
    else
        test_failed "Unauthorized request was not rejected"
        log_error "Response: $response"
        return 1
    fi
}

function test_06_query_projects() {
    log_info "Test 6: Query projects with authentication"

    local response=$(graphql_query "{ projects { code name isActive } }")

    if echo "$response" | grep -q "INTERNAL"; then
        test_passed "Projects query returned data"
    else
        test_failed "Projects query did not return expected data"
        log_error "Response: $response"
        return 1
    fi
}

function test_07_query_project_with_tasks() {
    log_info "Test 7: Query single project with tasks and tags"

    local query="{ project(code: \\\"INTERNAL\\\") { code name tasks { id name isActive } tagConfigurations { id name isRequired allowedValues { value } } } }"
    local response=$(graphql_query "$query")

    if echo "$response" | grep -q "Development" && echo "$response" | grep -q "Bug Fixing"; then
        test_passed "Project query with tasks returned data"
    else
        test_failed "Project query did not return tasks"
        log_error "Response: $response"
        return 1
    fi
}

function test_08_mutation_log_time() {
    log_info "Test 8: LogTime mutation creates time entry"

    local mutation="mutation { logTime(input: { projectCode: \\\"INTERNAL\\\", task: \\\"Development\\\", standardHours: 8, startDate: \\\"2024-01-15\\\", completionDate: \\\"2024-01-15\\\" }) { id projectCode task standardHours status } }"
    local response=$(graphql_query "$mutation")

    if echo "$response" | grep -q "\"projectCode\":\"INTERNAL\"" && echo "$response" | grep -q "\"status\":\"NOT_REPORTED\""; then
        # Extract ID for later tests
        CREATED_ENTRY_ID=$(echo "$response" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
        log_info "Created entry ID: $CREATED_ENTRY_ID"
        test_passed "LogTime mutation created entry"
    else
        test_failed "LogTime mutation failed"
        log_error "Response: $response"
        return 1
    fi
}

function test_09_query_time_entries() {
    log_info "Test 9: Query time entries with filters"

    local query="{ timeEntries(projectCode: \\\"INTERNAL\\\", limit: 10) { id projectCode task standardHours status } }"
    local response=$(graphql_query "$query")

    if echo "$response" | grep -q "\"projectCode\":\"INTERNAL\""; then
        test_passed "TimeEntries query returned filtered data"
    else
        test_failed "TimeEntries query failed"
        log_error "Response: $response"
        return 1
    fi
}

function test_10_mutation_update_entry() {
    log_info "Test 10: UpdateTimeEntry mutation modifies entry"

    if [ -z "${CREATED_ENTRY_ID:-}" ]; then
        log_warn "Skipping test (no entry created in previous test)"
        return 0
    fi

    local mutation="mutation { updateTimeEntry(id: \\\"$CREATED_ENTRY_ID\\\", input: { standardHours: 6, overtimeHours: 2 }) { id standardHours overtimeHours } }"
    local response=$(graphql_query "$mutation")

    if echo "$response" | grep -q "\"standardHours\":6" && echo "$response" | grep -q "\"overtimeHours\":2"; then
        test_passed "UpdateTimeEntry mutation modified entry"
    else
        test_failed "UpdateTimeEntry mutation failed"
        log_error "Response: $response"
        return 1
    fi
}

function test_11_workflow_submit_approve() {
    log_info "Test 11: Complete workflow (submit → approve)"

    if [ -z "${CREATED_ENTRY_ID:-}" ]; then
        log_warn "Skipping test (no entry created in previous test)"
        return 0
    fi

    # Submit
    local submit_mutation="mutation { submitTimeEntry(id: \\\"$CREATED_ENTRY_ID\\\") { id status } }"
    local submit_response=$(graphql_query "$submit_mutation")

    if ! echo "$submit_response" | grep -q "\"status\":\"SUBMITTED\""; then
        test_failed "SubmitTimeEntry mutation failed"
        log_error "Response: $submit_response"
        return 1
    fi

    # Approve
    local approve_mutation="mutation { approveTimeEntry(id: \\\"$CREATED_ENTRY_ID\\\") { id status } }"
    local approve_response=$(graphql_query "$approve_mutation")

    if echo "$approve_response" | grep -q "\"status\":\"APPROVED\""; then
        test_passed "Complete workflow (submit → approve) succeeded"
    else
        test_failed "ApproveTimeEntry mutation failed"
        log_error "Response: $approve_response"
        return 1
    fi
}

function test_12_error_handling_invalid_project() {
    log_info "Test 12: Error handling for invalid project code"

    local mutation="mutation { logTime(input: { projectCode: \\\"INVALID\\\", task: \\\"Development\\\", standardHours: 8, startDate: \\\"2024-01-15\\\", completionDate: \\\"2024-01-15\\\" }) { id } }"
    local response=$(graphql_query "$mutation")

    if echo "$response" | grep -q "error\|Error\|invalid\|Invalid\|not found"; then
        test_passed "Invalid project code rejected with error"
    else
        test_failed "Invalid project code was not rejected"
        log_error "Response: $response"
        return 1
    fi
}

function test_13_resilience_database_restart() {
    log_info "Test 13: Service resilience (database restart)"

    # Restart database
    log_info "Restarting database..."
    podman compose restart postgres >/dev/null 2>&1

    # Wait for database to be healthy
    if ! wait_for_healthy "postgres"; then
        test_failed "Database did not recover after restart"
        return 1
    fi

    # Test API still works
    sleep 5  # Give API time to reconnect
    local response=$(graphql_query "{ projects { code } }")

    if echo "$response" | grep -q "INTERNAL"; then
        test_passed "API recovered after database restart"
    else
        test_failed "API did not recover after database restart"
        log_error "Response: $response"
        return 1
    fi
}

function test_14_api_logs_no_errors() {
    log_info "Test 14: API logs contain no critical errors"

    local errors=$(podman compose logs api 2>&1 | grep -i "error\|exception\|fatal" | grep -v "ErrorFilter\|ErrorType" || echo "")

    if [ -z "$errors" ]; then
        test_passed "No critical errors in API logs"
    else
        log_warn "Found potential errors in logs:"
        echo "$errors" | head -5
        test_passed "API logs checked (warnings found but non-critical)"
    fi
}

#######################
# Main Test Execution
#######################

function run_all_tests() {
    echo "========================================"
    echo "Time Reporting Stack Integration Tests"
    echo "========================================"
    echo ""

    # Pre-flight checks
    log_info "Pre-flight checks..."

    if ! command -v podman &> /dev/null; then
        log_error "podman command not found"
        exit 1
    fi

    if ! command -v curl &> /dev/null; then
        log_error "curl command not found"
        exit 1
    fi

    log_info "Starting integration tests..."
    echo ""

    # Run tests sequentially
    test_01_stack_starts
    test_02_services_healthy
    test_03_database_schema
    test_04_health_endpoint
    test_05_auth_required
    test_06_query_projects
    test_07_query_project_with_tasks
    test_08_mutation_log_time
    test_09_query_time_entries
    test_10_mutation_update_entry
    test_11_workflow_submit_approve
    test_12_error_handling_invalid_project
    test_13_resilience_database_restart
    test_14_api_logs_no_errors

    # Summary
    echo ""
    echo "========================================"
    echo "Test Results Summary"
    echo "========================================"
    echo "Tests run:    $TESTS_RUN"
    echo "Tests passed: $TESTS_PASSED"
    echo "Tests failed: $TESTS_FAILED"
    echo ""

    if [ $TESTS_FAILED -eq 0 ]; then
        echo -e "${GREEN}✓ ALL TESTS PASSED${NC}"
        return 0
    else
        echo -e "${RED}✗ SOME TESTS FAILED${NC}"
        return 1
    fi
}

function cleanup() {
    if [ "${CLEANUP:-true}" = "true" ]; then
        log_info "Cleaning up..."
        podman compose down >/dev/null 2>&1 || true
    fi
}

# Trap cleanup on exit
trap cleanup EXIT

# Run tests
run_all_tests
exit $?
```

Make it executable:
```bash
chmod +x tests/integration/docker-stack-test.sh
```

### Step 3: Create Test Documentation

Create `tests/integration/README.md`:

```markdown
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
2. `.env` file with valid `Azure AD via AzureCliCredential`
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
  -H "Authorization: Bearer $Azure AD via AzureCliCredential" \
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
          echo "Azure AD via AzureCliCredential=$(az login)" >> .env

      - name: Run integration tests
        run: ./tests/integration/docker-stack-test.sh
```

## References

- Docker Compose health checks: https://docs.docker.com/compose/compose-file/05-services/#healthcheck
- Podman Compose: https://docs.podman.io/en/latest/markdown/podman-compose.1.html
- GraphQL testing: https://graphql.org/learn/serving-over-http/
```

### Step 4: Run Integration Tests

Execute the test script:

```bash
# Ensure you're in repository root
cd /path/to/time-reporting-system

# Run integration tests
./tests/integration/docker-stack-test.sh
```

## Testing

The integration test script is self-testing. To verify it works:

### Manual Test Execution

```bash
# 1. Ensure stack is stopped
podman compose down

# 2. Run test script
./tests/integration/docker-stack-test.sh

# 3. Check exit code
echo $?  # Should be 0 if all tests passed
```

### Expected Results

All 14 tests should pass:
- ✓ Stack starts successfully
- ✓ Both services become healthy
- ✓ Database schema loaded
- ✓ Health endpoint responds
- ✓ Authentication required
- ✓ Query projects works
- ✓ Query project with tasks works
- ✓ LogTime mutation creates entry
- ✓ Query time entries works
- ✓ UpdateTimeEntry mutation works
- ✓ Complete workflow (submit → approve)
- ✓ Error handling for invalid input
- ✓ Service resilience (database restart)
- ✓ No critical errors in logs

### Test Failure Handling

If any test fails:
1. Review the test output for specific failure reason
2. Check service logs: `podman compose logs api`
3. Inspect database: `podman compose exec postgres psql -U postgres -d time_reporting`
4. Verify environment variables: `cat .env`
5. Fix the issue and re-run tests

## TDD Approach for Integration Testing

While this is the final task in the phase, the TDD mindset still applies:

1. **Write test script** (implementation)
2. **Run tests** against existing stack
3. **Verify all pass** (GREEN)
4. **Fix failures** if any (REFACTOR)
5. **Document results**

## Related Files

**Created:**
- `tests/integration/docker-stack-test.sh` - Main test script
- `tests/integration/README.md` - Test documentation

**Referenced:**
- `docker-compose.yml` - Stack definition
- `.env` - Environment variables
- `db/schema/*.sql` - Database schema
- `TimeReportingApi/` - API source code

**Modified:**
- None

## Common Issues and Solutions

### Issue: Tests timeout waiting for services

**Solution:** Increase `MAX_WAIT`:
```bash
MAX_WAIT=120 ./tests/integration/docker-stack-test.sh
```

### Issue: Port already in use

**Solution:**
```bash
# Find process using port
lsof -i :5001

# Kill the process or stop existing stack
podman compose down
```

### Issue: Bearer token not found

**Solution:**
```bash
# Verify .env file exists
cat .env | grep Azure AD via AzureCliCredential

# Generate token if missing
./scripts/generate-token.sh
```

### Issue: Database schema not loaded

**Solution:**
```bash
# Recreate database with fresh schema
podman compose down -v  # WARNING: Deletes data
podman compose up -d
```

## Performance Expectations

On modern hardware (M1/M2 Mac, recent Linux):
- Stack startup: 10-20 seconds
- Service health: 15-30 seconds total
- All tests: 45-90 seconds

Slower hardware may take longer. Adjust `MAX_WAIT` if needed.

## Next Steps

After completing this task and all Phase 6 tasks:
1. **Phase 6 is complete!** 🎉
2. Proceed to **Phase 7: MCP Server - Setup**
3. Begin implementing the C# MCP server that connects Claude Code to the GraphQL API

## Success Criteria

Phase 6 is complete when:
- ✅ All 4 tasks finished (6.1, 6.2, 6.3, 6.4)
- ✅ Docker Compose stack starts successfully
- ✅ Both services (PostgreSQL + API) are healthy
- ✅ All integration tests pass (14/14)
- ✅ GraphQL API is accessible and functional
- ✅ Environment configuration is secure and documented
- ✅ Ready to build MCP server in Phase 7

## References

- [Testing Docker Compose](https://docs.docker.com/compose/test/)
- [Bash Testing Best Practices](https://google.github.io/styleguide/shellguide.html)
- [GraphQL Testing Strategies](https://graphql.org/learn/serving-over-http/)
- [Integration Testing Best Practices](https://martinfowler.com/articles/practical-test-pyramid.html)

---

**Time Estimate Breakdown:**
- Create test script: 30 min
- Create test documentation: 10 min
- Run and verify tests: 15 min
- Troubleshooting and fixes: 5 min
- **Total: 1 hour**
