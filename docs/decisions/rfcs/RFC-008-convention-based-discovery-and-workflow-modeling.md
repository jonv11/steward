---
type: rfc
status: Proposed
resolves: >-
  Convention-based artifact discovery, hierarchical path rules, frontmatter-driven
  classification, workflow/session modeling, and policy scalability gaps identified
  in maintainer review
---

# RFC-008: Convention-Based Artifact Discovery and Workflow Modeling

---

## 1. Context

Maintainer review of the Steward CLI and its self-dogfooding configuration surfaced several structural limitations in the current policy/configuration model. These limitations fall into two categories:

1. **Artifact discovery is file-by-file rather than convention-based.** Every artifact that Steward should recognize must currently be individually listed in `policy.yaml`. This does not scale: adding a new ADR, RFC, audit, or planning document requires a corresponding new `policy.yaml` entry for it to participate in governance.

2. **Workflow and session modeling has no configuration surface.** Common repository workflows (coding sessions, doc creation sessions, proposal flows) are implied by repo conventions but have no Steward-native representation.

### What works today

- `frontmatter_requirements` with path patterns can enforce frontmatter on document families (ADRs, RFCs, planning docs) without per-file entries. This is the closest existing mechanism to convention-based governance.
- `path-policy` provides path-based rules for naming, category, and structure.
- Artifact roles, importance levels, and role-linked defaults provide semantic classification.
- ADR-012 (accepted) establishes the direction for per-type artifact definitions.

### What does not work today

- There is no way to declare "all `.md` files under `docs/decisions/adrs/` are ADR artifacts" without listing each one.
- There is no way to auto-classify artifacts by path convention or frontmatter `type` field.
- There is no way to declare that a directory should contain at least N artifacts of a given family.
- There is no way to model repository workflows or session types in Steward configuration.
- `steward status`, `steward orient`, and `steward check` only see files explicitly declared as artifacts in `policy.yaml`; undeclared files in governed directories are invisible to artifact-level governance.

### Relationship to ADR-012

ADR-012 accepted the direction of per-type artifact definitions. This RFC proposes the specific design and extends the scope to include:
- Convention-based discovery (path patterns + frontmatter matching)
- Artifact family rules (directory-level expectations)
- Workflow/session modeling (new capability area)

---

## 2. Problem Statement

The current `policy.yaml` artifact model requires explicit per-file registration. This creates several problems:

1. **Brittle scaling.** Every new ADR, RFC, audit, or planning doc requires a manual `policy.yaml` update.
2. **Governance gaps.** Files added without a corresponding policy entry are invisible to artifact-level governance even if they are in a governed directory with correct frontmatter.
3. **Redundant configuration.** The same role, importance, and frontmatter expectations are repeated across entries that share a common convention.
4. **No workflow guidance.** Steward cannot guide contributors through standard repo workflows (coding session, doc creation, proposal).

---

## 3. Proposed Capability Areas

### 3.1 Artifact Family Definitions

A new `artifact_families` section in `policy.yaml` that declares reusable artifact families with convention-based matching:

```yaml
artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/decisions/adrs/ADR-*.md"
      frontmatter:
        type: adr
    role: governance
    importance: recommended
    frontmatter_schema:
      required: [type, status, category]
      allowed_values:
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
    naming_pattern: "ADR-{NNN}-{slug}.md"

  - family: rfc
    display_name: Request for Comments
    match:
      path_pattern: "docs/decisions/rfcs/RFC-*.md"
      frontmatter:
        type: rfc
    role: governance
    importance: recommended
    frontmatter_schema:
      required: [type, status, resolves]
      allowed_values:
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
    naming_pattern: "RFC-{NNN}-{slug}.md"

  - family: audit
    display_name: Audit Report
    match:
      path_pattern: "docs/audits/*.md"
    role: audit
    importance: optional
```

**Matching semantics:**
- `path_pattern`: Glob pattern. Files matching this pattern are candidates.
- `frontmatter.type`: If specified, the file must also have this frontmatter value.
- Both criteria can be combined (AND logic).
- Explicit `artifacts` entries always take precedence over family matches.

**Benefits:**
- New ADRs/RFCs/audits are automatically governed without policy changes.
- Frontmatter schema is declared once per family, not repeated per file.
- `steward status` and `steward check` can report family-level completeness.

### 3.2 Directory Expectations

Artifact families can declare directory-level expectations:

```yaml
artifact_families:
  - family: adr
    # ... matching and schema as above ...
    directory_expectations:
      min_count: 1
      description: "At least one ADR should exist to record architectural decisions"
```

This enables `steward check` to report when an expected artifact family has no instances, without requiring specific file paths.

### 3.3 Artifact Type Schema Integration

This RFC extends ADR-012's artifact type schema direction with concrete design:

- `frontmatter_schema` per family replaces scattered `frontmatter_requirements` entries.
- `naming_pattern` per family extends STWD-010 naming enforcement.
- `required_sections` per family (future) enables section-presence validation.

The existing `frontmatter_requirements` mechanism in `validation` remains supported as a lower-level fallback but should be considered secondary to family-level schema declarations.

### 3.4 Workflow Definitions (Future Direction)

A new `workflows` section to model repository session types:

```yaml
workflows:
  coding_session:
    description: Standard coding workflow
    steps:
      - check_git_clean
      - plan_changes
      - create_docs_if_needed
      - implement
      - test
      - commit_conventional

  doc_creation:
    description: Document creation workflow
    steps:
      - check_for_duplicate
      - classify_document_type
      - route_open_question_to_rfc
      - route_decision_to_adr
      - update_planning_artifacts

  proposal:
    description: New proposal or open question
    steps:
      - search_existing
      - create_rfc_if_new
      - update_decision_index
```

**Note:** Workflow definitions are advisory and for tooling guidance. They do not enforce execution order but provide discoverability for humans and agents about the intended workflow.

**Current limitation:** Steward has no workflow execution engine. This section defines the configuration surface; the execution and guidance capabilities require follow-up implementation work.

---

## 4. Migration Path

### Phase 1 (achievable with current Steward)
- Use `frontmatter_requirements` for path-pattern-based frontmatter enforcement.
- Keep explicit `artifacts` entries for structurally required files.
- Remove per-file entries for document families that share common conventions.

### Phase 2 (requires Steward enhancement)
- Implement `artifact_families` in policy schema and discovery engine.
- Update `steward status`, `check`, `orient`, and `explain` to recognize family-matched artifacts.
- Add family-level completeness reporting.

### Phase 3 (requires Steward enhancement)
- Implement `workflows` configuration surface.
- Add `steward workflow` command or integrate workflow guidance into existing commands.
- Add directory expectations validation.

---

## 5. Impact on Existing Configuration

- Existing `artifacts` entries continue to work unchanged.
- Existing `frontmatter_requirements` continue to work unchanged.
- New capabilities are additive and opt-in.
- Repositories not using artifact families behave identically to today.

---

## 6. Alternatives Considered

1. **Extend `artifacts` with glob patterns.** Simpler but conflates explicit artifact declarations with convention-based discovery. Families are a cleaner abstraction.
2. **External plugin system for discovery.** Over-engineered for the current need. Convention-based rules in policy.yaml are sufficient and more maintainable.
3. **Infer everything from directory structure without configuration.** Too implicit. Repositories vary too much in structure; explicit family declarations provide clarity and intentionality.

---

## 7. Consequences

- `policy.yaml` evolves from a file registry toward a policy-definition artifact.
- The number of explicit `artifacts` entries should decrease as families handle recurring patterns.
- New document instances in governed directories participate in governance automatically.
- Workflow modeling becomes a first-class configuration concern.
- Implementation requires changes to the discovery engine, validation engine, and multiple command surfaces.
- This RFC should be scheduled for implementation when artifact-type-schema work begins (per ADR-012).
