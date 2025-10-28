# User Guide: Time Reporting with Claude Code

**Natural Language Time Tracking for Developers**

Version: 1.0

---

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Basic Operations](#basic-operations)
4. [Advanced Workflows](#advanced-workflows)
5. [Best Practices](#best-practices)
6. [Troubleshooting](#troubleshooting)
7. [Reference](#reference)

---

## Introduction

The Time Reporting System integrates seamlessly with Claude Code, allowing you to track time using natural language commands. No need to remember complex syntax or navigate through UI forms - just describe what you did, and Claude Code will log it for you.

### What You Can Do

- **Log time entries** for work completed on projects
- **Query and search** your time entries with flexible filters
- **Update and modify** entries before submission
- **Move work** between projects
- **Submit time** for approval workflow
- **Get suggestions** for tracking ongoing work (auto-tracking)

### Prerequisites

Before using the system, ensure:

- Claude Code is installed and configured (see [Setup Guide](./integration/CLAUDE-CODE-SETUP.md))
- GraphQL API and database are running
- You know your available project codes

---

## Getting Started

### Your First Time Entry

The simplest way to log time:

```
User: "Log 8 hours of development work on INTERNAL project for today"
```

Claude Code will:
1. Understand you want to create a time entry
2. Use the INTERNAL project
3. Select "Development" task (common default)
4. Log 8 hours for today's date
5. Confirm the entry was created

**Response Example:**
```
I've logged your time entry:
- Project: INTERNAL - Internal Development
- Task: Development
- Hours: 8.0 (standard)
- Date: 2025-10-29
- Status: NOT_REPORTED
- Entry ID: a1b2c3d4-...

The entry has been created and is ready for submission when you're ready.
```

### Check Your Entries

View your recent time entries:

```
User: "Show me my time entries for this week"
```

**Response Example:**
```
Found 5 time entries for this week:

1. Monday, Oct 23 - INTERNAL / Development
   8.0 hours | Status: SUBMITTED

2. Tuesday, Oct 24 - CLIENT-A / Bug Fixing
   6.5 hours | Status: NOT_REPORTED

3. Wednesday, Oct 25 - INTERNAL / Code Review
   7.0 hours | Status: NOT_REPORTED

4. Thursday, Oct 26 - CLIENT-B / Feature Development
   8.5 hours | Status: NOT_REPORTED

5. Friday, Oct 27 - INTERNAL / Meetings
   2.0 hours | Status: NOT_REPORTED
```

### View Available Projects

See what projects you can log time to:

```
User: "What projects are available?"
```

**Response Example:**
```
Available Projects:

1. INTERNAL - Internal Development
   Tasks: Development, Bug Fixing, Code Review, Documentation, Meetings
   Tags: Type (Backend, Frontend, DevOps) | Priority (High, Medium, Low)

2. CLIENT-A - Client A Project
   Tasks: Feature Development, Bug Fixing, Testing, Deployment
   Tags: Sprint (Sprint 1, Sprint 2, Sprint 3) | Component (API, UI, Database)

3. CLIENT-B - Client B Project
   Tasks: Development, Bug Fixing, Code Review, Testing
   Tags: Environment (Dev, Staging, Production)
```

---

## Basic Operations

### Creating Time Entries

#### Simple Entry (Today)

```
"Log 8 hours of development on INTERNAL for today"
```

#### Specific Date

```
"Log 6 hours of bug fixing on CLIENT-A for yesterday"
```

or

```
"Log 7.5 hours of testing on CLIENT-B for October 25th"
```

#### With Date Range

```
"Log 16 hours of feature development on CLIENT-A from Monday to Tuesday"
```

#### With Description

```
"Log 8 hours of development on INTERNAL for today. Description: Implemented user authentication and OAuth integration"
```

#### With Overtime

```
"Log 8 hours standard and 2 hours overtime for development on INTERNAL today"
```

#### With Tags

```
"Log 8 hours of development on INTERNAL for today with tags: Type=Backend, Priority=High"
```

#### With Issue/Ticket ID

```
"Log 6 hours on JIRA-1234 for CLIENT-A feature development today"
```

---

### Querying Time Entries

#### All Entries

```
"Show me all my time entries"
```

#### By Date Range

```
"Show time entries for this week"
"Show time entries for October 2025"
"Show time entries between Oct 1 and Oct 15"
```

#### By Project

```
"Show all INTERNAL time entries"
"Show time entries for CLIENT-A"
```

#### By Status

```
"Show all not reported entries"
"Show submitted entries"
"Show approved time entries"
```

#### Combined Filters

```
"Show CLIENT-A entries from this month that are not reported"
"Show all submitted entries from last week"
```

#### Get Specific Entry

```
"Show details for time entry a1b2c3d4-..."
```

---

### Updating Time Entries

#### Update Hours

```
"Update entry a1b2c3d4 to 9.5 hours"
```

#### Update Description

```
"Update entry a1b2c3d4 description: Added unit tests and documentation"
```

#### Update Task

```
"Change entry a1b2c3d4 task to Code Review"
```

#### Update Multiple Fields

```
"Update entry a1b2c3d4: change hours to 8.5 and description to 'Completed API integration'"
```

#### Update Tags

```
"Update tags for entry a1b2c3d4: Type=Frontend, Priority=Medium"
```

**Note:** Only entries with status `NOT_REPORTED` or `DECLINED` can be updated.

---

### Moving Between Projects

Move a time entry to a different project or task:

```
"Move entry a1b2c3d4 from INTERNAL to CLIENT-A Feature Development"
```

or

```
"Change entry a1b2c3d4 to CLIENT-B Development task"
```

**Important:** When moving to a different project, all tags are cleared (tags are project-specific).

---

### Deleting Time Entries

Delete an entry you no longer need:

```
"Delete time entry a1b2c3d4"
```

or

```
"Remove yesterday's entry for INTERNAL"
```

**Note:** Only entries with status `NOT_REPORTED` or `DECLINED` can be deleted.

---

### Submitting for Approval

Submit one or more entries for approval:

#### Single Entry

```
"Submit time entry a1b2c3d4 for approval"
```

#### Multiple Entries

```
"Submit all my not reported entries for this week"
```

**Status Workflow:**
- `NOT_REPORTED` → `SUBMITTED` (via submit)
- `SUBMITTED` → `APPROVED` (by manager/admin)
- `SUBMITTED` → `DECLINED` (by manager/admin with comment)
- `DECLINED` → `SUBMITTED` (can resubmit after editing)
- `APPROVED` → (immutable, cannot edit/delete)

---

## Advanced Workflows

### Workflow 1: Daily Time Logging

**Scenario:** Log your work at the end of each day

1. **Review what you worked on:**
   ```
   "What did I work on today?"
   ```

2. **Log time for each task:**
   ```
   "Log 5 hours of development on CLIENT-A for today"
   "Log 2 hours of code review on INTERNAL for today"
   "Log 1 hour of meetings on INTERNAL for today"
   ```

3. **Review your entries:**
   ```
   "Show my entries for today"
   ```

4. **Submit at end of week:**
   ```
   "Submit all my not reported entries from this week"
   ```

---

### Workflow 2: Correcting Mistakes

**Scenario:** You logged time to the wrong project

1. **Find the entry:**
   ```
   "Show my entries for yesterday"
   ```

2. **Option A: Move to correct project**
   ```
   "Move entry a1b2c3d4 to CLIENT-B Development"
   ```

3. **Option B: Delete and recreate**
   ```
   "Delete entry a1b2c3d4"
   "Log 8 hours of development on CLIENT-B for yesterday"
   ```

---

### Workflow 3: Handling Declined Entries

**Scenario:** Manager declined your time entry with feedback

1. **Check declined entries:**
   ```
   "Show my declined time entries"
   ```

2. **View decline reason:**
   ```
   "Show details for entry a1b2c3d4"
   ```
   (Decline comment will be displayed)

3. **Make corrections:**
   ```
   "Update entry a1b2c3d4 description: [corrected description based on feedback]"
   ```

4. **Resubmit:**
   ```
   "Submit entry a1b2c3d4 for approval"
   ```

---

### Workflow 4: Working Across Projects

**Scenario:** You split time between multiple projects in one day

1. **Log each project separately:**
   ```
   "Log 4 hours of development on CLIENT-A for today"
   "Log 3 hours of bug fixing on INTERNAL for today"
   "Log 1 hour of meetings on CLIENT-B for today"
   ```

2. **Review total hours:**
   ```
   "Show my entries for today and total hours"
   ```

3. **Adjust if needed:**
   ```
   "Update CLIENT-A entry to 5 hours"
   "Update INTERNAL entry to 2.5 hours"
   ```

---

### Workflow 5: End of Week Submission

**Scenario:** Review and submit all time at end of week

1. **Review all entries:**
   ```
   "Show all my not reported entries"
   ```

2. **Check for missing days:**
   ```
   "Show my entries for this week by day"
   ```

3. **Fill in missing entries:**
   ```
   "Log 8 hours on INTERNAL Development for Wednesday"
   ```

4. **Submit all:**
   ```
   "Submit all my not reported entries for approval"
   ```

5. **Confirm submission:**
   ```
   "Show my submitted entries"
   ```

---

## Best Practices

### Time Entry Best Practices

1. **Log Time Daily**
   - Log time at the end of each day while work is fresh in your mind
   - Don't wait until end of week (you'll forget details)

2. **Be Specific with Descriptions**
   - Good: "Implemented OAuth2 authentication for user login"
   - Bad: "Coding"

3. **Use Appropriate Tasks**
   - Match the task to the actual work performed
   - If unsure, check available tasks: "Show tasks for PROJECT-X"

4. **Use Tags Effectively**
   - Add relevant metadata to help with reporting
   - Check required tags: "Show tag configurations for PROJECT-X"

5. **Review Before Submitting**
   - Double-check hours, projects, and dates
   - Ensure descriptions are meaningful

### Natural Language Tips

1. **Be Clear and Specific**
   - Good: "Log 8 hours of development on INTERNAL for October 25th"
   - Bad: "Log some time"

2. **Use Common Date References**
   - "today", "yesterday", "this week", "last Friday"
   - Specific dates: "October 25th" or "Oct 25"

3. **Project Codes vs Names**
   - Use project codes for accuracy: "CLIENT-A"
   - Claude Code can also understand project names: "Client A Project"

4. **Combine Multiple Operations**
   - "Log 8 hours on INTERNAL Development for today with Type=Backend"
   - Claude Code understands complex requests

5. **Ask for Help**
   - "What projects can I log time to?"
   - "Show me an example of logging time with tags"
   - "What's the status workflow?"

### Approval Workflow Best Practices

1. **Submit Regularly**
   - Submit weekly or bi-weekly (don't accumulate months of entries)
   - Makes corrections easier if declined

2. **Check for Declined Entries**
   - Review decline comments carefully
   - Address feedback before resubmitting

3. **Don't Edit Submitted Entries**
   - Wait for approval or decline
   - If urgent correction needed, ask manager to decline

4. **Approved Entries Are Final**
   - Cannot edit, delete, or move approved entries
   - Ensure accuracy before submitting

---

## Troubleshooting

### Problem: "Project not found" error

**Cause:** Project code doesn't exist or is inactive

**Solution:**
```
"Show available projects"
```
Check the exact project code and ensure it's active.

---

### Problem: "Task not valid for project" error

**Cause:** Task doesn't exist in project's available tasks

**Solution:**
```
"Show available tasks for PROJECT-X"
```
Use one of the listed tasks.

---

### Problem: "Tag not valid" error

**Cause:** Tag name or value doesn't match project configuration

**Solution:**
```
"Show tag configurations for PROJECT-X"
```
Check required tags and allowed values.

---

### Problem: Cannot update entry

**Cause:** Entry status is SUBMITTED or APPROVED

**Solution:**
- SUBMITTED: Wait for approval/decline, or ask manager to decline
- APPROVED: Cannot modify (immutable)
- DECLINED: You can update and resubmit

---

### Problem: Cannot delete entry

**Cause:** Entry status is SUBMITTED or APPROVED

**Solution:**
- Only NOT_REPORTED and DECLINED entries can be deleted
- If submitted, ask manager to decline first

---

### Problem: "Start date must be before completion date"

**Cause:** Date range is invalid

**Solution:**
Correct the dates:
```
"Update entry a1b2c3d4 dates: start Oct 23, end Oct 24"
```

---

### Problem: Lost entry ID

**Cause:** Forgot or lost the entry ID

**Solution:**
```
"Show my recent entries"
```
Find the entry by date, project, or hours, then use that ID.

---

## Reference

### Status Values

| Status | Description | Can Edit? | Can Delete? | Can Submit? |
|--------|-------------|-----------|-------------|-------------|
| `NOT_REPORTED` | Created, not submitted yet | Yes | Yes | Yes |
| `SUBMITTED` | Submitted for approval | No | No | No |
| `APPROVED` | Approved by manager | No | No | No |
| `DECLINED` | Declined by manager | Yes | Yes | Yes (resubmit) |

### Common Natural Language Patterns

#### Time Amounts
- "8 hours"
- "7.5 hours"
- "8 hours standard and 2 hours overtime"

#### Dates
- "today", "yesterday", "tomorrow"
- "Monday", "last Friday", "this Tuesday"
- "October 25th", "Oct 25", "10/25"
- "this week", "last week", "this month"

#### Projects
- "INTERNAL" (code)
- "Internal Development" (name)
- "CLIENT-A" (code)

#### Tasks
- "Development"
- "Bug Fixing"
- "Code Review"
- "Testing"
- "Deployment"

#### Filters
- "not reported", "submitted", "approved", "declined"
- "from Oct 1 to Oct 15"
- "between Monday and Friday"

### Quick Reference Commands

| What You Want | Example Command |
|---------------|-----------------|
| Log time | "Log 8 hours of development on INTERNAL for today" |
| View entries | "Show my time entries for this week" |
| Update hours | "Update entry ID to 9.5 hours" |
| Move project | "Move entry ID to CLIENT-A Development" |
| Delete entry | "Delete time entry ID" |
| Submit entry | "Submit entry ID for approval" |
| Check projects | "Show available projects" |
| Check tasks | "Show tasks for PROJECT-X" |
| Check tags | "Show tag configurations for PROJECT-X" |
| Get entry details | "Show details for entry ID" |

---

## Additional Resources

- **[Setup Guide](./integration/CLAUDE-CODE-SETUP.md)** - Configure Claude Code
- **[API Documentation](./API.md)** - GraphQL API reference
- **[MCP Tools](./prd/mcp-tools.md)** - Technical tool specifications
- **[Deployment Guide](./DEPLOYMENT.md)** - Deploy the system
- **[Workflows Documentation](./workflows/)** - Detailed workflow guides

---

## Tips for Maximum Productivity

1. **Create Shortcuts in Your Mind**
   - "log today" → "Log 8 hours of development on INTERNAL for today"
   - "show week" → "Show my entries for this week"

2. **Use Auto-Tracking** (if enabled)
   - Claude Code can suggest time entries based on your activity
   - Confirm or adjust suggestions as needed

3. **Batch Operations**
   - Review and submit all entries at once at end of week
   - Update multiple fields in one command

4. **Leverage Context**
   - Claude Code remembers recent projects and tasks
   - "Log another 4 hours" (if context is clear)

5. **Ask for Summaries**
   - "Summarize my time for this month"
   - "How many hours did I log on CLIENT-A this week?"

---

**Happy Time Tracking!**

For questions or issues, refer to the [Troubleshooting](#troubleshooting) section or check the [Setup Guide](./integration/CLAUDE-CODE-SETUP.md).

---

**Last Updated:** 2025-10-29
