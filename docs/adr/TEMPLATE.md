# ADR XXXX: [Short Title]

## Status

**[Proposed | Accepted | Deprecated | Superseded by ADR-YYYY]**

## Context

What is the issue we're facing? What forces are at play?

**Describe the problem:**
- What architectural challenge or decision point triggered this ADR?
- What constraints or requirements are driving the decision?
- What is the current situation (if applicable)?

**Example questions to answer:**
- What problem are we trying to solve?
- Why is this decision important?
- What happens if we don't make this decision?
- Are there business or technical constraints?

## Decision

What are we going to do about it?

**State the decision clearly and concisely:**
- What approach are we taking?
- What is the core architectural choice?

**Be specific:**
- ✅ "Use EF Core shadow properties for all foreign keys"
- ❌ "Use a better approach for foreign keys"

## Rationale

Why did we choose this approach over alternatives?

**Explain the reasoning:**
- What makes this the best solution for our context?
- What principles or values guided the decision?
- What evidence supports this choice?

**Link to project goals:**
- How does this support the project's architecture?
- How does this align with the team's values?

## Consequences

What are the trade-offs? What do we gain and what do we give up?

### Benefits

✅ **Benefit 1**
- Detailed explanation of positive consequence

✅ **Benefit 2**
- Detailed explanation of positive consequence

✅ **Benefit 3**
- Detailed explanation of positive consequence

### Costs

⚠️ **Cost 1**
- Detailed explanation of negative consequence or limitation

⚠️ **Cost 2**
- Detailed explanation of negative consequence or limitation

⚠️ **Cost 3**
- Detailed explanation of negative consequence or limitation

### Trade-off Assessment

**Decision: [Summarize why benefits outweigh costs, or why we accept the trade-offs]**

Example: "Safety first. The foot-gun elimination is worth the slight increase in query verbosity."

## Implementation

How do we apply this decision in practice?

**Provide concrete guidance:**
- Code examples showing the "right way"
- Patterns to follow
- Anti-patterns to avoid
- Configuration examples
- Query examples (before/after)

**Include:**
- ✅ Working code snippets
- ✅ Database schema changes (if applicable)
- ✅ Configuration examples
- ✅ Usage examples

**Structure:**
```
### Before
[Show old approach if refactoring]

### After
[Show new approach]

### Example Usage
[Show how developers should use this]
```

## Alternatives Considered

What other options did we evaluate and why did we reject them?

### Alternative 1: [Name]

**Approach**: Brief description of the alternative.

**Why rejected:**
- Reason 1
- Reason 2
- Reason 3

### Alternative 2: [Name]

**Approach**: Brief description of the alternative.

**Why rejected:**
- Reason 1
- Reason 2
- Reason 3

### Alternative 3: [Name]

**Approach**: Brief description of the alternative.

**Why rejected:**
- Reason 1
- Reason 2
- Reason 3

**Note**: Always include at least 2-3 alternatives to show you considered multiple approaches.

## References

- Git commit: `[hash]` - "[Commit message]"
- Related ADR: [XXXX - Title](xxxx-title.md)
- External references: Links to documentation, blog posts, Stack Overflow, etc.
- Database migration: `[MigrationName].cs` (if applicable)

---

## Tips for Writing ADRs

### What Makes a Good ADR?

1. **Clear Problem Statement**: Reader understands the "why" immediately
2. **Explicit Decision**: No ambiguity about what was decided
3. **Honest Trade-offs**: Both benefits and costs are acknowledged
4. **Actionable Guidance**: Developers know how to implement the decision
5. **Well-Researched**: Alternatives were considered and documented

### What NOT to Include in an ADR

❌ **Implementation details** - Keep it high-level, focus on the decision
❌ **Obvious choices** - Only document decisions with meaningful trade-offs
❌ **Future speculation** - Focus on what we know now, not hypothetical scenarios
❌ **Justifications** - Don't be defensive, just explain the rationale

### ADR Writing Checklist

Before finalizing an ADR, check:

- [ ] Title clearly describes the decision
- [ ] Context explains the problem without assuming reader knowledge
- [ ] Decision is stated explicitly and unambiguously
- [ ] Rationale explains WHY this choice was made
- [ ] Benefits section lists positive consequences
- [ ] Costs section lists negative consequences or limitations
- [ ] Trade-off assessment explains why benefits outweigh costs
- [ ] Implementation provides concrete, actionable guidance
- [ ] At least 2-3 alternatives are documented with rejection reasons
- [ ] References include relevant commits, migrations, or external links
- [ ] Code examples are tested and accurate
- [ ] Status is correct (Proposed, Accepted, Deprecated, Superseded)

### When to Write an ADR

Write an ADR when:
- ✅ Making a decision that affects system architecture
- ✅ Choosing between multiple valid approaches with trade-offs
- ✅ Establishing a pattern or principle for the project
- ✅ Changing a previous architectural decision
- ✅ Making a decision that future developers need to understand

Don't write an ADR for:
- ❌ Bug fixes (not architectural)
- ❌ Trivial implementation details
- ❌ Obvious or uncontested choices
- ❌ Temporary workarounds
