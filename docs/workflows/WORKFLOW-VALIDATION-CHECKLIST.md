# Manual Workflow Validation Checklist

## Pre-Flight Checks

- [ ] GraphQL API running and healthy
- [ ] Claude Code configured with MCP server
- [ ] Database seeded with test projects
- [ ] Bearer token configured correctly
- [ ] MCP connection verified

## Workflow Execution

### Create Operations
- [ ] Create basic time entry (4 hours, INTERNAL, Development)
- [ ] Create entry with overtime (6.5 + 1.5 hours)
- [ ] Create entry with date range (Monday to Friday)
- [ ] Create entry with tags

### Query Operations
- [ ] Query today's entries
- [ ] Query this week's entries
- [ ] Query by project (INTERNAL)
- [ ] Query by status (NOT_REPORTED)
- [ ] Query single entry by ID

### Update Operations
- [ ] Update hours (4h → 4.5h)
- [ ] Update task within same project
- [ ] Update date range
- [ ] Update tags
- [ ] Attempt update on SUBMITTED entry (should fail)

### Move Operations
- [ ] Move entry to different project (CLIENT-A → INTERNAL)
- [ ] Move and change task
- [ ] Move with tag revalidation

### Delete Operations
- [ ] Delete NOT_REPORTED entry
- [ ] Attempt delete SUBMITTED entry (should fail)

### Submit Operations
- [ ] Submit NOT_REPORTED entry
- [ ] Verify status changed to SUBMITTED
- [ ] Attempt submit already-SUBMITTED entry (should be idempotent)

### Error Handling
- [ ] Invalid project code error
- [ ] Invalid task error
- [ ] Invalid tags error
- [ ] Update submitted entry error
- [ ] Delete submitted entry error

## Database Verification

### After Each Operation
- [ ] Entry exists in database
- [ ] Fields match expected values
- [ ] Status is correct
- [ ] Timestamps updated (created_at, updated_at)

### Final State
- [ ] All entries accounted for
- [ ] No orphaned entries
- [ ] Status workflow followed correctly

## Natural Language Variations

- [ ] "Log X hours on Y" works
- [ ] "Track X hours, Y project" works
- [ ] "Create time entry: X, Y, Z" works
- [ ] "Show my entries for today" works
- [ ] "What did I log this week?" works
- [ ] "Update entry <id> to X hours" works
- [ ] "Move entry <id> to project Y" works

## Cleanup

- [ ] Test entries cleaned up
- [ ] Database in known state
- [ ] No test data pollution

---

**Pass Criteria:** All checkboxes ticked ✅

**Time to Complete:** ~45-60 minutes

**Next:** Run auto-tracking workflow test
