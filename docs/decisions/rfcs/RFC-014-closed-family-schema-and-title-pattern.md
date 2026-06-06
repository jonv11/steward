---
type: rfc
status: Proposed
description: Extends artifact family schemas with closed-field validation, deprecated-field migration, and per-family H1 title-pattern enforcement to make family schemas authoritative rather than merely additive
resolves: >-
  Frontmatter drift from undeclared fields accumulating silently in governed
  families, deprecated field names persisting without migration guidance, and
  H1 title-format conventions being unenforced despite consistent practical use
last_updated: 2026-06-06
---

# RFC-014: Closed Artifact Family Schema and Title Convention Enforcement

---

## 1. Context

RFC-008 introduced `artifact_families` with `frontmatter_schema.required` and
`frontmatter_schema.allowed_values` as the foundation for per-family governance.
STWD-003 enforces these requirements: it fires when required fields are absent and
when present values fall outside the declared vocabulary.

This model is **additive**: it validates fields that are declared, but does not
constrain which additional fields a document may carry. A file may include any
number of undeclared frontmatter keys and no rule will flag them.

Similarly, RFC-008 introduced `naming_pattern` (enforced by STWD-016) to constrain
filenames within a family. There is no parallel mechanism for constraining the H1
heading text of matched documents, despite H1 format conventions being consistently
applied across the ADR and RFC families.

### What works today

- `frontmatter_schema.required` flags missing fields (STWD-003, Error).
- `frontmatter_schema.allowed_values` flags out-of-vocabulary field values (STWD-003, Error).
- `naming_pattern` enforces filename format per family (STWD-016, Warning).
- `required_sections` enforces the presence of named headings per family (STWD-014, Warning).

### What does not work today

- There is no way to declare the *complete* set of valid frontmatter fields for a
  family. Undeclared fields accumulate silently.
- There is no way to declare that a field name is deprecated in favor of a
  replacement. Old names persist indefinitely with no tooling guidance.
- There is no way to enforce H1 heading format per family. Conventions such as
  `ADR-NNN: Title` exist but are unenforced.

### Evidence from self-dogfooding

Analysis of the Steward repository's own governed documents surfaces three categories
of drift that the current model cannot detect:

1. **Undeclared field accumulation.** Planning documents carry inconsistent optional
   fields across files in the same family: `document_id` and `version` appear in
   `milestone-plan.md`, `version` alone in `implementation-instructions.md` and
   `curation-notes.md`, `source_baseline` in `release-process.md` and
   `pre-1-0-readiness-plan.md`, and none of these in `workflow-guide.md`. All are
   valid per STWD-003 today. The policy cannot express which, if any, of these fields
   actually belong to the planning family schema.

2. **Deprecated field persistence.** `date:` appears as a frontmatter key in two
   documents despite `last_updated:` being the canonical auto-maintained date field
   declared in `governance.frontmatter.auto_fields`. These `date` fields are inert —
   they will never be auto-maintained — and there is no mechanism to detect or migrate
   them.

3. **Unenforced H1 conventions.** ADR documents consistently use `# ADR-NNN: Title`;
   RFC documents use `# RFC-NNN: Title`. These are correct in practice but entirely
   unenforced. A document with the right filename and frontmatter but a malformed H1
   (missing the ID prefix, wrong format) passes all current checks without complaint.

---

## 2. Problem Statement

The artifact family schema is currently **open-ended by construction**: it validates
what it knows about, but cannot distinguish between fields that belong to a family's
intentional schema and fields that accumulated through convention drift or copy-paste.
This has three concrete consequences:

1. **Invisible schema drift.** A field like `date` instead of `last_updated`, or
   `document_id` in a planning doc that was not intended to carry one, is silently
   accepted. Steward cannot report that the document deviates from its family's
   intended schema.

2. **No migration path for deprecated fields.** When a field is renamed, there is no
   mechanism to detect files still using the old name, no machine-readable declaration
   of the rename, and no auto-fix path. Stale fields require manual inspection to find.

3. **Unenforced H1 conventions.** The filename of an ADR is constrained by
   `naming_pattern` (STWD-016); its frontmatter is constrained by `frontmatter_schema`
   (STWD-003). But the H1 heading — which by convention carries the document's
   identifier and title — is free-form. The two enforced surfaces (filename,
   frontmatter) give no guarantee about the third (document title).

---

## 3. Proposed Capability Areas

### 3.1 `allowed_fields` — Closed Frontmatter Schema

A new optional key in `frontmatter_schema` that declares the complete set of valid
frontmatter field names for a family:

```yaml
artifact_families:
  - family: planning
    display_name: Planning Document
    match:
      path_pattern: "docs/planning/*.md"
    frontmatter_schema:
      required: [type, status, description]
      allowed_values:
        type: [planning]
        status: [Draft, Active, Completed, Superseded]
      allowed_fields: [type, status, description]
```

**Semantics:**

- When `allowed_fields` is absent, behavior is unchanged — the schema remains open-ended and backward-compatible.
- When `allowed_fields` is present, any frontmatter key in a matched document that is not in the list emits a STWD-003 diagnostic at Warning severity.
- The check is additive to existing required/allowed-values enforcement. All three constraints are evaluated independently for each file.
- `allowed_fields` does not need to repeat the `required` list; both lists are consulted independently. A field in `required` that is absent from `allowed_fields` is a policy authoring error and should be detected by `config validate` (see Section 4).
- Fields declared in `governance.frontmatter.auto_fields` are implicitly considered allowed in every family and do not need to appear in `allowed_fields`. This prevents Steward's own auto-maintenance from generating false-positive violations and decouples family schemas from global governance field names.

**Auto-fix behavior:**

STWD-003's `IFixableRule` implementation does not remove fields (non-destructive
by design). When `allowed_fields` violations are detected, the fix suggestion is a
remediation message naming the unexpected field and advising manual removal or
migration. Automated removal of undeclared frontmatter fields is not safe without
human review: an undeclared field may be meaningful to external tooling operating
outside Steward's governance model.

**Why Warning, not Error:**

Undeclared fields are governance imprecision, not broken governance. Error severity
would make `allowed_fields` a breaking change for any repository with organic
frontmatter growth — the normal state before schema tightening. Warning lets repos
adopt closed schemas incrementally, flagging drift without blocking check pipelines.

---

### 3.2 `deprecated_fields` — Guided Field Migration

A new optional key in `frontmatter_schema` that maps deprecated field names to their
canonical replacements:

```yaml
artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/decisions/adrs/ADR-*.md"
    frontmatter_schema:
      required: [type, status, category, description]
      allowed_values:
        type: [adr]
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
      deprecated_fields:
        date: last_updated
        document_id: ~
```

**Semantics:**

- When a matched document contains a field named in `deprecated_fields` with a
  non-null replacement, STWD-003 emits a Warning: `"Frontmatter field 'date' is
  deprecated in family 'adr'; use 'last_updated' instead."` The diagnostic includes
  the replacement name in its `details` dict:
  `{ "deprecatedField": "date", "replacementField": "last_updated" }`.
- When the replacement value is `null` (YAML `~`), the field is deprecated with no
  replacement. STWD-003 emits a Warning: `"Frontmatter field 'document_id' is
  deprecated in family 'adr' and should be removed."` The `details` dict is:
  `{ "deprecatedField": "document_id", "replacementField": null }`.
- If the deprecated field and its non-null replacement field are **both** present in
  the same document, severity escalates to Error: coexistence is unambiguous
  duplication that cannot be safely auto-resolved.
- A field listed in `deprecated_fields` does not trigger an `allowed_fields`
  unexpected-field warning when `allowed_fields` is also declared. The deprecation
  check takes precedence. Non-null replacement fields should appear in `allowed_fields`.

**Auto-fix behavior:**

Deprecated field changes are mechanically safe and deterministic. STWD-003 fix
implementation for deprecated fields will:

1. Read the current value of the deprecated field.
2. Remove the deprecated field from frontmatter.
3. If the replacement is non-null: insert the replacement field with the same
   value — unless the replacement is already present, in which case escalate to
   Error and skip the fix.
4. If the replacement is null: removal only; no field is inserted.

This makes deprecated field migration a first-class fixable operation:
`steward check --fix --apply` can batch-rename or batch-remove deprecated fields
across an entire governed directory with a single invocation.

---

### 3.3 `title_pattern` — H1 Heading Format Enforcement

A new optional key at the family root — parallel to `naming_pattern` — that enforces
the H1 heading text format for matched documents:

```yaml
artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/decisions/adrs/ADR-*.md"
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    title_pattern: "^ADR-[0-9]{3}: .+"
    frontmatter_schema:
      required: [type, status, category, description]

  - family: rfc
    display_name: Request for Comments
    match:
      path_pattern: "docs/decisions/rfcs/RFC-*.md"
    naming_pattern: "^RFC-[0-9]{3}-[a-z0-9-]+(?:-draft)?\\.md$"
    title_pattern: "^RFC-[0-9]{3}: .+"
    frontmatter_schema:
      required: [type, status, resolves]
```

**Semantics:**

- Enforced by a new rule **STWD-019** at Warning severity.
- "H1 heading" means the first level-1 heading in the document (`# Heading`). This
  is the document title by convention in all Steward-governed families.
- If no H1 is present in the document, STWD-019 does not fire — absent H1 is a
  separate structural concern outside this rule's scope.
- The pattern is a .NET-compatible regular expression. `config validate` rejects
  invalid regex values and emits an Error on invalid pattern declarations.
- Pattern matching is case-sensitive by default. H1 format conventions in the ADR
  and RFC families are case-sensitive in practice.

**Relationship to `naming_pattern` (STWD-016):**

`naming_pattern` enforces the filename; `title_pattern` enforces the document title.
They are independent and can be declared together or separately. The typical usage
is to declare both for families where the filename and H1 are expected to carry
corresponding identifiers — as in ADRs and RFCs.

**Auto-fix:**

STWD-019 is not auto-fixable. The H1 text is human-authored and may require editorial
judgment to correct. The diagnostic includes a remediation message showing the expected
pattern and the actual H1 found.

---

## 4. Rule and Config Changes Summary

**Rule changes:**

| Rule | Change | Default Severity |
|---|---|---|
| STWD-003 | Extended: detect undeclared fields when `allowed_fields` is present; detect and auto-fix deprecated fields when `deprecated_fields` is present | Warning (unexpected fields); Warning / Error (deprecated fields — Error when both old and replacement coexist) |
| STWD-019 *(new)* | `title_pattern` enforcement: H1 heading text of family-matched files must match the declared regex | Warning |

**`config validate` extensions:**

- Reject `allowed_fields` lists where any field in `frontmatter_schema.required` is
  absent from `allowed_fields`. A required field that is simultaneously disallowed is
  a schema contradiction.
- Reject `title_pattern` values that fail .NET regex compilation.
- Reject when `deprecated_fields` names a non-null replacement field that is absent
  from `allowed_fields` (when `allowed_fields` is also declared). Running `--fix` in
  this state would auto-rename the deprecated field to its replacement, which would
  immediately trigger a new unexpected-field warning — an invalid configuration that
  cannot be safely executed. Null replacements (removal-only) are exempt from this
  check.
- Warn when `deprecated_fields` and `required` both name the same field — a field
  that is simultaneously required and deprecated cannot be consistently satisfied.

**STWD-003 description update:**

The rule description should be broadened from `"Required frontmatter fields must be
present in Markdown files"` to `"Frontmatter must satisfy the declared family schema"`,
reflecting that STWD-003 now governs required fields, controlled vocabularies,
unexpected fields, and deprecated fields as a unified frontmatter governance surface.

---

## 5. Backward Compatibility

All three capabilities are strictly opt-in:

- `allowed_fields` absent → no change in behavior.
- `deprecated_fields` absent → no change in behavior.
- `title_pattern` absent → STWD-019 never fires for that family.

Repositories not using any of these keys behave identically to the behavior
established by RFC-008 and its v0.13.0 scope. Existing policy configurations require
no changes to remain valid.

---

## 6. Self-Dogfooding Configuration Changes Enabled

When this RFC is implemented, the Steward repository's own `policy.yaml` should be
updated to adopt all three capabilities. Concrete expected additions:

**`allowed_fields` for planning family** — surfaces the inconsistent optional fields
currently scattered across planning documents:

```yaml
- family: planning
  frontmatter_schema:
    allowed_fields: [type, status, description]
```

(`last_updated` is declared in `governance.frontmatter.auto_fields` and is implicitly
allowed in all families; it does not need to appear in `allowed_fields`.)

**`deprecated_fields` for families that used `date`** — detects and auto-migrates
the two documents currently using `date` instead of `last_updated`. The planning
family also uses null-replacement to remove the spurious `document_id` and `version`
fields that appear inconsistently across planning documents with no canonical
equivalent:

```yaml
- family: adr
  frontmatter_schema:
    deprecated_fields:
      date: last_updated

- family: review
  frontmatter_schema:
    deprecated_fields:
      date: last_updated

- family: planning
  frontmatter_schema:
    deprecated_fields:
      document_id: ~
      version: ~
```

**`title_pattern` for ADR and RFC families** — enforces the established H1
identifier-prefix convention:

```yaml
- family: adr
  title_pattern: "^ADR-[0-9]{3}: .+"

- family: rfc
  title_pattern: "^RFC-[0-9]{3}: .+"
```

---

## 7. Alternatives Considered

1. **Make `allowed_fields` violations an Error rather than Warning.** Rejected. Error
   severity would make adoption risky for repositories with organic frontmatter growth,
   which is the normal state before a schema tightening pass. Warning allows incremental
   adoption without breaking check pipelines.

2. **Make `deprecated_fields` advisory only (no auto-fix).** Rejected. Deprecated
   field renames are mechanically safe and deterministic. Auto-fix with dry-run is the
   highest-leverage use of the fix machinery and eliminates the manual grep-and-edit
   cycle entirely.

3. **Enforce H1 format via `required_sections` rather than a new `title_pattern` key.**
   Rejected. `required_sections` (STWD-014) checks for the *presence* of headings by
   matching heading text as a literal string. It cannot enforce a format *pattern* on
   the H1 specifically — it would require putting the exact expected H1 string in
   `required_sections`, conflating "this section must exist" with "the document title
   must match this format." A separate key with regex semantics is cleaner and more
   expressive.

4. **Introduce `forbidden_fields` rather than `allowed_fields`.** Rejected.
   `forbidden_fields` works for a small number of known-bad fields but does not
   express a complete schema. `allowed_fields` expresses a positive closed contract,
   which is more useful for drift detection and more understandable in policy review.
   Knowing what *is* allowed is more actionable than knowing only what is not.

5. **Implement `allowed_fields` as a new rule (STWD-019) rather than extending
   STWD-003.** Rejected. Frontmatter governance is a unified concern. STWD-003 already
   owns required fields and allowed values; extending it to cover allowed fields and
   deprecated fields keeps all frontmatter schema enforcement in one rule and one
   diagnostic category. A separate rule would split related diagnostics across two
   rule IDs for no architectural gain.

---

## 8. Consequences

- Artifact family schema moves from "partially constrained" to "fully constrainable."
  Repositories that want strict governance over document shape have a complete set of
  tools to achieve it without reaching outside the Steward model.
- STWD-003 gains responsibility for two new diagnostic categories. Its description
  should be broadened to reflect its role as the unified frontmatter governance rule.
- `config validate` gains new cross-key consistency checks. These are additive and
  do not change existing validation behavior.
- Repositories adopting `allowed_fields` must enumerate their fields explicitly in
  policy. This is a one-time authoring cost with ongoing drift protection.
- `steward check --fix --apply` becomes useful for frontmatter migration workflows:
  deprecated fields can be batch-renamed across a governed directory with a single
  command.
- The symmetry between `naming_pattern` (filename) and `title_pattern` (document
  title) makes the family schema model more complete: filename, frontmatter, sections,
  and document title are all governable surfaces for a matched family.

---

## 9. Out of Scope

| Item | Notes |
|---|---|
| Field type constraints (date, boolean, number, list) | RFC-008 deferred this; still not scheduled |
| Regex-based field value constraints | RFC-008 deferred this; still not scheduled |
| Cross-field constraints (if field A is present, field B must be too) | Requires a new conditional schema model; not designed |
| H2+ heading format enforcement | `title_pattern` is H1-only; section heading format is a separate concern |
| Automatic H1 correction | H1 text requires human judgment to correct; not safe to automate |
| Global (cross-family) deprecated-field declarations | Deprecated fields are per-family; no global registry in this RFC |
| Enforcement of the `---` horizontal rule separator after H1 | Structural presentation convention; too minor for a dedicated rule |
