---
type: audit
status: Historical
last_updated: 2026-04-18
standalone: true
---

# Maintainer Remarks Implementation Summary — 2026-04-18

## Scope

This pass implemented the 2026-04-18 maintainer remarks against the Steward CLI repository, reconciled repo-facing docs and planning artifacts to the actual code, and left the repo in a reviewable, test-backed, release-ready state.

## Implemented

1. Generated directory indexes now require a non-empty child-document `description` frontmatter field.
   - `directory-index` maintenance blocks with an actionable reason if any indexed source file lacks `description`.
   - STWD-003 now also enforces that requirement for files matched by configured `directory-index` artifacts.
   - This repo now dogfoods the behavior in [docs/decisions/decision-index.md](../../decisions/README.md) via steward-managed RFC and ADR index sections.

2. Markdown anchor-like selectors now work in the Markdown query surface.
   - `md query` accepts `#anchor-slug` selectors.
   - `md query` also accepts combined file-plus-fragment tokens such as `README.md#who-is-steward-for`.
   - Matching uses normalized anchor slugs derived from heading text.

3. Unique heading text is now enforced within a Markdown file.
   - New rule `STWD-017` warns when two headings normalize to the same anchor slug.
   - The repo docs touched in this pass were updated so the repo now passes the rule cleanly.

4. Configurable frontmatter date refresh on local modification is now implemented.
   - `governance.frontmatter.auto_fields.<field>: true` synthesizes `frontmatter-auto` maintenance using `today-if-local-change`.
   - The field name remains configurable.
   - Steward updates existing fields only and uses `git diff --name-only HEAD` as the local-change signal.
   - `maintain --apply` and `check --fix --apply` now apply frontmatter file edits, not just whole-file artifact rewrites.

5. NuGet package identity is aligned to the intended product name.
   - `PackageId` changed from `Steward.Cli` to `Steward`.
   - Project and assembly naming remain `Steward.Cli`.
   - This was chosen because nuget.org checks on 2026-04-18 found no public `Steward` or `Steward.Cli` package, so there was no concrete ownership or migration blocker before first public publication.

6. NuGet publication is activated in the release workflow.
   - `.github/workflows/release.yml` now pushes the packaged tool to nuget.org using `NUGET_ORG_API_KEY`.
   - Publication uses `--skip-duplicate` so rerunning a tagged release is safe.

7. Repo version and release story are reconciled to `0.15.0`.
   - Shared version metadata, changelog, README, release docs, milestone/current-state docs, package metadata, and release workflow examples now agree on `0.15.0`.

## Partially Implemented / External Verification

- No requested repo change was deferred in-code or in-docs.
- One release validation step remains external by nature: the first hosted tag run must prove GitHub Release asset publication plus the automated nuget.org push in GitHub-hosted execution.

## Deferred

- No user-requested feature was deferred.
- Existing broader roadmap items such as first-hour onboarding polish and universal JSON-envelope guarantees remain in the active pre-1.0 plan, but they were not part of this maintainer-remarks pass.

## Release And Packaging Decision

- Decision: publish the package as `Steward`, keep the command name `steward`, and keep the project/assembly identity `Steward.Cli`.
- Rationale:
  - The product name throughout repo-facing docs is "Steward".
  - Renaming before first public NuGet publication avoids a future migration tax.
  - nuget.org verification on 2026-04-18 found no public blocker for either `Steward` or `Steward.Cli`.

## Release Workflow Changes

- GitHub tag releases still build, test, package, generate changelog-backed notes, and publish GitHub Release assets.
- The workflow now also pushes the generated `.nupkg` to nuget.org with `NUGET_ORG_API_KEY`.
- Release docs and operator checklist now treat NuGet publication as part of the tagged release flow, with local manual push retained only as a recovery path.

## Tests Added Or Updated

- Added:
  - `tests/Steward.Core.Tests/UniqueHeadingTextRuleTests.cs`
- Updated:
  - `tests/Steward.Cli.Tests/MaintainCommandTests.cs`
  - `tests/Steward.Cli.Tests/MdQueryCommandTests.cs`
  - `tests/Steward.Core.Tests/DirectoryIndexMaintainerTests.cs`
  - `tests/Steward.Core.Tests/FrontmatterValidationRuleTests.cs`
  - `tests/Steward.Core.Tests/Maintenance/FrontmatterAutoMaintainerTests.cs`
  - `tests/Steward.Core.Tests/Maintenance/IndexMaintainerTests.cs`
  - `tests/Steward.Core.Tests/Maintenance/MaintenanceEngineTests.cs`
  - `tests/Steward.Core.Tests/MarkdownParserTests.cs`
  - `tests/Steward.Core.Tests/MdPathSelectorTests.cs`
  - `tests/Steward.Core.Tests/RuleRegistryTests.cs`

## Verification Performed

- `dotnet test Steward.sln -c Release --no-restore`
  - Passed: `452` core tests, `194` CLI tests.
- `dotnet pack src/Steward.Cli/Steward.Cli.csproj -c Release`
  - Passed and produced `Steward.0.15.0.nupkg`.
- `dotnet run --project src/Steward.Cli -- maintain --apply`
  - Passed and regenerated steward-managed artifacts.
- `dotnet run --project src/Steward.Cli -- check`
  - Passed with `0` warnings, `0` errors, and `1` existing STWD-013 info diagnostic for `docs/audits/code-quality-review-2025-07-23.md`.

## Exact Files Changed

- Workflow / release / packaging:
  - `.github/workflows/release.yml`
  - `Directory.Build.props`
  - `src/Steward.Cli/Steward.Cli.csproj`
  - `CHANGELOG.md`
- Repo policy and generated artifacts:
  - `.steward/policy.yaml`
  - `STRUCTURE.md`
  - `docs/decisions/decision-index.md`
- Product / current-state / planning docs:
  - `README.md`
  - `docs/planning-index.md`
  - `docs/implementation-status.md`
  - `docs/planning/milestone-plan.md`
  - `docs/planning/pre-1-0-readiness-plan.md`
  - `docs/planning/release-process.md`
  - `docs/planning/release-publication-checklist.md`
  - `docs/reviews/config-expressiveness-stress-test.md`
  - `docs/audits/maintainer-remarks-implementation-summary-2026-04-18.md`
- Decision records:
  - `docs/decisions/adrs/ADR-001-dotnet10-cli-architecture.md`
  - `docs/decisions/adrs/ADR-002-project-structure.md`
  - `docs/decisions/adrs/ADR-003-configuration-format-yaml.md`
  - `docs/decisions/adrs/ADR-004-markdown-parser-markdig.md`
  - `docs/decisions/adrs/ADR-005-validation-engine-design.md`
  - `docs/decisions/adrs/ADR-006-output-formatting-strategy.md`
  - `docs/decisions/adrs/ADR-007-test-strategy.md`
  - `docs/decisions/adrs/ADR-008-gitignore-handling.md`
  - `docs/decisions/adrs/ADR-009-packaging-distribution.md`
  - `docs/decisions/adrs/ADR-010-agent-usefulness-improvements.md`
  - `docs/decisions/adrs/ADR-011-domain-stewardship-through-generic-configuration.md`
  - `docs/decisions/adrs/ADR-012-artifact-type-schema-direction.md`
  - `docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md`
  - `docs/decisions/adrs/ADR-014-non-software-profile-scope.md`
  - `docs/decisions/rfcs/RFC-001-cli-command-structure.md`
  - `docs/decisions/rfcs/RFC-002-configuration-model.md`
  - `docs/decisions/rfcs/RFC-003-validation-and-diagnostics.md`
  - `docs/decisions/rfcs/RFC-004-markdown-structural-model.md`
  - `docs/decisions/rfcs/RFC-005-orientation-search-outline.md`
  - `docs/decisions/rfcs/RFC-006-maintenance-and-memory.md`
  - `docs/decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md`
  - `docs/decisions/rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md`
  - `docs/decisions/rfcs/RFC-009-typed-resource-addresses-and-search-alignment.md`
  - `docs/decisions/rfcs/RFC-010-consistent-json-output-envelope.md`
  - `docs/decisions/rfcs/RFC-011-markdown-split-and-extract-workflows.md`
- Core / CLI implementation:
  - `src/Steward.Cli/Commands/ExplainCommand.cs`
  - `src/Steward.Cli/Commands/MaintainCommand.cs`
  - `src/Steward.Cli/Commands/MdCommand.cs`
  - `src/Steward.Core/Maintenance/DirectoryIndexMaintainer.cs`
  - `src/Steward.Core/Maintenance/FrontmatterAutoMaintainer.cs`
  - `src/Steward.Core/Maintenance/IndexMaintainer.cs`
  - `src/Steward.Core/Maintenance/MaintenanceEngine.cs`
  - `src/Steward.Core/Maintenance/MaintenanceModels.cs`
  - `src/Steward.Core/Markdown/FrontmatterEditor.cs`
  - `src/Steward.Core/Markdown/MarkdownHeadings.cs`
  - `src/Steward.Core/Markdown/MarkdownParser.cs`
  - `src/Steward.Core/Markdown/MdPathSelector.cs`
  - `src/Steward.Core/Markdown/SplitPlanner.cs`
  - `src/Steward.Core/PathHelper.cs`
  - `src/Steward.Core/Validation/RuleRegistry.cs`
  - `src/Steward.Core/Validation/Rules/RequiredFrontmatterFieldRule.cs`
  - `src/Steward.Core/Validation/Rules/StaleArtifactRule.cs`
  - `src/Steward.Core/Validation/Rules/UniqueHeadingTextRule.cs`
- Test code:
  - `tests/Steward.Cli.Tests/MaintainCommandTests.cs`
  - `tests/Steward.Cli.Tests/MdQueryCommandTests.cs`
  - `tests/Steward.Core.Tests/DirectoryIndexMaintainerTests.cs`
  - `tests/Steward.Core.Tests/FrontmatterValidationRuleTests.cs`
  - `tests/Steward.Core.Tests/Maintenance/FrontmatterAutoMaintainerTests.cs`
  - `tests/Steward.Core.Tests/Maintenance/IndexMaintainerTests.cs`
  - `tests/Steward.Core.Tests/Maintenance/MaintenanceEngineTests.cs`
  - `tests/Steward.Core.Tests/MarkdownParserTests.cs`
  - `tests/Steward.Core.Tests/MdPathSelectorTests.cs`
  - `tests/Steward.Core.Tests/RuleRegistryTests.cs`
  - `tests/Steward.Core.Tests/UniqueHeadingTextRuleTests.cs`

## Follow-Up Backlog

- Capture the first hosted green run of `.github/workflows/release.yml`, including verified GitHub Release asset publication and verified nuget.org publication for `Steward`.
- Continue the active pre-1.0 work on README-first onboarding, repo-independent source-build execution guidance, and universal JSON contract behavior.
