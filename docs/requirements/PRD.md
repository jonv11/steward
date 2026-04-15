# Repository Steward — Product Requirements Document

- **Document ID:** PRD-0001
- **Version:** 0.10.0
- **Status:** Accepted
- **Source:** Derived from MRD-0001 (repository-steward-master-requirements.md)
- **Last updated:** 2026-04-15

---

## 1. Overview

**Repository Steward** is a configurable, multi-platform command-line tool that helps repositories stay organized, self-describing, checkable, safely editable, and maintainable over time.

It serves both human developers and AI coding agents as a first-class stewardship companion across the full lifecycle of a repository—from initial setup through ongoing maintenance of mature codebases.

The product name for the CLI binary is `steward`.

## 2. Problem Statement

Repositories accumulate structural drift over time: documentation goes stale, naming conventions erode, indexes fall out of sync, required artifacts go missing, and the repository becomes harder for new contributors—human or AI—to navigate and trust.

Existing tools address fragments of this problem (linters, formatters, doc generators) but none provide an integrated, configurable, repository-aware stewardship surface that:

- validates repository structure and content against declared policy
- provides orientation and search tailored to repository semantics
- maintains governed artifacts deterministically
- supports Markdown as a first-class structural document type
- works equally well for humans in terminals, AI agents in automation loops, and CI pipelines

## 3. Target Users

### Primary users

| User | Description |
|------|-------------|
| **Human developer** | Uses the CLI locally or in CI to check repository health, navigate the repository, search for content, and maintain governed artifacts |
| **AI coding agent** | Uses the CLI programmatically in an inspect → change → validate → remediate → finalize loop; consumes machine-readable output |

### Secondary users

| User | Description |
|------|-------------|
| **Repository maintainer** | Defines policy, profiles, and governance rules for a repository |
| **CI/CD pipeline** | Runs `steward check` as a gate to enforce repository contracts |

## 4. Goals

1. **Stewardship, not just validation.** Go beyond linting to provide orientation, search, maintenance, and workflow guidance.
2. **Dual-audience.** Serve humans and AI agents as equal first-class users with appropriate output modes.
3. **Configurable.** Support many repository archetypes through profiles, overlays, and pattern-based policy—not hardcoded conventions.
4. **Repository-contract-centric.** Separate repository semantics (policy) from tool behavior (runtime config). Policy is the shared contract.
5. **Markdown-native.** Treat Markdown as a first-class governed, queryable, and structurally editable document type.
6. **Deterministic.** All validation, maintenance, and generated outputs must be idempotent and minimal-diff.
7. **Safe.** Default to conservative automation: preview before apply, preserve ownership boundaries, never silently rewrite protected content.
8. **Offline and portable.** CLI-first, no host credentials required, works locally and in CI.

## 5. Non-Goals

1. **IDE plugin or GUI.** The product is CLI-only. IDE integration may come later via protocol (e.g., LSP), not as a primary surface.
2. **General-purpose linter.** The CLI does not lint source code syntax or style. It governs repository-level structure, artifacts, and content.
3. **Git hosting integration in the current core.** PR-workflow integration (GitHub, GitLab, etc.) is deferred for a later pre-1.0 milestone. The CLI operates on the local working tree and git state.
4. **Content generation via LLM.** The CLI performs deterministic operations. It does not generate prose or invoke AI models.
5. **Package manager.** The CLI does not manage code dependencies.
6. **Replacing existing build/CI tools.** It complements pipelines, not replaces them.

## 6. Repository Archetypes

The CLI must support varied repository types without hardcoding any single shape:

- Software repositories (source code, tests, docs)
- Documentation-heavy repositories
- Mixed code and documentation repositories
- Knowledge, content, lore, story, or creative repositories
- Structured non-code repositories (data, research, policy)

## 7. Primary Use Cases

### UC-01: Session-start orientation
A human or agent opens a repository and runs `steward orient` to understand what it contains, what matters, and where to start.

### UC-02: Pre-commit or CI validation
A developer runs `steward check` on changed files before committing—or CI runs it as a gate—to detect repository contract violations.

### UC-03: Full repository audit
A maintainer runs `steward check` with full scope to get a complete compliance report across the entire repository.

### UC-04: Repository-wide search
A developer or agent runs `steward search` to find content, headings, or artifacts across the repository with Markdown-aware context.

### UC-05: Structural Markdown inspection
An agent runs `steward md query` to extract specific sections, frontmatter, or structural elements from governed documents.

### UC-06: Structural Markdown editing
An agent or script runs `steward md edit` to insert, update, or ensure sections in governed documents with preview and apply modes.

### UC-07: Deterministic maintenance
A maintainer runs `steward maintain` to refresh indexes, structure docs, registries, or other governed machine-maintained artifacts.

### UC-08: Workflow completeness check
An agent runs `steward check` to determine what is still pending, what artifacts are stale, and whether the current work is complete per policy.

### UC-09: Policy authoring and validation
A maintainer creates or updates `.steward/policy.yaml` and runs `steward config validate` to check it for correctness.

### UC-10: Rule explainability
A developer runs `steward explain <rule-id>` to understand why a rule exists, what it checks, and how to remediate failures.

## 8. Functional Requirements

Requirements are grouped by capability area. Full traceability to MRD-0001 requirement IDs is in the [Requirements Traceability](requirements-traceability.md) document.

### 8.1 Core Identity (AREA-CORE)

- The CLI is a repository stewardship tool: it validates, orients, searches, maintains, and guides—not just checks. [REQ-CORE-001, REQ-CORE-006, REQ-POS-001, REQ-POS-002]
- It supports new and mature repositories equally. [REQ-CORE-002]
- It is CLI-first, offline-capable, scriptable, and suitable for local and CI use. [REQ-CORE-003]
- It works across repository archetypes without hardcoded assumptions. [REQ-CORE-004, REQ-CORE-007]
- Humans and AI agents are both first-class users. [REQ-CORE-005]

### 8.2 Configuration and Policy (AREA-CONFIG)

- Configuration is stored in-repo in a human-readable and agent-readable format. [REQ-CONFIG-001]
- Repository semantics (policy) and tool behavior (runtime config) are separated into distinct files. [REQ-CONFIG-002, REQ-CONFIG-003]
- Profiles provide useful defaults, are opt-in, and can be overlaid with repository-local customization. [REQ-CONFIG-004, REQ-CONFIG-007]
- Policy uses pattern-based rules, not only fixed conventions. [REQ-CONFIG-005]
- More specific policy overrides broader defaults deterministically. [REQ-CONFIG-006]
- Repository-specific terminology and labels are configurable. [REQ-CONFIG-008]
- Explicit exclude rules for junk, caches, binaries, secrets, and irrelevant paths are supported. [REQ-CONFIG-009]

### 8.3 Validation and Diagnostics (AREA-VALIDATION)

- The CLI provides deterministic, repeatable validation. [REQ-VALIDATE-001]
- Validation scopes: full repository, changed files, staged files, explicit paths. [REQ-VALIDATE-002]
- The effective change set is determined automatically where supported (git integration). [REQ-VALIDATE-003]
- Detected violations include: missing required artifacts, stale generated artifacts, stale indexes, broken internal references, path/naming violations, frontmatter violations, managed-scope violations. [REQ-VALIDATE-004]
- Machine-readable diagnostics include: rule ID, severity, path, and remediation detail. [REQ-VALIDATE-005]
- Human-readable output is also supported for the same validation flow. [REQ-VALIDATE-006]
- Exit codes distinguish: clean pass, validation failure, precondition/config/usage error, and runtime/internal failure. [REQ-VALIDATE-007]
- stdout/stderr behavior is stable for automation. [REQ-VALIDATE-008]
- Dry-run / preview is supported for deterministic fixes. [REQ-VALIDATE-009]
- Remediation guidance and next actions are surfaced on failure. [REQ-VALIDATE-010]
- Secrets and sensitive content are never leaked in diagnostics. [REQ-VALIDATE-011]

### 8.4 Workflow and Status (AREA-WORKFLOW)

- `steward check` is the canonical workflow entry point combining scoped validation, impact analysis, and completion-policy surfacing. [REQ-WORKFLOW-001, REQ-WORKFLOW-002]
- The CLI answers: what is pending, what is stale, what should be done next, whether work is complete. [REQ-WORKFLOW-003]
- Rules and failures are explainable. [REQ-WORKFLOW-004]
- A lightweight `steward status` surface shows current state without full validation. [REQ-WORKFLOW-005]
- Workflow behavior is driven by configurable policy, not hardcoded logic. [REQ-WORKFLOW-006]
- Completion-policy rules allow the definition of "done" to vary by repository. [REQ-WORKFLOW-007]
- The CLI supports the AI agent inner loop: inspect → change → validate → remediate → finalize. [REQ-WORKFLOW-008]
- The CLI is trustworthy enough for future higher-level protocol integration. [REQ-WORKFLOW-009]

### 8.5 Repository Orientation (AREA-ORIENT)

- `steward orient` provides a session-start understanding surface. [REQ-ORIENT-001]
- It presents a curated hierarchical map of important files and directories—not a raw filesystem dump. [REQ-ORIENT-002, REQ-ORIENT-003]
- Artifacts are classified: directories, files, authoritative, workflow, generated, supporting/reference. [REQ-ORIENT-004]
- Output supports both human-readable and machine-readable formats. [REQ-ORIENT-005]
- Depth and expansion behavior is configurable. [REQ-ORIENT-006]
- Configured start-here or important entry points are surfaced prominently. [REQ-ORIENT-007]
- Important roots and artifacts (current state, roadmap, policy, indexes) are highlighted. [REQ-ORIENT-008]
- Works across heterogeneous repository types. [REQ-ORIENT-009]
- Respects exclusions and sensitive paths. [REQ-ORIENT-010]
- Useful without requiring a full validation scan. [REQ-ORIENT-011]
- Optionally surfaces cheap signals (missing artifacts, stale indexes). [REQ-ORIENT-012]
- Distinct from check/workflow state. [REQ-ORIENT-013]

### 8.6 Repository Outline (AREA-OUTLINE)

- Richer outline and discovery commands beyond simple tree dumping. [REQ-OUTLINE-001]
- Curated tree views. [REQ-OUTLINE-002]
- Optional file sizes and line counts. [REQ-OUTLINE-003, REQ-OUTLINE-004]
- Markdown heading outlines and heading hierarchy introspection. [REQ-OUTLINE-005, REQ-OUTLINE-006]
- Spots oversized files and documentation bloat. [REQ-OUTLINE-007]
- Helps users choose where to work. [REQ-OUTLINE-008]

### 8.7 Repository Search (AREA-SEARCH)

- Dedicated repository-wide search capability. [REQ-SEARCH-001]
- Separate from check and orient. [REQ-SEARCH-002]
- Results include: file path, line, column/position, snippet, match kind. [REQ-SEARCH-003]
- Supports content search, heading-only search, and combined. [REQ-SEARCH-004]
- Markdown-aware heading context for results. [REQ-SEARCH-005]
- Machine-readable result output with stable schema. [REQ-SEARCH-006]
- .gitignore-aware and policy-aware filtering. [REQ-SEARCH-007]
- Scoping or filtering by repository areas or roles. [REQ-SEARCH-008]
- Live-scan-first, optional enrichment from indexes. [REQ-SEARCH-009]
- Useful on unconfigured repos via convention-based fallback. [REQ-SEARCH-010]
- Fast enough for session-start and inner-loop use. [REQ-SEARCH-011]
- Future: canonical resource addresses in results. [REQ-SEARCH-012]

### 8.8 Markdown Structural Engine (AREA-MARKDOWN)

- Markdown is a first-class governed document type. [REQ-MD-001]
- Structural selectors: frontmatter, headings/sections, heading paths, indexed headings, managed regions, lists, tables, code blocks. [REQ-MD-002]
- Structural query/inspection without mutation. [REQ-MD-003]
- Structural edit operations: ensure-section, set-section, insert-section, append-block, prepend-block, frontmatter-set, frontmatter-merge, frontmatter-validate. [REQ-MD-004]
- Heading insertion uses contextual inference (under → child, before/after → sibling). [REQ-MD-005]
- Edits preserve unrelated content and keep diffs minimal. [REQ-MD-006]
- Ambiguous selectors fail safely by default. [REQ-MD-007]
- Managed-scope ownership is enforced before mutation. [REQ-MD-008]
- Preview/plan before apply. [REQ-MD-009]
- Structural validation for governed Markdown. [REQ-MD-010]
- Large-document introspection and split guidance. [REQ-MD-011]
- Future: split/extract workflows in preview-first form. [REQ-MD-012]

### 8.9 Frontmatter (AREA-FRONTMATTER)

- Frontmatter validation for governed documents. [REQ-FM-001]
- Set, merge, and validate operations. [REQ-FM-002]
- Document-type-aware frontmatter expectations over time. [REQ-FM-003]
- Auto-maintenance of freshness and provenance fields where configured. [REQ-FM-004]
- Automatic updates are deterministic and policy-driven. [REQ-FM-005]
- Semantic fields are not silently rewritten by default. [REQ-FM-006]

### 8.10 Ownership and Managed Content (AREA-OWNERSHIP)

- Whole-file and mixed-ownership files are supported. [REQ-OWN-001]
- Managed region markers with enforcement. [REQ-OWN-002]
- Artifact classification: manual, generated, mixed, unclassified. [REQ-OWN-003]
- Invalid edits to generated or protected areas are prevented. [REQ-OWN-004]
- The CLI can report whether an artifact is governed, generated, manual, or mixed. [REQ-OWN-005]

### 8.11 Path and Filename Policy (AREA-PATH-POLICY)

- Ruleset-based filename and path policy format. [REQ-PATHPOL-001]
- Top-level `rulesets` array. [REQ-PATHPOL-002]
- Each ruleset declares targets, priority, and zero or more category arrays. [REQ-PATHPOL-003]
- Canonical categories: required, recommended, optional, discouraged, forbidden, reserved, deprecated, ignored. [REQ-PATHPOL-004]
- Each rule defines: id, selector, match, kind, description. [REQ-PATHPOL-005]
- Match values: `exact`, `glob`. [REQ-PATHPOL-006]
- Kind values: `file`, `directory`, `any`. [REQ-PATHPOL-007]
- Validation outcomes: pass, warning, error, skipped (distinct from policy categories). [REQ-PATHPOL-008]
- Required-entry checks are evaluated as independent presence obligations. [REQ-PATHPOL-009]
- `ignored` short-circuits evaluation. [REQ-PATHPOL-010]
- Deterministic precedence: ignored → higher priority → more specific targets → exact over glob → more specific selector → stricter category. [REQ-PATHPOL-011]
- No alternate vocabulary for canonical category names. [REQ-PATHPOL-012]
- Engine is reusable for validation and future maintenance. [REQ-PATHPOL-013]

### 8.12 Deterministic Maintenance (AREA-MAINTENANCE)

- The CLI evolves from validator to maintainer. [REQ-MAINT-001]
- Deterministic maintenance of governed documents and managed sections. [REQ-MAINT-002]
- mdpath-style precise document targeting. [REQ-MAINT-003]
- Managed blocks and sections inside documents. [REQ-MAINT-004]
- Auto-updating reference documents from actual repository state. [REQ-MAINT-005]
- Maintenance of indexes, registries, catalogs, glossaries. [REQ-MAINT-006]
- Anti-drift flows that refresh governed memory artifacts. [REQ-MAINT-007]
- Project-memory and state documents as explicit artifact roles. [REQ-MAINT-008]
- Frontmatter auto-maintenance for freshness and provenance. [REQ-MAINT-009]
- Deterministic tables, lists, registry rows with policy-defined structure and sorting. [REQ-MAINT-010]
- User-authored content outside managed scope is preserved. [REQ-MAINT-011]
- Preview-first for multi-file or structurally important changes. [REQ-MAINT-012]
- Coexists with workflow and orientation surfaces. [REQ-MAINT-013]

### 8.13 Structure Documents (AREA-STRUCTURE-DOC)

- Auto-maintained repository-structure or reference documents from live state. [REQ-STRUCTDOC-001]
- Updating managed sections inside larger human-authored documents. [REQ-STRUCTDOC-002]
- Rendering tree views, outlines, and structured references. [REQ-STRUCTDOC-003]
- Reducing drift between structure and documentation. [REQ-STRUCTDOC-004]
- Deterministic and minimal-diff. [REQ-STRUCTDOC-005]

### 8.14 .gitignore Awareness (AREA-GITIGNORE)

- All discovery, scan, and generation operations respect .gitignore semantics. [REQ-GITIGNORE-001]
- Noisy paths are excluded from all outputs. [REQ-GITIGNORE-002]
- .gitignore awareness is core behavior. [REQ-GITIGNORE-003]

### 8.15 Machine-Readable Memory Artifacts (AREA-MACHINE-MEMORY)

- Generate or maintain: manifest, search index, structured inventories. [REQ-MRM-001]
- Deterministic and refreshable. [REQ-MRM-002]
- Support downstream automation and agent workflows. [REQ-MRM-003]
- The CLI does not depend solely on stale cached artifacts. [REQ-MRM-004]

### 8.16 Human-Navigation Artifacts (AREA-HUMAN-NAV)

- Generate or maintain: repository maps, indexes, curated orientation views. [REQ-HNAV-001]
- Reduce onboarding time and agent confusion. [REQ-HNAV-002]
- Coherent, minimal-diff, and policy-driven. [REQ-HNAV-003]

### 8.17 State Documents (AREA-STATE-DOCS)

- Explicit support for memory-oriented artifacts: vision, roadmap, current state, codebase description, milestones, research, requirements, runbooks. [REQ-STATE-001]
- Discoverable and governable. [REQ-STATE-002]
- The CLI helps keep them coherent and up to date. [REQ-STATE-003]

### 8.18 Resource Addresses (AREA-RESOURCE-ADDRESS) — Future

- Path-based model is path-first, not path-only. [REQ-ADDR-001]
- Long-term: typed, URI-like resource addresses. [REQ-ADDR-002, REQ-ADDR-003]
- One conceptual address model across all surfaces. [REQ-ADDR-004]
- One general selector family with resource-specific evaluators. [REQ-ADDR-005]
- Current engine remains compatible with future generalization. [REQ-ADDR-006]

## 9. Non-Functional Requirements

### 9.1 Performance (AREA-PERFORMANCE)

- Fast enough for inner-loop and session-start use. [REQ-PERF-001]
- Targeted (scoped) validation is materially faster than full. [REQ-PERF-002]
- Orientation works without full validation and stays lightweight. [REQ-PERF-003]
- Search has clear performance behavior with depth/scoping limits. [REQ-PERF-004]
- Large repositories: curated views and default limits over full scans. [REQ-PERF-005]
- Large-file handling: guided by thresholds and introspection. [REQ-PERF-006]

### 9.2 Determinism (AREA-DETERMINISM)

- Rerunning on unchanged input produces no meaningful diff. [REQ-DET-001]
- Generated and maintained artifacts are stable and idempotent. [REQ-DET-002]
- Structural edits do not rewrite unrelated text. [REQ-DET-003]
- Maintenance flows are low-noise and review-friendly. [REQ-DET-004]
- Preview faithfully represents intended mutations. [REQ-DET-005]

### 9.3 Safety (AREA-SAFETY)

- Conservative automation over aggressive automation. [REQ-SAFE-001]
- Propose/preview/guide before risky changes. [REQ-SAFE-002]
- PR-oriented and review-friendly workflows preferred. [REQ-SAFE-003]
- Destructive operations default to preview-first. [REQ-SAFE-004]
- Ownership boundaries preserved. [REQ-SAFE-005]
- No silent hand-editing of generated/protected content. [REQ-SAFE-006]

### 9.4 Explainability (AREA-EXPLAIN)

- Rules, files, and outputs are explainable. [REQ-EXPLAIN-001]
- Surfacing in orient/search/map is explainable. [REQ-EXPLAIN-002]
- Validation failures are explainable for human and agent remediation. [REQ-EXPLAIN-003]
- Deeper explain surface beyond inline summaries. [REQ-EXPLAIN-004]

### 9.5 Testing (AREA-TESTING)

- Strongly test-backed. [REQ-TEST-001]
- Unit, integration, black-box, and snapshot tests. [REQ-TEST-002]
- Meaningful, deterministic, behavior-focused tests. [REQ-TEST-003]
- Safe for AI-assisted modification because contracts catch regressions. [REQ-TEST-004]

### 9.6 Distribution (AREA-DISTRIBUTION)

- No host credentials or platform lock-in required. [REQ-DIST-001]
- Optional host-specific integrations are deferred. [REQ-DIST-002]
- Compatible with CI and local contributor workflows. [REQ-DIST-003]

## 10. Constraints

| Constraint | Detail |
|------------|--------|
| **Runtime** | .NET 10, multi-platform (Windows, macOS, Linux) |
| **Language** | C# |
| **Distribution** | dotnet global/local tool and self-contained single-file publish |
| **Offline** | Must work without network access |
| **No host lock-in** | No GitHub/GitLab API required for core functionality |

## 11. Dependencies

| Dependency | Purpose |
|------------|---------|
| .NET 10 SDK | Build and runtime |
| System.CommandLine | CLI parsing, help, completions |
| Markdig | Markdown parsing and AST |
| YamlDotNet | YAML configuration parsing |
| Git CLI or libgit2sharp | .gitignore resolution, change-set detection |

## 12. Open Questions (Post-RFC)

All product-level open questions have been resolved through accepted RFCs. See [Decision Index](../decisions/decision-index.md).

Remaining forward-looking items intentionally scheduled later on the pre-1.0 line:
- REQ-ADDR-002/003: Typed URI-like resource address model
- REQ-MD-012: Split/extract workflows
- REQ-SEARCH-012: Canonical resource addresses in search results
- REQ-DIST-002: Git hosting platform integrations
