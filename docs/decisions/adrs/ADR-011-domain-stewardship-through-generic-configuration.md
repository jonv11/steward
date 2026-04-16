---
type: adr
status: Accepted
category: Product Direction
---

# ADR-011: Domain-Specific Stewardship Through Generic Configuration

---

## Context

Two maintainer use-case files (`docs/audits/maintainer-usecase-expectations.md` and `docs/audits/maintainer-usecase-ideas.md`) describe the needs of a story/worldbuilding/adaptation repository in detail. They request capabilities such as canon integrity validation, timeline consistency checking, plot-thread lifecycle enforcement, adaptation freshness tracking, and continuity-specific rules.

These requests are legitimate: the PRD explicitly names "knowledge, content, lore, story, or creative repositories" as a target archetype (PRD §6), and the configuration model is designed to support varied repository types without hardcoded assumptions (REQ-CORE-004, REQ-CORE-007, REQ-CONFIG-005).

The question is whether domain-specific validation and stewardship logic should be:
1. built into the core CLI as hardcoded capabilities (e.g., a `validate-canon` command, a `check-timeline` rule), or
2. expressed through generic, reusable policy mechanisms that domain-specific repositories configure to their own needs.

## Decision

**Domain-specific stewardship needs are served through generic, configurable policy mechanisms — not through hardcoded domain logic in the core CLI.**

Specifically:

1. **No domain-hardcoded rules.** The CLI does not include rules for canon validation, timeline consistency, plot-thread lifecycle, adaptation freshness, or any other domain-semantic logic. Those concepts belong to the consuming repository's policy configuration.

2. **Generic mechanisms enable domain expression.** The following generic mechanisms, when implemented, enable repositories to express domain-specific stewardship:
   - **Artifact type schemas** (see ADR-012): per-type frontmatter requirements, field types, required sections, naming patterns.
   - **Controlled vocabularies:** enum-type field validation prevents taxonomy drift for status, type, continuity_level, etc.
   - **Relationship type declarations:** policy-declared allowed references between artifact types, validated against frontmatter relationship fields.
   - **Lifecycle/status rules:** allowed status values and transition constraints per artifact type.
   - **Path-scoped enforcement:** per-directory rules that enforce separation boundaries (e.g., canon vs adaptation directories).

3. **Profiles encode domain-appropriate defaults.** A `story` or `worldbuilding` built-in profile provides reasonable default policy for the story/lore archetype, just as `software` provides defaults for code repositories. Profiles are starting points, not enforcement mechanisms.

4. **Domain-semantic reasoning is out of scope.** Logic that requires understanding domain semantics — such as "this character is dead and cannot appear in a later scene", "this timeline ordering is impossible", or "this adaptation note asserts a canon fact not present in the canon layer" — is beyond the CLI's scope. Those checks require domain ontology that cannot be expressed through generic policy mechanisms without unbounded complexity.

## Consequences

- The CLI remains archetype-agnostic. Adding story/lore support does not change the core rule engine, command surface, or architectural layering.
- Story/worldbuilding repositories configure their governance using the same mechanisms as software, documentation, or knowledge repositories.
- The burden of defining domain-specific validation shifts to the repository maintainer's policy files, which is the intended design per RFC-002 (configuration model) and the PRD's contract-centric philosophy.
- A `story` or `worldbuilding` profile lowers the adoption barrier by providing a curated starting-point configuration for that archetype.
- Items from the use-case files that request domain-semantic validation (canon integrity, timeline date arithmetic, plot-thread resolution logic, adaptation staleness comparison) are classified as out of scope for hardcoded implementation but partially addressable through generic mechanisms once the artifact type schema system (ADR-012) exists.
- This decision aligns with established product principles: "prefer generic, repo-agnostic capabilities over hardcoded domain logic" and "separate what the CLI enforces from what the policy declares."
