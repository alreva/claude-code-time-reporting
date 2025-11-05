# Phase 11: Integration & Testing

> **Note:** This phase documentation contains historical references to Azure AD token authentication. The current implementation uses Azure Entra ID authentication via `az login`. See updated setup guides in `docs/integration/CLAUDE-CODE-SETUP.md`.

**Total Tasks:** 5
**Estimated Time:** 5.5 hours
**Status:** Ready for Implementation

---

## Overview

Phase 11 focuses on comprehensive integration testing of the complete Time Reporting System, from Claude Code through the MCP server to the GraphQL API and PostgreSQL database.

**Key Objectives:**
- ✅ Configure Claude Code to work with the MCP server
- ✅ Create end-to-end test scenarios for all 7 MCP tools
- ✅ Test manual time logging workflow (create, query, update, delete, submit)
- ✅ Test auto-tracking feature (smart suggestions, session context)
- ✅ Test project migration workflow (move entries between projects)

---

## Task Breakdown

| Task | Description | Est. Time | Dependencies |
|------|-------------|-----------|--------------|
| [11.1](./task-11.1-claude-code-configuration.md) | Claude Code Configuration | 30 min | Phase 10 complete |
| [11.2](./task-11.2-e2e-test-scenarios.md) | E2E Test Scenarios | 2 hrs | Task 11.1 |
| [11.3](./task-11.3-manual-workflow-test.md) | Manual Workflow Test | 1 hr | Task 11.2 |
| [11.4](./task-11.4-auto-tracking-test.md) | Auto-tracking Test | 1 hr | Task 11.3 |
| [11.5](./task-11.5-migration-workflow-test.md) | Migration Workflow Test | 1 hr | Task 11.3 |

**Total:** 5.5 hours

---

## Quick Start

### Prerequisites

Before starting Phase 11, ensure:
- [ ] **Phase 10 complete** - All auto-tracking features implemented
- [ ] **GraphQL API deployed** - Running via Docker/Podman
- [ ] **Database seeded** - Test projects and tasks populated
- [ ] **MCP Server built** - `dotnet build` successful
- [ ] **.NET 10 SDK installed** - `dotnet --version` shows 8.0.x

**Verify:**
```bash
# Check deployment
/status

# Build MCP Server
/build-mcp

# Verify API health
curl http://localhost:5001/health
```

### Task Execution Order

**Follow tasks sequentially:**

1. **Task 11.1:** Set up Claude Code configuration
   - Create example config file
   - Document platform-specific setup
   - Generate Azure AD token
   - Configure MCP server in Claude Code

2. **Task 11.2:** Create E2E test infrastructure
   - Document test scenarios for all 7 tools
   - Create setup/teardown scripts
   - Write verification helpers
   - Test at least Scenario 01 (log_time)

3. **Task 11.3:** Test manual workflow
   - Execute complete day-of-work scenario
   - Test all CRUD operations
   - Validate natural language variations
   - Verify database state at each step

4. **Task 11.4:** Test auto-tracking
   - Test session detection
   - Verify context persistence
   - Test confirmation prompts
   - Validate suggestion accuracy

5. **Task 11.5:** Test migration workflow
   - Test moving entries between projects
   - Verify task revalidation
   - Test tag cleanup
   - Test batch migrations

---

## Deliverables

### Configuration Files
- `docs/integration/claude_desktop_config.json.example` - Example Claude Code config
- `docs/integration/CLAUDE-CODE-SETUP.md` - Setup and troubleshooting guide

### Test Infrastructure
- `tests/e2e/README.md` - E2E test overview
- `tests/e2e/setup-test-data.sh` - Test data setup script
- `tests/e2e/teardown-test-data.sh` - Test data cleanup script
- `tests/e2e/scripts/verify-*.sh` - Verification helper scripts

### Test Scenarios
- `tests/e2e/scenarios/01-log-time.md` - Log time tool scenarios
- `tests/e2e/scenarios/02-query-entries.md` - Query scenarios (to be created)
- `tests/e2e/scenarios/03-update-entry.md` - Update scenarios (to be created)
- `tests/e2e/scenarios/04-move-task.md` - Move task scenarios (to be created)
- `tests/e2e/scenarios/05-delete-entry.md` - Delete scenarios (to be created)
- `tests/e2e/scenarios/06-get-projects.md` - Get projects scenarios (to be created)
- `tests/e2e/scenarios/07-submit-workflow.md` - Workflow scenarios (to be created)

### Workflow Guides
- `docs/workflows/MANUAL-TIME-LOGGING.md` - Complete manual workflow
- `docs/workflows/AUTO-TRACKING-TEST.md` - Auto-tracking test guide
- `docs/workflows/MIGRATION-WORKFLOW-TEST.md` - Migration test guide
- `docs/workflows/WORKFLOW-VALIDATION-CHECKLIST.md` - Validation checklist

### Test Scripts
- `tests/integration/verify-mcp-connection.sh` - MCP connection verification
- `tests/auto-tracking/check-session-context.sh` - Session context checker
- `tests/auto-tracking/verify-auto-tracking.sh` - Auto-tracking verification
- `tests/migration/verify-migration.sh` - Migration verification
- `tests/migration/migration-history.sh` - Migration history tracker

---

## Test Coverage

### MCP Tools (7 total)

| Tool | Test Coverage | Scenarios |
|------|---------------|-----------|
| `log_time` | E2E, Manual, Auto-tracking | 6+ scenarios |
| `query_time_entries` | E2E, Manual | 5+ scenarios |
| `update_time_entry` | E2E, Manual | 4+ scenarios |
| `move_task_to_project` | E2E, Migration | 7+ scenarios |
| `delete_time_entry` | E2E, Manual | 3+ scenarios |
| `get_available_projects` | E2E, Manual, Auto-tracking | 2+ scenarios |
| `submit_time_entry` | E2E, Manual | 4+ scenarios |

### Workflows

| Workflow | Test Type | Duration |
|----------|-----------|----------|
| Manual Time Logging | Manual execution | 45-60 min |
| Auto-tracking | Manual execution | 45-60 min |
| Project Migration | Manual execution | 45-60 min |
| Approval Workflow | E2E scenario | 15 min |

### Features

| Feature | Test Coverage |
|---------|---------------|
| Create time entry | ✅ Comprehensive |
| Query entries | ✅ Comprehensive |
| Update entry | ✅ Comprehensive |
| Delete entry | ✅ Comprehensive |
| Submit workflow | ✅ Comprehensive |
| Auto-tracking | ✅ Comprehensive |
| Project migration | ✅ Comprehensive |
| Tag validation | ✅ Comprehensive |
| Status transitions | ✅ Comprehensive |
| Error handling | ✅ Comprehensive |

---

## Success Criteria

### Functional Requirements

- [ ] All 7 MCP tools accessible from Claude Code
- [ ] Natural language commands correctly interpreted
- [ ] GraphQL API calls successful
- [ ] Database state matches expected after each operation
- [ ] Error messages are clear and actionable
- [ ] Authentication works correctly

### Integration Points

- [ ] Claude Code ↔ MCP Server communication working
- [ ] MCP Server ↔ GraphQL API communication working
- [ ] GraphQL API ↔ PostgreSQL communication working
- [ ] Bearer token authentication successful
- [ ] Session context persistence working

### User Experience

- [ ] Commands are intuitive and natural
- [ ] Responses are clear and helpful
- [ ] Errors guide user to resolution
- [ ] Auto-tracking suggestions are timely and accurate
- [ ] Migration workflow is straightforward

### Documentation Quality

- [ ] Setup instructions are clear and complete
- [ ] Test scenarios are reproducible
- [ ] Verification steps are detailed
- [ ] Troubleshooting covers common issues
- [ ] Examples are realistic and helpful

---

## Common Issues & Solutions

### Issue 1: MCP Server Not Appearing in Claude Code

**Symptoms:**
- Claude Code doesn't show time-reporting tools
- MCP server doesn't start

**Solutions:**
1. Verify .NET 10 SDK installed: `dotnet --version`
2. Verify MCP Server builds: `cd TimeReportingMcp && dotnet build`
3. Check Claude Code config file path is correct
4. Restart Claude Code after config changes
5. Check Claude Code logs for errors

### Issue 2: Authentication Failures

**Symptoms:**
- "401 Unauthorized" errors
- Tools fail with authentication errors

**Solutions:**
1. Verify `Azure AD via AzureCliCredential` matches in both API and Claude Code config
2. Check API is running: `curl http://localhost:5001/health`
3. Restart API after token changes: `/deploy`
4. Check API logs: `docker-compose logs graphql-api`

### Issue 3: Database Connection Issues

**Symptoms:**
- Tools fail with database errors
- Cannot query entries

**Solutions:**
1. Verify database is running: `/db-start`
2. Check database health: `/db-psql` then `SELECT 1;`
3. Verify connection string in `.env`
4. Check database logs: `/db-logs`

### Issue 4: Auto-tracking Not Working

**Symptoms:**
- No suggestions appear
- Session context not persisting

**Solutions:**
1. Verify Phase 10 implementation complete
2. Check session context file exists: `~/.config/time-reporting/session-context.json`
3. Run auto-tracking tests: `./tests/auto-tracking/verify-auto-tracking.sh`
4. Check detection threshold settings

---

## Performance Benchmarks

### Expected Response Times

| Operation | Expected Time | Measurement |
|-----------|---------------|-------------|
| Log time (create) | < 500ms | MCP tool call to response |
| Query entries | < 1s | Including database query |
| Update entry | < 500ms | MCP tool call to response |
| Move task | < 1s | Including revalidation |
| Delete entry | < 500ms | MCP tool call to response |
| Get projects | < 500ms | Including tasks/tags |
| Submit entry | < 500ms | MCP tool call to response |

### Resource Usage

| Component | CPU | Memory | Notes |
|-----------|-----|--------|-------|
| MCP Server | < 5% | < 50MB | Idle state |
| GraphQL API | < 10% | < 200MB | Normal load |
| PostgreSQL | < 5% | < 100MB | Small dataset |

---

## Next Steps

After completing Phase 11:

1. **Review Results**
   - Document any issues discovered
   - Create issue reports for bugs
   - Note UX improvements for future

2. **Proceed to Phase 12**
   - API Documentation (Task 12.1)
   - MCP Setup Guide (Task 12.2)
   - User Guide (Task 12.3)
   - Architecture Diagram (Task 12.4)
   - Deployment Guide (Task 12.5)

3. **Optional Enhancements**
   - Automate E2E test execution
   - Add performance monitoring
   - Create CI/CD pipeline
   - Add metrics collection

---

## Resources

### Internal Documentation
- [Phase 10: Auto-tracking](../phase-10-auto-tracking/) - Auto-tracking implementation
- [PRD: MCP Tools](../../prd/mcp-tools.md) - Tool specifications
- [PRD: API Specification](../../prd/api-specification.md) - GraphQL schema
- [Integration Tests](../../../tests/integration/) - Existing integration tests

### External Resources
- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [Claude Code Documentation](https://docs.claude.com/claude-code)
- [HotChocolate Docs](https://chillicream.com/docs/hotchocolate)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

---

**Ready to begin? Start with [Task 11.1: Claude Code Configuration](./task-11.1-claude-code-configuration.md)!**
