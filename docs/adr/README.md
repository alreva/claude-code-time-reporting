# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the Time Reporting System project.

## What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences.

**An ADR documents:**
- **Context**: What problem or situation led to this decision?
- **Decision**: What did we decide to do?
- **Rationale**: Why did we choose this approach?
- **Consequences**: What are the trade-offs? (benefits and costs)
- **Implementation**: How do we apply this decision?

## When to Create an ADR

Create an ADR when you make a decision that:
- Affects the system's structure, patterns, or design principles
- Has long-term impact on the codebase
- Involves trade-offs between competing concerns
- Future developers need to understand the "why" behind the "what"
- Changes a previous architectural approach

**Examples of ADR-worthy decisions:**
- ✅ Choosing shadow properties over explicit FK properties
- ✅ Using C# for both API and MCP server (mono-stack)
- ✅ Normalized relational schema vs JSONB
- ✅ GraphQL over REST API
- ❌ Renaming a variable (too small)
- ❌ Fixing a bug (not architectural)
- ❌ Adding a utility function (not a design decision)

## ADR Naming Convention

ADRs are numbered sequentially:

```
0001-shadow-foreign-keys.md
0002-csharp-mono-stack.md
0003-naming-consistency.md
```

**Format**: `NNNN-kebab-case-title.md`

## ADR Structure

Each ADR should follow this structure:

```markdown
# [Number]. [Title]

## Status

[Proposed | Accepted | Deprecated | Superseded]

## Context

What is the issue we're facing? What forces are at play?

## Decision

What are we going to do about it?

## Rationale

Why did we choose this approach over alternatives?

## Consequences

### Benefits
- What we gain

### Costs
- What we give up or must accept

## Implementation

How do we apply this decision in practice? (code examples, patterns)

## Alternatives Considered

What other options did we evaluate and why did we reject them?
```

## Index of ADRs

| Number | Title | Status | Date |
|--------|-------|--------|------|
| [0001](0001-shadow-foreign-keys.md) | Shadow Foreign Keys | Accepted | 2025-10-28 |
| [0002](0002-csharp-mono-stack.md) | C# Mono-Stack Architecture | Accepted | 2025-10-28 |
| [0003](0003-naming-consistency.md) | Consistent Entity Naming Pattern | Accepted | 2025-10-28 |
| [0004](0004-normalized-schema.md) | Fully Normalized Schema | Accepted | 2025-10-28 |
| [0005](0005-relational-over-jsonb.md) | Relational Schema Over JSONB | Accepted | 2025-10-28 |
| [0006](0006-hotchocolate-conventions-over-resolvers.md) | HotChocolate Conventions Over Custom Resolvers | Accepted | 2025-10-28 |

## Process for Claude Code

**When a system design discussion occurs:**

1. **Recognize ADR Moment**: If the discussion involves architectural trade-offs or design decisions
2. **Announce**: "This feels like an ADR - we're making an architectural decision about X"
3. **Document Decision**: Create a new ADR in this directory
4. **Update Index**: Add entry to the table above
5. **Commit**: Commit the ADR separately from implementation

## References

- [Michael Nygard's ADR Template](https://github.com/joelparkerhenderson/architecture-decision-record)
- [ADR Tools](https://github.com/npryce/adr-tools)
