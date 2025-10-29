# Task 13.1: Create GraphQL Operation Files

**Phase:** 13 - StrawberryShake Migration
**Estimated Time:** 1 hour
**Dependencies:** Phase 7 (MCP Server Setup), Phase 8-9 (MCP Tools)
**Status:** Pending

---

## Objective

Create `.graphql` operation files for all 7 MCP tools to replace hardcoded string queries with strongly-typed StrawberryShake generated code.

---

## Background

Currently, the MCP server uses hardcoded GraphQL query strings with manual type definitions. StrawberryShake is already configured and working (proof-of-concept with `LogTime.graphql` and `GetProjects.graphql`). This task completes the migration by creating operation files for the remaining 5 tools.

---

## Acceptance Criteria

1. ✅ All 7 MCP tools have corresponding `.graphql` operation files
2. ✅ Each operation requests only the fields actually used by the tool
3. ✅ Operation names follow StrawberryShake conventions (PascalCase)
4. ✅ All operations include proper input types and variables
5. ✅ Build succeeds with all operations generating typed code
6. ✅ No duplicate operation definitions

---

## Implementation Steps

### Step 1: Create Remaining Query Operations

**File: `TimeReportingMcp/GraphQL/QueryTimeEntries.graphql`**

```graphql
query QueryTimeEntries(
  $projectCode: String
  $startDate: Date
  $endDate: Date
  $status: TimeEntryStatus
) {
  timeEntries(
    where: {
      and: [
        { project: { code: { eq: $projectCode } } }
        { startDate: { gte: $startDate } }
        { completionDate: { lte: $endDate } }
        { status: { eq: $status } }
      ]
    }
  ) {
    nodes {
      id
      project {
        code
        name
      }
      projectTask {
        taskName
      }
      standardHours
      overtimeHours
      startDate
      completionDate
      status
      description
      issueId
      createdAt
      updatedAt
    }
    totalCount
  }
}
```

### Step 2: Create Remaining Mutation Operations

**File: `TimeReportingMcp/GraphQL/UpdateTimeEntry.graphql`**

```graphql
mutation UpdateTimeEntry($id: UUID!, $input: UpdateTimeEntryInput!) {
  updateTimeEntry(id: $id, input: $input) {
    id
    project {
      code
      name
    }
    projectTask {
      taskName
    }
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    description
    issueId
    updatedAt
  }
}
```

**File: `TimeReportingMcp/GraphQL/DeleteTimeEntry.graphql`**

```graphql
mutation DeleteTimeEntry($id: UUID!) {
  deleteTimeEntry(id: $id)
}
```

**File: `TimeReportingMcp/GraphQL/MoveTaskToProject.graphql`**

```graphql
mutation MoveTaskToProject(
  $entryId: UUID!
  $newProjectCode: String!
  $newTask: String!
) {
  moveTaskToProject(
    entryId: $entryId
    newProjectCode: $newProjectCode
    newTask: $newTask
  ) {
    id
    project {
      code
      name
    }
    projectTask {
      taskName
    }
    standardHours
    overtimeHours
    startDate
    completionDate
    status
    updatedAt
  }
}
```

**File: `TimeReportingMcp/GraphQL/SubmitTimeEntry.graphql`**

```graphql
mutation SubmitTimeEntry($id: UUID!) {
  submitTimeEntry(id: $id) {
    id
    status
    updatedAt
  }
}
```

### Step 3: Verify Existing Operations

Ensure these already exist from the proof-of-concept:
- ✅ `TimeReportingMcp/GraphQL/GetProjects.graphql` (already created)
- ✅ `TimeReportingMcp/GraphQL/LogTime.graphql` (already created)

### Step 4: Build and Verify Code Generation

```bash
/build-mcp
```

**Expected Output:**
- Generate C# Clients started
- Generate TimeReportingClient completed
- Build succeeded

**Verify Generated Interfaces:**
```bash
grep "interface I.*Query\|interface I.*Mutation" TimeReportingMcp/obj/Debug/net10.0/berry/*.cs
```

Should show:
- `IGetAvailableProjectsQuery`
- `ILogTimeMutation`
- `IQueryTimeEntriesQuery`
- `IUpdateTimeEntryMutation`
- `IDeleteTimeEntryMutation`
- `IMoveTaskToProjectMutation`
- `ISubmitTimeEntryMutation`

### Step 5: Verify ITimeReportingClient Interface

Check that the generated client has all operations:

```bash
grep -A 20 "interface ITimeReportingClient" TimeReportingMcp/obj/Debug/net10.0/berry/*.cs
```

Expected properties:
```csharp
public partial interface ITimeReportingClient
{
    global::TimeReportingMcp.Generated.IGetAvailableProjectsQuery GetAvailableProjects { get; }
    global::TimeReportingMcp.Generated.ILogTimeMutation LogTime { get; }
    global::TimeReportingMcp.Generated.IQueryTimeEntriesQuery QueryTimeEntries { get; }
    global::TimeReportingMcp.Generated.IUpdateTimeEntryMutation UpdateTimeEntry { get; }
    global::TimeReportingMcp.Generated.IDeleteTimeEntryMutation DeleteTimeEntry { get; }
    global::TimeReportingMcp.Generated.IMoveTaskToProjectMutation MoveTaskToProject { get; }
    global::TimeReportingMcp.Generated.ISubmitTimeEntryMutation SubmitTimeEntry { get; }
}
```

---

## Testing

### Unit Test: Verify Operation Files Exist

```csharp
[Fact]
public void AllToolsHaveGraphQLOperations()
{
    var operations = new[]
    {
        "GraphQL/GetProjects.graphql",
        "GraphQL/LogTime.graphql",
        "GraphQL/QueryTimeEntries.graphql",
        "GraphQL/UpdateTimeEntry.graphql",
        "GraphQL/DeleteTimeEntry.graphql",
        "GraphQL/MoveTaskToProject.graphql",
        "GraphQL/SubmitTimeEntry.graphql"
    };

    foreach (var op in operations)
    {
        var path = Path.Combine("TimeReportingMcp", op);
        Assert.True(File.Exists(path), $"Operation file missing: {op}");
    }
}
```

### Build Test

```bash
/build-mcp
```

All operations must generate without errors.

---

## Definition of Done

- [x] All 7 `.graphql` operation files created
- [x] Build succeeds with code generation
- [x] Generated `ITimeReportingClient` has all 7 operations
- [x] No hardcoded queries remain in the codebase (verified in next task)
- [x] All operation files follow consistent naming conventions
- [x] Operations request only necessary fields (no over-fetching)

---

## Files Created

```
TimeReportingMcp/GraphQL/
├── GetProjects.graphql          # ✅ Already exists
├── LogTime.graphql              # ✅ Already exists
├── QueryTimeEntries.graphql     # 🆕 To create
├── UpdateTimeEntry.graphql      # 🆕 To create
├── DeleteTimeEntry.graphql      # 🆕 To create
├── MoveTaskToProject.graphql    # 🆕 To create
└── SubmitTimeEntry.graphql      # 🆕 To create
```

---

## Notes

- **Field Selection**: Only request fields actually used by each tool to minimize response size
- **Nullable Fields**: Some fields (description, issueId) are optional - handle appropriately
- **Error Handling**: StrawberryShake provides `result.IsErrorResult()` for error checking
- **Type Safety**: All input types (LogTimeInput, UpdateTimeEntryInput, etc.) are auto-generated

---

## Related

- **ADR**: [0009-strawberryshake-typed-graphql-client.md](../../adr/0009-strawberryshake-typed-graphql-client.md)
- **Next Task**: [Task 13.2 - Refactor Tool Handlers](./task-13.2-refactor-tool-handlers.md)
- **Previous**: Phase 10 (Auto-tracking) completed
