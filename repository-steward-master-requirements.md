---
gitusername: jonv11
gitrepository: steward
---
# Repository Steward CLI Master Requirements

- **Document ID:** MRD-0001
- **Version:** 0.1.0
- **Status:** Draft
- **Traceability Status:** Pending source mapping

## Scope

Normalized master requirements across the current conversation and current project direction for a configurable repository stewardship CLI for humans and AI agents.

## Traceability Model

- **Source reference format:** `DOC-ID#section-or-anchor`
- **Allowed source types:** `PRD`, `ADR`, `RFC`, `VISION`, `ROADMAP`, `REQUIREMENTS`, `CONVERSATION`
- **Note:** `source_refs` are intentionally left empty where exact source document IDs or sections were not provided in this chat.

## Requirement Schema

Each requirement uses the following fields:

- **ID** — stable requirement identifier
- **Statement** — normative requirement statement
- **Details** — optional constrained subpoints that are part of the same requirement
- **Source refs** — traceability list to source documents or sections

---

## AREA-CORE — Core product requirements

### REQ-CORE-001
**Statement:** The CLI must be usable as a repository stewardship tool, not just a validator.

**Source refs:** None yet

### REQ-CORE-002
**Statement:** The CLI must support both new repositories and mature or existing repositories.

**Source refs:** None yet

### REQ-CORE-003
**Statement:** The CLI must remain CLI-first, usable offline, scriptable, and suitable for local use and CI.

**Source refs:** None yet

### REQ-CORE-004
**Statement:** The CLI must be useful across multiple repository archetypes.

**Details:**
- software repositories
- docs-heavy repositories
- mixed code/docs repositories
- knowledge/content/lore/story/manga repositories
- other structured non-code repositories

**Source refs:** None yet

### REQ-CORE-005
**Statement:** The CLI must support both humans and AI agents as first-class users.

**Source refs:** None yet

### REQ-CORE-006
**Statement:** The CLI must help the repository become self-describing, discoverable, checkable, safely updateable, and maintainable over time.

**Source refs:** None yet

### REQ-CORE-007
**Statement:** The product must avoid being overfit to this repository and instead remain configurable and broadly applicable.

**Source refs:** None yet

---

## AREA-CONFIG — Config, policy, and profile requirements

### REQ-CONFIG-001
**Statement:** The CLI must support a human-readable and agent-readable configuration model stored in the repository.

**Source refs:** None yet

### REQ-CONFIG-002
**Statement:** The CLI must separate repository contract or repository semantics from runtime or tool behavior.

**Source refs:** None yet

### REQ-CONFIG-003
**Statement:** Repository policy must define shared repository semantics, and runtime config must not silently override those semantics in enforced mode.

**Source refs:** None yet

### REQ-CONFIG-004
**Statement:** The CLI must support profiles, overlays, and repository-local customization rather than one hardcoded repository shape.

**Source refs:** None yet

### REQ-CONFIG-005
**Statement:** The CLI must support pattern-based repository models instead of fixed conventions only.

**Source refs:** None yet

### REQ-CONFIG-006
**Statement:** More specific policy must override broader defaults deterministically.

**Source refs:** None yet

### REQ-CONFIG-007
**Statement:** Profiles must provide useful defaults but remain opt-in, and repository-local policy must remain the final contract.

**Source refs:** None yet

### REQ-CONFIG-008
**Statement:** The product must support repository-specific terminology and labels where needed.

**Source refs:** None yet

### REQ-CONFIG-009
**Statement:** The product must support explicit exclude rules for junk, caches, binaries, secrets, generated outputs, and irrelevant paths.

**Source refs:** None yet

---

## AREA-VALIDATION — Validation, audit, and checking requirements

### REQ-VALIDATE-001
**Statement:** The CLI must provide a deterministic audit or check capability.

**Source refs:** None yet

### REQ-VALIDATE-002
**Statement:** The CLI must support validation scopes.

**Details:**
- full repository
- changed files
- staged files
- explicit paths

**Source refs:** None yet

### REQ-VALIDATE-003
**Statement:** The CLI must determine the effective change set where supported.

**Source refs:** None yet

### REQ-VALIDATE-004
**Statement:** The CLI must detect relevant repository contract violations.

**Details:**
- missing required artifacts
- stale generated artifacts
- stale machine-readable indexes
- broken internal references
- path or naming violations
- frontmatter violations
- managed-scope violations

**Source refs:** None yet

### REQ-VALIDATE-005
**Statement:** The CLI must support machine-readable diagnostics.

**Details:**
- rule identifier
- severity
- path
- remediation-oriented detail

**Source refs:** None yet

### REQ-VALIDATE-006
**Statement:** The CLI must also support human-readable output for the same validation flow.

**Source refs:** None yet

### REQ-VALIDATE-007
**Statement:** The CLI must support structured exit semantics that distinguish clean pass, validation failure, precondition/config/usage error, and runtime/internal failure.

**Source refs:** None yet

### REQ-VALIDATE-008
**Statement:** The CLI must keep stdout and stderr behavior stable for automation and scripting.

**Source refs:** None yet

### REQ-VALIDATE-009
**Statement:** The CLI must support dry-run or preview for deterministic fixes and maintenance actions.

**Source refs:** None yet

### REQ-VALIDATE-010
**Statement:** The CLI must surface remediation guidance and next actions when checks fail.

**Source refs:** None yet

### REQ-VALIDATE-011
**Statement:** The CLI must avoid leaking secrets or sensitive content in diagnostics.

**Source refs:** None yet

---

## AREA-WORKFLOW — Workflow-surface and agent-operability requirements

### REQ-WORKFLOW-001
**Statement:** The CLI must provide a canonical workflow entry point centered around check or an equivalent command.

**Source refs:** None yet

### REQ-WORKFLOW-002
**Statement:** The canonical workflow entry point must combine scoped validation, impact analysis, and completion-policy surfacing.

**Source refs:** None yet

### REQ-WORKFLOW-003
**Statement:** The CLI must help answer what is still pending, what artifacts are stale, what should be done next, and whether work is complete.

**Source refs:** None yet

### REQ-WORKFLOW-004
**Statement:** The CLI must support explainability of rules and failures.

**Source refs:** None yet

### REQ-WORKFLOW-005
**Statement:** The CLI should support a lightweight status or current-state surface.

**Source refs:** None yet

### REQ-WORKFLOW-006
**Statement:** Workflow-state behavior must be driven by configurable repository policy, not hardcoded self-hosting logic.

**Source refs:** None yet

### REQ-WORKFLOW-007
**Statement:** The CLI must support completion-policy rules so the definition of done can vary by repository type.

**Source refs:** None yet

### REQ-WORKFLOW-008
**Statement:** The CLI must be useful in the agent inner loop.

**Details:**
- inspect
- change
- validate
- remediate
- finalize

**Source refs:** None yet

### REQ-WORKFLOW-009
**Statement:** The CLI must be trustworthy enough for future higher-level automation or protocol integration.

**Source refs:** None yet

---

## AREA-MARKDOWN — Structural Markdown requirements

### REQ-MD-001
**Statement:** Markdown must be a first-class governed document type.

**Source refs:** None yet

### REQ-MD-002
**Statement:** The CLI must support structural selectors for Markdown nodes.

**Details:**
- frontmatter
- headings or sections
- heading paths
- indexed headings
- managed regions
- lists
- tables
- code blocks

**Source refs:** None yet

### REQ-MD-003
**Statement:** The CLI must support structural query or inspection without mutation.

**Source refs:** None yet

### REQ-MD-004
**Statement:** The CLI must support structural edit operations.

**Details:**
- ensure section
- set section
- insert section
- append block
- prepend block
- frontmatter set
- frontmatter merge
- frontmatter validate

**Source refs:** None yet

### REQ-MD-005
**Statement:** Heading insertion must use contextual inference by default.

**Details:**
- under implies child heading
- before or after implies sibling heading

**Source refs:** None yet

### REQ-MD-006
**Statement:** Structural edits must preserve unrelated content and keep diffs minimal.

**Source refs:** None yet

### REQ-MD-007
**Statement:** Ambiguous selectors must fail safely by default.

**Source refs:** None yet

### REQ-MD-008
**Statement:** Managed-scope ownership must be enforced before mutation.

**Source refs:** None yet

### REQ-MD-009
**Statement:** The CLI must support preview or plan before apply.

**Source refs:** None yet

### REQ-MD-010
**Statement:** The CLI must support structural validation for governed Markdown.

**Source refs:** None yet

### REQ-MD-011
**Statement:** The CLI must support large-document introspection and split guidance.

**Source refs:** None yet

### REQ-MD-012
**Statement:** The CLI must eventually support split or extract workflows in preview-first form before risky apply-mode refactors.

**Source refs:** None yet

---

## AREA-ORIENT — Repository orientation and hierarchical map requirements

### REQ-ORIENT-001
**Statement:** The CLI must provide a repository orientation surface for session-start understanding.

**Source refs:** None yet

### REQ-ORIENT-002
**Statement:** It must present a hierarchical map of important files and directories.

**Source refs:** None yet

### REQ-ORIENT-003
**Statement:** That map must be curated, not just a raw filesystem dump.

**Source refs:** None yet

### REQ-ORIENT-004
**Statement:** The orientation surface must distinguish meaningful classes.

**Details:**
- directories
- files
- authoritative artifacts
- workflow artifacts
- generated artifacts
- supporting or reference artifacts

**Source refs:** None yet

### REQ-ORIENT-005
**Statement:** It must support both human-readable and machine-readable hierarchical output.

**Source refs:** None yet

### REQ-ORIENT-006
**Statement:** It must support configurable depth and expansion behavior.

**Source refs:** None yet

### REQ-ORIENT-007
**Statement:** It must surface configured start-here or important entrypoints prominently.

**Source refs:** None yet

### REQ-ORIENT-008
**Statement:** It must highlight important roots and artifacts.

**Details:**
- current state
- roadmap
- implementation priorities
- policy
- workflow docs
- index artifacts

**Source refs:** None yet

### REQ-ORIENT-009
**Statement:** It must work across heterogeneous repository types.

**Source refs:** None yet

### REQ-ORIENT-010
**Statement:** It must respect exclusions and sensitive paths.

**Source refs:** None yet

### REQ-ORIENT-011
**Statement:** It must be useful without requiring a full validation scan.

**Source refs:** None yet

### REQ-ORIENT-012
**Statement:** It should optionally surface cheap signals such as missing important artifacts or likely stale index or map artifacts.

**Source refs:** None yet

### REQ-ORIENT-013
**Statement:** It must remain distinct from check workflow state.

**Source refs:** None yet

---

## AREA-SEARCH — Repository-wide search and contextual discovery requirements

### REQ-SEARCH-001
**Statement:** The CLI must provide a dedicated repository-wide search capability.

**Source refs:** None yet

### REQ-SEARCH-002
**Statement:** Search must be separate from check and separate from orient.

**Source refs:** None yet

### REQ-SEARCH-003
**Statement:** Search must return directly usable results for humans and agents.

**Details:**
- file path
- line
- column or position when available
- snippet
- match kind

**Source refs:** None yet

### REQ-SEARCH-004
**Statement:** Search must support content search, heading-only search, and combined heading or content search.

**Source refs:** None yet

### REQ-SEARCH-005
**Statement:** It must support Markdown-aware heading context for results where relevant.

**Source refs:** None yet

### REQ-SEARCH-006
**Statement:** It must support machine-readable result output with a stable schema.

**Source refs:** None yet

### REQ-SEARCH-007
**Statement:** It must support .gitignore-aware and policy-aware filtering.

**Source refs:** None yet

### REQ-SEARCH-008
**Statement:** It must support scoping or filtering by meaningful repository areas or roles.

**Source refs:** None yet

### REQ-SEARCH-009
**Statement:** It must be live-scan-first, with optional enrichment from search-index-like artifacts when safe.

**Source refs:** None yet

### REQ-SEARCH-010
**Statement:** It must remain useful on unconfigured repositories via conservative convention-based fallback heuristics.

**Source refs:** None yet

### REQ-SEARCH-011
**Statement:** It must be fast enough for session-start or inner-loop use.

**Source refs:** None yet

### REQ-SEARCH-012
**Statement:** It should eventually allow canonical resource addresses to be surfaced in results.

**Source refs:** None yet

---

## AREA-MAINTENANCE — Deterministic maintenance and repository-memory requirements

### REQ-MAINT-001
**Statement:** The CLI must evolve from validator to maintainer.

**Source refs:** None yet

### REQ-MAINT-002
**Statement:** It must support deterministic maintenance of governed documents and managed sections.

**Source refs:** None yet

### REQ-MAINT-003
**Statement:** It must support a simple mdpath-style or equivalent precise document targeting model.

**Source refs:** None yet

### REQ-MAINT-004
**Statement:** It must support managed blocks and managed sections inside documents.

**Source refs:** None yet

### REQ-MAINT-005
**Statement:** It must support auto-updating repository reference documents from actual repository state.

**Source refs:** None yet

### REQ-MAINT-006
**Statement:** It must support deterministic maintenance of indexes, registries, catalogs, glossaries, and similar artifacts.

**Source refs:** None yet

### REQ-MAINT-007
**Statement:** It must support anti-drift maintenance flows that refresh governed memory artifacts over time.

**Source refs:** None yet

### REQ-MAINT-008
**Statement:** It must support project-memory documents and state documents as explicit artifact roles.

**Source refs:** None yet

### REQ-MAINT-009
**Statement:** It must support frontmatter auto-maintenance for freshness and provenance fields where configured.

**Source refs:** None yet

### REQ-MAINT-010
**Statement:** It must support deterministic tables, lists, and registry rows inside governed Markdown when policy declares structure and sorting rules.

**Source refs:** None yet

### REQ-MAINT-011
**Statement:** It must preserve user-authored content outside declared managed scope.

**Source refs:** None yet

### REQ-MAINT-012
**Statement:** It must support preview-first planning for maintenance actions, especially multi-file or structurally important changes.

**Source refs:** None yet

### REQ-MAINT-013
**Statement:** It must coexist cleanly with workflow surfaces and orientation or search surfaces rather than duplicate them.

**Source refs:** None yet

---

## AREA-STRUCTURE-DOC — Auto-maintained repository reference and structure-doc requirements

### REQ-STRUCTDOC-001
**Statement:** The CLI must support auto-maintained repository-structure or reference documents generated from live repository state.

**Source refs:** None yet

### REQ-STRUCTDOC-002
**Statement:** It must support updating a specific managed section inside a larger human-authored document.

**Source refs:** None yet

### REQ-STRUCTDOC-003
**Statement:** It should support rendering tree views, outlines, and structured reference sections from the actual repository.

**Source refs:** None yet

### REQ-STRUCTDOC-004
**Statement:** It should help reduce drift between repository structure and documentation.

**Source refs:** None yet

### REQ-STRUCTDOC-005
**Statement:** This capability must be deterministic and minimal-diff.

**Source refs:** None yet

---

## AREA-GITIGNORE — .gitignore-aware discovery and scan requirements

### REQ-GITIGNORE-001
**Statement:** Discovery, orientation, search, outline, index generation, structure generation, and maintenance scans must respect .gitignore semantics properly.

**Source refs:** None yet

### REQ-GITIGNORE-002
**Statement:** Junk, build, cache, temp, generated, and other noisy paths must not pollute repository maps, indexes, searches, structure docs, outlines, or scans.

**Source refs:** None yet

### REQ-GITIGNORE-003
**Statement:** .gitignore awareness must be treated as core repository-understanding behavior, not a minor convenience.

**Source refs:** None yet

---

## AREA-OUTLINE — Rich outline and discovery requirements

### REQ-OUTLINE-001
**Statement:** The CLI must support richer outline and discovery commands than simple tree dumping.

**Source refs:** None yet

### REQ-OUTLINE-002
**Statement:** It should support curated tree views.

**Source refs:** None yet

### REQ-OUTLINE-003
**Statement:** It should support optional file sizes.

**Source refs:** None yet

### REQ-OUTLINE-004
**Statement:** It should support optional line counts.

**Source refs:** None yet

### REQ-OUTLINE-005
**Statement:** It should support Markdown heading outlines.

**Source refs:** None yet

### REQ-OUTLINE-006
**Statement:** It should support heading hierarchy introspection.

**Source refs:** None yet

### REQ-OUTLINE-007
**Statement:** It should help spot oversized files and documentation bloat.

**Source refs:** None yet

### REQ-OUTLINE-008
**Statement:** It should help humans and agents choose where to work.

**Source refs:** None yet

---

## AREA-FRONTMATTER — Frontmatter requirements

### REQ-FM-001
**Statement:** The CLI must validate frontmatter for governed documents.

**Source refs:** None yet

### REQ-FM-002
**Statement:** It must support frontmatter set, merge, and validate operations.

**Source refs:** None yet

### REQ-FM-003
**Statement:** It must support document-type-aware frontmatter expectations over time.

**Source refs:** None yet

### REQ-FM-004
**Statement:** It should support automatic maintenance of freshness and provenance fields where configured.

**Source refs:** None yet

### REQ-FM-005
**Statement:** Automatic frontmatter updates must be deterministic and policy-driven.

**Source refs:** None yet

### REQ-FM-006
**Statement:** Semantic fields should not be silently rewritten by default.

**Source refs:** None yet

---

## AREA-OWNERSHIP — Managed content and ownership requirements

### REQ-OWN-001
**Statement:** The CLI must support whole-file ownership and mixed-ownership files.

**Source refs:** None yet

### REQ-OWN-002
**Statement:** It must support managed region markers and enforcement.

**Source refs:** None yet

### REQ-OWN-003
**Statement:** It must classify artifacts as manual or owned, generated, mixed, or unclassified where needed.

**Source refs:** None yet

### REQ-OWN-004
**Statement:** It must prevent invalid edits to generated or protected areas.

**Source refs:** None yet

### REQ-OWN-005
**Statement:** It must be able to tell a user or agent whether a surfaced artifact is governed, generated, manual, or mixed.

**Source refs:** None yet

---

## AREA-PATH-POLICY — Pattern-based path and filename policy requirements

### REQ-PATHPOL-001
**Statement:** The CLI must support a ruleset-based filename and path policy format.

**Source refs:** None yet

### REQ-PATHPOL-002
**Statement:** The format must use a top-level rulesets array.

**Source refs:** None yet

### REQ-PATHPOL-003
**Statement:** Each ruleset must declare targets, priority, and zero or more canonical category arrays.

**Source refs:** None yet

### REQ-PATHPOL-004
**Statement:** Canonical policy categories are fixed.

**Details:**
- required
- recommended
- optional
- discouraged
- forbidden
- reserved
- deprecated
- ignored

**Source refs:** None yet

### REQ-PATHPOL-005
**Statement:** Each rule entry must define id, selector, match, kind, and description.

**Source refs:** None yet

### REQ-PATHPOL-006
**Statement:** `match` values are limited to `exact` and `glob`.

**Source refs:** None yet

### REQ-PATHPOL-007
**Statement:** `kind` values are limited to `file`, `directory`, and `any`.

**Source refs:** None yet

### REQ-PATHPOL-008
**Statement:** Validation outcomes must remain distinct from policy categories and be limited to pass, warning, error, and skipped.

**Source refs:** None yet

### REQ-PATHPOL-009
**Statement:** Required-entry checks must be evaluated independently as presence obligations.

**Source refs:** None yet

### REQ-PATHPOL-010
**Statement:** `ignored` must short-circuit evaluation.

**Source refs:** None yet

### REQ-PATHPOL-011
**Statement:** Evaluation must be deterministic by defined precedence.

**Details:**
- ignored
- higher priority
- more specific targets
- exact over glob
- more specific selector
- stricter category

**Source refs:** None yet

### REQ-PATHPOL-012
**Statement:** The format must avoid alternate vocabulary for canonical category names.

**Source refs:** None yet

### REQ-PATHPOL-013
**Statement:** The engine must be reusable for both validation and future maintenance.

**Source refs:** None yet

---

## AREA-RESOURCE-ADDRESS — Broader typed resource-address requirements

### REQ-ADDR-001
**Statement:** The current path-based ruleset model must be treated as path-first, not path-only.

**Source refs:** None yet

### REQ-ADDR-002
**Statement:** The long-term direction is a broader typed resource-address model.

**Source refs:** None yet

### REQ-ADDR-003
**Statement:** The CLI should evolve toward typed, URI-like or scheme-like resource addresses.

**Details:**
- filesystem paths
- Markdown documents, headings, or section paths
- managed regions
- frontmatter
- tables or lists
- registries

**Source refs:** None yet

### REQ-ADDR-004
**Statement:** The system should reuse one conceptual address model across policy, search, orientation, deterministic maintenance, and structural editing.

**Source refs:** None yet

### REQ-ADDR-005
**Statement:** The implementation should favor one general selector or address family with resource-specific evaluators rather than several unrelated selector systems.

**Source refs:** None yet

### REQ-ADDR-006
**Statement:** The path-policy engine should remain compatible with future generalization.

**Source refs:** None yet

---

## AREA-MACHINE-MEMORY — Machine-readable repository-memory requirements

### REQ-MRM-001
**Statement:** The CLI must generate or maintain machine-readable repository memory artifacts.

**Details:**
- manifest
- search index
- structured inventories
- workflow-relevant summaries where applicable

**Source refs:** None yet

### REQ-MRM-002
**Statement:** Those artifacts must be deterministic and refreshable.

**Source refs:** None yet

### REQ-MRM-003
**Statement:** They must support downstream automation and agent workflows.

**Source refs:** None yet

### REQ-MRM-004
**Statement:** The CLI must not depend solely on stale cached or generated artifacts to function.

**Source refs:** None yet

---

## AREA-HUMAN-NAV — Human-navigation artifact requirements

### REQ-HNAV-001
**Statement:** The CLI must generate or maintain human-navigation artifacts.

**Details:**
- repository maps
- indexes
- curated orientation views
- reference structure documents

**Source refs:** None yet

### REQ-HNAV-002
**Statement:** These artifacts must reduce onboarding time and agent confusion.

**Source refs:** None yet

### REQ-HNAV-003
**Statement:** They should remain coherent, minimal-diff, and policy-driven.

**Source refs:** None yet

---

## AREA-STATE-DOCS — Repository memory and state-document requirements

### REQ-STATE-001
**Statement:** The product must explicitly support memory-oriented artifacts.

**Details:**
- vision
- roadmap
- current state
- codebase or repository description
- milestones or phases
- research documents
- requirements documents
- runbooks, how-to, and troubleshooting documents

**Source refs:** None yet

### REQ-STATE-002
**Statement:** These should be treated as discoverable, governable repository artifacts rather than incidental prose.

**Source refs:** None yet

### REQ-STATE-003
**Statement:** The CLI should help keep them coherent and up to date over time.

**Source refs:** None yet

---

## AREA-PERFORMANCE — Performance requirements

### REQ-PERF-001
**Statement:** The CLI must remain fast enough for inner-loop and session-start use.

**Source refs:** None yet

### REQ-PERF-002
**Statement:** Targeted validation should be materially faster than full validation.

**Source refs:** None yet

### REQ-PERF-003
**Statement:** Orientation should be useful without full validation and should stay lightweight.

**Source refs:** None yet

### REQ-PERF-004
**Statement:** Search should have clear performance targets and depth or scoping behavior.

**Source refs:** None yet

### REQ-PERF-005
**Statement:** Large-repository behavior should favor curated views and default limits over overwhelming full scans.

**Source refs:** None yet

### REQ-PERF-006
**Statement:** Large-file and oversized-section handling should be guided by thresholds and introspection rather than risky implicit refactors.

**Source refs:** None yet

---

## AREA-DETERMINISM — Determinism and minimal-diff requirements

### REQ-DET-001
**Statement:** Re-running a command on unchanged input should produce no meaningful diff.

**Source refs:** None yet

### REQ-DET-002
**Statement:** Generated artifacts and maintained artifacts must be stable and idempotent.

**Source refs:** None yet

### REQ-DET-003
**Statement:** Structural edits must not rewrite unrelated text.

**Source refs:** None yet

### REQ-DET-004
**Statement:** Maintenance flows must be low-noise and review-friendly.

**Source refs:** None yet

### REQ-DET-005
**Statement:** Preview must faithfully represent intended mutations before apply.

**Source refs:** None yet

---

## AREA-SAFETY — Safety requirements

### REQ-SAFE-001
**Statement:** Conservative automation is preferred over aggressive automation.

**Source refs:** None yet

### REQ-SAFE-002
**Statement:** The CLI should propose, preview, or guide before applying risky changes.

**Source refs:** None yet

### REQ-SAFE-003
**Statement:** PR-oriented or review-friendly workflows are preferred to opaque mutation.

**Source refs:** None yet

### REQ-SAFE-004
**Statement:** Destructive or structurally significant operations should default to preview-first.

**Source refs:** None yet

### REQ-SAFE-005
**Statement:** The product must preserve ownership boundaries and avoid unintended rewrites.

**Source refs:** None yet

### REQ-SAFE-006
**Statement:** Generated or protected content should not be silently hand-edited without clear enforcement.

**Source refs:** None yet

---

## AREA-EXPLAIN — Explainability requirements

### REQ-EXPLAIN-001
**Statement:** The CLI must explain why rules, files, or outputs matter.

**Source refs:** None yet

### REQ-EXPLAIN-002
**Statement:** It should explain why an artifact is surfaced in orientation, search, or map results.

**Source refs:** None yet

### REQ-EXPLAIN-003
**Statement:** It must explain validation failures sufficiently for human and agent remediation.

**Source refs:** None yet

### REQ-EXPLAIN-004
**Statement:** It should support a deeper explain surface beyond inline summaries.

**Source refs:** None yet

---

## AREA-TESTING — Testing and trust requirements

### REQ-TEST-001
**Statement:** The product must be strongly test-backed.

**Source refs:** None yet

### REQ-TEST-002
**Statement:** Unit, integration, black-box, and snapshot-style tests are expected where appropriate.

**Source refs:** None yet

### REQ-TEST-003
**Statement:** Tests must be meaningful, deterministic, and behavior-focused.

**Source refs:** None yet

### REQ-TEST-004
**Statement:** The CLI should be safe for AI-assisted modification because tests and contracts are strong enough to catch regression.

**Source refs:** None yet

---

## AREA-DISTRIBUTION — Distribution and integration requirements

### REQ-DIST-001
**Statement:** The core product must remain usable without host credentials or platform lock-in.

**Source refs:** None yet

### REQ-DIST-002
**Statement:** Optional integration points may exist for PR-oriented or host-specific workflows later.

**Source refs:** None yet

### REQ-DIST-003
**Statement:** The CLI should remain compatible with CI and local contributor workflows.

**Source refs:** None yet

---

## AREA-POSITIONING — Product-positioning requirements

### REQ-POS-001
**Statement:** The CLI should be understood not only as a repository validator or linter, but also as a stewardship tool, workflow companion, search or orientation companion, deterministic maintenance engine, and repository memory system.

**Source refs:** None yet

### REQ-POS-002
**Statement:** Its differentiated value should come from helping repositories stay organized, self-describing, safely editable, and coherent over time for both humans and AI agents.

**Source refs:** None yet
