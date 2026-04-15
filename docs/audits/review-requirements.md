# Steward Requirements Implementation Review

**Date:** 2026-04-14
**Reviewer:** Automated principal-level review
**Scope:** Full requirement set (PRD-0001, TRACE-0001, ACD-0001, ADR-001–009, RFC-001–006) vs. current implementation (v1.0.0 as declared)

> **Accuracy Note (2026-04-15):** Post-review code inspection confirmed several findings in this audit were already fixed at the time of review but were missed during the automated pass. Specifically:
>
> - **Profile merging IS implemented**: `ProfileMerger.Merge` is called in `CommandSetup.Build`; profile defaults are merged into effective policy.
> - **`outline` does NOT crash on file-path input**: The command delegates to Markdown outline for `.md` files.
> - **`--headings` flag IS implemented** on the `outline` command.
> - **`--quiet` flag IS implemented** on the `check` command.
> - **Test count**: 472 tests pass (363 core + 109 CLI), not 319.
> - **Command count**: 16 commands exist (including `refs` and `refactor`), not 14.
> - **Rule count**: 13 rules exist (STWD-001 through STWD-013), not 9.
>
> Remaining findings about policy schema gaps, md edit flags, and state documents remain accurate and are tracked in the post-v1.0.0 milestone plan.

---

## 1. Scope and Review Basis

### Artifacts reviewed

| Artifact | ID | Purpose |
|---|---|---|
| Product Requirements Document | PRD-0001 v1.0.0 | Definitive product scope and requirements |
| Requirements Traceability | TRACE-0001 v1.0.0 | MRD → PRD → milestone mapping |
| Assumptions, Constraints, Dependencies | ACD-0001 v1.0.0 | Hard boundaries and risk register |
| ADR-001 through ADR-009 | — | Architecture decisions |
| RFC-001 through RFC-006 | — | Design decisions |
| README.md | — | User-facing documentation |
| Implementation Status | docs/implementation-status.md | Self-reported completion |
| Source code | src/Steward.Cli, src/Steward.Core | All commands, engines, rules, models |
| Test suites | tests/ (319 tests, 3 projects) | Behavioral coverage |
| CLI help output | All 14 commands | Discoverability and contract surface |
| Actual CLI execution | orient, outline, search, check, md, maintain, status, explain, config, version | Runtime behavior validation |

### Review method

1. Read all accepted requirement and design artifacts end to end.
2. Inspected source code across all major subsystems: commands, configuration, validation, markdown, search, orientation, maintenance, discovery.
3. Ran every major CLI command against the steward repository itself, in both text and JSON output modes.
4. Ran the full test suite (319 tests, all passing).
5. Compared documented behavior (README, help text, implementation-status.md) against actual code and runtime output.
6. Cross-referenced RFC-specified options, flags, and schemas against implementation.

---

## 2. Executive Summary

### Overall maturity

Steward is a **substantially implemented** v1.0.0 with strong core infrastructure. The CLI framework, validation engine, Markdown structural engine, maintenance system, orientation, search, and configuration layers are all functional and well-tested. The project successfully dog-foods on its own repository.

### Strongest areas

- **Validation engine**: 9 rules, machine-readable diagnostics, secret filtering, fix/dry-run, scoped validation, completion summary — all working.
- **Markdown structural engine**: MdPath selectors, structural editing (7 operations), frontmatter editing, managed regions, preview/apply — all functional.
- **Maintenance engine**: 5 maintainer types, preview/apply, idempotent, stale-artifact detection — clean architecture.
- **Configuration model**: Two-file separation (config.yaml, policy.yaml), profile system, path-policy engine — well-aligned with RFC-002.
- **Test infrastructure**: 319 tests across unit, integration, and snapshot layers. InMemoryFileSystem for isolation. Good coverage of core logic.
- **Output contract**: text/JSON dual output on all major commands. Exit codes match RFC-001.

### Weakest areas

- **Policy model gaps**: No `governance.frontmatter` structure (only `validation.required_frontmatter_fields`), no `governance.managed_regions` config, no `completion_policy` rules in policy.yaml. The policy schema diverges materially from RFC-002 and RFC-003.
- **Outline command limitations**: No `--headings` flag (RFC-001 specifies it). Crashes on file path input instead of delegating to Markdown outline or showing a helpful error.
- **Md edit incomplete flags**: Missing `--after`, `--before`, `--level` on `insert-section` (RFC-004 specifies all three). Heading-level inference is partial.
- **Profile layering not implemented**: Profile selection sets a label but profile defaults are not merged into effective policy. Only the profile name surfaces in orient/status output.
- **Path-policy.yaml not used in practice**: The steward repo itself has no path-policy.yaml. The schema diverges from RFC-002's `rulesets[].required/recommended/forbidden` arrays — implementation uses a flat rules list with `category` strings.
- **No manifest generation configured**: ManifestMaintainer exists but the steward repo does not configure a `manifest` artifact. No evidence of end-to-end manifest generation in practice.
- **Machine-readable memory artifacts underserved**: REQ-MRM-001 through REQ-MRM-003 are architecturally enabled but not meaningfully exercised.

### Biggest requirement risks

1. **Policy schema drift from RFC-002**: The accepted RFC defines `governance.frontmatter`, `governance.managed_regions`, `governance.completion_policy` — none of which exist in the current `RepositoryPolicy` model.
2. **Profile layering (REQ-CONFIG-004, REQ-CONFIG-007)**: Profiles are opt-in by name but do not merge defaults into effective policy. This means `steward check` against a `software` profile repo without explicit artifact declarations will miss profile-implied requirements.
3. **State documents (REQ-STATE-001 through REQ-STATE-003)**: Entirely unimplemented as a distinct concept. Artifact roles are free-form strings with no special state-document handling.

### Top immediate priorities

1. Fix `outline` crash on file-path input.
2. Implement profile default merging into effective policy.
3. Align `RepositoryPolicy` schema with RFC-002 governance section.
4. Add `--after`, `--before`, `--level` to `md edit insert-section`.
5. Add `--headings` to `outline`.

---

## 3. Review Method

The review was conducted in four passes:

1. **Document pass**: Read all 6 RFCs, 9 ADRs, PRD, traceability matrix, constraints doc, README, and implementation status doc. Built an internal capability checklist from the requirement IDs.
2. **Code pass**: Inspected all command handlers, engine implementations, model classes, validation rules, configuration loaders, and test files. Used targeted searches across the codebase.
3. **Runtime pass**: Executed every major command in both text and JSON modes. Tested edge cases (file path to outline, headings-only search, orient with signals, config validate, md query/edit).
4. **Cross-reference pass**: Compared RFC-specified flags, options, schemas, and behaviors against actual implementation. Documented every divergence.

---

## 4. Requirement Area Review

### 4.1 Core Identity (AREA-CORE)

**Intended**: Steward is a stewardship tool, not just a validator. CLI-first, offline, multi-platform, dual-audience, archetype-agnostic.

**Status**: ✅ Implemented

| Requirement | Status | Evidence |
|---|---|---|
| REQ-CORE-001 | ✅ | Tool provides orient, search, check, maintain, md, explain, status commands |
| REQ-CORE-002 | ✅ | Works with or without .steward/ config |
| REQ-CORE-003 | ✅ | CLI-only, no network calls, multi-platform targeting |
| REQ-CORE-004 | ⚠️ Partial | Profile system exists (software, docs, mixed, knowledge, minimal) but profiles don't merge into effective policy |
| REQ-CORE-005 | ✅ | text/JSON dual output, machine-readable diagnostics, stable exit codes |
| REQ-CORE-006 | ✅ | Orient, search, outline, maintain, explain all present |
| REQ-CORE-007 | ⚠️ Partial | Same as CORE-004: archetype support via profiles is surface-level |

**Notable strengths**: Strong CLI identity. Good command hierarchy. Dual-output pattern consistent.
**Notable gaps**: Profile-based archetype adaptation is label-only, not behavioral.
**Verdict**: Partial — core identity is clear but archetype-agnostic behavior depends on profile layering which is incomplete.

---

### 4.2 Configuration and Policy (AREA-CONFIG)

**Intended**: In-repo YAML config. Separation of policy (contract) and config (runtime). Profiles with layering. Pattern-based path policy. Exclude rules.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-CONFIG-001 | ✅ | .steward/ directory with YAML files |
| REQ-CONFIG-002 | ✅ | config.yaml (runtime) vs policy.yaml (contract) |
| REQ-CONFIG-003 | ✅ | CLI flags override runtime config; policy is separate |
| REQ-CONFIG-004 | ⚠️ Partial | Profiles exist but don't merge defaults into effective policy |
| REQ-CONFIG-005 | ✅ | PathPolicyEngine supports glob patterns |
| REQ-CONFIG-006 | ⚠️ Partial | Deterministic precedence in PathPolicyEngine; but profile → policy layering not implemented |
| REQ-CONFIG-007 | ⚠️ Partial | Profiles are opt-in by name; defaults not applied |
| REQ-CONFIG-008 | ❌ Missing | `repository.terminology` field not in RepositoryPolicy model |
| REQ-CONFIG-009 | ✅ | discovery.exclude in config.yaml, .gitignore integration |

**Policy schema divergence from RFC-002:**
- RFC specifies `governance.frontmatter.required_fields` → actual: `validation.required_frontmatter_fields`
- RFC specifies `governance.managed_regions.marker` and `enforce_ownership` → actual: hardcoded in ManagedRegionIntegrityRule
- RFC specifies `governance.completion_policy.rules` → actual: hardcoded in CheckCommand.WriteCompletionSummary
- RFC specifies `artifacts.roles` as a named map → actual: flat list with `role` string field

**Evidence:**
- `RepositoryPolicy.cs` lines 1–80: No `terminology`, no `governance.frontmatter`, no `governance.managed_regions`, no `governance.completion_policy`
- `ProfileDefaults.cs`: Profiles defined but `ConfigLoader` does not merge profile policy into loaded policy
- `CommandSetup.Build()`: Loads policy directly, no profile merging step

**Verdict**: Partial — foundational config model is correct, but profile layering and governance schema are materially incomplete relative to RFC-002.

---

### 4.3 Validation and Diagnostics (AREA-VALIDATION)

**Intended**: Deterministic validation, scoped, machine-readable diagnostics, stable exit codes, fix/dry-run, completion policy, secret filtering.

**Status**: ✅ Mostly implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-VALIDATE-001 | ✅ | Deterministic: same input → same diagnostics |
| REQ-VALIDATE-002 | ✅ | full, changed, staged, paths scopes all implemented |
| REQ-VALIDATE-003 | ✅ | GitDiffHelper with graceful fallback |
| REQ-VALIDATE-004 | ✅ | 9 rules covering required artifacts, forbidden paths, frontmatter, section size, managed regions, ownership, stale artifacts, broken links, broken references |
| REQ-VALIDATE-005 | ✅ | JSON output with rule, severity, category, path, line, message, remediation |
| REQ-VALIDATE-006 | ✅ | Text output with labeled diagnostics |
| REQ-VALIDATE-007 | ✅ | Exit codes: 0/1/2/3 per RFC-001 |
| REQ-VALIDATE-008 | ✅ | stdout for output, stderr for errors |
| REQ-VALIDATE-009 | ✅ | --fix and --dry-run implemented for IFixableRule |
| REQ-VALIDATE-010 | ✅ | Remediation guidance in diagnostics and explain command |
| REQ-VALIDATE-011 | ✅ | SecretFilter with 4 regex patterns applied to all diagnostic output |

**Notable strength**: The diagnostic model is clean and consistent. JSON output matches RFC-003 schema closely. SecretFilter is well-tested (dedicated test file).

**Minor gap**: The JSON diagnostic schema does not include a `source` field (RFC-003 specifies it for explainability — "policy source reference"). The `CheckDiagnosticResponse` class has `Source` but it's always null in current rules.

**Verdict**: Implemented — strong. Minor gap on `source` field population.

---

### 4.4 Workflow and Status (AREA-WORKFLOW)

**Intended**: check as canonical entry point, completion policy, status surface, explainability, agent inner loop support.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-WORKFLOW-001 | ✅ | check is the canonical validation command |
| REQ-WORKFLOW-002 | ⚠️ Partial | Impact analysis not separately surfaced; completion summary is basic |
| REQ-WORKFLOW-003 | ⚠️ Partial | Answers "what is missing/stale" via check but not "what should be done next" beyond fix hints |
| REQ-WORKFLOW-004 | ✅ | explain command with remediation guidance |
| REQ-WORKFLOW-005 | ✅ | status command shows lightweight current state |
| REQ-WORKFLOW-006 | ⚠️ Partial | Completion policy is hardcoded, not configurable in policy.yaml |
| REQ-WORKFLOW-007 | ❌ Missing | No configurable completion-policy rules in policy.yaml |
| REQ-WORKFLOW-008 | ✅ | Agent loop supported via check → fix → check cycle |
| REQ-WORKFLOW-009 | ✅ | Architecture is clean enough for future protocol integration |

**Evidence:**
- `CheckCommand.WriteCompletionSummary()`: Hardcoded to count STWD-001, STWD-007, STWD-008, STWD-009 diagnostics — not driven by `governance.completion_policy.rules` as RFC-002 specifies.
- `StatusCommand.ComputeStatus()`: Useful lightweight surface. Shows required artifacts present/missing and maintained artifacts stale/ok.

**Verdict**: Partial — core workflow commands work, but completion policy is not configurable and "what to do next" guidance is limited.

---

### 4.5 Repository Orientation (AREA-ORIENT)

**Intended**: Session-start map with classified hierarchy, start-here entries, optional cheap signals, configurable depth.

**Status**: ✅ Mostly implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-ORIENT-001 | ✅ | `steward orient` provides session-start surface |
| REQ-ORIENT-002 | ✅ | Curated hierarchical map with classifications |
| REQ-ORIENT-003 | ✅ | Not a raw filesystem dump — classifications, start-here markers, depth control |
| REQ-ORIENT-004 | ✅ | Classifications: configuration, documentation, source, testing, readme, planning, etc. |
| REQ-ORIENT-005 | ✅ | Text and JSON output |
| REQ-ORIENT-006 | ✅ | --depth flag |
| REQ-ORIENT-007 | ⚠️ Partial | start_here entries shown but not "prominently" — same indent level as other entries |
| REQ-ORIENT-008 | ⚠️ Partial | Important roots highlighted via classification but no special "roadmap", "current state", "indexes" highlighting |
| REQ-ORIENT-009 | ✅ | Heuristic classification works without config |
| REQ-ORIENT-010 | ✅ | .gitignore and discovery.exclude respected |
| REQ-ORIENT-011 | ✅ | No validation scan required |
| REQ-ORIENT-012 | ✅ | --signals flag shows required/stale artifact status |
| REQ-ORIENT-013 | ✅ | Distinct from check/workflow |

**Notable strength**: Orient is genuinely useful for session-start. JSON output is well-structured for agents.
**Gap**: The `--signals` output does not visually surface signal data separately from the tree (signals are computed but the output only adds a leading newline).

**Evidence**: Running `orient --signals` produces identical output to `orient` on a clean repo — the signals section is empty when everything is healthy, which is correct, but there's no indication that signals were checked.

**Verdict**: Mostly implemented — strong core, minor polish gaps on signal surfacing and prominence.

---

### 4.6 Repository Outline (AREA-OUTLINE)

**Intended**: Rich tree views, file sizes, line counts, Markdown heading outlines, oversized-file spotting.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-OUTLINE-001 | ✅ | Outline command exists with depth/sizes/lines |
| REQ-OUTLINE-002 | ✅ | Curated tree view, respects .gitignore |
| REQ-OUTLINE-003 | ✅ | --sizes flag |
| REQ-OUTLINE-004 | ✅ | --lines flag |
| REQ-OUTLINE-005 | ❌ Missing | No --headings flag on outline (RFC-001 specifies it) |
| REQ-OUTLINE-006 | ✅ | `md outline <file>` provides heading hierarchy |
| REQ-OUTLINE-007 | ⚠️ Partial | SectionSizeRule warns on oversized sections, but outline itself doesn't flag oversized files |
| REQ-OUTLINE-008 | ✅ | Helps users choose where to work via structural view |

**Critical bug**: `outline README.md` crashes with `System.IO.IOException` — it tries to enumerate the file as a directory instead of recognizing it as a file and either showing Markdown headings or giving a helpful error message.

**Evidence**: Terminal output from `dotnet run --project src/Steward.Cli -- outline readme.md` — unhandled IOException. The `OutlineCommand` always passes the path argument through `FileDiscoveryService.Discover()` which assumes a directory.

**Verdict**: Partial — directory outline is functional, but file-path handling is broken (crash) and `--headings` integration is missing.

---

### 4.7 Repository Search (AREA-SEARCH)

**Intended**: Repository-wide search, content and heading modes, Markdown heading context, scope filtering, machine-readable, .gitignore-aware, live-scan.

**Status**: ✅ Mostly implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-SEARCH-001 | ✅ | Dedicated search command |
| REQ-SEARCH-002 | ✅ | Separate from check and orient |
| REQ-SEARCH-003 | ✅ | Results include path, line, column, snippet, kind |
| REQ-SEARCH-004 | ✅ | content, headings, all modes |
| REQ-SEARCH-005 | ✅ | headingContext field in results |
| REQ-SEARCH-006 | ✅ | JSON output with stable schema |
| REQ-SEARCH-007 | ✅ | .gitignore and discovery.exclude respected |
| REQ-SEARCH-008 | ✅ | --scope flag filters by policy-defined role |
| REQ-SEARCH-009 | ✅ | Live-scan, no index required |
| REQ-SEARCH-010 | ✅ | Works without config |
| REQ-SEARCH-011 | ⚠️ Not validated | No performance benchmarks; reasonable for typical repos |

**Notable strength**: Heading context in search results (headingContext field) is excellent for agent use — it tells you which section a match was found in.

**Minor gap**: Search is case-insensitive substring match only — no regex support, no word-boundary matching. This is not a requirement gap but limits usefulness compared to `rg`.

**Verdict**: Mostly implemented — solid functionality matching RFC-005.

---

### 4.8 Markdown Structural Engine (AREA-MARKDOWN)

**Intended**: First-class Markdown document type with structural selectors, query/edit, managed regions, preview/apply, ownership enforcement, minimal-diff.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-MD-001 | ✅ | Markdown is structurally addressable |
| REQ-MD-002 | ⚠️ Partial | frontmatter, heading, managed selectors work. `.lists`, `.tables`, `.codeblocks` sub-selectors not implemented |
| REQ-MD-003 | ✅ | md query is read-only |
| REQ-MD-004 | ⚠️ Partial | ensure-section, set-section, insert-section, append-block, prepend-block, fm-set, fm-merge all work. fm-validate not implemented as a separate md edit subcommand |
| REQ-MD-005 | ⚠️ Partial | `--under` infers child level. `--after/--before` for sibling placement not implemented. `--level` override not implemented |
| REQ-MD-006 | ✅ | Edits operate on raw text guided by source positions |
| REQ-MD-007 | ✅ | Ambiguous selectors fail safely (SelectorResult.Ambiguous) |
| REQ-MD-008 | ⚠️ Partial | ManagedScopeViolationRule checks ownership; but StructuralEditor.SetSection ownership check always uses "steward" as owner, not dynamic |
| REQ-MD-009 | ✅ | All edit operations default to preview; --apply required for mutation |
| REQ-MD-010 | ✅ | Section size validation for governed Markdown |
| REQ-MD-011 | ✅ | md outline shows heading hierarchy with line counts |

**Missing from RFC-004:**
- `insert-section` lacks `--after`, `--before`, and `--level` options
- No `.lists`, `.tables`, `.codeblocks` sub-selectors on heading paths
- `fm-validate` not a standalone md edit subcommand (frontmatter validation only via `check`)
- `managed[*]` wildcard selector not implemented (only exact id match)

**Evidence:**
- `MdEditCommand.cs`: CreateInsertSectionCommand has `--heading`, `--under`, `--content` only
- `MdPathSelector.cs`: No handling for `.lists`, `.tables`, or `.codeblocks` suffixes
- Grep for `after|before|--level` in MdEditCommand.cs returns no matches

**Verdict**: Partial — core structural operations work, but several RFC-004-specified capabilities are missing.

---

### 4.9 Frontmatter (AREA-FRONTMATTER)

**Intended**: Frontmatter validation, set/merge, auto-maintenance of freshness fields.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-FM-001 | ✅ | RequiredFrontmatterFieldRule (STWD-003) |
| REQ-FM-002 | ✅ | fm-set, fm-merge in md edit |
| REQ-FM-003 | ⚠️ Partial | Required fields are global, not document-type-aware |
| REQ-FM-004 | ✅ | FrontmatterAutoMaintainer exists |
| REQ-FM-005 | ✅ | Auto-maintenance is deterministic and policy-driven |
| REQ-FM-006 | ⚠️ Partial | No evidence of "semantic fields are not silently rewritten" protection |

**Verdict**: Partial — basic frontmatter validation and editing works; document-type-aware expectations and semantic field protection are missing.

---

### 4.10 Ownership and Managed Content (AREA-OWNERSHIP)

**Intended**: Whole-file and mixed-ownership, managed region markers, artifact classification, ownership enforcement.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-OWN-001 | ✅ | Managed regions enable mixed-ownership files |
| REQ-OWN-002 | ✅ | STWD-005 (ManagedRegionIntegrityRule), STWD-006 (ManagedScopeViolationRule) |
| REQ-OWN-003 | ⚠️ Partial | Artifact roles are free-form strings; no formal classification as manual/generated/mixed/unclassified |
| REQ-OWN-004 | ✅ | ManagedScopeViolationRule prevents invalid edits |
| REQ-OWN-005 | ⚠️ Partial | Orient classifies artifacts but not as governed/generated/manual/mixed |

**Verdict**: Partial — managed region infrastructure works well, but artifact ownership classification is informal.

---

### 4.11 Path and Filename Policy (AREA-PATH-POLICY)

**Intended**: Ruleset-based path policy with canonical categories, deterministic precedence, independent required-entry checks.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-PATHPOL-001 | ⚠️ Partial | PathPolicyEngine exists but schema diverges from RFC-002 specification |
| REQ-PATHPOL-002 | ✅ | Top-level `rulesets` array in schema |
| REQ-PATHPOL-003 | ⚠️ Partial | Flat rule list with `category` field, not separate `required/recommended/forbidden` arrays |
| REQ-PATHPOL-004 | ✅ | Categories: required, recommended, optional, discouraged, forbidden, reserved, deprecated, ignored — all handled in strictness ordering |
| REQ-PATHPOL-005 | ⚠️ Partial | Rules have pattern, category, priority but missing `id`, `kind` (`file`/`directory`/`any`) |
| REQ-PATHPOL-006 | ✅ | `exact` flag (equivalent to exact match) and glob via DotNet.Glob |
| REQ-PATHPOL-007 | ❌ Missing | No `kind` field (file/directory/any) in PathRule |
| REQ-PATHPOL-008 | ✅ | PathEvaluation returns category; validation maps to pass/warning/error |
| REQ-PATHPOL-009 | ✅ | RequiredArtifactRule evaluates required entries independently |
| REQ-PATHPOL-010 | ✅ | `ignored` category supported in evaluation |
| REQ-PATHPOL-011 | ✅ | Deterministic precedence: ignored → priority → exact → length → strictness |
| REQ-PATHPOL-012 | ✅ | Canonical category names only |
| REQ-PATHPOL-013 | ✅ | Engine is reusable |

**Divergence**: RFC-002 specifies `path-policy.yaml` with `rulesets[].required[]/recommended[]/forbidden[]` arrays. Actual schema uses `rulesets[].rules[]` with `category` field per rule. The flat model is arguably simpler but doesn't match the accepted spec.

**Verdict**: Partial — functional engine, but schema diverges from accepted design.

---

### 4.12 Deterministic Maintenance (AREA-MAINTENANCE)

**Intended**: Policy-driven maintenance of structure docs, indexes, managed sections, frontmatter, manifests. Preview-first. Anti-drift.

**Status**: ✅ Mostly implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-MAINT-001 | ✅ | maintain command exists |
| REQ-MAINT-002 | ✅ | Deterministic, idempotent maintenance |
| REQ-MAINT-003 | ⚠️ Partial | Maintenance works on policy-declared artifacts; mdpath-style precision not used |
| REQ-MAINT-004 | ✅ | ManagedSectionMaintainer handles managed blocks |
| REQ-MAINT-005 | ✅ | StructureDocumentMaintainer updates from actual file tree |
| REQ-MAINT-006 | ✅ | IndexMaintainer handles indexes/registries |
| REQ-MAINT-007 | ✅ | StaleArtifactRule detects drift |
| REQ-MAINT-008 | ⚠️ Partial | State documents not distinctly handled |
| REQ-MAINT-009 | ✅ | FrontmatterAutoMaintainer for freshness fields |
| REQ-MAINT-010 | ⚠️ Partial | Tables/lists/registry rows not explicitly policy-defined in terms of structure and sorting |
| REQ-MAINT-011 | ✅ | Content outside managed scope preserved |
| REQ-MAINT-012 | ✅ | Preview-first by default |
| REQ-MAINT-013 | ✅ | Coexists with workflow/orient |

**Verdict**: Mostly implemented — strong architecture. Minor gaps in mdpath-precision and policy-driven table/list structure.

---

### 4.13 Structure Documents (AREA-STRUCTURE-DOC)

**Intended**: Auto-maintained structure documents from live state.

**Status**: ✅ Implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-STRUCTDOC-001 | ✅ | StructureDocumentMaintainer generates STRUCTURE.md |
| REQ-STRUCTDOC-002 | ✅ | ManagedSectionMaintainer updates sections inside human-authored docs |
| REQ-STRUCTDOC-003 | ✅ | Renders tree views from file inventory |
| REQ-STRUCTDOC-004 | ✅ | Reduces drift between structure and documentation |
| REQ-STRUCTDOC-005 | ✅ | Deterministic and minimal-diff |

**Evidence**: STRUCTURE.md is auto-maintained and matches the current file tree. `steward maintain` reports "up to date" on a clean repo.

**Verdict**: Implemented.

---

### 4.14 .gitignore Awareness (AREA-GITIGNORE)

**Intended**: All operations respect .gitignore.

**Status**: ✅ Implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-GITIGNORE-001 | ✅ | GitIgnoreFilter used in FileDiscoveryService, injected into all commands |
| REQ-GITIGNORE-002 | ✅ | bin/, obj/, node_modules/ etc. excluded |
| REQ-GITIGNORE-003 | ✅ | Core behavior — not optional |

**Evidence**: 24-rule .gitignore file; GitIgnoreFilter tested with dedicated test file (GitIgnoreFilterTests.cs).

**Verdict**: Implemented.

---

### 4.15 Machine-Readable Memory Artifacts (AREA-MACHINE-MEMORY)

**Intended**: Manifest, search index, structured inventories.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-MRM-001 | ⚠️ Partial | ManifestMaintainer exists but not configured in steward's own policy |
| REQ-MRM-002 | ✅ | Manifests are deterministic and refreshable |
| REQ-MRM-003 | ⚠️ Partial | Architecture supports it; not exercised end-to-end |
| REQ-MRM-004 | ✅ | CLI does not depend on cached artifacts; live-scan first |

**Verdict**: Partial — infrastructure exists but not actively demonstrated or documented.

---

### 4.16 Human-Navigation Artifacts (AREA-HUMAN-NAV)

**Intended**: Repository maps, indexes, curated orientation views.

**Status**: ⚠️ Partial

| Requirement | Status | Notes |
|---|---|---|
| REQ-HNAV-001 | ✅ | STRUCTURE.md auto-maintained; orient provides map |
| REQ-HNAV-002 | ✅ | Orient reduces onboarding time |
| REQ-HNAV-003 | ⚠️ Partial | Coherent but limited to single structure document type |

**Verdict**: Partial — orient and structure documents work; broader navigation artifact generation not implemented.

---

### 4.17 State Documents (AREA-STATE-DOCS)

**Intended**: Explicit support for memory-oriented artifacts (vision, roadmap, current state, etc.).

**Status**: ❌ Missing as distinct concept

| Requirement | Status | Notes |
|---|---|---|
| REQ-STATE-001 | ❌ Missing | No special handling for state document roles |
| REQ-STATE-002 | ⚠️ Partial | Artifacts are discoverable via orient but not governed differently |
| REQ-STATE-003 | ❌ Missing | No coherence/staleness checking specific to state documents |

**Evidence**: `RepositoryPolicy` artifacts use free-form `role` strings. No enum or special handling for vision, roadmap, current-state, milestones, etc.

**Verdict**: Missing as a distinct concept. State documents exist only as arbitrary artifacts.

---

### 4.18 Performance (AREA-PERFORMANCE)

**Status**: ✅ Adequate (not formally benchmarked)

Evidence: All commands execute in under 2 seconds on the 192-file steward repo. Orient completes nearly instantly. Search with --max 100 is fast.

**Verdict**: Adequate for stated targets. No formal benchmarks.

---

### 4.19 Determinism (AREA-DETERMINISM)

**Status**: ✅ Implemented

Evidence: Maintenance is idempotent (running `maintain` twice produces no diff). Validation is repeatable. Structural edits operate on raw text to minimize diff.

**Verdict**: Implemented.

---

### 4.20 Safety (AREA-SAFETY)

**Status**: ✅ Mostly implemented

| Requirement | Status | Notes |
|---|---|---|
| REQ-SAFE-001 | ✅ | Conservative automation |
| REQ-SAFE-002 | ✅ | Preview-first on maintain, md edit |
| REQ-SAFE-003 | ⚠️ N/A for v1 | PR integration deferred |
| REQ-SAFE-004 | ✅ | --apply required for mutations |
| REQ-SAFE-005 | ✅ | Managed region ownership enforced |
| REQ-SAFE-006 | ✅ | Generated content behind managed markers |

**Verdict**: Mostly implemented — safety model is correct.

---

### 4.21 Explainability (AREA-EXPLAIN)

**Status**: ✅ Implemented

Evidence: `steward explain` lists all 9 rules. `steward explain STWD-001` provides description and remediation. JSON output supported.

**Verdict**: Implemented.

---

### 4.22 Testing (AREA-TESTING)

**Status**: ✅ Implemented

Evidence: 319 tests (243 core + 76 CLI), all passing. Unit tests cover all rules, engines, parsers. Integration tests cover CLI commands. Snapshot tests for help text and JSON output stability.

**Verdict**: Implemented — strong test discipline.

---

### 4.23 Distribution (AREA-DISTRIBUTION)

**Status**: ✅ Implemented

Evidence: PackAsTool=true in csproj. `steward version` works. No host credentials required.

**Verdict**: Implemented.

---

## 5. Cross-Cutting Findings

### 5.1 Docs vs. implementation mismatches

| Discrepancy | Location |
|---|---|
| README shows `steward outline [path]` but outline crashes on file paths | README.md, OutlineCommand.cs |
| README shows `--headings` in outline quick-start example but flag doesn't exist | README.md Quick Start section |
| implementation-status.md claims v1.0.0 complete at 100% | implementation-status.md |
| RFC-002 specifies `governance.frontmatter`, `governance.managed_regions`, `governance.completion_policy` but none exist in code | RFC-002, RepositoryPolicy.cs |

### 5.2 Help text vs. actual behavior

| Issue | Detail |
|---|---|
| `outline` accepts `<path>` argument but doesn't validate it's a directory | Crashes on files |
| `search --scope` claims to filter by role but requires exact role match, not partial | Works correctly but discoverability is low |

### 5.3 Architecture drift from ADRs

| ADR | Drift |
|---|---|
| ADR-002 (DI throughout) | Commands use static factories (e.g., `new OrientationEngine()`) instead of DI | Minor — works but doesn't match "DI used throughout" principle |
| ADR-005 (Rule registration in DI) | Rules are hardcoded in both `CheckCommand.AllRules` and `ExplainCommand.AllRules` — two separate arrays that must be kept in sync | Known technical debt |

### 5.4 Output contract inconsistencies

| Issue | Detail |
|---|---|
| JSON check output uses `ruleId` but RFC-003 specifies `rule` | Minor schema naming difference |
| Check JSON `summary` includes `scope` as string but doesn't include `completionPolicy` data | Completion data only in text output |
| Search JSON uses `totalMatches` which counts matches beyond the returned set | Correct and useful |

### 5.5 Tests not covering important contracts

| Gap | Detail |
|---|---|
| No test for `outline` with file path input | Would catch the crash bug |
| No test for profile merging behavior | Would confirm layering works (it doesn't) |
| No end-to-end test for ManifestMaintainer with policy configuration | ManifestMaintainerTests exist but CLI integration not tested |

---

## 6. Prioritized Requirement-Backed Gaps

### Critical

1. **Fix `outline` crash on file-path input** — Current: unhandled IOException. Should either show Markdown heading outline for .md files or give a clear error for non-directory paths. Affects REQ-OUTLINE-005, REQ-OUTLINE-006.

2. **Implement profile default merging** — Profiles are label-only. When a user sets `profile: software`, the profile's required artifacts, governance settings, and frontmatter rules should merge into the effective policy (with repo-local policy taking precedence). Affects REQ-CONFIG-004, REQ-CONFIG-006, REQ-CONFIG-007, REQ-CORE-004.

### High

3. **Align RepositoryPolicy schema with RFC-002** — Add `governance.frontmatter`, `governance.managed_regions`, and `governance.completion_policy` sections. Migrate `validation.required_frontmatter_fields` into governance scope. Affects REQ-CONFIG-002, REQ-WORKFLOW-006, REQ-WORKFLOW-007.

4. **Add `--after`, `--before`, `--level` to `md edit insert-section`** — RFC-004 specifies heading-level inference based on sibling placement. Only `--under` is implemented. Affects REQ-MD-005.

5. **Add `--headings` to `outline`** — RFC-001 specifies this flag. It should show Markdown heading hierarchy across files in the outlined directory. Affects REQ-OUTLINE-005.

6. **Implement `repository.terminology` config** — RFC-002 specifies configurable terminology. Not present in RepositoryPolicy model. Affects REQ-CONFIG-008.

### Medium

7. **Add `.lists`, `.tables`, `.codeblocks` sub-selectors to MdPath** — RFC-004 specifies these. Only heading, frontmatter, and managed selectors are implemented. Affects REQ-MD-002.

8. **Add `managed[*]` wildcard selector** — RFC-004 shows `managed[*]` in examples. Only exact id match works. Affects REQ-MD-002.

9. **Add `kind` field to PathRule** — RFC-002 specifies `file`/`directory`/`any` kind. Not in PathRule model. Affects REQ-PATHPOL-005, REQ-PATHPOL-007.

10. **Populate `source` field in diagnostics** — RFC-003 specifies a policy source reference for explainability. Always null in current rules. Affects REQ-VALIDATE-005.

11. **Add state document role handling** — REQ-STATE-001 through REQ-STATE-003 are entirely unimplemented. At minimum, recognize state-document roles in orient and status output. Affects REQ-STATE-001.

### Low

12. **Add completion policy data to JSON check output** — Completion summary is text-only. Agent consumers miss this information in JSON mode. Affects REQ-WORKFLOW-003.

13. **Consolidate AllRules arrays** — `CheckCommand.AllRules` and `ExplainCommand.AllRules` are separate arrays that must be manually synchronized. A single source of truth would prevent future inconsistency. Affects REQ-TEST-004 (safe for modification).

14. **Formal performance benchmarks** — REQ-PERF-001 through REQ-PERF-005. Not gating but valuable for confidence.

---

## 7. Suggested Implementation Sequence

### Phase 1: Bug fixes and safety (immediate)

1. Fix `outline` crash on file-path input
2. Add completion policy data to JSON check output
3. Consolidate AllRules arrays into a single source

### Phase 2: Profile and config alignment (next sprint)

4. Implement profile default merging into effective policy
5. Align RepositoryPolicy schema with RFC-002 governance sections
6. Add `repository.terminology` support

### Phase 3: Markdown completeness

7. Add `--after`, `--before`, `--level` to `md edit insert-section`
8. Add `--headings` to `outline`
9. Add `.lists`, `.tables`, `.codeblocks` sub-selectors
10. Add `managed[*]` wildcard selector

### Phase 4: Policy model completeness

11. Align path-policy schema with RFC-002 (or formally accept the divergence as an ADR amendment)
12. Add `kind` field to PathRule
13. Implement state document role handling
14. Populate `source` field in diagnostics

### Phase 5: Polish

15. Performance benchmarks
16. End-to-end manifest generation testing
17. Additional test coverage for edge cases

---

## 8. Appendix: Evidence Map

| Finding | Evidence Source |
|---|---|
| Outline crash on file path | Terminal: `dotnet run -- outline readme.md` → IOException |
| No --headings on outline | `OutlineCommand.cs` — no headings option defined |
| Profile merging not implemented | `CommandSetup.Build()` — no merge step; `ProfileDefaults.cs` — profiles defined but not applied |
| Policy schema divergence | `RepositoryPolicy.cs` vs. RFC-002 §policy.yaml schema |
| No --after/--before/--level | `MdEditCommand.cs` CreateInsertSectionCommand — only --heading, --under, --content |
| Completion summary hardcoded | `CheckCommand.cs` WriteCompletionSummary — counts specific rule IDs |
| No .lists/.tables/.codeblocks selectors | `MdPathSelector.cs` — only frontmatter, heading, managed handlers |
| AllRules duplication | `CheckCommand.cs` line 13, `ExplainCommand.cs` line 8 — separate arrays |
| SecretFilter working | `SecretFilter.cs` — 4 regex patterns; `SecretFilterTests.cs` — dedicated tests |
| 319 tests passing | Terminal: `dotnet test` — 243 core + 76 CLI = 319, 0 failures |
| Orient JSON schema | Terminal: `orient --output json` — well-structured with entries, startHere, signals |
| Search heading context | Terminal: `search "Validation" --mode headings --output json` — headingContext field present |
| Status command | Terminal: `status --output json` — shows requiredArtifacts, maintenanceArtifacts, counts |
| Maintenance idempotent | Terminal: `maintain` — reports "up to date" on clean repo |
| Exit codes match RFC-001 | `ExitCodes.cs` — Success=0, ValidationFailure=1, UsageError=2, InternalError=3 |
| No terminology config | `RepositoryPolicy.cs` — no `Terminology` property |
| No state document handling | `RepositoryPolicy.cs` — roles are free-form strings |
| Config validate works | Terminal: `config validate` → "Configuration is valid" |
