# Auto-Tracking Workflow Test Guide

## Overview

This guide tests the intelligent auto-tracking feature that suggests time entries based on detected work sessions.

**Feature Capabilities:**
- ✅ Detects active coding sessions
- ✅ Suggests time entries based on duration
- ✅ Remembers last project/task
- ✅ Prompts for confirmation
- ✅ Persists context across sessions

---

## Prerequisites

- [ ] Phase 10 (Auto-tracking) complete
- [ ] GraphQL API running
- [ ] Claude Code configured with MCP server
- [ ] Session context file path: `~/.config/time-reporting/session-context.json`

**Verify auto-tracking implementation:**
```bash
./tests/auto-tracking/verify-auto-tracking.sh
```

---

## Test Scenarios

### Scenario 1: First Session - No Prior Context

**Goal:** Test auto-tracking suggestion when no prior session context exists.

**Setup:**
1. Clear session context:
   ```bash
   rm -f ~/.config/time-reporting/session-context.json
   ```

2. Start coding session (simulate 2 hours of work)

**Expected Behavior:**

After ~30 minutes of activity, Claude Code should proactively suggest:

```
💡 Time Tracking Suggestion

I noticed you've been working for about 2.0 hours.
Would you like to log this time?

Suggested entry:
- Project: (needs selection)
- Task: (needs selection)
- Hours: 2.0
- Date: Today (2025-10-29)

Available projects:
- INTERNAL: Internal Development
- CLIENT-A: Client A Project

Would you like to log this time? If yes, which project and task?
```

**User Response:**
```
"Yes, log it to INTERNAL, Development"
```

**Expected Result:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: INTERNAL
Task: Development
Hours: 2.0
Date: 2025-10-29
Status: NOT_REPORTED

Session context saved:
- Last Project: INTERNAL
- Last Task: Development
- Last Entry Time: 14:30
```

**Verification:**
```bash
# Check session context was saved
cat ~/.config/time-reporting/session-context.json
```

**Expected:**
```json
{
  "lastProject": "INTERNAL",
  "lastTask": "Development",
  "lastEntryTime": "2025-10-29T14:30:00Z",
  "totalHoursToday": 2.0
}
```

---

### Scenario 2: Continuing Session - Context Exists

**Goal:** Test that auto-tracking uses saved context for quick suggestions.

**Setup:**
1. Session context exists from Scenario 1
2. Continue working for another 1.5 hours

**Expected Behavior:**

After ~30 minutes of additional activity:

```
💡 Time Tracking Suggestion

I noticed you've been working for about 1.5 hours since your last entry.

Suggested entry (based on previous session):
- Project: INTERNAL (same as last time)
- Task: Development (same as last time)
- Hours: 1.5
- Date: Today (2025-10-29)

Log this time to INTERNAL, Development?
```

**User Response Options:**

**Option A: Accept**
```
"Yes"
```

**Expected Result:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: INTERNAL
Task: Development
Hours: 1.5
Date: 2025-10-29

Total hours today: 3.5
```

**Option B: Reject**
```
"No, I was actually working on CLIENT-A bug fixing"
```

**Expected Result:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Project: CLIENT-A
Task: Bug Fixing
Hours: 1.5
Date: 2025-10-29

Session context updated:
- Last Project: CLIENT-A
- Last Task: Bug Fixing
```

---

### Scenario 3: Cross-Project Work - Context Switch

**Goal:** Test auto-tracking when switching between projects during the day.

**Setup:**
1. Morning: 3 hours on INTERNAL, Development
2. Afternoon: 2 hours on CLIENT-A, Bug Fixing
3. Evening: 1.5 hours on INTERNAL, Code Review

**Expected Behavior:**

**Morning (after 3 hours):**
```
💡 Suggestion: Log 3.0 hours to INTERNAL, Development?
```

**Accept → Entry created, context saved (INTERNAL, Development)**

**Afternoon (after 2 hours):**
```
💡 Suggestion: Log 2.0 hours to INTERNAL, Development? (based on last session)
```

**User corrects:**
```
"No, I was on CLIENT-A, Bug Fixing"
```

**Entry created, context updated (CLIENT-A, Bug Fixing)**

**Evening (after 1.5 hours):**
```
💡 Suggestion: Log 1.5 hours to CLIENT-A, Bug Fixing?
```

**User corrects:**
```
"No, INTERNAL, Code Review"
```

**Entry created, context updated (INTERNAL, Code Review)**

**Verification:**
```bash
podman exec time-reporting-db psql -U postgres -d time_reporting -c "
SELECT te.project_code, pt.task_name, te.standard_hours
FROM time_entries te
JOIN project_tasks pt ON te.project_task_id = pt.id
WHERE te.start_date = CURRENT_DATE
ORDER BY te.created_at;"
```

**Expected:**
```
project_code | task_name   | standard_hours
-------------|-------------|----------------
INTERNAL     | Development | 3.00
CLIENT-A     | Bug Fixing  | 2.00
INTERNAL     | Code Review | 1.50
```

---

### Scenario 4: Threshold Testing - Short Sessions

**Goal:** Verify auto-tracking doesn't suggest entries for very short work sessions.

**Setup:**
1. Work for 15 minutes
2. Take a break
3. Work for another 10 minutes

**Expected Behavior:**

No suggestion should be made (threshold not reached, typically 30+ minutes).

**Verification:**
- Claude Code should NOT prompt for time logging
- No entries created automatically
- User can still manually log time if desired

**Manual Override:**
```
"Log 25 minutes (0.42 hours) of meetings on INTERNAL for now"
```

**Expected:**
```
✅ Time entry created successfully!

Entry ID: <uuid>
Hours: 0.42
```

---

### Scenario 5: Multi-Day Context Persistence

**Goal:** Test that session context persists across different days.

**Setup:**
1. **Day 1 (Monday):** Work 8 hours on INTERNAL, Development
2. **Day 2 (Tuesday):** Continue on same project/task

**Expected Behavior:**

**Day 1 - End of day:**
```
💡 Suggestion: Log 8.0 hours to INTERNAL, Development?
```

**User accepts → Context saved**

**Day 2 - After 2 hours:**
```
💡 Time Tracking Suggestion

I noticed you've been working for about 2.0 hours.

You were last working on INTERNAL, Development (yesterday).
Continue on the same project/task?

Suggested entry:
- Project: INTERNAL
- Task: Development
- Hours: 2.0
- Date: Today (2025-10-30)
```

**User Response:**
```
"Yes, same project and task"
```

**Expected Result:**
Entry created with yesterday's context applied to today.

---

### Scenario 6: Error Handling - Invalid Context

**Goal:** Test behavior when saved context references invalid project/task.

**Setup:**
1. Manually edit session context to reference non-existent project:
   ```json
   {
     "lastProject": "DELETED-PROJECT",
     "lastTask": "Invalid Task"
   }
   ```

2. Work for 2 hours

**Expected Behavior:**

Auto-tracking should detect invalid context and prompt for new selection:

```
💡 Time Tracking Suggestion

I noticed you've been working for about 2.0 hours.

⚠️  Note: Your last project (DELETED-PROJECT) is no longer available.

Please select a project:
- INTERNAL: Internal Development
- CLIENT-A: Client A Project

Which project were you working on?
```

**User Response:**
```
"INTERNAL, Development"
```

**Expected Result:**
- Entry created
- Context updated with valid project/task
- Invalid context cleared

---

## Testing Checklist

### Detection Heuristics
- [ ] 30-minute threshold triggers suggestion
- [ ] Short sessions (<30 min) don't trigger
- [ ] Long sessions (>4 hours) trigger with warning
- [ ] Idle time detection works (no suggestions during breaks)

### Session Context
- [ ] First session: prompts for project/task selection
- [ ] Subsequent sessions: suggests last project/task
- [ ] Context persists across app restarts
- [ ] Context updates after each entry
- [ ] Invalid context handled gracefully

### Suggestion Formatting
- [ ] Prompts are user-friendly and clear
- [ ] Hours formatted correctly (e.g., 2.5 hours, not 2.50000)
- [ ] Project/task suggestions are relevant
- [ ] Available options listed when needed
- [ ] Date defaults to today

### User Interaction
- [ ] User can accept suggestion
- [ ] User can reject and specify different project/task
- [ ] User can ignore suggestion (dismiss)
- [ ] User can manually override suggested hours
- [ ] User can add tags during confirmation

### Persistence
- [ ] Context file created on first suggestion
- [ ] Context file updated after each entry
- [ ] Context loads on MCP server restart
- [ ] Context survives Claude Code restart
- [ ] Context clears when explicitly requested

### Edge Cases
- [ ] No activity detected: no suggestions
- [ ] Rapid project switching: context updates correctly
- [ ] Overtime hours: suggestion includes breakdown
- [ ] Weekend work: suggestions still work
- [ ] Context file corruption: handled gracefully (reset)

---

## Verification Scripts

### Check Session Context

Create `tests/auto-tracking/check-session-context.sh`:

```bash
#!/bin/bash

CONTEXT_FILE="$HOME/.config/time-reporting/session-context.json"

if [[ ! -f "$CONTEXT_FILE" ]]; then
    echo "❌ Session context file not found"
    echo "   Expected: $CONTEXT_FILE"
    exit 1
fi

echo "✅ Session context file exists"
echo
echo "Current context:"
cat "$CONTEXT_FILE" | jq .
```

### Verify Auto-Tracking Feature

Create `tests/auto-tracking/verify-auto-tracking.sh`:

```bash
#!/bin/bash

set -e

echo "=== Auto-Tracking Verification ==="
echo

# Step 1: Check SessionContext class exists
echo "1. Checking SessionContext implementation..."
if grep -q "class SessionContext" TimeReportingMcp/AutoTracking/SessionContext.cs 2>/dev/null; then
    echo "✅ SessionContext class found"
else
    echo "❌ SessionContext class not found"
    exit 1
fi
echo

# Step 2: Check DetectionHeuristics exists
echo "2. Checking DetectionHeuristics implementation..."
if grep -q "class DetectionHeuristics" TimeReportingMcp/AutoTracking/DetectionHeuristics.cs 2>/dev/null; then
    echo "✅ DetectionHeuristics class found"
else
    echo "❌ DetectionHeuristics class not found"
    exit 1
fi
echo

# Step 3: Check ContextPersistence exists
echo "3. Checking ContextPersistence implementation..."
if grep -q "class ContextPersistence" TimeReportingMcp/AutoTracking/ContextPersistence.cs 2>/dev/null; then
    echo "✅ ContextPersistence class found"
else
    echo "❌ ContextPersistence class not found"
    exit 1
fi
echo

# Step 4: Check tests pass
echo "4. Running auto-tracking tests..."
cd TimeReportingMcp.Tests
if dotnet test --filter "FullyQualifiedName~AutoTracking" --verbosity quiet > /dev/null 2>&1; then
    echo "✅ All auto-tracking tests pass"
else
    echo "❌ Some auto-tracking tests failed"
    exit 1
fi
cd ..
echo

echo "=== Verification Complete ==="
```

---

## Definition of Done

- [ ] All 6 test scenarios documented
- [ ] Testing checklist complete with all categories
- [ ] Verification scripts created and tested
- [ ] At least 3 scenarios executed manually
- [ ] Session context persistence verified
- [ ] Detection heuristics tested with different thresholds
- [ ] User interaction patterns validated
- [ ] Edge cases handled correctly

---

## Next Steps

After completing this task:
1. Proceed to **Task 11.5: Migration Workflow Test** to test project migration scenarios
2. Document any UX improvements discovered during testing
3. Consider creating automated tests for auto-tracking logic (future enhancement)

---

## Resources

- [Phase 10 Tasks](../../docs/tasks/phase-10-auto-tracking/) - Auto-tracking implementation
- [Session Context Implementation](../../docs/tasks/phase-10-auto-tracking/task-10.1-session-context-manager.md)
- [Detection Heuristics](../../docs/tasks/phase-10-auto-tracking/task-10.2-detection-heuristics.md)
- [Confirmation Prompts](../../docs/tasks/phase-10-auto-tracking/task-10.3-confirmation-prompts.md)
- [Context Persistence](../../docs/tasks/phase-10-auto-tracking/task-10.4-context-persistence.md)
