---
type: adr
status: Accepted
category: Product Architecture
description: Defines the direction for per-type artifact schemas in policy-driven governance
---

# ADR-012: Artifact Type Schema System Direction

---

## Context

The use-case consolidation analysis (`docs/history/audits/usecase-consolidation-proposal.md`) identifies the artifact type schema system as the single most impactful capability gap. Of 55 canonical use-case items, 12 directly depend on per-type artifact definitions.

Currently, Steward validates frontmatter fields globally (`required_frontmatter_fields` in policy.yaml, enforced by STWD-003). Path-policy provides path-based rules. Artifact declarations in policy.yaml assign roles and requirement levels. However, there is no unified mechanism to declare that artifacts of a given type must have specific frontmatter fields with specific value constraints, specific required sections, specific naming patterns, or specific lifecycle status values.

The PRD already anticipates this: "Document-type-aware frontmatter expectations over time" (REQ-FM-003). Scoped frontmatter requirements (G7-02) and naming enforcement (G7-03) address pieces of this need. But the use-case analysis shows that a coherent artifact type definition system is the right abstraction — one that unifies frontmatter requirements, section expectations, naming rules, field constraints, and lifecycle policies under a single per-type declaration.

## Decision

**Steward should support a per-type artifact definition system in policy.yaml.** This system:

1. **Declares artifact types** with explicit names (e.g., `character`, `chapter`, `decision-record`, `api-spec`). Types are repository-specific strings, not a hardcoded taxonomy.

2. **Associates types with files** through matching criteria — path patterns, frontmatter `type` field values, or both. The matching mechanism must be deterministic and documented.

3. **Declares per-type validation expectations:**
   - Required and optional frontmatter fields (extending G7-02 scoped frontmatter)
   - Field value constraints: allowed values (enum), pattern (regex), data type (string, date, list, boolean, number)
   - Required and optional Markdown sections (heading names)
   - Filename/naming pattern requirements (extending G7-03)
   - Allowed status values and lifecycle transitions

4. **Integrates with existing validation rules.** Type-aware validation extends existing rules (STWD-003, path-policy engine) rather than creating a parallel validation system.

5. **Supports controlled vocabularies.** Fields with `allowed_values` constraints serve as controlled vocabularies, preventing taxonomy drift for fields like `status`, `type`, `tags`, and domain-specific labels.

6. **Defaults to non-breaking.** Repositories without artifact type definitions continue to work. Type schemas are additive — they add validation, they do not change the behavior of untyped files.

7. **Requires a follow-up RFC for design specification.** This ADR establishes the product direction. The exact YAML schema, type-to-file matching semantics, inheritance model (type hierarchy, default overrides), and interaction with existing G7 items require an RFC before implementation.

## Consequences

- A new section in policy.yaml (e.g., `artifact_types:`) becomes the primary mechanism for domain-specific governance expression.
- STWD-003 (frontmatter validation) evolves from global-only to type-aware.
- A new validation rule (or rule family) validates section presence per type.
- Controlled vocabulary enforcement becomes a configuration concern, not a core feature per domain.
- The story/worldbuilding profile (ADR-011) can ship meaningful defaults once this system exists: character types with frontmatter schemas, chapter types with required sections, etc.
- Multiple use-case items that are currently classified as "proposed" or "future" become implementable once this system is delivered.
- The design RFC should be created when the later pre-1.0 artifact-type milestone is scheduled for implementation, not before — to avoid premature specification without implementation context. **[Resolved in v0.13.0: RFC-008 is the follow-up RFC and has been accepted. Its §8 narrows the v0.13.0 implementation contract.]**
- Existing G7 items (G7-02 scoped frontmatter, G7-03 naming enforcement, G7-12 three-level classification, G7-13 role-linked defaults) are building blocks that the type schema system unifies and extends.
