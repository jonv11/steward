# Implementation Status

Last updated: 2025-07-18

## Overview

| Milestone | Status | Completion |
|-----------|--------|------------|
| v0.8.0 — Deterministic Maintenance | ✅ Complete | 100% |
| v0.9.0 — Workflow Completeness | ✅ Complete | 100% |
| v1.0.0 — Release Readiness | ✅ Complete | 100% |

**Total tests: 319 (all passing)**
**Validation rules: 9 (STWD-001 through STWD-009)**
**Commands: 14**

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

---

## Test Coverage Summary

| Test File | Tests | Coverage Area |
|-----------|-------|---------------|
| BrokenArtifactReferenceRuleTests | 6 | STWD-009 rule |
| BrokenInternalLinkRuleTests | 5 | STWD-008 rule |
| ManifestMaintainerTests | 6 | Manifest generation |
| StaleArtifactRuleTests | 7 | STWD-007 + IFixableRule |
| CheckFixTests | 8 | --fix, --dry-run, --scope, completion summary |
| ExplainCommandTests | 8 | Explain with all 9 rules |
| CheckCommandTests | 2 | Disabled rules, JSON severity |
| MaintainCommandTests | 3+ | Preview/apply/idempotency |
| StatusCommandTests | 3+ | Text/JSON output |
| CliSnapshotTests | 2 | Help and JSON schema stability |

---

## Deferred / Not In Scope

| Item | Reason |
|------|--------|
| `AllRequiredPresentRule` / `NoStaleIndexesRule` as separate classes | Aggregated in CheckCommand's completion summary — same diagnostic effect |
| State-document roles as enum | Artifact roles remain free-form strings; no user-facing impact since orient/status display them as-is |
| Performance profiling benchmarks | Informational only, not gating for v1.0.0 |
| Snapshot tests for all 14 commands | Root help and check JSON covered; remaining commands have dedicated integration tests |
