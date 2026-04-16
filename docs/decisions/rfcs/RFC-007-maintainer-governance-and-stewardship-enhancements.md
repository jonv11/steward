---
type: rfc
status: Accepted
accepted: 2026-04-15
resolves: >-
  Maintainer-governance gaps, policy explainability gaps, and stewardship workflow
  improvements identified through repo-maintainer review and follow-up product analysis
---

# RFC-007: Maintainer Governance and Repository Stewardship Enhancements

- **Authoring intent:** Consolidate closely related product-level enhancements before any implementation-specific ADRs are created

---

## 1. Context

Steward is already useful as a repository stewardship CLI, especially through the `check`, `maintain`, and `orient` workflow. However, recent maintainer-focused review surfaced a set of governance and maintenance gaps that materially limit Steward's value as the primary tool for keeping a repository consistent, discoverable, and correctly governed.

The most visible gaps already identified include:

- inability to detect incomplete planning/index artifacts when source files exist but are not referenced
- inability to declare and enforce directory-specific naming conventions
- inability to scope frontmatter requirements by path pattern
- lack of per-path rule suppression
- lack of freshness signaling for manually maintained state documents
- lack of deterministic maintenance for mechanically derived index sections
- limited feedback after deterministic fixes are applied
- artifact roles acting primarily as taxonomy rather than behavior-driving signals

In addition to those explicit maintainer requests, further analysis suggests adjacent product gaps that are not yet formally captured:

- it is hard to explain the effective policy that applies to one path
- valid configuration can still be ineffective, dead, shadowed, or misleading without being reported
- documents can be structurally valid yet effectively undiscoverable
- `steward check` does not fully expose practical impact or likely downstream refresh obligations
- common refactors such as moving or renaming governed Markdown files remain manual and error-prone
- there is no clear governance coverage view showing which important repo areas remain outside the stewardship surface
- onboarding an existing mature repository still requires high-effort manual authoring of `.steward/` files
- maintenance does not yet model explicit dependencies between maintained artifacts
- staged-scope validation does not fully answer whether the staged state is commit-complete
- repository references can be searched textually but not inspected relationally

These gaps are tightly related. They all concern the same product theme: Steward should not only validate declared governance, but should also help maintainers understand, express, inspect, and safely evolve that governance with lower friction and higher trust.

---

## 2. Problem Statement

Steward currently helps maintain repositories once governance is already well expressed. It is weaker at:

1. explaining what governance applies to a given path and why
2. warning when governance exists on paper but is incomplete, ineffective, or partially disconnected from actual repository practice
3. helping maintainers safely perform common stewardship operations that otherwise create drift
4. surfacing repository-level discoverability and completeness gaps before they become long-lived maintenance problems

As a result, maintainers still need custom scripts, manual inspection, `git diff`, ad hoc search, or external reasoning to answer important questions such as:

- what rules actually apply to this file?
- which config entries are dead or shadowed?
- which valid docs are orphaned from normal repo navigation?
- what else likely needs refresh after this change?
- can I safely move or rename this governed document?
- which important repo areas remain outside Steward's declared governance surface?
- which maintained artifacts depend on which sources?
- is my staged commit complete, or am I about to commit only half of a stewardship-relevant change?

That undermines Steward's long-term goal of being the primary repository stewardship companion for both humans and AI agents.

---

## 3. Goals

This RFC proposes product-level enhancements that improve Steward in the following ways:

1. **Improve policy explainability.** A maintainer should be able to understand the effective policy applying to a path without manually merging multiple files and precedence rules.
2. **Improve governance confidence.** Steward should detect not only invalid config, but also ineffective or misleading governance declarations.
3. **Improve discoverability.** Steward should help surface documents and repo areas that are technically valid yet hard to find or operationally disconnected.
4. **Improve workflow trust.** Steward should better show practical downstream impact and commit completeness, not only isolated rule failures.
5. **Improve safe stewardship operations.** Common maintenance and refactor flows should be preview-first and deterministic.
6. **Improve mature-repo adoption.** Existing repositories should be easier to bootstrap into Steward with strong initial suggestions.
7. **Preserve Steward's design principles.** All enhancements must remain deterministic, preview-first where relevant, explainable, multi-platform, and compatible with human and AI use.

---

## 4. Non-Goals

This RFC does not define:

- low-level implementation architecture or project layering changes
- specific parser or serializer library changes
- plugin systems or external rule-loading models
- IDE integration or GUI features
- LLM-driven prose generation or autonomous policy authoring
- broad redefinition of core Steward command philosophy

Those concerns should be handled in follow-up ADRs only if needed after product decisions are accepted.

---

## 5. Proposed Capability Areas

### 5.1 Effective policy explanation for a path

Steward should provide a way to explain the effective governance for a given file or directory.

Examples:

```bash
steward explain path docs/decisions/adrs/ADR-007-test-strategy.md
steward policy match docs/planning-index.md --output json
```

The output should show, at minimum:

- matched artifact declarations
- matched path-policy rules and their precedence
- effective role/classification
- effective frontmatter requirements
- effective maintenance participation
- managed-region ownership expectations
- suppressions or overrides that changed behavior
- source locations for each effective rule or declaration

This capability is distinct from `config validate` and from rule-level `explain`. It answers: **what applies here, and why?**

### 5.2 Configuration doctor for ineffective governance

Steward should add a `config doctor`-style surface that detects configuration and policy that are valid but ineffective, stale, redundant, shadowed, or misleading.

Examples of issues that should be detectable:

- rulesets that match no current paths
- entries fully shadowed by higher-priority or more specific rules
- redundant exclusions already covered elsewhere
- declared `start_here` paths that are missing or ignored
- disabled rules that suppress no current diagnostics and have no current effect
- artifact declarations that never participate in orient, check, maintain, or status in any meaningful way
- maintenance declarations whose source patterns match nothing

This capability improves trust in Steward configuration as a living governance contract rather than a pile of historically accumulated YAML.

### 5.3 Orphaned-but-valid document detection

Steward should surface documents that are structurally valid but effectively undiscoverable.

A document may be considered a candidate orphan if it is not:

- linked from configured `start_here` or other curated entry points
- referenced from a declared index or planning hub where expected
- surfaced by repository orientation as an important artifact
- intentionally marked as standalone, generated, archival, or private to a narrow workflow

This should be advisory by default. The goal is not to require all documents to be globally linked, but to identify cases where governance is nominally correct while navigation and discovery remain weak.

### 5.4 Change-impact output in `steward check`

`steward check` should more explicitly report likely downstream stewardship impact.

Examples:

- modifying an ADR may imply that a decision index, planning index, or state document should be refreshed
- changing a curated hub document may imply broader discoverability checks
- changing files under governed directories may imply naming, index, or reference integrity consequences

The output should remain deterministic and lightweight. This is not meant to be speculative AI reasoning; it is a policy-driven impact signal.

### 5.5 Safe move/rename workflow for governed artifacts

Steward should support preview-first move or rename workflows for governed content, especially Markdown and indexed documentation.

Examples:

```bash
steward refactor move docs/old.md docs/new.md
steward md move docs/decisions/rfcs/RFC-001-old-name.md docs/decisions/rfcs/RFC-001-new-name.md --preview
```

The workflow should be able to propose deterministic updates to:

- relative Markdown links
- known governed indexes and registries
- selected policy references where applicable
- optionally, frontmatter self-references or canonical-path fields if such features exist

This capability helps shift Steward from drift detection after manual refactors toward safer stewardship during refactors.

### 5.6 Governance coverage reporting

Steward should provide a repository-level view of where governance is present, thin, inconsistent, or absent.

This report should help answer questions such as:

- which important directories or docs are not classified or governed?
- which prominent docs are not reachable from current orientation entry points?
- which Markdown-heavy areas have no frontmatter expectations?
- which maintained artifact types are absent even though the repo appears to contain fitting content?
- which important repo zones are effectively outside current Steward coverage?

This is not a failure report. It is a maturity and completeness surface for maintainers.

### 5.7 Bootstrap-by-analysis for mature repositories

Steward should improve `init` and related setup flows for existing repositories.

Examples:

```bash
steward init --analyze
steward config suggest
```

Suggested outputs may include:

- likely repository type/profile
- likely `start_here` paths
- likely artifact roles
- likely exclusion patterns
- initial path-policy candidates
- likely maintained artifact opportunities such as structure docs or decision indexes

Suggestions must remain reviewable and preview-first. The goal is to reduce setup friction, not silently generate governance.

### 5.8 Maintenance dependency modeling

Steward should support explicit dependency relationships between maintained artifacts and their source domains.

Examples:

- planning index depends on RFC and ADR directories
- structure doc depends on file-tree state
- glossary depends on docs/reference terminology sources

This enables:

- ordered maintenance planning
- incremental invalidation
- clearer impact reporting
- more efficient refresh operations

The model should remain deterministic and policy-driven.

### 5.9 Staged-scope completeness checks

When validating staged work, Steward should better answer whether the **staged state** is complete and internally coherent.

Examples of advisory diagnostics:

- a changed governed source implies a maintained artifact refresh that is unstaged or absent
- a staged planning index update references a file that is not staged or not yet present
- a staged rename left unresolved governed references outside the staged set

This capability is specifically about commit completeness, not generic full-repo drift.

### 5.10 Reference graph queries

Steward should support relational inspection of repository references.

Examples:

```bash
steward refs docs/planning-index.md
steward refs --to docs/decisions/adrs/ADR-007-test-strategy.md
steward refs --from docs/requirements/PRD.md --output json
```

This should expose inbound and outbound references at least for Markdown-governed content, and optionally later for other declared artifact types.

This is distinct from `search`. Search answers “where does text match?” while refs answers “what points to what?”

---

## 6. Command Surface Changes

This RFC proposes the following product-surface additions or extensions.

### 6.1 New or extended commands

Potential command surfaces include:

- `steward explain path <path>`
- `steward policy match <path>`
- `steward config doctor`
- `steward discoverability` or `steward status --discoverability`
- `steward refactor move <old> <new>`
- `steward refs <path>`
- `steward init --analyze`
- `steward config suggest`

### 6.2 Extensions to existing commands

- `steward check`
  - richer impact reporting
  - staged completeness diagnostics
- `steward maintain`
  - dependency-aware planning and ordering
- `steward status`
  - optional governance coverage and discoverability summaries
- `steward explain`
  - path-focused effective policy explanation

The exact command naming may be refined before acceptance, but the capability boundaries should remain clear.

---

## 7. Policy and Schema Implications

This RFC likely requires additions to one or more Steward configuration files. The product-level intent is accepted here; precise schema should be finalized during implementation design.

Possible new schema concepts include:

### 7.1 Discoverability / orphan suppression hints

```yaml
artifacts:
  - path: docs/archive/old-notes.md
    role: archive
    discoverability:
      intentionally_unlinked: true
```

### 7.2 Maintenance dependencies

```yaml
maintenance:
  artifacts:
    - id: planning-index
      path: docs/planning-index.md
      type: index
      depends_on:
        - docs/decisions/rfcs/
        - docs/decisions/adrs/
```

### 7.3 Bootstrap guidance or importance hints

```yaml
repository:
  bootstrap_hints:
    favored_roots:
      - docs/
      - src/
```

### 7.4 Path explainability metadata

No new schema may be required if this can be derived entirely from existing config plus policy source locations. However, output contracts for explainability will likely need to be documented.

This RFC does not mandate specific final keys. It authorizes the capability direction.

---

## 8. Diagnostics and Output Implications

Some proposed features may surface as diagnostics, while others are report-only or query-like.

Expected additions may include new advisory diagnostics such as:

- orphaned-document
- dead-config
- shadowed-rule
- staged-incomplete-maintenance
- unresolved-impact-refresh
- governance-coverage-gap

These should respect Steward's existing output principles:

- deterministic
- machine-readable in JSON
- human-readable in text
- stable stdout contract
- explainable with remediation guidance when applicable

Not every capability should be a validation rule. Some are better exposed as reports or explain/query surfaces rather than as `check` diagnostics.

---

## 9. Interaction with Existing Accepted Direction

This RFC is intended to extend, not replace, prior accepted product direction.

It is compatible with:

- RFC-001 command hierarchy
- RFC-002 configuration and policy separation
- RFC-003 deterministic diagnostics and scoped validation
- RFC-005 distinct boundaries between orient, outline, and search
- RFC-006 maintenance as explicit, deterministic, preview-first stewardship

The proposed enhancements should preserve those principles while filling important maintainer and governance gaps.

---

## 10. Backward Compatibility

These enhancements should be backward-compatible by default.

Acceptance criteria for compatibility:

- existing repositories without new config keys continue to function
- new advisory surfaces default to safe, low-noise behavior
- existing commands retain their current core semantics
- optional schema additions are non-breaking when omitted
- unconfigured repos still work with conservative fallback behavior

Any stricter enforcement should remain opt-in unless a later accepted RFC explicitly changes the contract.

---

## 11. Milestone and Phasing Guidance

Recommended phasing:

### Phase A — highest-value low-to-medium complexity

- effective policy explanation for a path
- config doctor for ineffective governance
- change-impact output in `check`
- reference graph queries (basic Markdown support)

### Phase B — medium complexity, high maintainer value

- orphaned-but-valid document detection
- governance coverage reporting
- staged-scope completeness diagnostics
- maintenance dependency modeling

### Phase C — higher complexity workflow evolution

- safe move/rename workflows with governed reference repair
- mature-repo bootstrap-by-analysis and guided suggestion flows

This phasing is guidance only. Final milestone mapping should be reflected later in the PRD and traceability artifacts after acceptance.

---

## 12. Acceptance Criteria

This RFC should be considered accepted only if the final agreed direction satisfies the following product criteria:

1. A maintainer can inspect the effective governance applying to a path without manually resolving precedence across multiple files.
2. Steward can report at least one meaningful class of valid-but-ineffective governance configuration.
3. Steward can surface at least one meaningful class of discoverability gap that is not reducible to broken links.
4. `steward check` can surface at least basic deterministic downstream impact signals for changed or staged work.
5. Steward has a defined product path toward safe governed move/rename workflows.
6. The governance surface can better express and inspect repo maturity beyond raw pass/fail validation.
7. All adopted capabilities remain deterministic, preview-first where relevant, and compatible with existing output contracts.

---

## 13. Open Product Questions — Resolved

All questions have been resolved as of acceptance:

1. **Command surface placement:** Path explanation lives under `steward explain path <path>` as a subcommand of the existing `explain` command, consistent with RFC-001 command hierarchy. Config doctor lives under `steward config doctor`.
2. **Diagnostic vs report boundary:** Phase 1–2 findings surface as `check` diagnostics with standard rule IDs, severity, and remediation. Phase 4 coverage and discoverability findings surface as report-style output under `steward status --coverage` and `steward status --discoverability`, not as check diagnostics.
3. **Bootstrap inference threshold:** Conservative defaults only. High-confidence suggestions (e.g., clear `start_here` candidates like README.md) are surfaced; speculative inferences are suppressed. All suggestions are preview-first (never auto-applied).
4. **Move/rename scope:** Initial implementation is Markdown-first. Broader artifact support (policy references, config paths) may be added later.
5. **Dependency modeling:** Hybrid approach. Explicit `depends_on` declarations in policy are primary. `index_of` declarations imply dependency automatically. Cycle detection is mandatory.
6. **Schema additions:** All new schema fields are optional and non-breaking when omitted. Required additions: `validation.path_overrides`, `validation.frontmatter_requirements`, `path_rule.must_match` (regex), `artifact.index_of`, `artifact.freshness`, `artifact.importance`. All other state is derived from existing config.
7. **Naming pattern syntax:** `must_match` uses .NET regular expressions (`System.Text.RegularExpressions`). Regex is more expressive than glob for filename validation and .NET provides native, well-tested support. Patterns are validated at config-load time.
8. **Freshness source:** Git commit timestamps are the primary freshness signal. A frontmatter `last_updated` field overrides git when present. Filesystem modification time is the fallback when git is unavailable. This provides reliable CI behavior while allowing manual override.

---

## 14. Deferred Items

The following remain out of scope for this RFC unless separately proposed later:

- external rule/plugin ecosystems
- GUI or IDE-specific stewardship UX
- hosted SCM integration workflows
- AI-generated documentation or policy authoring
- speculative graph/knowledge-base layers beyond practical repository stewardship needs

---

## 15. Summary

Steward already validates and maintains governed repositories usefully, but it remains weaker than it should be at helping maintainers express, inspect, trust, and evolve governance itself.

This RFC proposes a coherent next step: improve policy explainability, ineffective-governance detection, discoverability analysis, impact signaling, safe stewardship workflows, governance coverage visibility, mature-repo bootstrap ergonomics, maintenance dependency awareness, staged completeness checks, and relational reference inspection.

Taken together, these enhancements would make Steward more credible as a true repository stewardship companion rather than only a validation-and-maintenance tool.
