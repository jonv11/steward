# Code Quality & Maintainability Review — 2025-07-23

**Reviewer:** Senior principal engineer (automated deep review)
**Scope:** Full codebase — `src/Steward.Cli` (18 files), `src/Steward.Core` (69 files), test projects
**Baseline:** 598 tests passing (407 core, 191 CLI). Zero compiler warnings.
**Post-fix:** 599 tests passing (408 core, 191 CLI). Zero compiler warnings.

---

## Summary

Overall code quality is high for a pre-1.0 project. The architecture is clean (2-project split, clear formatter abstraction, registry-based rules), naming is consistent, and test coverage is thorough. The issues found are incremental maintenance debt — duplicated helpers, a redundant DTO class, a missing well-known constant — not structural problems.

Six fixes were applied. All preserve existing behavior and pass the full test suite.

---

## Fixes Applied

### 1. Eliminated `FlattenSectionsForJson` duplication (MdCommand + OutlineCommand)

**Files:** `Commands/MdCommand.cs`, `Commands/OutlineCommand.cs`
**Problem:** `MdCommand.FlattenForJson` and `OutlineCommand.FlattenSectionsForJson` were byte-for-byte identical implementations (recursive section flattening for JSON output) — 15 lines each.
**Fix:** Made `MdCommand.FlattenForJson` `internal static`; removed the duplicate from `OutlineCommand`; updated call site to `MdCommand.FlattenForJson`.
**Lines removed:** ~15

### 2. Consolidated `AllRules` static field (ExplainCommand → CheckCommand)

**Files:** `Commands/ExplainCommand.cs`
**Problem:** Both `CheckCommand` and `ExplainCommand` maintained independent `static readonly IValidationRule[] AllRules = RuleRegistry.CreateAllRules()` fields, allocating two separate arrays of the same 13 rules. `CheckCommand.AllRules` was already `internal` and referenced by `ConfigCommand`.
**Fix:** Replaced `ExplainCommand`'s private field with a property delegating to `CheckCommand.AllRules`.
**Impact:** One fewer array allocation at startup; single source of truth for rule instances across all commands.

### 3. Eliminated `RepositoryStatusWithCoverage` class (StatusCommand)

**Files:** `Commands/StatusCommand.cs`
**Problem:** `RepositoryStatusWithCoverage` was a near-complete copy of `RepositoryStatus` (14 identical properties) plus one `Coverage` property. The construction site manually copied every field from `status` to the new object — a maintenance trap where adding a property to `RepositoryStatus` would silently drop it from coverage output.
**Fix:** Added `CoverageResponse? Coverage { get; set; }` to `RepositoryStatus`; simplified call site to set `status.Coverage` directly; removed `RepositoryStatusWithCoverage` entirely. JSON shape preserved because `JsonIgnoreCondition.WhenWritingNull` is the default serializer setting.
**Lines removed:** ~30

### 4. Added `"state-document"` to `WellKnownRoles.StateDocumentRoles`

**Files:** `Configuration/WellKnownRoles.cs`, `Commands/StatusCommand.cs`, `WellKnownRolesTests.cs`
**Problem:** The literal role `"state-document"` is widely used — `BootstrapAnalyzer` assigns it to milestone plans, implementation status, roadmaps, and pre-release blockers. `RoleDefaults` maps it to `required` importance with 30-day freshness. But `WellKnownRoles.StateDocumentRoles` did not include it, so `WellKnownRoles.IsStateDocumentRole("state-document")` returned false. `StatusCommand` worked around this with a private `IsStateDocumentRole` wrapper that added an explicit `string.Equals` check.
**Fix:** Added `StateDocument = "state-document"` constant to `WellKnownRoles`; added it to `StateDocumentRoles` and `AllRoles` sets. Inlined the now-trivial `StatusCommand.IsStateDocumentRole` wrapper to call `WellKnownRoles.IsStateDocumentRole` directly, then removed the dead method.
**Impact:** Bug fix — `WellKnownRoles.IsStateDocumentRole` now correctly recognizes the most commonly used state-document role. Tests updated to reflect 6 state-document roles and 11 total roles.

### 5. Removed trailing blank line in `VersionCommand.cs`

**Files:** `Commands/VersionCommand.cs`
**Trivial cleanup:** Extra blank line before closing brace removed.

### 6. Inlined dead `StatusCommand.IsStateDocumentRole` wrapper

**Files:** `Commands/StatusCommand.cs`
**Part of Fix 4.** After adding `"state-document"` to `WellKnownRoles`, the private wrapper became a trivial passthrough. Replaced the single call site with `WellKnownRoles.IsStateDocumentRole(artifact.Role)` and removed the method.

---

## Findings Deferred (With Rationale)

> **Status key:** ✅ Fixed | ⏸ Permanently deferred (rationale stands) | 🔲 Open

### D1. Path normalization — `.Replace('\\', '/')` scattered 30+ times ✅

**Fixed 2026-04-17.** Created `PathHelper.NormalizeSeparators(string)` and `PathHelper.NormalizeAndTrim(string)` in `src/Steward.Core/PathHelper.cs`. Replaced all 70+ inline calls across 23 files. Added `using Steward.Core;` to the 13 Core sub-namespace files that required it. Zero new warnings; all 599 tests pass.

### D2. `BuildTree` duplication (OrientCommand + OutlineCommand) ⏸

Identical ~20-line tree-building algorithm with different node types. A generic `BuildTree<T>` would be clean. **Permanently deferred:** the methods are private, stable, trivially simple, and unlikely to diverge. The abstraction cost (new generic type or interface) outweighs the duplication cost.

### D3. `WriteSectionsText` / `WriteOutlineText` near-duplication ⏸

Similar section-rendering methods in `OutlineCommand` (styled) and `MdCommand` (plain). Not byte-for-byte identical — different styling and prefix handling. **Permanently deferred:** merging would require a configuration parameter or callback for styling, adding complexity without proportional value.

### D4. `IScopeResolver.cs` — 5 types in one file ✅

**Fixed 2026-04-17.** Split into 6 files under `src/Steward.Core/Validation/`: `IScopeResolver.cs` (interface only), `FullScopeResolver.cs`, `ChangedScopeResolver.cs`, `StagedScopeResolver.cs`, `PathsScopeResolver.cs`, `GitDiffHelper.cs`. Pure structural change — no behavior altered.

### D5. `ConfigCommand` validate/show bypass `CommandSetup.TryBuild` ⏸

These subcommands create their own `PhysicalFileSystem` and `ConfigLoader` to validate config files before the full command context exists. **Not a bug** — this is intentional. They need to report config errors, which `TryBuild` would swallow or fail on. Documented here to prevent future "cleanup" that would break the design intent.

### D6. `StatusCommand.cs` is ~600 lines with many inner DTOs ⏸

The file has 8 inner classes (`RepositoryStatus`, `ArtifactFamilySummary`, `ArtifactStatus`, `StateDocumentStatus`, `MaintenanceStatus`, `CoverageResult`, `CoverageResponse`). Extractable, but all are tightly coupled to `StatusCommand`'s computation and serialization logic. **Permanently deferred:** the cohesion is genuine — these types exist only for `StatusCommand`. Splitting into separate files would add file count without improving navigability.

### D7. `CheckCommand.cs` is ~350 lines with inline response DTOs ⏸

Similar to D6 — many inner types but cohesive. The `AllRules` field, fix computation, completion tracking, impact signals, and staged completeness are all check-specific. **Permanently deferred:** same rationale as D6.

---

## Metrics

| Metric | Original | After fixes 1–6 | After D1 + D4 |
| ------ | -------- | --------------- | ------------- |
| Tests passing | 598 | 599 | 599 |
| Compiler warnings | 0 | 0 | 0 |
| Duplicate `FlattenSectionsForJson` | 2 copies | 1 | 1 |
| Duplicate `AllRules` arrays | 2 | 1 (property delegate) | 1 |
| `RepositoryStatusWithCoverage` class | 18 lines + 15-line copy site | Eliminated | Eliminated |
| `WellKnownRoles.StateDocumentRoles` | 5 roles (missing "state-document") | 6 roles (correct) | 6 roles (correct) |
| Dead `IsStateDocumentRole` wrapper | 1 | 0 | 0 |
| Inline path separator normalizations | 70+ across 23 files | 70+ across 23 files | 0 (all use `PathHelper`) |
| `IScopeResolver.cs` type count | — | 5 types in 1 file | 1 type per file (6 files) |
