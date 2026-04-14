# Milestone Plan — v0.1.0 through v1.0.0

- **Document ID:** PLAN-0002
- **Version:** 1.0.0
- **Status:** Accepted

---

## Milestone Summary

| Milestone | Title | Primary areas |
|-----------|-------|---------------|
| v0.1.0 | Project Foundation | CORE, project setup |
| v0.2.0 | Discovery and Orientation | ORIENT, OUTLINE, GITIGNORE |
| v0.3.0 | Configuration and Path Policy | CONFIG, PATH-POLICY |
| v0.4.0 | Validation and Check | VALIDATION, WORKFLOW (partial) |
| v0.5.0 | Markdown Structural Engine | MARKDOWN (query), FRONTMATTER (validate) |
| v0.6.0 | Search | SEARCH |
| v0.7.0 | Markdown Editing and Managed Regions | MARKDOWN (edit), OWNERSHIP, FRONTMATTER (edit) |
| v0.8.0 | Deterministic Maintenance | MAINTENANCE, STRUCTURE-DOC, MACHINE-MEMORY, HUMAN-NAV |
| v0.9.0 | Workflow Completeness and Explainability | WORKFLOW, STATE-DOCS, EXPLAIN |
| v1.0.0 | Release Readiness | PERFORMANCE, SAFETY, TESTING, DISTRIBUTION, POSITIONING |

---

## v0.1.0 — Project Foundation

### Objective
Establish the project structure, CLI framework, and foundational infrastructure so that subsequent milestones can build feature commands incrementally.

### Scope
- Solution and project scaffolding (ADR-002)
- CLI entry point with System.CommandLine (ADR-001)
- Global options: `--output`, `--verbosity`, `--no-color`, `--config`
- `steward version` command
- `steward --help` and per-command help
- Output formatter abstraction with text and JSON implementations (ADR-006)
- `IFileSystem` abstraction for testability
- Exit code constants (RFC-001: 0, 1, 2, 3)
- Test infrastructure: xUnit + FluentAssertions + Verify (ADR-007)
- Basic project CI capability (`dotnet build`, `dotnet test`)

### Prerequisites
None — first milestone.

### Acceptance criteria
- `steward version` prints version and exits 0.
- `steward --help` shows command list and global options.
- `steward nonexistent` exits with code 2.
- `steward version --output json` produces valid JSON.
- All tests pass. Solution builds on Windows, macOS, and Linux.

### Not in scope
- File discovery, .gitignore handling.
- Any feature commands (check, orient, search, etc.).
- Configuration loading.

---

## v0.2.0 — Discovery and Orientation

### Objective
Build the file discovery engine with .gitignore support, and deliver the `orient` and `outline` commands for repository understanding.

### Scope
- .gitignore-aware file discovery engine (ADR-008)
- `IIgnoreFilter` implementation with nested .gitignore support
- Directory traversal with early pruning of ignored paths
- `steward orient` command (RFC-005)
  - Curated hierarchical repository map
  - Default depth with `--depth` option
  - Heuristic artifact detection for unconfigured repos
  - Human-readable and JSON output
- `steward outline [path]` command (RFC-005)
  - Directory tree with `--sizes` and `--lines` options
  - .gitignore-aware, exclude-aware
  - Human-readable and JSON output
- Tests: unit tests for discovery/ignore, integration tests for orient/outline

### Prerequisites
v0.1.0 (CLI framework, output formatters).

### Acceptance criteria
- `steward orient` on a sample repo shows curated tree with classified entries.
- `steward orient --output json` produces valid JSON with the documented schema.
- `steward outline --sizes --lines` shows file sizes and line counts.
- Ignored files (matching .gitignore) never appear in output.
- Works on an unconfigured repository (no `.steward/` directory).

### Not in scope
- Policy-driven artifact roles (requires CONFIG).
- Markdown heading outlines (requires MARKDOWN engine).
- Cheap health signals in orient (requires VALIDATION).

---

## v0.3.0 — Configuration and Path Policy

### Objective
Implement the configuration and policy loading system and the path policy evaluation engine.

### Scope
- Config directory discovery (`.steward/`)
- Config loading: `config.yaml`, `policy.yaml`, `path-policy.yaml` (RFC-002, ADR-003)
- Profile system with built-in profiles: `software`, `docs`, `mixed`, `knowledge`, `minimal`
- Policy layering and precedence (RFC-002)
- `steward init [--profile <name>]` — scaffold `.steward/` with profile defaults
- `steward config validate` — check config/policy for correctness
- `steward config show [--effective]` — display merged effective config
- Path policy YAML format (REQ-PATHPOL-001 through REQ-PATHPOL-013)
- Path policy evaluation engine with deterministic precedence
- Discovery exclude rules merged from .gitignore + config + policy
- Orient and outline now use policy-driven artifact roles and start-here entries
- Tests: config loading, profile merging, path policy evaluation

### Prerequisites
v0.2.0 (file discovery, orient, outline).

### Acceptance criteria
- `steward init --profile software` creates `.steward/` with correct files.
- `steward config validate` reports errors for invalid YAML, unknown fields.
- `steward config show --effective` displays merged config from all layers.
- Path policy evaluates required/forbidden/naming rules correctly.
- Precedence follows documented order (ignored → priority → specificity → strictness).
- `steward orient` uses policy artifact roles and start-here entries when configured.

### Not in scope
- Validation output (handled in v0.4.0).
- Frontmatter schema in policy.
- Completion policy rules.

---

## v0.4.0 — Validation and Check

### Objective
Deliver the validation engine and `steward check` command with path policy rules, basic diagnostics, scoped validation, and exit code semantics.

### Scope
- Validation engine (ADR-005)
  - `IValidationRule` interface and rule registry
  - `ValidationContext` and `ValidationResult`
  - Scope resolution: full, changed, staged, paths
- Git integration for change-set detection (changed/staged scopes)
- Path policy validation rules:
  - Required artifact presence
  - Forbidden path detection
  - Naming pattern violations
- Diagnostic model: rule ID, severity, category, path, message, remediation
- `steward check` command (RFC-001, RFC-003)
  - `--scope full|changed|staged`
  - `--paths <path>...`
  - `--output text|json`
- Exit code semantics: 0 (pass), 1 (failures), 2 (usage error), 3 (internal error)
- Human-readable check output with severity labels, remediation hints
- Machine-readable check output (JSON diagnostic array)
- `steward orient --signals` — cheap health signals (missing required artifacts)
- Tests: validation engine, path policy rules, exit codes, scope resolution

### Prerequisites
v0.3.0 (config/policy loading, path policy engine).

### Acceptance criteria
- `steward check` reports missing required artifacts as errors.
- `steward check` reports forbidden paths as errors.
- `steward check --scope changed` only evaluates changed files.
- `steward check --output json` produces valid diagnostics JSON.
- Exit code is 0 when all checks pass, 1 when errors exist.
- Remediation hints appear for each violation.
- `steward orient --signals` shows missing required artifacts.

### Not in scope
- Frontmatter validation rules (requires MARKDOWN engine).
- Managed-scope, broken-reference, or stale-artifact rules.
- `--fix` / `--dry-run` (requires fixable rules).
- Completion policy.

---

## v0.5.0 — Markdown Structural Engine

### Objective
Build the Markdown structural model and deliver query/inspection commands.

### Scope
- Markdig integration (ADR-004)
- `StructuredDocument` model: frontmatter, sections, heading hierarchy, content blocks
- mdpath selector parser and evaluator (RFC-004)
  - `frontmatter`, `frontmatter.<field>`
  - `heading[Name]`, `heading[Parent/Child]`, `heading[#N]`
  - `managed[id]`
- `steward md query <file> <selector>` — extract structural content
- `steward md outline <file>` — heading hierarchy with line counts
- Frontmatter extraction and schema validation
  - Frontmatter validation rules for `steward check`
  - Required fields, type expectations per policy
- `steward outline <file> --headings` — heading outline for Markdown files
- Large-document introspection: section line counts, oversize warnings
- Tests: structural model, selector parsing, query operations, frontmatter validation

### Prerequisites
v0.4.0 (validation engine for frontmatter rules to plug into check).

### Acceptance criteria
- `steward md query README.md frontmatter` returns the frontmatter block.
- `steward md query doc.md "heading[Goals]"` returns the Goals section content.
- `steward md outline doc.md` shows heading hierarchy with line counts.
- Selectors that match zero elements return empty result with no error.
- Selectors that match ambiguously return an error.
- `steward check` now includes frontmatter validation rules.
- Large sections trigger info-level diagnostics at configured thresholds.

### Not in scope
- Edit/mutation operations (v0.7.0).
- Managed region editing (v0.7.0).
- Frontmatter set/merge operations (v0.7.0).

---

## v0.6.0 — Search

### Objective
Deliver repository-wide search with content and heading modes, heading context, and policy-aware filtering.

### Scope
- `steward search <query>` command (RFC-005)
  - `--mode content|headings|all` (default: all)
  - `--scope <area>` — filter by policy-defined artifact roles
  - `--max <n>` — result limit (default: 100)
  - `--output text|json`
- Content search: full-text search across repository files
- Heading search: search Markdown headings only
- Combined mode: both content and heading matches
- Result fields: path, line, column, snippet, match kind, heading context
- Heading context: nearest parent heading for content matches in Markdown files
- .gitignore-aware and policy-aware filtering
- Convention-based fallback for unconfigured repos
- Live-scan-first (no index dependency)
- Tests: search engine, heading context, filtering, result format

### Prerequisites
v0.5.0 (Markdown structural model for heading context).

### Acceptance criteria
- `steward search "validation"` finds content matches with heading context.
- `steward search "Goals" --mode headings` finds heading matches only.
- `steward search --output json` produces stable JSON result schema.
- Ignored files never appear in results.
- `--scope authoritative` filters to policy-defined authoritative artifacts.
- Default result limit is 100; `--max 10` limits to 10.

### Not in scope
- Search index artifacts.
- Canonical resource addresses in results.
- Performance optimization (basic correctness first).

---

## v0.7.0 — Markdown Editing and Managed Regions

### Objective
Deliver structural Markdown editing with managed regions, ownership enforcement, and preview/apply workflow.

### Scope
- Managed region markers: `<!-- steward:managed:begin id="..." owner="..." -->` (RFC-004)
- Managed region detection in structural model
- Ownership enforcement: refuse to edit non-owned regions
- `steward md edit <file> <operation>` with preview/apply (RFC-004)
  - `ensure-section`, `set-section`, `insert-section`
  - `append-block`, `prepend-block`
  - `fm-set`, `fm-merge`, `fm-validate`
- Heading level inference (under → child, before/after → sibling)
- Preview mode (default): show unified diff of intended changes
- Apply mode (`--apply`): write changes
- Minimal-diff editing: raw-text edits guided by structural model positions
- Ownership and managed-scope validation rules for `steward check`
- Tests: edit operations, managed regions, ownership enforcement, minimal-diff verification

### Prerequisites
v0.5.0 (Markdown structural model and query).

### Acceptance criteria
- `steward md edit doc.md ensure-section --heading "New Section" --under "Parent"` creates child heading in preview.
- `steward md edit doc.md fm-set --key status --value draft --apply` updates frontmatter.
- Editing inside a managed region with wrong owner produces an error.
- Preview output shows unified diff; apply modifies the file.
- Edits outside the target area produce no diff.
- `steward check` detects broken managed-region markers.
- `steward check` detects managed-scope violations (manual edits in generated regions).

### Not in scope
- Automated split/extract workflows (deferred per REQ-MD-012).
- Maintenance-driven managed section updates (v0.8.0).

---

## v0.8.0 — Deterministic Maintenance

### Objective
Deliver the `steward maintain` command with auto-maintained artifacts, anti-drift detection, and structure document generation.

### Scope
- `steward maintain` command (RFC-006)
  - `--scope <artifact-id>` — target specific artifacts
  - `--preview` (default) / `--apply`
  - `--output text|json`
- Maintenance artifact types:
  - Structure documents (file tree rendering)
  - Indexes and registries (file inventory)
  - Managed sections (update content between markers)
  - Frontmatter auto-fields (freshness, provenance)
- Anti-drift detection in `steward check`
  - Stale-artifact diagnostics when maintained artifacts differ from expected
- Machine-readable manifest generation (`.steward/generated/manifest.json`)
- Policy-driven maintenance declarations
- Preview output: per-artifact maintenance plan
- Content preservation: only managed regions and declared generated artifacts are modified
- `steward check --fix` and `--dry-run` for validation rules that have deterministic fixes
- Tests: maintenance engine, structure doc generation, anti-drift detection, content preservation

### Prerequisites
v0.7.0 (managed region editing, ownership enforcement).

### Acceptance criteria
- `steward maintain` shows preview of all pending maintenance actions.
- `steward maintain --apply` updates maintained artifacts.
- Running `steward maintain --apply` twice produces no diff.
- `steward check` reports stale maintained artifacts.
- Content outside managed regions is never modified.
- `steward check --dry-run` shows what `--fix` would change.
- `steward check --fix` applies deterministic fixes.

### Not in scope
- Complex multi-file refactoring.
- Catalog/glossary generation (future enhancement).

---

## v0.9.0 — Workflow Completeness and Explainability

### Objective
Complete the `steward check` workflow surface with completion policy, add `steward status` and `steward explain`, and finalize workflow-oriented behavior.

### Scope
- Completion policy rules in `steward check` (RFC-003)
  - All-required-present, no-stale-indexes, custom policy rules
  - Completion summary in check output
- `steward status` command (RFC-001)
  - Lightweight current-state without full validation
  - Shows pending work, stale artifacts, completeness signals
- `steward explain <rule-id>` command (RFC-001)
  - Explain what a rule checks, why it matters, how to remediate
  - Machine-readable and human-readable output
- Broken-reference detection rules for `steward check`
  - Internal Markdown links that don't resolve
  - Artifact references that point to missing files
- State-document artifact roles in policy (REQ-STATE-001 through REQ-STATE-003)
  - vision, roadmap, current-state, etc. as explicit roles
  - Surfaced in orient and status
- Agent inner-loop optimization
  - Ensure check + fix + maintain cycle is efficient
  - Structured output enables reliable agent parsing
- Tests: completion policy, status, explain, broken-reference rules

### Prerequisites
v0.8.0 (maintenance, anti-drift, fix support).

### Acceptance criteria
- `steward check` includes completion-policy diagnostics.
- `steward status` shows current state without running full validation.
- `steward explain REQ-PATH-REQUIRED-001` explains the rule clearly.
- Broken internal links are detected and reported with remediation.
- State-document roles appear in orient and status output.
- An agent can run `steward check --output json | steward check --fix | steward maintain --apply` as a reliable loop.

### Not in scope
- Protocol integration (MCP, LSP, etc.).
- Advanced analytics or repository health scoring.

---

## v1.0.0 — Release Readiness

### Objective
Complete the first stable release by addressing performance, safety, cross-platform validation, testing completeness, documentation polish, and distribution packaging.

### Scope
- **Performance validation**
  - Profile key commands on repositories with 1K, 10K, and 50K files
  - Ensure targeted validation is faster than full
  - Ensure orient and outline are fast (<2s on 10K-file repos)
  - Default limits prevent overwhelming output
- **Safety audit**
  - Verify all mutation commands default to preview
  - Verify secret filtering works in diagnostic output
  - Verify ownership boundaries enforced in all mutation paths
- **Cross-platform testing**
  - Run full test suite on Windows, macOS, and Linux
  - Verify path handling is correct on all platforms
  - Verify output formatting is correct on all platforms
- **Test completeness**
  - Review coverage for all validation rules
  - Ensure snapshot tests cover text and JSON output for all commands
  - Ensure integration tests cover all exit code paths
- **Documentation**
  - README with quick start, installation, and usage
  - Command reference (can be auto-generated from System.CommandLine)
  - Configuration reference
  - Migration guide for unconfigured → configured repos
- **Distribution packaging (ADR-009)**
  - dotnet tool packaging
  - Self-contained single-file builds for all target RIDs
  - Version stamping
- **Dog-fooding**
  - Steward's own repository uses `.steward/` configuration
  - `steward check` passes on the steward repo

### Prerequisites
v0.9.0 (all feature functionality).

### Acceptance criteria
- All planned v1.0 requirements are implemented or explicitly deferred with justification.
- Full test suite passes on all three platforms.
- Performance meets stated targets.
- No known mutation commands default to apply without preview.
- Distribution packages are buildable.
- README and command reference are complete and accurate.
- `steward check` passes on the steward repository itself.

### Explicitly deferred beyond v1.0.0
- REQ-ADDR-002/003: Typed URI-like resource address model
- REQ-MD-012: Automated split/extract workflows
- REQ-SEARCH-012: Canonical resource addresses in search results
- REQ-DIST-002: Git hosting platform integrations
- Native AOT compilation
- Homebrew/Scoop/apt packaging
- Plugin/extensibility system
- Protocol integration (MCP, LSP)
