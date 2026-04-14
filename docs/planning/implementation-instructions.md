# Implementation Instructions

- **Document ID:** PLAN-0003
- **Version:** 1.0.0
- **Status:** Accepted

Per-milestone implementation guide. Directly usable by a coding agent or human developer.

---

## v0.1.0 — Project Foundation

### What to implement

1. **Solution and projects**
   - Create `steward.sln` at repository root.
   - Create `src/Steward.Cli/Steward.Cli.csproj` — Console app, `net10.0`, `PackAsTool=true`, `ToolCommandName=steward`.
   - Create `src/Steward.Core/Steward.Core.csproj` — Class library, `net10.0`.
   - Create `tests/Steward.Core.Tests/Steward.Core.Tests.csproj` — xUnit test project.
   - Create `tests/Steward.Cli.Tests/Steward.Cli.Tests.csproj` — xUnit test project.
   - Create `tests/Steward.TestFixtures/Steward.TestFixtures.csproj` — Class library for shared test helpers.
   - Add NuGet references: `System.CommandLine` to Cli, `xunit` + `FluentAssertions` to test projects.

2. **CLI entry point**
   - `Program.cs`: Build root command with System.CommandLine.
   - Register global options: `--output` (text/json), `--verbosity` (quiet/normal/verbose/debug), `--no-color`, `--config`.
   - Register `version` command.
   - Set up DI container with `Microsoft.Extensions.DependencyInjection`.

3. **Version command**
   - `steward version` prints assembly version, .NET runtime version, OS.
   - `steward version --output json` returns JSON object with version info.

4. **Output formatter abstraction**
   - `IOutputFormatter` interface in Core with `WriteObject<T>`.
   - `TextFormatter` and `JsonFormatter` implementations in Cli.
   - JSON uses `System.Text.Json` with camelCase.
   - Text respects `--no-color` and terminal detection.

5. **Exit code constants**
   - `ExitCodes` static class: `Success = 0`, `ValidationFailure = 1`, `UsageError = 2`, `InternalError = 3`.

6. **IFileSystem abstraction**
   - Interface in Core: `FileExists`, `DirectoryExists`, `ReadAllText`, `ReadAllLines`, `GetFiles`, `GetDirectories`, `GetFileInfo`.
   - `PhysicalFileSystem` implementation in Cli.
   - `InMemoryFileSystem` in TestFixtures for unit testing.

### Tests to write
- `VersionCommandTests`: verify text output, JSON output, exit code 0.
- `GlobalOptionsTests`: verify help text includes all global options.
- `ExitCodeTests`: verify unknown command returns exit code 2.
- `JsonFormatterTests`: verify camelCase, valid JSON output.
- `TextFormatterTests`: verify no ANSI codes when `--no-color`.

### Docs to update
- None (docs already created in planning phase).

### Conventions
- Use `Directory.Build.props` for shared project properties (nullable, implicit usings, version).
- Use `.editorconfig` for C# coding style.
- Add `.gitignore` for .NET (bin/, obj/, *.user, etc.).

### Completion criteria
- `dotnet build` succeeds.
- `dotnet test` passes all tests.
- `steward version` works.
- `steward --help` shows command list.

---

## v0.2.0 — Discovery and Orientation

### What to implement

1. **IIgnoreFilter and GitIgnoreFilter**
   - Parse `.gitignore` files (root + nested).
   - Support: negation (`!`), directory patterns (`dir/`), `**` recursive, comments, blank lines.
   - `IsIgnored(relativePath, isDirectory)` method.
   - Cache parsed patterns per directory.

2. **FileDiscoveryService**
   - Walks directory tree using `IFileSystem`.
   - Applies `IIgnoreFilter` with early directory pruning.
   - Returns `DiscoveredFile` records: path, size, isDirectory.
   - Respects additional exclude patterns from config (when available, else empty).

3. **OrientationEngine**
   - Takes discovered files, classifies them using heuristics (in unconfigured mode).
   - Heuristic classification: README → authoritative, LICENSE → authoritative, docs/ → documentation, src/ → source, tests/ → testing, .github/ → workflow.
   - Builds curated hierarchical map with configurable depth.
   - Returns `OrientationResult` model.

4. **Orient command**
   - `steward orient [--depth N] [--output text|json]`
   - Text: indented tree with classification labels.
   - JSON: nested object with path, classification, children.

5. **OutlineEngine**
   - Directory outline: file tree with optional sizes and line counts.
   - Returns `OutlineResult` model.

6. **Outline command**
   - `steward outline [path] [--depth N] [--sizes] [--lines] [--output text|json]`
   - Text: indented tree, sizes in human-readable format.
   - JSON: array of entries with path, size, lineCount, isDirectory.

### Tests to write
- `GitIgnoreFilterTests`: patterns, negation, nested files, directory patterns, `**` globs.
- `FileDiscoveryServiceTests`: discovers files, prunes ignored dirs, respects excludes.
- `OrientationEngineTests`: heuristic classification, depth limiting.
- `OutlineEngineTests`: sizes, line counts, depth limiting.
- `OrientCommandTests` (integration): verify text and JSON output on fixture repo.
- `OutlineCommandTests` (integration): verify text and JSON output.

### Conventions
- All file paths in output use forward slashes.
- Sizes displayed as B, KB, MB.

### Completion criteria
- `steward orient` on a sample repo shows classified tree.
- `steward outline --sizes --lines` shows sizes and line counts.
- No ignored files appear in any output.
- Works on repo with no `.steward/` directory.

---

## v0.3.0 — Configuration and Path Policy

### What to implement

1. **Config directory discovery**
   - Look for `.steward/` starting from CWD, walking up to repo root (first `.git` parent).
   - If `--config` flag provided, use that path.

2. **Config and policy models**
   - `StewardConfig` class: profile, output, discovery/exclude.
   - `RepositoryPolicy` class: repository info, artifacts/roles, governance, validation.
   - `PathPolicyDocument` class: rulesets array with rules.
   - YAML deserialization using YamlDotNet with strongly-typed models.
   - Add `YamlDotNet` NuGet package to Core.

3. **Profile system**
   - Built-in profiles as embedded JSON/YAML resources: `software`, `docs`, `mixed`, `knowledge`, `minimal`.
   - Each profile provides default policy values.
   - Merge: built-in defaults → profile → repo policy.

4. **Path policy engine**
   - Parse `path-policy.yaml` into ruleset model.
   - Add `DotNet.Glob` NuGet package to Core.
   - Evaluate paths against rulesets with documented precedence.
   - Canonical categories: required, recommended, optional, discouraged, forbidden, reserved, deprecated, ignored.
   - `ignored` short-circuits. Higher priority wins. More specific wins. Exact over glob. Stricter category wins.
   - Return evaluation result per path.

5. **Init command**
   - `steward init [--profile <name>]`
   - Creates `.steward/config.yaml` and `.steward/policy.yaml` with profile defaults.
   - Does not overwrite existing files.

6. **Config commands**
   - `steward config validate` — parse and validate config/policy files.
   - `steward config show [--effective]` — display merged configuration.

7. **Update orient and outline**
   - Orient now reads policy for artifact roles, start-here entries.
   - Discovery now merges config excludes with .gitignore.

### Tests to write
- `ConfigLoaderTests`: YAML parsing, missing fields, unknown fields warning.
- `ProfileMergerTests`: layering precedence.
- `PathPolicyEngineTests`: all 8 categories, precedence rules, glob vs exact, ignored short-circuit, required presence checks.
- `InitCommandTests` (integration): creates files, respects existing.
- `ConfigValidateTests` (integration): valid and invalid configs.

### Completion criteria
- `steward init --profile software` scaffolds correct config.
- `steward config validate` catches invalid YAML and schema errors.
- Path policy evaluates correctly per documented precedence.
- Orient uses policy-driven classification.

---

## v0.4.0 — Validation and Check

### What to implement

1. **Diagnostic model**
   - `Diagnostic` record: RuleId, Severity, Category, Path, Line, Message, Remediation, Source.
   - `DiagnosticSeverity` enum: Error, Warning, Info.
   - `ValidationResult` record: Summary (scope, filesChecked, errors, warnings, infos, pass), Diagnostics list.

2. **IValidationRule interface**
   - `RuleId`, `Category`, `DefaultSeverity`, `Description`.
   - `EvaluateAsync(ValidationContext)` returns diagnostics.
   - `ValidationContext`: RepositoryInfo, EffectivePolicy, TargetPaths, FileSystem, CancellationToken.

3. **Scope resolution**
   - `IScopeResolver` interface.
   - `FullScopeResolver`: all discovered files.
   - `ChangedScopeResolver`: files changed vs merge base (shell out to `git diff --name-only`).
   - `StagedScopeResolver`: files in staging area (`git diff --cached --name-only`).
   - `PathsScopeResolver`: explicitly provided paths.

4. **Path policy validation rules**
   - `RequiredArtifactRule`: checks that required paths exist.
   - `ForbiddenPathRule`: checks that forbidden paths don't exist.
   - `NamingPatternRule`: checks path names against naming rules.

5. **Validation engine**
   - Discovers all registered `IValidationRule` from DI.
   - Resolves scope.
   - Runs rules.
   - Collects diagnostics.
   - Returns `ValidationResult`.

6. **Check command**
   - `steward check [--scope full|changed|staged] [--paths ...] [--output text|json]`
   - Text output: severity labels, paths, messages, remediation hints, summary.
   - JSON output: full `ValidationResult` schema.
   - Exit code: 0 if pass, 1 if errors, 2 if usage error.

7. **Secret filtering**
   - `SecretFilter` class: scans diagnostic messages for common secret patterns.
   - Applied in output pipeline before formatting.

8. **Orient --signals**
   - `steward orient --signals` appends missing-required-artifact warnings.

### Tests to write
- `DiagnosticTests`: model construction and serialization.
- `RequiredArtifactRuleTests`: missing and present artifacts.
- `ForbiddenPathRuleTests`: forbidden paths detected.
- `ValidationEngineTests`: runs rules, collects diagnostics, respects scope.
- `ScopeResolverTests`: full, changed (with mock git), staged, paths.
- `CheckCommandTests` (integration): exit codes, text output, JSON output.
- `SecretFilterTests`: redacts secrets, passes clean content.

### Completion criteria
- `steward check` detects missing required artifacts and forbidden paths.
- Exit codes are correct.
- JSON output is valid and follows schema.
- Secret patterns are redacted.

---

## v0.5.0 — Markdown Structural Engine

### What to implement

1. **Markdig integration**
   - Add `Markdig` NuGet package to Core.
   - `MarkdownParser` service wrapping Markdig with YAML frontmatter extension.

2. **StructuredDocument model**
   - `StructuredDocument`: FilePath, Frontmatter, Sections, ManagedRegions, RawContent.
   - `Section`: Heading, Level, Range (start/end line), Children, ContentBlocks.
   - `ContentBlock`: Type (list, table, codeblock), Range.
   - `ManagedRegion`: Id, Owner, Range.
   - `FrontmatterBlock`: RawYaml, Fields (Dictionary).

3. **MdPath selector parser**
   - Parse selector strings: `frontmatter`, `frontmatter.field`, `heading[Name]`, `heading[Parent/Child]`, `heading[#N]`, `managed[id]`.
   - Return `SelectorResult` with matched elements and their ranges.
   - Error on ambiguous match (multiple matches without index).

4. **Md query command**
   - `steward md query <file> <selector> [--output text|json]`
   - Text: outputs the matched content.
   - JSON: outputs structured result with selector, match info, content.

5. **Md outline command**
   - `steward md outline <file> [--output text|json]`
   - Shows heading hierarchy with line counts per section.

6. **Frontmatter validation rules**
   - `RequiredFrontmatterFieldRule`: checks that required fields exist.
   - `FrontmatterTypeRule`: checks field types match expectations.
   - Register with validation engine for `steward check`.

7. **Section size thresholds**
   - Policy can define `governance.section_size_warning_threshold` (default: 500 lines).
   - Info-level diagnostic for sections exceeding threshold.

8. **Outline --headings for Markdown files**
   - `steward outline doc.md --headings` shows heading hierarchy.

### Tests to write
- `MarkdownParserTests`: parses frontmatter, headings, sections, code blocks, tables, lists.
- `StructuredDocumentTests`: correct section hierarchy, line ranges.
- `MdPathSelectorTests`: all selector types, ambiguity detection, empty results.
- `MdQueryCommandTests` (integration): text and JSON output for various selectors.
- `MdOutlineCommandTests` (integration): heading hierarchy.
- `FrontmatterValidationRuleTests`: required fields, type checks.
- Snapshot tests for query and outline output.

### Completion criteria
- `steward md query doc.md frontmatter` returns frontmatter.
- `steward md query doc.md "heading[Goals]"` returns section content.
- `steward md outline doc.md` shows heading hierarchy with line counts.
- Ambiguous selectors produce clear error.
- `steward check` includes frontmatter validation rules.

---

## v0.6.0 — Search

### What to implement

1. **SearchEngine**
   - Full-text content search across discovered files.
   - Heading search: scan Markdown files for heading matches.
   - Combined mode: both content and heading matches.
   - Returns `SearchResult` with matches.

2. **SearchMatch model**
   - Path, Line, Column, Snippet, MatchKind (Content/Heading), HeadingContext.
   - HeadingContext: nearest parent heading for content matches in Markdown files.

3. **Heading context resolver**
   - For content matches in Markdown files, find the nearest parent heading using the structural model.
   - Lazy parsing: only parse Markdown files that have content matches.

4. **Filtering**
   - .gitignore and policy-aware (uses discovery exclude).
   - `--scope <role>` filters to artifacts matching a policy-defined role.
   - `--max <n>` limits results (default 100).

5. **Search command**
   - `steward search <query> [--mode content|headings|all] [--scope <role>] [--max N] [--output text|json]`
   - Text: path:line snippet with heading context.
   - JSON: array of match objects.

### Tests to write
- `SearchEngineTests`: content search, heading search, combined, max limit.
- `HeadingContextTests`: correct parent heading resolution.
- `SearchFilterTests`: .gitignore filtering, role filtering.
- `SearchCommandTests` (integration): text and JSON output, modes, filtering.

### Completion criteria
- `steward search "term"` finds matches with heading context.
- `steward search "term" --mode headings` searches headings only.
- JSON output has stable schema.
- Ignored files excluded. Role filtering works.

---

## v0.7.0 — Markdown Editing and Managed Regions

### What to implement

1. **Managed region detection**
   - Parse `<!-- steward:managed:begin id="..." owner="..." -->` / `<!-- steward:managed:end id="..." -->` markers.
   - Add to `StructuredDocument.ManagedRegions`.

2. **Ownership enforcement**
   - Before any edit in a managed region, verify the operation's owner matches the region's declared owner.
   - Reject edits with clear error if ownership mismatch.

3. **Structural editor**
   - Operates on raw text guided by structural model source positions.
   - Operations: ensure-section, set-section, insert-section, append-block, prepend-block.
   - Heading level inference (under → child, before/after → sibling).
   - Returns `EditResult` with unified diff and new content.

4. **Frontmatter editor**
   - `fm-set`: set a single field.
   - `fm-merge`: merge YAML input into existing frontmatter.

5. **Md edit command**
   - `steward md edit <file> <operation> [args] [--preview|--apply] [--output text|json]`
   - Default: preview (show diff).
   - `--apply`: write changes.
   - Text preview: unified diff.
   - JSON preview: structured diff object.

6. **Managed-scope validation rules**
   - `ManagedRegionIntegrityRule`: matching begin/end markers.
   - `ManagedScopeViolationRule`: detect edits in generated/protected regions.
   - Register with validation engine.

### Tests to write
- `ManagedRegionParserTests`: various marker formats, nested, malformed.
- `OwnershipEnforcementTests`: correct owner, wrong owner, no region.
- `StructuralEditorTests`: all operations, heading inference, minimal diff verification.
- `FrontmatterEditorTests`: set, merge.
- `MdEditCommandTests` (integration): preview vs apply, all operations.
- Snapshot tests verifying minimal diffs.

### Completion criteria
- All edit operations work in preview and apply modes.
- Ownership enforcement prevents unauthorized edits.
- Diffs are minimal (only intended changes).
- Managed region rules in `steward check` work.

---

## v0.8.0 — Deterministic Maintenance

### What to implement

1. **Maintenance engine**
   - Reads maintenance declarations from policy.
   - For each declared artifact, computes expected content vs actual content.
   - Returns `MaintenancePlan` with per-artifact actions.

2. **Artifact maintainers**
   - `StructureDocumentMaintainer`: generates tree view from file discovery.
   - `IndexMaintainer`: generates file index from discovered artifacts matching a glob.
   - `ManagedSectionMaintainer`: updates content between managed region markers.
   - `FrontmatterAutoMaintainer`: updates declared auto-fields (e.g., last_updated).

3. **Maintain command**
   - `steward maintain [--scope <id>] [--preview|--apply] [--output text|json]`
   - Preview: shows per-artifact plan.
   - Apply: writes changes.

4. **Anti-drift rules**
   - `StaleArtifactRule`: compares current content of maintained artifacts against expected.
   - Reports `stale-artifact` category diagnostics in `steward check`.

5. **Check --fix and --dry-run**
   - `IFixableRule` interface: `ComputeFixesAsync`.
   - `--dry-run`: shows what `--fix` would change.
   - `--fix`: applies deterministic fixes.

6. **Machine-readable manifest**
   - Generate `.steward/generated/manifest.json` as a maintenance artifact.
   - Contains file inventory, artifact roles, heading index.

### Tests to write
- `StructureDocMaintainerTests`: deterministic output, idempotent.
- `IndexMaintainerTests`: correct entries, sorting.
- `ManagedSectionMaintainerTests`: updates between markers, preserves outside.
- `StaleArtifactRuleTests`: detects stale, passes fresh.
- `MaintainCommandTests` (integration): preview, apply, idempotency.
- `CheckFixTests` (integration): --fix applies, --dry-run shows plan.

### Completion criteria
- `steward maintain` shows preview of all pending maintenance.
- `steward maintain --apply` updates artifacts.
- Running twice produces no diff.
- `steward check` detects stale maintained artifacts.
- Content outside managed scope is never modified.
- `steward check --fix` works for fixable rules.

---

## v0.9.0 — Workflow Completeness and Explainability

### What to implement

1. **Completion policy rules**
   - `AllRequiredPresentRule`, `NoStaleIndexesRule`, custom rules from policy.
   - `completion-policy` category diagnostics in check output.
   - Completion summary section in check text output.

2. **Status command**
   - `steward status [--output text|json]`
   - Shows: pending work, stale artifacts, completeness signals.
   - Does not run full validation—uses cheap checks only.

3. **Explain command**
   - `steward explain <rule-id> [--output text|json]`
   - Shows: rule description, what it checks, why it matters, how to remediate.
   - Reads rule metadata from registered `IValidationRule` instances.

4. **Broken-reference rules**
   - `BrokenInternalLinkRule`: Markdown links pointing to non-existent files.
   - `BrokenArtifactReferenceRule`: policy-declared artifact references that don't resolve.

5. **State-document roles**
   - vision, roadmap, current-state, etc. as policy artifact roles.
   - Surfaced in orient and status.

### Tests to write
- `CompletionPolicyRuleTests`: combined pass/fail scenarios.
- `StatusCommandTests` (integration): text and JSON output.
- `ExplainCommandTests` (integration): known and unknown rule IDs.
- `BrokenLinkRuleTests`: valid links, broken links, external links skipped.

### Completion criteria
- `steward check` includes completion policy summary.
- `steward status` shows lightweight state.
- `steward explain` works for all registered rules.
- Broken links detected.

---

## v1.0.0 — Release Readiness

### What to implement

1. **Performance profiling**
   - Measure orient, outline, check, search on repos of 1K, 10K, 50K files.
   - Optimize hot paths if needed.
   - Ensure targeted validation is faster than full.

2. **Safety audit**
   - Review all mutation paths for preview-first default.
   - Review secret filtering coverage.
   - Review ownership enforcement in all edit/maintain paths.

3. **Cross-platform validation**
   - Run tests on Windows, macOS, Linux.
   - Fix any path-handling issues.

4. **README and documentation**
   - README.md with: overview, installation, quick start, command reference summary.
   - Link to docs/ for detailed planning and decision docs.

5. **Distribution packaging**
   - Verify `PackAsTool` and `ToolCommandName` work.
   - Build self-contained binaries for all 6 RIDs.
   - Verify `steward version` reports correct version.

6. **Dog-fooding**
   - Create `.steward/config.yaml` and `.steward/policy.yaml` for this repository.
   - `steward check` passes on the steward repo.

### Tests to update
- Ensure snapshot tests cover all commands in both output formats.
- Ensure exit code paths are all tested.
- Add performance benchmark tests (not gating, informational).

### Completion criteria
- All tests pass on all platforms.
- Performance meets targets.
- README is complete.
- Distribution packages build.
- `steward check` passes on the steward repo.
- All v1.0 requirements implemented or explicitly deferred with justification.
