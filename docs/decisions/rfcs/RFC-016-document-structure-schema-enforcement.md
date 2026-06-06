---
type: rfc
status: Accepted
description: Extends artifact family schemas with a section_schema block that declares required and optional H2 sections, complementing STWD-014 required_sections and STWD-020 section_pattern
resolves: >-
  Document families such as RFC, ADR, and PRD have implicit structural templates (which sections must exist,
  in what order) with no policy mechanism to enforce them; coding agents and contributors have no
  machine-checkable contract for document completeness
last_updated: 2026-06-06
---

# RFC-016: Document Structure Schema Enforcement

---

## 1. Context

Steward's artifact family governance currently covers:

- Filename format — `naming_pattern` (STWD-016)
- Frontmatter field presence and vocabulary — `frontmatter_schema` (STWD-003)
- H1 title format — `title_pattern` (STWD-019)
- H2 heading format — `section_pattern` (STWD-020, RFC-015)
- Named section presence — `required_sections` (STWD-014)

RFC-015 completed H2 *format* enforcement. What still cannot be expressed in policy is a document *template*: an ordered declaration of which H2 sections are required, which are optional, and (optionally) whether the section order must be maintained. This gap means that PRD, RFC, and ADR families have conventional structural templates that are written down in contributing guides but not enforced by Steward.

### What STWD-014 (`required_sections`) does not cover

`required_sections` is a flat list of heading names that must be present at any heading level (H1–H6). It cannot express:

- Which sections are optional vs. required
- The expected ordering of sections
- An exhaustive template (flagging sections not in the schema)

### What `section_pattern` (STWD-020) does not cover

`section_pattern` validates the *format* of all H2 headings via a regex. It cannot express which specific sections must appear; it only asserts that headings that do appear conform to a format.

---

## 2. Problem Statement

Document families with structural templates have no machine-checkable contract for document completeness or structure. Three consequences:

1. **Required sections go missing undetected.** An RFC without a "Problem Statement" section or a PRD without a "Goals" section passes all current checks. A coding agent completing a document skeleton can omit required sections and receive no feedback from `steward check`.

2. **No ordered structure enforcement.** An RFC that presents "Alternatives Considered" before "Problem Statement" passes all checks. Order conventions are documented but not validated.

3. **Template-based document families are first-class contributors cannot verify.** Contributors relying on `steward check` to validate their documents get no signal about structural completeness beyond presence of individual sections.

---

## 3. Proposed Capability

### 3.1 `section_schema` — Document Structure Template Enforcement

A new optional block at the artifact family root that defines the document template:

```yaml
artifact_families:
  - family: rfc
    section_schema:
      heading_match: contains   # "exact" | "contains" — default "contains"
      enforce_order: false      # require sections appear in schema order — default false
      allow_extra: true         # allow H2s not in the schema — default true
      sections:
        - heading: "Context"
          required: true
        - heading: "Problem Statement"
          required: true
        - heading: "Proposed"
          required: true
        - heading: "Alternatives Considered"
          required: false
        - heading: "Out of Scope"
          required: false
        - heading: "Consequences"
          required: false

  - family: adr
    section_schema:
      sections:
        - heading: "Context"
          required: true
        - heading: "Decision"
          required: true
        - heading: "Consequences"
          required: true
```

**Semantics:**

- Enforced by a new rule **STWD-021** at Warning severity.
- `section_schema` applies to H2 headings only (consistent with STWD-020).
- `heading_match: contains` (default) — a document H2 heading satisfies a schema entry if the schema heading text is a case-insensitive substring of the actual H2 text. This handles numbered sections: schema `heading: "Context"` matches document `## 1. Context`.
- `heading_match: exact` — the schema heading must equal the H2 heading text (case-insensitive).
- `required: true` (default per entry) — a diagnostic is emitted if no H2 in the document satisfies this entry.
- `required: false` — optional; no diagnostic if missing, but position is tracked for order enforcement.
- `allow_extra: false` — any H2 not matching any schema entry emits a diagnostic.
- `enforce_order: true` — schema entries that are present in the document must appear in the order listed in `sections`. Emits one diagnostic per out-of-order section.
- If a file has no H2 headings, STWD-021 does not fire for missing-section checks (parallel to STWD-020 behavior).

### 3.2 Diagnostics

| Condition | Rule | Severity | Message form |
|---|---|---|---|
| Required section missing | STWD-021 | Warning | `Required section 'X' is missing from 'file.md' [family: rfc].` |
| Unexpected H2 (allow_extra: false) | STWD-021 | Warning | `Section 'X' in 'file.md' is not defined in the section_schema for family 'rfc'.` |
| Section out of order (enforce_order: true) | STWD-021 | Warning | `Section 'X' (line N) appears out of order in 'file.md'; schema requires it after 'Y' [family: rfc].` |

---

## 4. Rule and Config Changes Summary

**Rule changes:**

| Rule | Change | Default Severity |
|---|---|---|
| STWD-021 *(new)* | `section_schema` enforcement: required sections must be present, extra sections flagged if configured, section order enforced if configured | Warning |

**`config validate` extensions:**

- Reject `section_schema.heading_match` values other than `"exact"` or `"contains"`.
- Warn if `section_schema.sections` is null or empty.

**No changes to existing rules:** STWD-014, STWD-020, and STWD-019 are unchanged.

---

## 5. Backward Compatibility

Strictly opt-in:

- `section_schema` absent → STWD-021 never fires for that family.
- `enforce_order: false` (default) → no order enforcement unless explicitly enabled.
- `allow_extra: true` (default) → extra sections are silently allowed.

Repositories not using `section_schema` behave identically to current behavior.

---

## 6. Relationship to Existing Rules

| Rule | What it checks | Relationship to STWD-021 |
|---|---|---|
| STWD-014 `required_sections` | Named headings present, any level, case-insensitive | Complementary. STWD-014 checks any level; STWD-021 is H2-only and adds optional/order semantics. Both can coexist. |
| STWD-020 `section_pattern` | Format of all H2 headings via regex | Orthogonal. STWD-020 validates heading format; STWD-021 validates which headings exist. |
| STWD-019 `title_pattern` | H1 heading format | Orthogonal. STWD-021 does not touch H1. |

---

## 7. Alternatives Considered

1. **Extend STWD-014 with `required: false` and ordering.** STWD-014 is a flat list of strings with any-level semantics and no ordering concept. Extending it would require a breaking config change and conflate two different mental models (any-level presence vs. H2-structured template). A new key on the family definition is cleaner.

2. **Template files instead of inline schema.** Require a template `.md` file per family and diff against it. Rejected: too prescriptive about content, harder to configure, and couples validation to file-system lookup.

3. **Integrate section_schema into `section_pattern`.** `section_pattern` is a single regex applied uniformly. A named-section schema is structurally different. Conflating them would make `section_pattern` semantics ambiguous.

4. **Per-section patterns (map from section name to format).** Also considered for this RFC. Deferred — RFC-015 already deferred per-section format patterns. `section_schema` addresses structural presence and order, which is the higher-value gap.

---

## 8. Out of Scope

| Item | Notes |
|---|---|
| H3+ section validation | `section_schema` is H2-only; subsection templates are out of scope |
| Auto-fix for missing sections | Requires generating section content; not safe to automate |
| Section content validation | Only heading presence and order are checked |
| Cross-family schema inheritance | Each family defines its own schema independently |

---

## 9. Self-Dogfooding Configuration Changes Enabled

When STWD-021 is implemented, `.steward/policy.yaml` may adopt `section_schema` for the RFC and ADR families:

```yaml
- family: rfc
  section_schema:
    heading_match: contains
    sections:
      - heading: "Context"
        required: true
      - heading: "Problem Statement"
        required: true
      - heading: "Proposed"
        required: true
      - heading: "Alternatives Considered"
        required: false
      - heading: "Consequences"
        required: false

- family: adr
  section_schema:
    sections:
      - heading: "Context"
        required: true
      - heading: "Decision"
        required: true
      - heading: "Consequences"
        required: true
```
