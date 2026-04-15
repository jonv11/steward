# Implementation Status

Last updated: 2026-04-15

## Overview

| Milestone | Status | Completion |
|-----------|--------|------------|
| v0.8.0 — Deterministic Maintenance | ✅ Complete | 100% |
| v0.9.0 — Workflow Completeness | ✅ Complete | 100% |
| v1.0.0 — Release Readiness | ✅ Complete | 100% |
| Post-v1 — RFC-007 Governance Enhancements | 🔶 In Progress | See below |

**Total tests: 472 (all passing)**
**Validation rules: 13 (STWD-001 through STWD-013)**
**Commands: 16**

---

## v0.8.0 — Deterministic Maintenance

### Maintenance Engine ✅
- `MaintenanceEngine` orchestrates 5 maintainers via registry pattern
- `MaintenancePlan` with per-artifact actions, expected vs actual content, has-changes detection

### Artifact Maintainers ✅
| Maintainer | Type | File |
|------------|------|------|
| StructureDocumentMaintainer | `structure-document` | `src/Steward.Core/Maintenance/StructureDocumentMaintainer.cs` |
| IndexMaintainer | `index` | `src/Steward.Core/Maintenance/IndexMaintainer.cs` |
| ManagedSectionMaintainer | `managed-section` | `src/Steward.Core/Maintenance/ManagedSectionMaintainer.cs` |
| FrontmatterAutoMaintainer | `frontmatter-auto` | `src/Steward.Core/Maintenance/FrontmatterAutoMaintainer.cs` |
| ManifestMaintainer | `manifest` | `src/Steward.Core/Maintenance/ManifestMaintainer.cs` |

### Maintain Command ✅
- `steward maintain [--scope <id>] [--apply] [--output text|json]`
- Default: preview mode (safe by default)
- Apply mode writes changes
- Idempotent (running twice produces no diff)

### Anti-Drift Rules ✅
- STWD-007 `StaleArtifactRule`: detects maintained artifacts that are stale
- Reports `stale-artifact` category diagnostics in `steward check`

### Check --fix and --dry-run ✅
- `IFixableRule` interface in `src/Steward.Core/Validation/IFixableRule.cs`
- `StaleArtifactRule` implements `IFixableRule` with `ComputeFixesAsync`
- `--fix`: applies deterministic fixes for stale maintained artifacts
- `--dry-run`: shows what `--fix` would change without applying

### Machine-Readable Manifest ✅
- `ManifestMaintainer` generates `.steward/generated/manifest.json`
- Contains file inventory, extensions, heading index from Markdown files
- Registered as maintenance artifact type `manifest`

---

## v0.9.0 — Workflow Completeness

### Completion Policy ✅
- Check text output includes completion summary section
- Counts required artifacts missing, stale artifacts, broken links, broken references
- Actionable guidance (e.g., "run 'steward maintain --apply'")

### Status Command ✅
- `steward status [--output text|json]`
- Shows: repository name/type, file count, start-here entries
- Required artifacts with OK/MISSING status
- Maintained artifacts with OK/STALE status
- Completeness: `{present}/{required}` required artifacts present

### Explain Command ✅
- `steward explain <rule-id> [--output text|json]`
- Lists all 9 rules when no argument given
- Shows rule metadata + remediation guidance for each
- Covers STWD-001 through STWD-009

### Broken-Reference Rules ✅
| Rule | ID | Category | Description |
|------|----|----------|-------------|
| BrokenInternalLinkRule | STWD-008 | broken-link | Internal Markdown links should resolve to existing files |
| BrokenArtifactReferenceRule | STWD-009 | broken-reference | Policy artifact paths should resolve to existing files |

### Scoped Validation ✅
- `--scope full|changed|staged`: validation scope via git diff integration
- `--paths <file> [<file>...]`: validate explicit file paths only
- `IScopeResolver` with FullScopeResolver, ChangedScopeResolver, StagedScopeResolver, PathsScopeResolver
- Graceful fallback to full scope when git is unavailable

---

## v1.0.0 — Release Readiness

### Safety Audit ✅
- `SecretFilter` with 4 regex patterns (API keys, AWS keys, secrets/tokens, connection strings)
- Applied to all diagnostic output (messages and remediation)
- All mutation paths default to preview-first (maintain, md edit)

### Cross-Platform ✅
- Path handling uses OS-independent separators
- InMemoryFileSystem normalizes paths for testing
- GitDiffHelper normalizes backslashes from git output

### README and Documentation ✅
- README.md: overview, installation, quick start, command reference, global options
- Validation rules table: all 9 rules documented
- Configuration sections: config.yaml, policy.yaml, path-policy.yaml, profiles

### Distribution Packaging ✅
- `PackAsTool=true`, `ToolCommandName=steward` in csproj
- `dotnet tool install --global Steward.Cli` ready
- `steward version` reports correct version

### Dog-Fooding ✅
- `.steward/config.yaml` and `.steward/policy.yaml` configured for this repository
- `steward check` passes clean on the steward repo (0 errors, 0 warnings)

---

## Validation Rules Reference

| ID | Rule Class | Category | Severity | Fixable | File |
|----|-----------|----------|----------|---------|------|
| STWD-001 | RequiredArtifactRule | path-policy | Error | No | `Rules/RequiredArtifactRule.cs` |
| STWD-002 | ForbiddenPathRule | path-policy | Error | No | `Rules/ForbiddenPathRule.cs` |
| STWD-003 | RequiredFrontmatterFieldRule | frontmatter | Error | No | `Rules/RequiredFrontmatterFieldRule.cs` |
| STWD-004 | SectionSizeRule | governance | Info | No | `Rules/SectionSizeRule.cs` |
| STWD-005 | ManagedRegionIntegrityRule | structure | Error | No | `Rules/ManagedRegionIntegrityRule.cs` |
| STWD-006 | ManagedScopeViolationRule | ownership | Warning | No | `Rules/ManagedScopeViolationRule.cs` |
| STWD-007 | StaleArtifactRule | stale-artifact | Warning | **Yes** | `Rules/StaleArtifactRule.cs` |
| STWD-008 | BrokenInternalLinkRule | broken-link | Warning | No | `Rules/BrokenInternalLinkRule.cs` |
| STWD-009 | BrokenArtifactReferenceRule | broken-reference | Warning | No | `Rules/BrokenArtifactReferenceRule.cs` |
| STWD-010 | NamingConventionRule | path-policy | Warning | No | `Rules/NamingConventionRule.cs` |
| STWD-011 | IndexCompletenessRule | index-completeness | Warning | No | `Rules/IndexCompletenessRule.cs` |
| STWD-012 | FreshnessRule | freshness | Warning | No | `Rules/FreshnessRule.cs` |
| STWD-013 | OrphanedDocumentRule | discoverability | Info | No | `Rules/OrphanedDocumentRule.cs` |

---

## Post-v1 — RFC-007 Governance Enhancements (In Progress)

Implementation of items from the [RFC-007 Governance Enhancements Backlog](planning/rfc-007-governance-enhancements-backlog.md). These extend the v1.0.0 baseline with new rules, commands, maintainers, and configuration capabilities.

### New Validation Rules ✅

| Rule | ID | Category | Backlog Item | Description |
|------|----|----------|--------------|-------------|
| NamingConventionRule | STWD-010 | path-policy | G7-03 | Enforces `must_match` regex naming conventions in path-policy |
| IndexCompletenessRule | STWD-011 | index-completeness | G7-08 | Checks that all .md files in `index_of` directories are linked from the index |
| FreshnessRule | STWD-012 | freshness | G7-09 | Detects state documents exceeding `freshness.max_age_days` window |
| OrphanedDocumentRule | STWD-013 | discoverability | G7-14 | Finds Markdown files unreachable from any navigation surface |

### New Commands ✅

| Command | Backlog Item | Description |
|---------|--------------|-------------|
| `steward refs <path>` | G7-18 | Show inbound and outbound Markdown references for a file (`--to`, `--from`) |
| `steward refactor move <old> <new>` | G7-19 | Move/rename a file and update all Markdown references (`--preview`, `--apply`) |

### New Core Components ✅

| Component | Backlog Item | Description |
|-----------|--------------|-------------|
| DirectoryIndexMaintainer | G7-10 | Maintenance generator for directory-based index tables |
| MoveEngine | G7-19 | Reference-aware file move/rename with link rewriting |
| BootstrapAnalyzer | G7-20 | Heuristic analysis for `steward init --analyze` suggestions |
| RoleDefaults / WellKnownRoles | G7-13 | Role-linked behavioral defaults (e.g., freshness for state-documents) |
| DocumentCache | — | Caching layer for parsed Markdown documents across rule evaluations |

### Additional Enhancements ✅

| Enhancement | Backlog Item | Description |
|-------------|--------------|-------------|
| Maintenance dependency modeling | G7-11 | `depends_on` declarations for ordered maintenance |
| Change-impact analysis | G7-15 | Downstream impact detection in check output |
| Governance coverage reporting | G7-16 | `steward status --coverage` for governance maturity view |
| Staged-scope completeness | G7-17 | Coherence checking for staged commit sets |
| `--quiet` flag on check | — | Suppress output, return exit code only |
| `--headings` flag on outline | — | Inline Markdown heading outlines in directory tree |
| Outline file-path handling | — | `outline README.md` delegates to Markdown outline instead of crashing |
| Profile default merging | — | Profile defaults merge into effective policy via ProfileMerger |

### RFC-007 Items Not Yet Implemented

| Item | Summary | Status |
|------|---------|--------|
| G7-01 | Per-path rule suppression (`validation.path_overrides`) | Planned — v1.1.0 |
| G7-02 | Scoped frontmatter requirements per path pattern | Planned — v1.1.0 |
| G7-04 | Post-fix and maintain diff output | Planned — v1.1.0 |
| G7-05 | Rule scope transparency in `explain --verbosity verbose` | Planned — v1.1.0 |
| G7-06 | Effective policy explanation (`steward explain path <path>`) | Planned — v1.2.0 |
| G7-07 | Configuration doctor (`steward config doctor`) | Planned — v1.2.0 |
| G7-12 | Three-level artifact classification (required/recommended/optional) | Planned — v1.3.0 |

---

## Test Coverage Summary

| Test File | Tests | Coverage Area |
|-----------|-------|---------------|
| BrokenArtifactReferenceRuleTests | 6 | STWD-009 rule |
| BrokenInternalLinkRuleTests | 5 | STWD-008 rule |
| ManifestMaintainerTests | 6 | Manifest generation |
| StaleArtifactRuleTests | 7 | STWD-007 + IFixableRule |
| NamingConventionRuleTests | — | STWD-010 rule |
| IndexCompletenessRuleTests | — | STWD-011 rule |
| FreshnessRuleTests | — | STWD-012 rule |
| OrphanedDocumentRuleTests | — | STWD-013 rule |
| DirectoryIndexMaintainerTests | — | Directory-index generation |
| MoveEngineTests | — | Reference-aware move engine |
| BootstrapAnalyzerTests | — | Bootstrap analysis heuristics |
| RoleDefaultsTests | — | Role-linked defaults |
| MaintenanceDependencyTests | — | Dependency modeling |
| CheckFixTests | 8 | --fix, --dry-run, --scope, completion summary |
| ExplainCommandTests | 8 | Explain with all 13 rules |
| CheckCommandTests | 2 | Disabled rules, JSON severity |
| MaintainCommandTests | 3+ | Preview/apply/idempotency |
| StatusCommandTests | 3+ | Text/JSON output |
| RefsCommandTests | — | Reference graph queries |
| ChangeImpactTests | — | Change-impact analysis |
| GovernanceCoverageTests | — | Governance coverage |
| StagedCompletenessTests | — | Staged-scope completeness |
| CliSnapshotTests | 2 | Help and JSON schema stability |

---

## Deferred / Not In Scope

| Item | Reason |
|------|--------|
| `AllRequiredPresentRule` / `NoStaleIndexesRule` as separate classes | Aggregated in CheckCommand's completion summary — same diagnostic effect |
| State-document roles as enum | Artifact roles remain free-form strings; role-linked defaults (G7-13) provide behavioral meaning without constraining the taxonomy |
| Performance profiling benchmarks | Informational only, not gating for v1.0.0 |
| Snapshot tests for all 16 commands | Root help and check JSON covered; remaining commands have dedicated integration tests |
| G7-01 per-path rule suppression | Planned for v1.1.0, not yet implemented |
| G7-02 scoped frontmatter requirements | Planned for v1.1.0, not yet implemented |
| G7-04 post-fix/maintain diff output | Planned for v1.1.0, not yet implemented |
| G7-05 rule scope transparency in explain | Planned for v1.1.0, not yet implemented |
| G7-06 effective policy explanation | Planned for v1.2.0, requires ADR |
| G7-07 configuration doctor | Planned for v1.2.0, requires ADR |
| G7-12 three-level artifact classification | Planned for v1.3.0, requires ADR |
| v1.6.0 artifact type schema system | Planned for v1.6.0, requires design RFC (T6-07) |
