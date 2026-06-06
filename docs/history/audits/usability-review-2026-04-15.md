---
type: audit
status: Historical
standalone: true
---
# CLI Usability and Configurability Review — 2026-04-15

> Historical scope note (2026-04-16): This review is preserved as ergonomics/configuration evidence for the repo state at the time of review. Current authoritative state now lives in [implementation-status.md](../plans/implementation-status-2026-06-06.md) and the active planning docs under [docs/planning/](../plans/planning-index-2026-06-06.md).

## Scope

Full usability, ergonomics, and configurability review. Emphasis on how the CLI feels to adopt and adapt, with attention to configuration design, command coherence, and ease of repo-specific tuning.

## What was fixed in this pass

| Change | Files | Rationale |
|--------|-------|-----------|
| `maintain --scope` renamed to `--artifact` | `MaintainCommand.cs` | Three commands used `--scope` to mean three different things: file scope (check), artifact id (maintain), policy role (search). Resolved by giving each the right semantic name. |
| `search --scope` renamed to `--role` | `SearchCommand.cs` | See above. `--role` accurately describes filtering by policy-defined artifact role. |
| `maintain --apply --output json` emitted two JSON objects | `MaintainCommand.cs` | Invalid JSON output broke any consumer that parsed the result. The plan evaluation and apply result are now combined into one object with `applied: bool` and optional `changes: []`. |
| `GovernanceConfig.SectionSizeWarningThreshold` made `int?` | `RepositoryPolicy.cs`, `ProfileMerger.cs` | The merger compared the field to the magic value 500 to detect "not set", silently overwriting any explicit user declaration of `section_size_warning_threshold: 500`. Nullable removes the ambiguity. |
| `check --scope` unknown value silently fell through to `full` | `CheckCommand.cs` | Added `AcceptOnlyFromAmong("full", "changed", "staged")`. Invalid values now fail at parse time with exit code 2 instead of silently running a full check. |
| `maintain --diff` ignored when `--apply` was also passed | `MaintainCommand.cs` | The diff output block was inside the preview-only branch. `--diff` now works in both modes. |
| Verify snapshot tests hung on mismatch | `tests/.../Helpers/VerifyInit.cs` | By default, Verify launches a GUI diff tool and waits for it to close on any snapshot failure, hanging the test runner. Added a `[ModuleInitializer]` that sets `DiffEngine_Disabled=true`. |
| `config` subcommand descriptions sharpened | `ConfigCommand.cs` | `show`, `validate`, `doctor`, and `suggest` each had generic descriptions. Updated to precisely describe what each subcommand does so help output is self-navigating. |
| `init` next-steps expanded | `InitCommand.cs` | Next-steps guide did not mention `config suggest` or `path-policy.yaml`. Both are important early steps for repo adaptation. |
| README command table expanded | `README.md` | `config doctor`, `config suggest`, and `explain path` were implemented but absent from the table. Users and agents deriving capability from the README had an incomplete picture. |
| README configuration guide expanded | `README.md` | Added full `policy.yaml` example showing `frontmatter_requirements`, `severity_overrides`, and `path_overrides`; added `path-policy.yaml` naming convention example; added adoption workflow section. |

**Tests changed:** snapshot updated, `Check_Scope_Invalid_FallsBackToFull` corrected to expect exit 2, `Scope_FiltersToSingleArtifact` updated for `--artifact` rename. New tests: `Apply_WithJsonOutput_ProducesSingleJsonObject`, `Diff_ShowsChanges_WhenCombinedWithApply`, `Merge_GovernanceThreshold_ExplicitlySet500_IsRespected`, `Merge_GovernanceThreshold_NotSet_FallsBackToProfile`, `Search_RoleOption_IsAccepted`, `Check_InvalidScope_ReturnsUsageError`.

**Result:** 479 tests pass (366 core, 113 CLI), 0 failures.

---

## Remaining items for follow-up

### 1. `GitDiffHelper` has no stdin protection

**What:** `GitDiffHelper.RunGitDiff` calls `process.StandardOutput.ReadToEnd()` without first closing `StandardInput`. On some git configurations (credential helpers, GPG signing), this can cause git to block waiting for stdin input. The 10-second `WaitForExit` timeout is a partial mitigation, but if git is reading stdin rather than running, it won't exit.

**Impact:** `steward check --scope changed` and `--scope staged` can hang indefinitely in affected environments. This was observed to silently stall test runs when tests used `--scope changed` in a repository with no commits.

**Fix:** Add `process.StandardInput.Close()` immediately after `Process.Start()` in `GitDiffHelper.RunGitDiff`. Consider also adding a separate stderr drain to prevent deadlocks on large stderr output.

**Priority:** Medium — only manifests in `changed`/`staged` scope and specific git environments, but the failure mode is a silent hang with no error message.

---

### 2. `config show --effective` does not display the merged policy

**What:** `config show --effective` prints the resolved *runtime* defaults (output format, verbosity, discovery excludes) but not the merged policy that commands will actually use. The merged policy is the combination of `policy.yaml` and the active profile's defaults, applied by `ProfileMerger.Merge`. A user who sets `profile: software` but writes a minimal `policy.yaml` may not realize the profile's artifact list is being injected.

**Impact:** Operators and AI agents cannot verify what governance rules are effectively in force without reading both `policy.yaml` and the profile source code. This violates the principle that the CLI should make its own behavior inspectable.

**Fix:** Add a `show --merged-policy` flag (or extend `--effective`) that prints the output of `ProfileMerger.Merge(policy, profilePolicy)` as YAML. Alternatively, include it as an additional section in the JSON output of `config show`.

**Priority:** Medium — affects operator confidence, documentation workflows, and AI agent usability.

---

### 3. `config validate` only catches YAML parse errors

**What:** `config validate` runs `ConfigLoader.LoadConfig`, `LoadPolicy`, and `LoadPathPolicy` and reports any `YamlException`. It does not validate semantic correctness: unknown maintainer types, missing required fields (`id`, `path` in maintenance artifacts), impossible `depends_on` references, or invalid severity override targets.

**Impact:** A user can write `type: nonexistent-type` in a maintenance artifact, pass `config validate`, and only discover the problem when `steward maintain` runs silently produces no output. The same applies to malformed `frontmatter_requirements` patterns and invalid rule IDs in `disabled_rules`.

**Fix:** Add a semantic validation pass in `ConfigLoader` or a separate `ConfigSemanticValidator` class that checks: maintenance artifact type is a known value; `id` and `path` are non-empty on maintenance artifacts; `depends_on` references exist; `disabled_rules` entries match known rule IDs; `severity_overrides` keys match known rule IDs; `frontmatter_requirements[].pattern` is a valid glob.

**Priority:** Medium — `config validate` currently gives false confidence. The fix is well-scoped.

---

### 4. Maintenance artifact types `frontmatter-auto` and `manifest` are undocumented

**What:** The README `policy.yaml` example only shows `structure-document` and `directory-index`. The `frontmatter-auto` and `manifest` maintainer types are implemented and tested but have no README examples. The `fields`, `managed_section`, `targets`, and `source` properties of `MaintenanceArtifactDef` are similarly absent from the README.

**Impact:** Adopters cannot discover or use these types without reading source code. The CLI becomes partially opaque for one of its most config-sensitive features.

**Fix:** Add a `### Maintenance artifact types` section to the README listing all four types (`structure-document`, `directory-index`, `frontmatter-auto`, `manifest`) with a minimal YAML example for each and a description of which properties apply.

**Priority:** Low-Medium — affects discoverability and adoption for maintenance-heavy repos.

---

### 5. Two overlapping frontmatter requirement declarations

**What:** Required frontmatter fields can be declared in two places in `policy.yaml`:
- `governance.frontmatter.required_fields` — applied globally to all Markdown files
- `validation.required_frontmatter_fields` — also applied globally  
- `validation.frontmatter_requirements[].required_fields` — applied per glob pattern

The interaction between `governance.frontmatter.required_fields` and `validation.required_frontmatter_fields` is not documented. Looking at `FrontmatterValidationRule`, both are likely additive, but a user cannot tell from the README or help output.

**Impact:** Operators who use both fields may get duplicate or inconsistent enforcement without understanding why. New adopters who search "required frontmatter" may land on either key and miss the other.

**Fix:** Consolidate to one canonical path. Either deprecate `validation.required_frontmatter_fields` in favor of `governance.frontmatter.required_fields`, or document both explicitly in the README with a note that they are additive. Whichever path is chosen, add a `config doctor` check that warns when both are non-empty.

**Priority:** Low-Medium — a source of ongoing confusion for new adopters.

---

### 6. `explain path <file>` error message when no config is present

**What:** When `steward explain path <file>` is run outside a steward-managed repository (no `.steward/` found), `CommandSetup.TryBuild` fails and returns a generic configuration error: "Could not load steward configuration. Run from a steward-managed repository." The exit code is `InternalError` (3).

**Impact:** The error is technically correct but the exit code is wrong — this is a usage error (2), not an internal error (3). An operator running `explain path` in the wrong directory gets a misleading signal for scripting purposes.

**Fix:** Change the exit code to `ExitCodes.UsageError` and improve the message to: "Not inside a steward-managed repository. Run 'steward init' to initialize, or use '--config' to specify the config directory."

**Priority:** Low — small fix, primarily affects exit code correctness for scripting.

---

### 7. `search` has no directory scope option

**What:** `search --role <role>` restricts results to artifacts with a specific policy role. There is no way to restrict results to a specific directory subtree (e.g., `steward search TODO docs/`). Users often want to search within a specific area of the repo without first declaring all files in that area as policy artifacts.

**Impact:** Power users have to post-filter `--output json` results externally. This is a usability gap for repos with large doc trees.

**Fix:** Add `--path <glob>` to `SearchCommand` that filters `ctx.Files` before passing to `SearchEngine`. The implementation is small — it reuses the existing `FileDiscoveryService` pattern. Alternatively, accept a trailing positional argument after the query (e.g., `steward search <query> [path-glob]`).

**Priority:** Low — quality-of-life for large repos, no architectural complexity.

---

### 8. `config suggest` output is not machine-readable

**What:** `config suggest` analyzes the repository and prints artifact and exclude suggestions as plain text. It does not support `--output json`.

**Impact:** Automation pipelines and AI agents that want to use suggestions programmatically must parse free-form text. The feature's highest-value use case is bootstrapping policy.yaml without manual editing — a task typically done by agents.

**Fix:** Honour the global `--output json` flag in `CreateSuggestCommand`. The `BootstrapAnalyzer.Analyze` result is already a typed object (`BootstrapSuggestion`); it just needs to pass through `ctx.Formatter.WriteObject`.

**Priority:** Low — the JSON path already exists in the command; it is a small completion.
