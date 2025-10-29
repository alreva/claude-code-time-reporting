# Task 13.3: Remove Old GraphQL Client Code

**Phase:** 13 - StrawberryShake Migration
**Estimated Time:** 30 minutes
**Dependencies:** Task 13.2 (Tool Handlers Refactored)
**Status:** Pending

---

## Objective

Remove obsolete GraphQL client code (`GraphQLClientWrapper`, manual type definitions, and related utilities) now that all tools use StrawberryShake's generated client.

---

## Background

After migrating all tools to StrawberryShake, the old `GraphQLClientWrapper` and manual type definitions in `Models/GraphQLModels.cs` are no longer used. This task cleans up the codebase by removing obsolete code.

---

## Acceptance Criteria

1. ✅ `GraphQLClientWrapper.cs` deleted
2. ✅ `Models/GraphQLModels.cs` deleted (manual type definitions)
3. ✅ Old `GraphQL.Client` packages remain (still used by StrawberryShake)
4. ✅ No references to deleted files remain in codebase
5. ✅ All tests pass after deletion
6. ✅ Build succeeds with no warnings

---

## Implementation Steps

### Step 1: Verify No Usage of GraphQLClientWrapper

**Search for references:**
```bash
grep -r "GraphQLClientWrapper" TimeReportingMcp/ --include="*.cs" --exclude-dir=obj --exclude-dir=bin
```

**Expected:** No results (all tools now use `ITimeReportingClient`)

If any references found, refactor them first before proceeding.

---

### Step 2: Verify No Usage of Manual GraphQL Models

**Search for manual type references:**
```bash
grep -r "TimeEntryData\|ProjectData\|TaskData\|TagData\|LogTimeResponse\|GetProjectsResponse" TimeReportingMcp/ --include="*.cs" --exclude-dir=obj --exclude-dir=bin
```

**Expected:** No results (all tools use generated types from `TimeReportingMcp.Generated` namespace)

---

### Step 3: Delete GraphQLClientWrapper

**File to delete:** `TimeReportingMcp/Utils/GraphQLClientWrapper.cs`

```bash
git rm TimeReportingMcp/Utils/GraphQLClientWrapper.cs
```

**Code being removed (~50 lines):**
```csharp
// Old wrapper around GraphQL.Client for hardcoded string queries
public class GraphQLClientWrapper
{
    private readonly GraphQLHttpClient _client;

    public GraphQLClientWrapper(string apiUrl, string bearerToken)
    {
        _client = new GraphQLHttpClient(apiUrl, new SystemTextJsonSerializer());
        _client.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public async Task<GraphQLResponse<T>> SendMutationAsync<T>(GraphQLRequest request)
    {
        // ...
    }

    public async Task<GraphQLResponse<T>> SendQueryAsync<T>(GraphQLRequest request)
    {
        // ...
    }
}
```

---

### Step 4: Delete Manual GraphQL Models

**File to delete:** `TimeReportingMcp/Models/GraphQLModels.cs`

```bash
git rm TimeReportingMcp/Models/GraphQLModels.cs
```

**Code being removed (~200 lines):**
```csharp
// Manual type definitions that matched GraphQL schema
public class TimeEntryData
{
    public Guid Id { get; set; }
    public ProjectData Project { get; set; } = null!;
    public ProjectTaskData ProjectTask { get; set; } = null!;
    // ... ~50 lines
}

public class ProjectData
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    // ... ~30 lines
}

public class LogTimeResponse
{
    public TimeEntryData LogTime { get; set; } = null!;
}

public class GetProjectsResponse
{
    public List<ProjectData> Projects { get; set; } = new();
}

// ... ~150 more lines of manual types
```

---

### Step 5: Keep GraphQL.Client Packages

**DO NOT REMOVE** these packages (StrawberryShake depends on them):
- `GraphQL.Client` - Used by StrawberryShake internally
- `GraphQL.Client.Serializer.SystemTextJson` - JSON serialization

**Verify in `.csproj`:**
```xml
<ItemGroup>
  <PackageReference Include="GraphQL.Client" />
  <PackageReference Include="GraphQL.Client.Serializer.SystemTextJson" />

  <!-- StrawberryShake packages -->
  <PackageReference Include="StrawberryShake" />
  <PackageReference Include="StrawberryShake.Server" />
  <PackageReference Include="StrawberryShake.Transport.Http" />
</ItemGroup>
```

Leave this unchanged.

---

### Step 6: Clean Up Unused Imports

**Files to check for unused usings:**
```bash
find TimeReportingMcp/Tools -name "*.cs" -exec grep "using GraphQL" {} \;
```

Remove any `using GraphQL;` statements that are no longer needed.

**Example cleanup in tool files:**
```csharp
// Before
using GraphQL;
using TimeReportingMcp.Models;  // GraphQLModels.cs types

// After
using TimeReportingMcp.Generated;  // StrawberryShake generated types
```

---

### Step 7: Build and Test

**Build:**
```bash
/build-mcp
```

**Expected:** Build succeeds with 0 warnings, 0 errors

**Test:**
```bash
/test-mcp
```

**Expected:** All 97 tests pass

---

## Testing

### Verify Deletion

**Check files no longer exist:**
```bash
ls TimeReportingMcp/Utils/GraphQLClientWrapper.cs 2>/dev/null && echo "ERROR: File still exists" || echo "✅ File deleted"
ls TimeReportingMcp/Models/GraphQLModels.cs 2>/dev/null && echo "ERROR: File still exists" || echo "✅ File deleted"
```

### Verify No Dead References

**Search entire codebase:**
```bash
grep -r "GraphQLClientWrapper\|TimeEntryData\|ProjectData\|LogTimeResponse" TimeReportingMcp/ \
  --include="*.cs" --exclude-dir=obj --exclude-dir=bin
```

**Expected:** No results

### Build Verification

```bash
/build-mcp
```

Must succeed with no errors or warnings about missing types.

---

## Definition of Done

- [x] `GraphQLClientWrapper.cs` deleted
- [x] `Models/GraphQLModels.cs` deleted
- [x] No references to deleted code remain
- [x] Build succeeds (0 warnings, 0 errors)
- [x] All 97 MCP tests pass
- [x] Unused `using` statements cleaned up
- [x] Git history preserved with proper commit message

---

## Files Deleted

```
TimeReportingMcp/
├── Utils/
│   └── GraphQLClientWrapper.cs     # ❌ Deleted (~50 lines)
└── Models/
    └── GraphQLModels.cs            # ❌ Deleted (~200 lines)
```

**Total lines removed:** ~250 lines

---

## Benefits

- ✅ **Reduced Codebase Size**: 250 fewer lines to maintain
- ✅ **Single Source of Truth**: Only StrawberryShake-generated types exist
- ✅ **No Type Drift**: Impossible for manual types to drift from schema
- ✅ **Cleaner Architecture**: Clear separation between operations (.graphql) and usage (tools)

---

## Notes

- **Git History**: Use `git rm` to properly track file deletions
- **Commit Message**: "Remove obsolete GraphQL client code after StrawberryShake migration"
- **Reversibility**: Old code preserved in git history if needed for reference
- **Dependencies**: GraphQL.Client packages remain (used by StrawberryShake)

---

## Commit Message Template

```
Remove obsolete GraphQL client code after StrawberryShake migration

Delete GraphQLClientWrapper and manual GraphQL type definitions now that
all MCP tools use StrawberryShake's strongly-typed generated client.

Removed:
- TimeReportingMcp/Utils/GraphQLClientWrapper.cs (~50 lines)
- TimeReportingMcp/Models/GraphQLModels.cs (~200 lines)

Kept:
- GraphQL.Client packages (still needed by StrawberryShake)

Benefits:
- 250 fewer lines to maintain
- Single source of truth for types (generated code only)
- Impossible for types to drift from schema

All 97 tests pass ✅

Related: Task 13.3, ADR 0009 (StrawberryShake)
```

---

## Related

- **Previous Task**: [Task 13.2 - Refactor Tool Handlers](./task-13.2-refactor-tool-handlers.md)
- **Next Task**: [Task 13.4 - Update Documentation](./task-13.4-update-documentation.md)
- **ADR**: [0009-strawberryshake-typed-graphql-client.md](../../adr/0009-strawberryshake-typed-graphql-client.md)
