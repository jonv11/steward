---
type: rfc
status: Accepted
description: Extends artifact family schemas with per-family H2 section-heading format enforcement, complementing the H1 title_pattern enforcement introduced by RFC-014
resolves: >-
  H2 section heading conventions being unenforced despite consistent practical use
  in governed families such as RFCs and ADRs, which use numbered-section H2 formats
  by convention
last_updated: 2026-06-06
---

# RFC-015: Section Heading Pattern Enforcement

---

## 1. Context

RFC-014 introduced `title_pattern` (enforced by STWD-019) to constrain the H1 heading
text of files matched by an artifact family. Section 9 of RFC-014 explicitly deferred
H2+ enforcement:

> **H2+ heading format enforcement** — `title_pattern` is H1-only; section heading
> format is a separate concern.

The `title_pattern` / STWD-019 model is H1-specific by design: the H1 is the document
title, there is exactly one per file, and its format often mirrors the filename
identifier (e.g., `RFC-NNN: Title`, `ADR-NNN: Title`).

H2 headings are structurally different: a file may contain zero or many, they are
section markers rather than document titles, and their format conventions vary by
family. This structural difference justifies separate design rather than a simple
parameter extension to STWD-019.

### What works today

- `title_pattern` (STWD-019) enforces H1 heading format per family (RFC-014).
- `required_sections` (STWD-014) enforces the *presence* of named headings by
  exact-string match; it cannot enforce format.
- `naming_pattern` (STWD-016) enforces filename format per family.
- `frontmatter_schema.required` / `allowed_values` (STWD-003) enforces frontmatter
  field presence and vocabulary.

### What does not work today

There is no way to declare that H2 headings within a family must satisfy a format
pattern. Section heading conventions that exist in practice are unenforced.

### Evidence from self-dogfooding

The Steward repository's own RFC and ADR families use numbered H2 sections consistently:

- RFC documents use `## 1. Context`, `## 2. Problem Statement`, `## 3. …`, etc.
- ADR documents use `## Context`, `## Decision`, `## Consequences` (fixed names, no
  number prefix).

These are unwritten conventions: a file with the right filename, frontmatter, and H1
but malformed H2 headings (e.g., `## context` lowercased, or `## 1 Context` missing the
period) passes all current checks without complaint.

---

## 2. Problem Statement

`title_pattern` closed the enforcement gap on the document title surface. The H2
surface — the next most structurally significant heading level — remains fully
unconstrained per-family. Repositories that use H2 format conventions as a structural
signal (numbered sections, capitalized-only headings, date-prefixed entries in
changelogs) have no policy mechanism to detect deviations.

Three concrete consequences:

1. **Unenforced structural conventions.** RFC documents in this repository use
   `## N. Title` consistently but without any enforcement. A new contributor can
   introduce `## decision` or `## 3 Alternatives` and no tool will object.

2. **No policy expression for structural format.** `required_sections` can assert that
   a section named "Context" exists; it cannot assert that the heading is capitalized,
   numbered, or follows any other format constraint. The two concerns — presence and
   format — are independently useful and belong in different mechanisms.

3. **Asymmetry between H1 and H2 governance.** Artifact families now have a complete
   governance surface for filenames (`naming_pattern`), frontmatter (`frontmatter_schema`),
   and document titles (`title_pattern`). Section headings are the only unaddressable
   surface.

---

## 3. Proposed Capability

### 3.1 `section_pattern` — H2 Heading Format Enforcement

A new optional key at the family root that enforces the format of all H2 headings in
matched files:

```yaml
artifact_families:
  - family: rfc
    display_name: Request for Comments
    match:
      path_pattern: "docs/decisions/rfcs/RFC-*.md"
    naming_pattern: "^RFC-[0-9]{3}-[a-z0-9-]+(?:-draft)?\\.md$"
    title_pattern: "^RFC-[0-9]{3}: .+"
    section_pattern: "^[0-9]+\\. .+"

  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/decisions/adrs/ADR-*.md"
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    title_pattern: "^ADR-[0-9]{3}: .+"
    section_pattern: "^[A-Z][A-Za-z ]+"
```

**Semantics:**

- Enforced by a new rule **STWD-020** at Warning severity.
- `section_pattern` applies to every H2 heading (`## …`) in the matched file.
- If a matched file contains no H2 headings, STWD-020 does not fire — absent H2s
  are outside this rule's scope (parallel to STWD-019 / absent H1 behavior).
- Each H2 heading that does not match the declared pattern emits one diagnostic,
  including the heading text and the line number where it appears.
- Pattern matching is case-sensitive by default (parallel to `title_pattern`).
- The pattern is a .NET-compatible regular expression. `config validate` rejects
  invalid regex values with an Error.

**Relationship to `required_sections` (STWD-014):**

`required_sections` checks that specific section names are *present*. `section_pattern`
checks that all H2 headings *conform to a format*. They are orthogonal:
`required_sections` can require a "Context" section to exist; `section_pattern` can
require all H2s to start with a capital letter. Both can be declared together.

**Relationship to `title_pattern` (STWD-019):**

`title_pattern` constrains the single H1. `section_pattern` constrains all H2s
uniformly. Together they govern the two most structurally significant heading levels
of a document. H3+ enforcement is explicitly out of scope for this RFC (see Section 8).

**Why uniform H2 enforcement rather than per-section patterns:**

Per-section patterns (a map from section names to format patterns) would require
defining key semantics: exact match vs. regex match, case sensitivity, what happens
when a key matches zero sections. These questions involve substantial design surface
and would make the config harder to read and validate.

Uniform H2 enforcement is simpler to specify, simpler to implement, simpler to
validate in `config validate`, and covers the most common practical use cases:
numbered sections, capitalization conventions, and prefix-based format disciplines.
Per-section format enforcement is deferred (see Section 8).

**Auto-fix:**

STWD-020 is not auto-fixable. Section heading text is human-authored and may require
editorial judgment to correct. The diagnostic includes the expected pattern, the
actual heading text, and the line number.

---

## 4. Rule and Config Changes Summary

**Rule changes:**

| Rule | Change | Default Severity |
|---|---|---|
| STWD-020 *(new)* | `section_pattern` enforcement: all H2 headings in family-matched files must match the declared regex | Warning |

**`config validate` extensions:**

- Reject `section_pattern` values that fail .NET regex compilation.

**No changes to existing rules:** STWD-019 (`title_pattern`) and STWD-014
(`required_sections`) are unchanged.

---

## 5. Backward Compatibility

The new capability is strictly opt-in:

- `section_pattern` absent → STWD-020 never fires for that family.

Repositories not using `section_pattern` behave identically to their current behavior.
Existing policy configurations require no changes.

---

## 6. Self-Dogfooding Configuration Changes Enabled

When this RFC is implemented, the Steward repository's own `policy.yaml` may adopt
`section_pattern` for the RFC and ADR families:

```yaml
- family: rfc
  section_pattern: "^[0-9]+\\. .+"

- family: adr
  section_pattern: "^[A-Z][A-Za-z ]+"
```

These declarations would surface any H2 headings in existing RFC and ADR documents
that deviate from the established conventions.

---

## 7. Alternatives Considered

1. **Extend STWD-019 with a `level` parameter rather than introducing STWD-020.**
   Rejected. STWD-019 is named and described as a *title* pattern rule — its rule ID,
   description, and diagnostic messages are specific to document titles (H1). Extending
   it with a level parameter would conflate two structurally different concerns and
   make the rule description misleading. H2 headings are section markers, not document
   titles; a distinct rule is cleaner and independently queryable via rule suppression
   and reporting.

2. **Per-section patterns (`section_patterns` as a name-to-regex map) instead of a
   uniform pattern.** Rejected for this RFC. The key semantics (exact match vs. regex
   match, missing-section behavior, multi-match behavior) introduce significant design
   scope beyond what is needed for the primary use cases. Uniform enforcement covers
   the cases already observed in practice. Per-section patterns are noted as a future
   enhancement (Section 8).

3. **Apply `section_pattern` to all heading levels (H2, H3, H4, …) uniformly.**
   Rejected. H3+ heading conventions are typically subsection-specific and less likely
   to have uniform format requirements across a whole family. Scoping to H2 keeps the
   feature well-defined and avoids emitting diagnostics for deeply nested subsections
   that authors do not intend to format uniformly. H3+ support is deferred (Section 8).

4. **Warning vs. Error severity.** Warning is correct for the same reason as
   `title_pattern`: heading format is a convention that may vary for edge-case sections
   (introductory sections, appendices) and is not a structural integrity failure.
   Error severity would make adoption risky for families with any organic heading
   variation. Warning allows incremental adoption.

---

## 8. Out of Scope

| Item | Notes |
|---|---|
| H3+ heading format enforcement | `section_pattern` is H2-only; deeper level enforcement is a separate concern |
| Per-section patterns (map from section name to format regex) | Requires design of key-matching semantics; deferred to a future RFC |
| Automatic H2 correction | Section heading text requires human judgment; not safe to automate |
| H2 heading presence enforcement | Covered by `required_sections` (STWD-014); outside this rule's scope |
| Case-insensitive pattern matching option | Not observed as a need in practice; can be added via inline `(?i)` flag in the pattern if required |

---

## 9. Consequences

- The artifact family governance surface gains coverage over H2 section heading format,
  completing the symmetry: filename (`naming_pattern`), document title (`title_pattern`),
  H2 headings (`section_pattern`), frontmatter (`frontmatter_schema`), section presence
  (`required_sections`).
- STWD-020 is a new, narrowly scoped rule. It is independently suppressible and
  independently configurable per family.
- Repositories with H2 format conventions in any governed family can declare those
  conventions in policy and detect deviations via `steward check`.
- `config validate` gains a new regex compilation check for `section_pattern`, parallel
  to the existing check for `title_pattern`.
