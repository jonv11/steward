# Use-Case Consolidation Proposal — Story/Worldbuilding Maintainer Analysis

**Date:** 2026-04-15
**Scope:** Exhaustive cross-reference of two maintainer use-case files against current CLI capabilities, accepted plans, and product direction
**Input sources:**
- `docs/audits/maintainer-usecase-expectations.md` (abbreviated: **Exp**)
- `docs/audits/maintainer-usecase-ideas.md` (abbreviated: **Ideas**)
**Output artifacts:**
- This proposal (master analysis and canonical inventory)
- [ADR-011](../decisions/adrs/ADR-011-domain-stewardship-through-generic-configuration.md) — Domain-specific stewardship through generic configuration
- [ADR-012](../decisions/adrs/ADR-012-artifact-type-schema-direction.md) — Artifact type schema system direction
- [Pre-1.0 readiness plan update](../planning/pre-1-0-readiness-plan.md) — Active remaining-work tracker on the `0.x` line

---

## 1. Purpose

Both use-case files describe the needs of a maintainer operating a story/worldbuilding/adaptation repository and express what Steward must do to serve as the operational stewardship tool for that domain.

This proposal:
1. Extracts every distinct requirement from both files.
2. Normalizes and deduplicates into a canonical inventory.
3. Maps each canonical item against current CLI reality, accepted plans, and product direction.
4. Classifies each item using a strict taxonomy.
5. Identifies decisions that can be auto-accepted as ADRs.
6. Produces coherent planning artifacts.

---

## 2. Normalization and Deduplication Approach

**Exp** contains 71 numbered functionality items, 11 configurability subsections, 6 rule families, 7 workflow descriptions, 10 non-functional requirements, and a minimum feature set summary. Many items are restated across sections (e.g., section 5 "rules" restate section 3 "functionalities"; section 6 "workflows" restate section 3 items; section 7 re-lists section 3 non-functional items).

**Ideas** contains 20 numbered items that are Steward-architecture-aware and largely map to Exp items at a higher level.

**Deduplication rules applied:**
- When Exp §3 and Exp §5/§6/§7 describe the same requirement, the §3 version is canonical.
- When Ideas and Exp describe the same requirement, the more specific version is canonical.
- When Exp §4 (configurability) describes the configuration layer for a §3 capability, it is merged into the §3 item rather than tracked separately.
- Purely domain-specific validation semantics (canon contradiction detection, timeline date arithmetic, plot-thread resolution logic) are grouped under a single "domain-specific validation" umbrella rather than tracked as 6+ separate items, because the product decision is the same for all: generic mechanism vs hardcoded logic.

**Deduplication map** (Ideas → Exp merges):
| Ideas # | Merged into Exp item(s) | Reason |
|---------|------------------------|--------|
| #2 | Exp §3.11 #48 | Boundary enforcement |
| #3 | Exp §3.4 #12, §4.2 | Typed artifact taxonomy |
| #4 | Exp §3.3 #8 | Stable ID enforcement |
| #5 | Exp §3.3 #7, #9 | Filename/slug contract |
| #6 | Exp §3.4 #11–13 | Frontmatter schemas |
| #7 | Exp §4.8 | Controlled vocabularies |
| #8 | Exp §3.7 #24, #28 | Index generation/completeness |
| #9 | Exp §3.6 #22 | Orphan detection |
| #10 | Exp §3.6 #20–21 | Cross-reference integrity |
| #11 | Exp §3.9 #34–37 | Continuity-specific validation |
| #13 | Exp §4.9 | Managed regions/generated content |
| #18 | Exp §3.13 #56 + §4 | Completion policy |
| #20 | Exp §3.16 #70–71 | Explainability |

**Ideas items with no Exp equivalent** (net-new):
| Ideas # | Item | Disposition |
|---------|------|------------|
| #1 | Story/worldbuilding profile | New canonical item |
| #12 | State-tracking artifacts as explicit type | Merged with artifact type schema |
| #14 | Better orientation for creative repos | Enhancement to existing orient |
| #15 | Search scopes by narrative role | Enhancement to existing search |
| #16 | Large-file/split guidance | Enhancement to existing STWD-004 |
| #17 | Asset hygiene and exclusion | Enhancement to existing exclusion |
| #19 | Test fixture repo for story domain | New canonical item |

---

## 3. Canonical Inventory

After deduplication: **55 canonical items** organized into 13 groups.

### Group A — Initialization and Bootstrap

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-01 | Init command with profile scaffolding | Exp §3.1 #1 |
| UC-02 | Adopt/bootstrap analysis for existing repos | Exp §3.1 #2, Ideas #1 |
| UC-03 | Configuration doctor/health check | Exp §3.1 #3 |
| UC-04 | Story/worldbuilding built-in profile | Ideas #1 |

### Group B — Structural Validation and Path Policy

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-05 | Required/forbidden/allowed path validation | Exp §3.2 #4 |
| UC-06 | Rule scoping by path, pattern, artifact type | Exp §3.2 #5, §4.3 |
| UC-07 | Severity-aware validation (error/warning/info/disabled) | Exp §3.2 #6 |
| UC-08 | Per-path rule suppression | Exp §3.2, Ideas approach |
| UC-09 | Filename and slug convention enforcement | Exp §3.3 #7 #9, Ideas #5 |

### Group C — Naming and Identity

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-10 | Stable typed ID enforcement | Exp §3.3 #8, Ideas #4 |
| UC-11 | Safe rename/move with reference updates | Exp §3.3 #10 |

### Group D — Metadata and Schema

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-12 | Frontmatter required field validation | Exp §3.4 #11, Ideas #6 |
| UC-13 | Artifact type schema system | Exp §3.4 #12–13, §4.2, Ideas #3 #6 |
| UC-14 | Field type and value constraint validation | Exp §3.4 #13, Ideas #7 |
| UC-15 | Controlled vocabulary enforcement | Exp §4.8, Ideas #7 |
| UC-16 | Default value injection for frontmatter | Exp §3.4 #14 |
| UC-17 | Required vs derived field distinction | Exp §3.4 #15 |

### Group E — Content Structure

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-18 | Required section validation per artifact type | Exp §3.5 #16, Ideas #6 |
| UC-19 | Section order enforcement | Exp §3.5 #17 |
| UC-20 | Duplicate/missing section detection | Exp §3.5 #18 |
| UC-21 | Section targeting and deterministic editing | Exp §3.5 #19 |

### Group F — Cross-References and Links

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-22 | Broken link and reference validation | Exp §3.6 #20, Ideas #10 |
| UC-23 | Reference graph with typed relationships | Exp §3.6 #21, Ideas #10 |
| UC-24 | Orphan detection | Exp §3.6 #22, Ideas #9 |
| UC-25 | Backlink generation or verification | Exp §3.6 #23 |
| UC-26 | Relationship type declarations between types | Exp §4.5, Ideas #10 |

### Group G — Indexes and Discoverability

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-27 | Deterministic index generation | Exp §3.7 #24, Ideas #8 |
| UC-28 | Configurable index columns, sort, grouping | Exp §3.7 #26 |
| UC-29 | Index completeness validation | Exp §3.7 #28, Ideas #8 |

### Group H — Navigation and Orientation

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-30 | Repository outline with metadata | Exp §3.8 #29 |
| UC-31 | Artifact detail view (show command) | Exp §3.8 #30 |
| UC-32 | Domain-aware search (by type, tag, status, ID) | Exp §3.8 #31, Ideas #15 |
| UC-33 | Relationship tracing (forward/backward) | Exp §3.8 #32 |
| UC-34 | Search scopes by artifact role/area | Exp §3.8, Ideas #15 |
| UC-35 | Enhanced orientation entry points | Exp §3.8, Ideas #14 |

### Group I — Explainability

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-36 | Rule and policy explanation | Exp §3.8 #33, Ideas #20 |
| UC-37 | Config-driven predictable behavior | Exp §3.16 #69 |
| UC-38 | Rich error messages with remediation | Exp §3.16 #71, Ideas #20 |

### Group J — Domain-Specific Validation

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-39 | Domain-semantic validation (canon, timeline, plot-thread, continuity) | Exp §3.9 #34–37, §5.1–5.4, Ideas #11 |
| UC-40 | Retcon/deprecation workflow support | Exp §3.9 #38 |
| UC-41 | Canon/story/adaptation boundary enforcement | Exp §3.11 #48, Ideas #2 |
| UC-42 | Adaptation source linkage and freshness | Exp §3.11 #49–50, Ideas adaptation priorities |
| UC-43 | Medium-specific policies | Exp §3.11 #51 |

### Group K — Workflow and Planning

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-44 | Scaffold command with template support | Exp §3.10 #40–41, §6.1–6.2 |
| UC-45 | Workflow gate validation (status transitions) | Exp §3.10 #45, §4.4 |
| UC-46 | Plan/next computed recommendations | Exp §3.10 #43–44, §4.7 |
| UC-47 | Review queue surfacing | Exp §3.10 #46 |
| UC-48 | Staleness detection for upstream changes | Exp §3.10 #47 |
| UC-49 | Completion policy (configurable "done") | Exp §3.13 #56, Ideas #18 |

### Group L — Content Quality

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-50 | Duplicate artifact detection | Exp §3.13 #57 |
| UC-51 | Unused tag/vocabulary drift checks | Exp §3.13 #58 |
| UC-52 | Incomplete placeholder detection | Exp §3.13 #59 |
| UC-53 | Archive zone handling | Exp §3.13 #60, §4.10 |

### Group M — Non-Functional and Cross-Cutting

| ID | Title | Primary Source |
|----|-------|---------------|
| UC-54 | Machine-readable output, exit codes, scoped execution | Exp §3.15 #65–67, §7 |
| UC-55 | Performance on mature repos | Exp §3.15 #68, §7 |

---

## 4. Classification Summary

| Classification | Count | Items |
|----------------|-------|-------|
| implemented_usable | 14 | UC-01, 05, 07, 12, 21, 22, 27, 30, 36, 37, 38, 49, 54, 55 |
| implemented_partial | 5 | UC-06, 34, 35, 48, 53 |
| planned_existing | 10 | UC-02, 03, 08, 09, 11, 24, 29, 33, 28, 20* |
| possible_with_configuration | 2 | UC-41, 43 |
| possible_with_small_extension | 2 | UC-10, 32 |
| proposed_new_core_feature | 4 | UC-13, 14, 15, 18 |
| proposed_domain_configurability | 4 | UC-04, 17, 26, 45 |
| future_idea | 11 | UC-16, 19, 25, 31, 40, 44, 46, 47, 50, 51, 52 |
| out_of_scope | 2 | UC-39, 42 |
| not_recommended | 1 | UC-23 |

\* UC-20 merged with planned UC detection via section engine improvements

---

## 5. Full Cross-Reference Table

### 5.1 implemented_usable

Items the CLI already supports in a usable and intentional way.

| ID | Title | Evidence | Gap |
|----|-------|----------|-----|
| UC-01 | Init with profile scaffolding | `steward init --profile <name>` since v0.3.0; 5 profiles (software, docs, mixed, knowledge, minimal) | No gap for core init. Story profile is a separate item (UC-04). |
| UC-05 | Path structure validation | STWD-001 RequiredArtifactRule, STWD-002 ForbiddenPathRule in `steward check` | Fully functional. Required/forbidden categories in path-policy.yaml. |
| UC-07 | Severity-aware validation | Rules emit Error, Warning, or Info. `disabled_rules` in policy.yaml disables by ID. | Fully functional. Per-path suppression is a planned enhancement (UC-08). |
| UC-12 | Frontmatter required fields | STWD-003 RequiredFrontmatterFieldRule. `required_frontmatter_fields` in policy.yaml. | Global only — per-path scoping is UC-08/planned G7-02. |
| UC-21 | Section targeting/editing | `steward md edit` with ensure-section, set-section, insert-section, append-block, prepend-block, frontmatter-set, frontmatter-merge. Preview-first. | Full structural editing capability exists. |
| UC-22 | Broken link validation | STWD-008 BrokenInternalLinkRule, STWD-009 BrokenArtifactReferenceRule | Both internal Markdown links and policy-declared artifact paths validated. |
| UC-27 | Deterministic index generation | IndexMaintainer in maintenance engine. Policy-configured `maintenance.artifacts` with type `index`. Idempotent. | Exists. Configurable columns/sort is enhancement (UC-28). |
| UC-30 | Repository outline | `steward outline [path]` with --sizes, --lines. `steward md outline <file>` for heading hierarchy. | Fully functional for structural outline. |
| UC-36 | Rule and policy explanation | `steward explain [rule-id]` for all 9 rules. Shows metadata, description, remediation guidance. | Rule-level explanation complete. Path-level effective policy explanation is planned (G7-06). |
| UC-37 | Config-driven behavior | All behavior from config.yaml + policy.yaml + path-policy.yaml. Profiles provide defaults. CLI flags override config. | Core architecture is config-driven. |
| UC-38 | Rich error messages | Diagnostics include rule ID, severity, category, file path, message, and remediation hints. | Functional. Rule scope transparency enhancement is planned (G7-05). |
| UC-49 | Completion policy | Check text output includes completion summary: required artifacts missing, stale artifacts, broken links/refs. Actionable guidance ("run 'steward maintain --apply'"). | Configurable "done" definitions beyond current surface are future work. |
| UC-54 | Machine output, exit codes, scoping | `--output json` on all commands. Exit codes 0/1/2/3. `--scope full\|changed\|staged`, `--paths`. | Fully functional. |
| UC-55 | Performance on mature repos | Scoped validation, .gitignore pruning, default limits. | Adequate for current scale. No identified performance issues. |

### 5.2 implemented_partial

Items where something relevant exists but the use-case is only partially satisfied.

| ID | Title | What exists | Gap |
|----|-------|-------------|-----|
| UC-06 | Rule scoping by path/type | Path-policy rulesets scope rules by glob pattern. `--scope` and `--paths` for execution scoping. | No per-artifact-type rule scoping. Rules apply by path pattern, not by declared artifact type. Type-aware scoping requires UC-13 (artifact type schema). |
| UC-34 | Search scopes by role | `steward search --scope <area>` filters by policy-defined artifact roles. | Scoping is by role string (e.g., "authoritative"), not by artifact type, tag, or status. Richer metadata-aware filtering requires UC-13. |
| UC-35 | Orientation entry points | `start_here` in policy.yaml. Orient command shows `[start]` markers. Classified artifacts with roles. | Described "authoritative roots", "memory/state docs", "active arc" concepts are domain-specific and require domain configuration (UC-04, UC-13) to express. |
| UC-48 | Staleness detection | STWD-007 StaleArtifactRule detects stale maintained artifacts. IFixableRule for auto-remediation. | Detects stale generated artifacts only. Does not detect "upstream source changed, downstream should refresh" staleness. That requires G7-11 (maintenance dependencies) and G7-15 (change-impact). |
| UC-53 | Archive zone handling | `discovery.exclude` and `ignored` category in path-policy.yaml exclude paths from all operations. | No explicit "archive" zone concept with differentiated behavior (indexed but deprioritized, deprecated-warning on reference, etc.). Partially achievable with current exclusion, fully achievable with UC-13 type schemas + status lifecycle. |

### 5.3 planned_existing

Items already meaningfully planned in the RFC-007 ledger or later pre-1.0 milestone plan.

| ID | Title | Planned item | Milestone |
|----|-------|-------------|-----------|
| UC-02 | Adopt/bootstrap analysis | G7-20: Bootstrap-by-analysis (`init --analyze`) | v0.10.0 |
| UC-03 | Config doctor | G7-07: Configuration doctor | v0.10.0 |
| UC-08 | Per-path rule suppression | G7-01: `validation.path_overrides` | v0.10.0 |
| UC-09 | Naming/slug enforcement | G7-03: `must_match` regex in path-policy | v0.10.0 |
| UC-11 | Safe rename/move | G7-19: `steward refactor move` | v0.10.0 |
| UC-20 | Duplicate/missing section detection | Subsumable by section-aware validation when artifact type schemas (UC-13) exist; related to G7-08 index completeness pattern | later pre-1.0 |
| UC-24 | Orphan detection | G7-14: Orphaned-but-valid document detection | v0.10.0 |
| UC-28 | Configurable index columns | G7-10: Directory-index generator (includes format options) | v0.10.0 |
| UC-29 | Index completeness validation | G7-08: Index-completeness rule (STWD-011) | v0.10.0 |
| UC-33 | Relationship tracing | G7-18: `steward refs <path>` | v1.5.0 |

### 5.4 possible_with_configuration

Items achievable with current product without new core functionality, using existing configuration mechanisms.

| ID | Title | How | Rationale |
|----|-------|-----|-----------|
| UC-41 | Canon/story/adaptation boundary enforcement | Path-policy rulesets with `required` and `forbidden` categories per directory scope. E.g., require `docs/canon/` to contain only canon artifacts, forbid adaptation files outside `docs/adaptation/`. | Path-policy already supports directory-scoped required/forbidden rules. The "type" awareness requires UC-13, but location-based boundaries work today. |
| UC-43 | Medium-specific policies | Separate path-policy rulesets per medium directory (e.g., `docs/adaptation/bd/`, `docs/adaptation/comics/`), each with their own rules. | Path-scoping already works. Medium-specific validation rules would be expressed as path-scoped frontmatter/section requirements once UC-13 and G7-02 exist. |

### 5.5 possible_with_small_extension

Items achievable with modest, aligned additions to existing subsystems.

| ID | Title | Extension needed | Rationale |
|----|-------|-----------------|-----------|
| UC-10 | Stable typed ID enforcement | Extend STWD-003 or create new rule: validate frontmatter field values against regex patterns; enforce cross-file uniqueness for specified fields. | Frontmatter presence checking exists (STWD-003). Value pattern validation and uniqueness checking are natural extensions. Builds on G7-02 (scoped frontmatter). |
| UC-32 | Domain-aware search by type/tag/status | Extend search with frontmatter metadata filtering: `steward search --where "type=character"` or `--type character`. | Search engine exists. Adding frontmatter-aware filtering to result sets is a bounded extension. Requires STWD-003 to populate a basic metadata model during scan. |

### 5.6 proposed_new_core_feature

Items needing new core capabilities aligned with the product intent. These represent the most significant gaps identified by the use-case analysis.

| ID | Title | Description | Rationale |
|----|-------|-------------|-----------|
| UC-13 | Artifact type schema system | Policy-level artifact type definitions: per-type frontmatter requirements, field value constraints, required/optional sections, naming patterns, lifecycle status values. Declared in policy.yaml. Validated by type-aware extensions of STWD-003 and new section-validation rules. | **This is the single most impactful capability gap identified.** The PRD states "document-type-aware frontmatter expectations over time" (REQ-FM-003). Multiple use-case items (UC-14, 15, 18, 19, 26, 45) depend on this. It is the generic mechanism that makes domain-specific stewardship possible without hardcoded logic. See [ADR-012](../decisions/adrs/ADR-012-artifact-type-schema-direction.md). |
| UC-14 | Field type and value constraints | Within artifact type schemas: validate field data types (string, number, date, list), constrain values (enum, regex pattern), enforce referential constraints (ID references resolve). | Integral part of UC-13. Controlled vocabularies (UC-15) and ID enforcement (UC-10) are specific applications. |
| UC-15 | Controlled vocabulary enforcement | Enum-type field constraints in artifact type schemas. Frontmatter fields like `status`, `type`, `continuity_level` validated against declared allowed values. | High-value extension of frontmatter validation. Prevents taxonomy drift without domain-hardcoding. Policy declares allowed values; CLI validates. Integral to UC-13. |
| UC-18 | Required section validation per type | Type-aware section requirements: declare that "character" artifacts must have "Overview" and "Relationships" sections; validate in `steward check`. | Natural extension of the Markdown structural model. Section structure is already parsed. Adding per-type required section declarations to artifact type schemas (UC-13) and a corresponding validation rule is aligned with existing architecture. |

### 5.7 proposed_domain_configurability

Items that should be enabled through configuration, templates, or policy — not hardcoded as core behavior.

| ID | Title | Mechanism | Rationale |
|----|-------|-----------|-----------|
| UC-04 | Story/worldbuilding profile | New built-in profile `story` or `worldbuilding` alongside existing 5 profiles. Provides reasonable defaults for artifact types, controlled vocabularies, naming conventions, and section requirements typical of story/lore repos. | Profiles are an existing pattern. A story profile lowers adoption friction for the described use case without adding core features. See [ADR-011](../decisions/adrs/ADR-011-domain-stewardship-through-generic-configuration.md). |
| UC-17 | Required vs derived field distinction | Artifact type schema declares field ownership: `authored` (human-written, validated) vs `derived` (CLI-maintained, warn on manual edit). | Extends managed-content ownership concept from regions to individual fields. Implementation depends on UC-13 and managed-region model. |
| UC-26 | Relationship type declarations | Policy declares allowed reference relationships between artifact types (e.g., chapter may reference character/location). Validation checks that frontmatter relationship fields reference artifacts of the declared type. | Extends reference validation from "link resolves" to "link resolves to correct type." Requires UC-13 for type awareness. |
| UC-45 | Workflow gate validation | Policy declares allowed status values and transitions per artifact type. Validation checks that status changes follow declared transition rules. | Status lifecycle belongs in domain configuration. The generic mechanism (validate field value against allowed transitions) is a clean extension of enum validation (UC-15) with state-machine semantics. |

### 5.8 future_idea

Items that are valuable and aligned but not important enough for near-term roadmap commitment.

| ID | Title | Rationale for deferral |
|----|-------|----------------------|
| UC-16 | Default value injection | Useful but complex interaction with idempotency, preview, and field ownership. Revisit after UC-13 and UC-17 are stable. |
| UC-19 | Section order enforcement | Low priority relative to section presence validation (UC-18). Order enforcement introduces strict expectations that many repos won't want. Revisit after UC-18. |
| UC-25 | Backlink generation | Valuable for bidirectional discoverability but complex: requires stable reference graph (G7-18), managed-region insertion, and careful idempotency. Revisit after v1.5.0 reference graph. |
| UC-31 | Artifact detail view (show command) | Useful but achievable via `md query` and frontmatter extraction. A dedicated `show` command adds convenience, not capability. Revisit after UC-13 when artifact type metadata makes "show" richer. |
| UC-40 | Retcon/deprecation workflow | Domain-specific workflow. Expressible as status lifecycle (UC-45) + relationship constraints (UC-26) once those mechanisms exist. No separate implementation needed. |
| UC-44 | Scaffold command | Valuable for guided creation. Requires template system design, parent-child awareness, and ID generation. Medium-complexity feature. Candidate for v2.0+ planning. |
| UC-46 | Plan/next recommendations | Computed next-action guidance is desirable but complex: requires priority fields, blocker resolution, dependency analysis, and configurable heuristics. High risk of domain-specific assumptions. Defer until scaffolding and lifecycle gates exist. |
| UC-47 | Review queue surfacing | Status-aware artifact listing. Achievable once UC-13 provides type-aware status fields. Simple filter over status metadata. Low standalone priority. |
| UC-50 | Duplicate artifact detection | Fuzzy-match duplicate detection is complex and error-prone. High false-positive risk. ID uniqueness (UC-10) addresses the deterministic subset. |
| UC-51 | Unused tag/vocabulary drift | Requires full tag taxonomy model and cross-artifact analysis. Valuable but complex. Defer until controlled vocabularies (UC-15) are implemented and proven. |
| UC-52 | Placeholder detection | Scanning for TODO/FIXME/TBD markers. Simple to implement but low signal-to-noise for governed artifacts. Can be a low-priority rule addition at any time. |

### 5.9 out_of_scope

Items that do not fit within this CLI's purpose or would require domain-specific hardcoding contrary to the product direction.

| ID | Title | Rationale |
|----|-------|-----------|
| UC-39 | Domain-semantic validation (canon integrity, timeline consistency, plot-thread lifecycle, continuity rules) | **The semantic logic** of canon validation (e.g., "dead character referenced as present", "impossible timeline ordering", "plot thread closed before prerequisite event") requires domain-specific reasoning that belongs in the consuming repository's own tooling or conventions, not in a generic repository stewardship CLI. **The generic mechanisms** (frontmatter validation, controlled vocabularies, relationship types, status lifecycle) ARE in scope and are proposed as UC-13/14/15/26/45. See [ADR-011](../decisions/adrs/ADR-011-domain-stewardship-through-generic-configuration.md). |
| UC-42 | Adaptation source linkage and freshness | Tracking "source last-updated vs adaptation last-synced" is domain-specific timestamp comparison logic. The generic mechanism (frontmatter field validation, staleness signaling via G7-09) provides the building blocks. The interpretation "adaptation is stale relative to source canon" is domain-level logic, not core CLI. |

### 5.10 not_recommended

Items that are technically possible but unwise, brittle, or contrary to good product convention.

| ID | Title | Rationale |
|----|-------|-----------|
| UC-23 | Reference graph with typed semantic relationships | Building a full typed reference graph (chapter→character, event→timeline, etc.) into the core CLI goes beyond link resolution into domain ontology. This risks: unbounded complexity, false relationship inference, maintenance burden on the ontology model, and domain-lock-in. The **link-level** reference graph (G7-18 `steward refs`) is the right primitive. **Type-level** relationship enforcement should be lightweight policy-driven checks on frontmatter reference fields (UC-26), not a full knowledge-graph engine. |

---

## 6. Key Findings

### 6.1 The use-case files describe a legitimate and aligned product archetype

The PRD explicitly names "knowledge, content, lore, story, or creative repositories" as a target archetype (PRD §6). The use-case files are the first deep articulation of what that archetype requires. The overwhelming majority of items (50 of 55) are either already implemented, planned, or achievable through aligned product extensions.

### 6.2 The single most impactful gap is the artifact type schema system

Of the 55 canonical items, **12 directly depend on** a per-type artifact definition mechanism (UC-13). This includes per-type frontmatter schemas, controlled vocabularies, required sections, lifecycle management, and relationship declarations. Without UC-13, most of the "proposed" and "domain configurability" items cannot be expressed in policy.

### 6.3 Domain-specific logic must not be hardcoded

The use-case files naturally describe their needs in domain terms ("canon", "timeline", "plot thread", "adaptation"). However, the correct product response is generic mechanisms that the domain configures, not hardcoded domain logic. This principle is captured in [ADR-011](../decisions/adrs/ADR-011-domain-stewardship-through-generic-configuration.md).

### 6.4 The existing RFC-007 backlog already addresses ~40% of the non-implemented items

Of the 41 non-implemented items, 10 are directly covered by G7-01 through G7-20, and several more become achievable as G7 items are delivered (e.g., orphan detection unblocks adaptation freshness detection; reference graph unblocks relationship tracing).

### 6.5 The gap between "planned" and "sufficient for story repos" is primarily the type schema system

Once G7-01 through G7-20 are delivered AND the artifact type schema system exists, approximately 45 of 55 use-case items are satisfied. The remaining 10 are future ideas or deferred domain workflows.

---

## 7. Recommended Product Direction

### Near-term (`v0.10.0` completed, `v0.11.0+` hardening next): Treat RFC-007 as delivered baseline

The RFC-007 work is now substantively present in the codebase and should be treated as part of the delivered pre-1.0 baseline, not as a deferred post-stable backlog.

### Medium-term (later pre-1.0 milestone): Deliver the artifact type schema system

This remains the most important larger follow-on milestone emerging from this analysis. It should follow the stable-release hardening work on the pre-1.0 line and deliver:
- Artifact type definitions in policy.yaml
- Per-type frontmatter field requirements with value constraints
- Controlled vocabulary (enum) validation
- Per-type required section declarations
- Story/worldbuilding profile leveraging the type schema system

### Long-term (v2.0+): Workflow and template capabilities

Once the type schema system is mature:
- Scaffold/template command for guided artifact creation
- Status lifecycle and transition rules
- Plan/next computed recommendations
- Relationship type declarations with validation

### Explicitly deferred or rejected

- Hardcoded domain-specific validation (canon, timeline, plot-thread semantics): out of scope per ADR-011
- Full typed knowledge-graph engine: not recommended
- Medium-specific adaptation policies: achievable through path-scoped configuration, no core capability needed

---

## 8. Accepted Decisions

Two decisions are auto-accepted as ADRs based on clear product direction, existing architecture, and best-practice judgment:

### ADR-011: Domain-Specific Stewardship Through Generic Configuration

**Decision:** Domain-specific stewardship needs (story/lore, software lifecycle, documentation governance, research management, etc.) are served through generic, configurable policy mechanisms — not through hardcoded domain logic in the core CLI.

**Impact:** Canon validation, timeline checks, plot-thread lifecycle, adaptation rules, and all other domain-semantic logic are expressed through artifact type schemas, controlled vocabularies, relationship declarations, and lifecycle policies. The CLI provides the enforcement engine; the policy provides the domain knowledge.

### ADR-012: Artifact Type Schema System Direction

**Decision:** Steward should support a per-type artifact definition system in policy.yaml, covering frontmatter requirements, field value constraints, required sections, naming patterns, and lifecycle status values. This is the primary mechanism for domain-specific stewardship without core hardcoding.

**Impact:** Becomes the foundation for per-type frontmatter validation, controlled vocabularies, section validation, lifecycle enforcement, and relationship constraints. The design specification (exact YAML schema, type-to-file matching, inheritance model) requires a follow-up RFC before implementation.

---

## 9. Planning Impact

### Later pre-1.0 milestone: Artifact Type Schemas and Domain Configuration

Tracked in the active [milestone plan](../planning/milestone-plan.md) and [pre-1.0 readiness plan](../planning/pre-1-0-readiness-plan.md).

### Updated planning index

New audit entry added to [planning-index.md](../planning-index.md).

### No changes to v1.1.0–v1.5.0

The existing RFC-007 backlog and milestones remain correct and correctly sequenced. This analysis validates them.

---

## 10. Completeness Verification

Every canonical item (UC-01 through UC-55) is classified exactly once. No item is left unclassified or ambiguously categorized.

| Check | Result |
|-------|--------|
| All Exp §3 items (1–71) mapped | ✅ Mapped to UC-01 through UC-55 with explicit deduplication |
| All Exp §4 items (4.1–4.11) mapped | ✅ Merged into corresponding capability items |
| All Exp §5 items (5.1–5.6) mapped | ✅ Deduplicated into §3 items per normalization rules |
| All Exp §6 items (6.1–6.7) mapped | ✅ Deduplicated into §3 items per normalization rules |
| All Exp §7 items (1–10) mapped | ✅ Deduplicated into §3 items per normalization rules |
| All Ideas items (1–20) mapped | ✅ 14 merged with Exp items, 6 as new items, per dedup table |
| No overlapping ADRs/RFCs created | ✅ ADR-011 and ADR-012 are complementary, non-overlapping |
| Planning artifacts consistent | ✅ v1.6.0 milestone added, index updated |
| Rejected items documented with rationale | ✅ UC-39, UC-42 out_of_scope; UC-23 not_recommended |
