---
type: audit
status: Historical
standalone: true
---
# Code Quality Pass — 2026-04-16

- **Status:** Complete
- **Scope:** Full codebase quality pass — correctness, consistency, dead patterns, naming, cross-cutting smells

---

## 1. What was reviewed

- All source files under `src/Steward.Core/` and `src/Steward.Cli/`
- Configuration, validation rules, maintenance maintainers, Markdown subsystem, CLI commands
- Test files for coverage gaps related to identified defects
- Cross-cutting concerns: consistency of naming, patterns, and error messages

---

## 2. Issues found

### Correctness

**`BrokenArtifactReferenceRule` (STWD-009) double-reported artifacts with `importance: required`**

`BrokenArtifactReferenceRule` used `!artifact.Required` (the raw `required: bool` field) to avoid double-reporting with STWD-001. STWD-001 resolves importance through `RequiredArtifactRule.ResolveImportance`, which accounts for the `importance:` field and role-linked defaults. An artifact declared as `importance: required` (without setting `required: true`) would be reported by both STWD-001 (as an error) and STWD-009 (as a warning) for the same missing file. **This was a bug.** The fix: use `RequiredArtifactRule.ResolveImportance(artifact) != "required"` as the dedup guard.

### Code smell — unusual control flow

**`goto` labels in `ConfigLoader.ValidateConfig`**

`ValidateConfig` used `goto ValidateOutput:` to skip the profile validity check and fall through to the output settings check. Using `goto` for control flow in C# is a recognized smell (it hides the actual flow, looks like an oversight, and is incompatible with reader expectations). The method was refactored to straightforward `if` blocks.

### Consistency

**`CheckCommand.AllRules` was a computed property; `ExplainCommand.AllRules` was a static field**

`CheckCommand.AllRules` was declared as `=> RuleRegistry.CreateAllRules()`, creating new rule instances on every access. `ExplainCommand.AllRules` was declared as `static readonly ... = RuleRegistry.CreateAllRules()`, creating instances once. Both access patterns appeared in tests. Changed `CheckCommand.AllRules` to a `static readonly` field, matching the `ExplainCommand` pattern.

**`IndexMaintainer.UpdateManagedSection` was `private static` and hardcoded `Type = "index"`**

`DirectoryIndexMaintainer.UpdateManagedSection` is an instance method and uses `Type` (the interface property). `IndexMaintainer.UpdateManagedSection` was `private static` and repeated the literal string `"index"` in three places. Made it a non-static method and replaced the literals with `Type`.

### Inaccurate user-facing message

**Verbosity validation error omitted `debug`**

`ValidateConfig` reported "Valid values: quiet, normal, verbose." but the `Verbosity` enum has four values: `Quiet`, `Normal`, `Verbose`, `Debug`. The message was corrected to "quiet, normal, verbose, debug."

---

## 3. What was fixed

| Change | File | Rationale |
|--------|------|-----------|
| Refactored `goto` labels to structured `if` | [ConfigLoader.cs](../../../src/Steward.Core/Configuration/ConfigLoader.cs) | `goto` is a code smell in C#; structured control flow is clearer and easier to reason about |
| Fixed verbosity validation error message | [ConfigLoader.cs](../../../src/Steward.Core/Configuration/ConfigLoader.cs) | Error message omitted `debug` as a valid value |
| `CheckCommand.AllRules` → `static readonly` field | [CheckCommand.cs](../../../src/Steward.Cli/Commands/CheckCommand.cs) | Consistency with `ExplainCommand`; avoids re-allocating rule instances on every access |
| `BrokenArtifactReferenceRule` dedup guard uses resolved importance | [BrokenArtifactReferenceRule.cs](../../../src/Steward.Core/Validation/Rules/BrokenArtifactReferenceRule.cs) | Fixes double-reporting for artifacts using `importance: required` instead of `required: true` |
| `IndexMaintainer.UpdateManagedSection` made non-static, uses `Type` | [IndexMaintainer.cs](../../../src/Steward.Core/Maintenance/IndexMaintainer.cs) | Consistency with `DirectoryIndexMaintainer`; removes hardcoded string literals |
| New test: `Evaluate_ImportanceRequiredArtifactMissing_NotReported` | [BrokenArtifactReferenceRuleTests.cs](../../../tests/Steward.Core.Tests/BrokenArtifactReferenceRuleTests.cs) | Locks in the STWD-009 dedup behavior for `importance: required` artifacts |

**Result:** 505 tests pass (367 core, 138 CLI), 0 failures.

---

## 4. What was not changed and why

**Frontmatter date-parsing duplication between `FreshnessRule` and `StatusCommand`**

Both classes contain a near-identical `TryGetFrontmatterDate` helper (parse YAML frontmatter `last_updated:` field). They live in different assemblies (`Steward.Core` and `Steward.Cli`). Deduplication would require either promoting the helper to a public API in Core, or introducing a new shared utility class. Left as a follow-up — the duplication is real but the risk of a mid-pass refactor across assembly boundaries exceeds the benefit at this time.

**`ResolveImportance` duplicated in `RequiredArtifactRule` and `StatusCommand`**

Same cross-assembly boundary concern as above. The cleanest resolution is a public static helper in `Core.Configuration`, but that is a separate naming/architecture decision. The STWD-009 fix correctly calls `RequiredArtifactRule.ResolveImportance` (same assembly, internal access) rather than duplicating it again.

**`MaintainCommand.CountDiffLines` called from `CheckCommand`**

A minor coupling between two command classes. A shared formatting utility would be cleaner, but the current dependency is shallow and the coupling is well-contained. Not worth churning for a one-function dependency.

**Async rule evaluation uses `GetAwaiter().GetResult()` in CLI commands**

Known pre-1.0 tradeoff. Safe in a CLI process (no `SynchronizationContext`). All rule implementations are synchronous wrapped in `Task.FromResult`. Changing this would require a more pervasive async threading model change.

**`ProfileDefaults` `"minimal"` profile README has `required: false` but role `"authoritative"`**

The role `"authoritative"` maps to `"required"` importance via `RoleDefaults`, which appears to contradict the explicit `required: false`. However, the `minimal` profile is intentionally sparse and this behavior may be intentional. Needs explicit product-level confirmation before changing, since it affects behavior when no `importance:` override is declared.

---

## 5. Follow-up items

### FRA-001: Extract shared frontmatter-date parsing utility to Core — **Resolved**

Added `FrontmatterEditor.TryGetLastUpdatedDate(IFileSystem, string) → DateTime?` as a public static
method on the existing `FrontmatterEditor` class ([FrontmatterEditor.cs](../../../src/Steward.Core/Markdown/FrontmatterEditor.cs)).
Removed the near-identical private copies from `FreshnessRule` and `StatusCommand`.
Both callers now reference the shared implementation.

### FRA-002: Move `ResolveImportance` to a public shared utility — **Resolved**

Added `ArtifactDefinition.ResolveImportance()` as a public instance method on
[RepositoryPolicy.cs](../../../src/Steward.Core/Configuration/RepositoryPolicy.cs).
Removed the `internal static` copy from `RequiredArtifactRule` and the `private static` copy from
`StatusCommand`. `BrokenArtifactReferenceRule` now calls `artifact.ResolveImportance()` directly,
eliminating the cross-class dependency on `RequiredArtifactRule`. Updated the test in
`RequiredArtifactRuleTests` accordingly.

### FRA-003: Confirm `"minimal"` profile README importance intent — **Confirmed intentional; no change**

The `minimal` profile's README has `required: false` but role `"authoritative"`, which causes
`ResolveImportance()` to return `"required"` via the role default.
Existing tests (`MinimalProfile_StatusTreatsReadmeAsRequiredViaRoleDefaults` and the `FailureCases`
theory in `ProfileReadinessTests`) explicitly assert this behavior and document it as deliberate:
the minimal profile does enforce a README, using role-linked defaults rather than the explicit flag.
No code change needed.
