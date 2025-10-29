# Task 13.4: Update Documentation for StrawberryShake

**Phase:** 13 - StrawberryShake Migration
**Estimated Time:** 1 hour
**Dependencies:** Task 13.3 (Old Code Removed)
**Status:** Pending

---

## Objective

Update project documentation to reflect the StrawberryShake migration, including architecture docs, setup guides, and development workflow documentation.

---

## Background

The MCP server architecture has changed significantly with the introduction of StrawberryShake. Documentation needs to reflect:
- New typed client approach
- Updated development workflow
- New dependencies and build process
- Updated code examples

---

## Acceptance Criteria

1. ✅ Architecture docs updated with StrawberryShake details
2. ✅ MCP setup guide includes StrawberryShake configuration
3. ✅ Development workflow docs explain `.graphql` file creation
4. ✅ All code examples updated to show typed client usage
5. ✅ ADR 0009 referenced in relevant documentation
6. ✅ README updated with new dependencies

---

## Files to Update

### 1. Update ARCHITECTURE.md

**Section to add: "GraphQL Client Layer (StrawberryShake)"**

```markdown
## GraphQL Client Layer

### StrawberryShake Typed Client

The MCP server uses **StrawberryShake v15** for strongly-typed GraphQL client code generation.

**Architecture:**
```
.graphql files → StrawberryShake Generator → C# Typed Client
                                              ↓
                         MCP Tools use ITimeReportingClient
```

**Benefits:**
- **Compile-time safety**: Schema mismatches caught at build time
- **IntelliSense**: Full autocomplete for all operations
- **Auto-sync**: Generated types always match API schema
- **Less boilerplate**: ~250 lines of manual code eliminated

**Configuration:**
- `.graphqlrc.json` - StrawberryShake configuration
- `schema.graphql` - Downloaded from API
- `GraphQL/*.graphql` - Operation definitions
- `obj/berry/*.cs` - Generated client code

**Related:** [ADR 0009 - StrawberryShake Typed GraphQL Client](./docs/adr/0009-strawberryshake-typed-graphql-client.md)
```

### 2. Update docs/integration/CLAUDE-CODE-SETUP.md

**Add section: "StrawberryShake Code Generation"**

```markdown
## Development Workflow with StrawberryShake

### Schema Updates

When the GraphQL API schema changes, regenerate the MCP client:

1. **Download latest schema:**
   ```bash
   curl -s "http://localhost:5001/graphql?sdl" > TimeReportingMcp/schema.graphql
   ```

2. **Rebuild MCP project:**
   ```bash
   /build-mcp
   ```

   StrawberryShake automatically regenerates typed client code.

3. **Fix any compilation errors** (schema breaking changes)

### Adding New Operations

To add a new MCP tool with GraphQL operation:

1. **Create `.graphql` operation file:**
   ```bash
   # Example: TimeReportingMcp/GraphQL/NewOperation.graphql
   query GetSomething($id: UUID!) {
     something(id: $id) {
       id
       name
     }
   }
   ```

2. **Rebuild to generate code:**
   ```bash
   /build-mcp
   ```

3. **Use generated operation in tool:**
   ```csharp
   public class NewTool
   {
       private readonly ITimeReportingClient _client;

       public async Task<ToolResult> ExecuteAsync(JsonElement args)
       {
           var result = await _client.GetSomething.ExecuteAsync(id);

           if (result.IsErrorResult())
           {
               return CreateErrorResult(result.Errors);
           }

           return CreateSuccessResult(result.Data!.Something);
       }
   }
   ```

**Generated Types Location:**
- `TimeReportingMcp/obj/Debug/net10.0/berry/*.cs`
- Namespace: `TimeReportingMcp.Generated`
```

### 3. Update README.md

**Add to Dependencies section:**

```markdown
### MCP Server Dependencies

- **.NET 10.0** - Console application framework
- **StrawberryShake 15.1.11** - Strongly-typed GraphQL client code generation
- **GraphQL.Client** - GraphQL HTTP client (used by StrawberryShake)
- **System.Text.Json** - JSON serialization

**Code Generation:**
- StrawberryShake generates C# client code from `.graphql` operation files
- Code generation runs automatically during build
- Generated code location: `obj/Debug/net10.0/berry/`
```

**Update "Building the Project" section:**

```markdown
## Building the Project

### Full Build

```bash
/build
```

This builds both API and MCP server projects.

**For MCP Server:**
- Downloads/parses GraphQL schema
- Generates typed client code from `.graphql` files
- Compiles MCP server with generated code

**Generated Files (gitignored):**
- `TimeReportingMcp/obj/Debug/net10.0/berry/*.cs` - StrawberryShake client

### MCP-Only Build

```bash
/build-mcp
```

Faster iteration when only working on MCP tools.
```

### 4. Update docs/prd/architecture.md

**Section: "MCP Server Implementation"**

Update code examples to show StrawberryShake usage instead of hardcoded strings.

**Before (in docs):**
```csharp
var mutation = new GraphQLRequest
{
    Query = @"mutation LogTime($input: LogTimeInput!) { ... }",
    Variables = new { input }
};
```

**After (update docs to):**
```csharp
// Tools use strongly-typed generated client
public class LogTimeTool
{
    private readonly ITimeReportingClient _client;

    public async Task<ToolResult> ExecuteAsync(JsonElement args)
    {
        var input = new LogTimeInput { /* ... */ };
        var result = await _client.LogTime.ExecuteAsync(input);

        if (result.IsErrorResult())
        {
            return CreateErrorResult(result.Errors);
        }

        return CreateSuccessResult(result.Data!.LogTime);
    }
}
```

### 5. Update CLAUDE.md (Project Instructions)

**Add to "Development Commands" section:**

```markdown
### Schema Updates

When API schema changes:

1. **Download schema:**
   ```bash
   curl -s "http://localhost:5001/graphql?sdl" > TimeReportingMcp/schema.graphql
   ```

2. **Rebuild MCP:**
   ```bash
   /build-mcp
   ```

StrawberryShake regenerates typed client automatically.

### StrawberryShake Files

**Committed to git:**
- `.graphqlrc.json` - Configuration
- `GraphQL/*.graphql` - Operation definitions
- `schema.graphql` - GraphQL schema

**Generated (gitignored):**
- `obj/Debug/net10.0/berry/*.cs` - Typed client code
```

### 6. Create Migration Guide (Optional)

**File:** `docs/STRAWBERRYSHAKE-MIGRATION.md`

Document the migration for future reference:
- Why we migrated
- Before/after comparison
- Lessons learned
- Benefits realized

---

## Testing

### Documentation Review

**Check all code examples compile:**
1. Copy code examples from updated docs
2. Verify they compile in actual codebase
3. Fix any inconsistencies

**Verify links work:**
```bash
# Check internal links in markdown
grep -r "\[.*\](\..*\.md)" docs/ | while read line; do
  echo "Checking: $line"
  # Verify referenced files exist
done
```

### Build Documentation Site (if applicable)

If using documentation generator (e.g., MkDocs, Docusaurus):
```bash
# Build docs site
mkdocs build  # or equivalent
```

---

## Definition of Done

- [x] All architecture docs reference StrawberryShake
- [x] Setup guides explain `.graphql` file workflow
- [x] Code examples show typed client usage (not hardcoded strings)
- [x] Dependencies list includes StrawberryShake 15.1.11
- [x] Build process explains code generation
- [x] ADR 0009 linked from relevant docs
- [x] All internal doc links work
- [x] Code examples verified to compile

---

## Files to Update

```
docs/
├── ARCHITECTURE.md                  # Add StrawberryShake section
├── README.md                        # Update dependencies and build process
├── prd/architecture.md              # Update code examples
├── integration/CLAUDE-CODE-SETUP.md # Add schema update workflow
└── STRAWBERRYSHAKE-MIGRATION.md     # Optional: Migration guide

CLAUDE.md                            # Add schema update commands
```

---

## Documentation Checklist

- [ ] All references to `GraphQLClientWrapper` removed from docs
- [ ] All code examples updated to use `ITimeReportingClient`
- [ ] Schema download workflow documented
- [ ] Build process explains code generation
- [ ] Dependencies section lists StrawberryShake
- [ ] Architecture diagrams updated (if any)
- [ ] Setup guides mention `.graphqlrc.json`
- [ ] ADR 0009 referenced where appropriate

---

## Notes

- **Backward Compatibility**: Old documentation preserved in git history
- **Future Reference**: ADR 0009 is the authoritative source for decision rationale
- **Code Examples**: Prefer real, working code snippets over pseudo-code
- **Links**: Always use relative paths for internal doc links

---

## Commit Message Template

```
Update documentation for StrawberryShake migration

Reflect architectural changes from hardcoded GraphQL strings to
strongly-typed StrawberryShake client in all project documentation.

Updated:
- ARCHITECTURE.md - Add StrawberryShake section
- README.md - Update dependencies and build process
- CLAUDE.md - Add schema update workflow
- docs/prd/architecture.md - Update code examples
- docs/integration/CLAUDE-CODE-SETUP.md - Add development workflow

Changes:
- All code examples now show ITimeReportingClient usage
- Build process explains StrawberryShake code generation
- Schema update workflow documented
- Dependencies list includes StrawberryShake 15.1.11

Related: Task 13.4, ADR 0009 (StrawberryShake)
```

---

## Related

- **Previous Task**: [Task 13.3 - Remove Old Client Code](./task-13.3-remove-old-client-code.md)
- **ADR**: [0009-strawberryshake-typed-graphql-client.md](../../adr/0009-strawberryshake-typed-graphql-client.md)
- **Phase Complete**: Phase 13 - StrawberryShake Migration
