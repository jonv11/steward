# Implementation Status

Last updated: 2026-06-05

## Current Baseline

Steward is currently on **`v0.17.0`**. The repository is **still pre-1.0**: intentional public `0.x` releases are allowed when readiness evidence is green and the release process is followed, but `v1.0.0` is reserved for a future stable release and is not authorized yet. Versioning governance is recorded in [ADR-013](decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

| Area | Current state |
|------|---------------|
| Version line | `0.x.y` only until explicit stable-release approval |
| Current repo version | `0.17.0` |
| Tests | 732 passing (493 core, 239 CLI) |
| Validation rules | 18 (`STWD-001` through `STWD-018`) |
| Artifact families | `artifact_families` section now supported in `policy.yaml`; ADRs and RFCs governed by families in this repo |
| Maintainer types | 6 (`structure-document`, `index`, `directory-index`, `managed-section`, `frontmatter-auto`, `manifest`) |
| JSON contract | Agent-safe mainline contract delivered: the standard envelope is now the only JSON mode, structured errors and handoff fields are broader, and universal expected-failure coverage still remains later pre-1.0 work |
| Packaging | `dotnet pack` succeeds cleanly for `Steward.0.17.0.nupkg` |
| Repo quality gates | `markdownlint-cli2` with repo config, `steward check` enforced in CI/release on Linux, build/test/pack matrix across Windows, Linux, and macOS |
| Public pre-1.0 release path | Tag-driven GitHub Release workflow, changelog-backed notes, automated nuget.org publication, `.nupkg` + curated binary bundles + checksums |
| Active readiness tracker | [Pre-1.0 Readiness Plan](planning/pre-1-0-readiness-plan.md) |

## Delivered Lineage

| Milestone | State | Notes |
|-----------|-------|-------|
| `v0.1.0` | Delivered | CLI scaffolding, help/version, initial command model |
| `v0.2.0` | Delivered | Orientation, outline, git-aware discovery foundations |
| `v0.3.0` | Delivered | Config/policy loading, profiles, path-policy baseline |
| `v0.4.0` | Delivered | Validation engine, diagnostics, explainability foundations |
| `v0.5.0` | Delivered | Markdown query/address basics |
| `v0.6.0` | Delivered | Search surfaces |
| `v0.7.0` | Delivered | Structural Markdown editing and ownership protections |
| `v0.8.0` | Delivered | Deterministic maintenance and stale-artifact enforcement |
| `v0.9.0` | Delivered | Workflow completeness surfaces |
| `v0.10.0` | Delivered | Pre-1.0 governance hardening, version reset, semantic config validation, completion-policy wiring, and status/readiness surfacing cleanup |
| `v0.11.0` | Delivered | Stable-release hardening: B5 profile scope (ADR-014), B6 scoped validation fix, B7 status JSON coverage, contract tests, exit-code tests, dependency stabilization, publication checklist |
| `v0.12.0` | Delivered | CLI fidelity, governance deepening, and Markdown subsystem completion (init scaffolding, md query --pattern fix, preview/apply standardization, coverage exclude, explain filtering, config suggest/doctor deepening, fm-validate) |
| `v0.13.0` | Delivered | Artifact type schema RFC and base implementation: `artifact_families` in policy, deterministic family classification, type-aware frontmatter validation, family awareness in `status`, `orient`, `explain path`, `config doctor` |
| `v0.14.0` | Delivered | Release automation and public pre-1.0 distribution discipline: changelog-backed release notes, GitHub Release workflow and assets, release-intent labels, release docs, and publication metadata hardening |
| `v0.15.0` | Delivered | JSON output envelope consistency (RFC-010), Markdown split/extract workflows (RFC-011), severity_overrides runtime implementation, explain path family-applicability fixes |
| `v0.16.0` | Delivered | First-hour onboarding path, repo-independent source-build guidance, agent-safe JSON contract baseline (CC-01 through CC-10), `config suggest` confidence/exclusions, help/runtime polish, and explicit-artifact/family-governance coherence |
| `v0.17.0` | Delivered | Documentation overhaul, standard-envelope-only JSON mode, deeper machine handoff surfaces, skill-family governance, and resilient inaccessible-path/exception handling |

## What Was Established In `v0.10.0`

- Shared version metadata now comes from `Directory.Build.props`, and `steward version` now reports the current repo version.
- The repo no longer needs per-project version duplication.
- `config validate` now catches semantic problems such as unknown rule ids, invalid maintainer types, bad `depends_on` references, and invalid path-policy regex/glob declarations.
- `check` completion summaries now follow repository policy instead of hardcoded rule assumptions.
- `status` now surfaces required artifacts, recommended artifacts, and state-document coverage instead of only hard-required entries.
- `config show --effective` now surfaces the merged effective policy in text mode as well as JSON.
- The repo now includes a GitHub Actions matrix for build/test/pack on Windows, macOS, and Linux; the first hosted green run remains part of the stable-release evidence trail.
- Public-facing docs now describe explainability, profile readiness, and dependency posture more conservatively and explicitly.
- Accepted RFC command/config artifacts now align more closely with the current CLI and policy model, reducing decision drift for contributors and coding agents.
- Non-software profile behavior now has fixture-backed CLI coverage across `init`, `config validate`, `config show --effective`, `status`, `orient`, `check`, and `config doctor`; the remaining B5 work is an explicit keep/narrow release decision, not missing execution evidence.
- The accepted RFC-007 governance-enhancement work is materially present in the codebase and should be treated as part of the delivered pre-1.0 baseline, not as a hypothetical post-`1.0.0` future.

## What Is New In The v0.11.0 Work

- **B6 resolved:** Scoped validation (`check --scope changed/staged`) no longer produces false diagnostics on clean trees. `ValidationContext` now includes `AllDiscoveredFiles` for repo-wide obligation rules.
- **B7 resolved:** `status --coverage --output json` now includes a `coverage` object with `governedCount`, `totalMarkdownFiles`, `percentage`, and `ungoverned` list.
- **B5 resolved (ADR-014):** Non-software profile scope decision recorded. `init --profile` now offers `software`, `docs`, `minimal`. `mixed` and `knowledge` deferred until contracts are enriched.
- **Exit-code regression tests:** 7 tests covering all 4 exit codes (Success, ValidationFailure, UsageError, InternalError).
- **Stable-surface contract tests:** 10 tests covering check/status/orient JSON shapes, text output contracts, version output, and scoped-check regression.
- **Dependency stabilization:** `Microsoft.Extensions.DependencyInjection.Abstractions` upgraded from preview to GA 10.0.6, and `System.CommandLine` is now on stable `2.0.0`.
- **Publication checklist:** [Release publication checklist](planning/release-publication-checklist.md) with local verification, tagging, NuGet publication, and self-contained binary steps.

## Known Defects In `v0.10.0`

The following known defects were identified in the 2026-04-16 CLI review cycle. Both have been resolved in the v0.11.0 work:

- ~~**Scoped validation false positives (B6).**~~ Resolved.
- ~~**JSON coverage parity gap (B7).**~~ Resolved.

## What Is New In v0.12.0

- **Init scaffolding fix:** `init --profile software` now scaffolds placeholder files for required artifacts so that a subsequent `check` no longer fails immediately on STWD-001.
- **`md query --pattern` fix:** Argument parsing ambiguity between positional file and `--pattern` resolved; batch mode now works correctly.
- **Preview/apply standardization:** `check --fix` now previews fixes by default; `--fix --apply` commits changes. `--dry-run` is retained as a hidden deprecated alias.
- **Coverage exclude support:** New `coverage.exclude` config section allows excluding paths (e.g., test fixtures) from `status --coverage` calculations.
- **Explain path filtering:** `explain <path>` now filters rules to only those applicable to the target file based on type, artifact status, and config presence.
- **Config suggest deepening:** `config suggest` now detects decision directories, planning documents, state documents, and subdirectory index files.
- **Config doctor deepening:** `config doctor` now reports dead suppressions (unrecognized rule IDs in disabled_rules), unreachable path-override patterns, and unreachable frontmatter-requirement patterns.
- **`fm-validate` added:** `md edit fm-validate <file>` validates frontmatter against policy requirements (global + scoped), completing the RFC-004 edit operation set.
- **Mixed/knowledge profiles:** Remain deferred per ADR-014; no changes in this milestone.

## What Is New In v0.13.0

- **`artifact_families` in policy:** `policy.yaml` now supports a top-level `artifact_families:` section. Each family declares a `match:` criterion (path glob, frontmatter key-value, or both) and an optional `frontmatter_schema:` with `required:` fields and `allowed_values:` constraints.
- **Deterministic family classification (`ArtifactFamilyClassifier`):** A shared engine classifies files against families using declaration-order first-match semantics and AND criteria. Explicit `artifacts[]` entries always take precedence.
- **Type-aware frontmatter validation (STWD-003 extended):** `check` now enforces family-level `frontmatter_schema` requirements. Diagnostics include `[family: name]` for traceability.
- **`explain path` family awareness:** Shows `Family: name (DisplayName)` and surfaces family-level required fields and allowed values.
- **`status` family summary:** Text and JSON output include a per-family matched-file count under `Artifact Families:` / `artifactFamilies`.
- **`orient` family classification:** Files matched by a family are classified as `family:{name}` in the orientation tree.
- **`config doctor` unreachable family patterns:** Doctor now reports families whose `path_pattern` matches zero discovered files.
- **`config validate` family validation:** Validates glob syntax, duplicate names, blank required fields, and invalid importance in `artifact_families`.
- **`ProfileMerger` fix:** `ArtifactFamilies` is now preserved through profile merging (was silently dropped before this milestone).
- **Dogfooding migration:** This repo's `.steward/policy.yaml` migrated ADR and RFC governance from `frontmatter_requirements` to `artifact_families`.
- **RFC-008 accepted:** RFC-008 accepted with §8 narrowing the v0.13.0 scope. Deferred: workflow modeling.
- **STWD-014 (RequiredSectionsRule):** Files matched by an artifact family must contain all headings declared in `required_sections`.
- **STWD-015 (FamilyMinCountRule):** Artifact families with `directory_expectations.min_count` must contain at least the declared number of files.
- **STWD-016 (FamilyNamingPatternRule):** Files matched by an artifact family must satisfy the family's `naming_pattern` regex.

## What Is New In v0.14.0

### Session-start and text-UX coherence

The text-mode entry surfaces are now more deliberate and trustworthy. `orient` is compact by default in text mode, `--full` restores the full classified inventory, and `--tree` renders actual hierarchy instead of mixing indentation with full paths. Compact tree views now preserve real ancestors rather than implying false parent-child relationships. `outline` now renders an actual tree by default, adds recursive folder counts via `--counts`, and shows aggregate directory sizes when `--sizes` is requested. `status`, `orient`, and `outline` now use semantic text styling for headings and key status/classification tokens, so `--no-color` has meaningful scope without changing machine-oriented JSON behavior.

### Pre-1.0 release process completion

The repository now contains a coherent operator path for intentional public `0.x` releases:

- `CHANGELOG.md` is now the canonical release-notes source.
- `.github/release-labels.json` defines the repo-managed pre-1.0 release-intent labels (`release:none`, `release:patch`, `release:minor`).
- `.github/workflows/pr-release-intent.yml` requires exactly one release-intent label on non-draft pull requests to the default branch.
- `.github/workflows/release-labels.yml` plus `scripts/release/Sync-ReleaseLabels.ps1` keep the GitHub label set synchronized with the repo-managed manifest.
- `.github/workflows/release.yml` publishes GitHub Releases from tags, validates the tag/version match, builds/tests, attaches the `.nupkg`, curated self-contained bundles, and `SHA256SUMS.txt`, and sources release notes from the matching changelog entry.
- [Release Process](planning/release-process.md) now explains how version bumps, changelog updates, labels, tagging, GitHub Releases, and automated NuGet publication fit together.

### Maintainer review hardening

A comprehensive maintainer review pass also addressed several cross-cutting concerns:

- **Culture/locale fix:** `Program.Main()` now sets `InvariantCulture` before command parsing, ensuring deterministic output across all environments.
- **JSON/text output parity:** `steward check --output json` now includes impact signals and staged completeness, matching the text output.
- **Document frontmatter migration:** All ADR, RFC, PRD, planning, and requirements documents migrated from markdown-bullet metadata to YAML frontmatter blocks.
- **Policy scalability:** `policy.yaml` refactored from 19 per-file artifact entries to 11 structurally essential ones, with convention-based `frontmatter_requirements` for document families.
- **RFC-008 accepted and partially delivered:** [Convention-Based Discovery and Workflow Modeling](decisions/rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md) now governs the current artifact-family baseline and captures the deferred follow-on work.

## What Is New In v0.15.0

- **JSON output envelope (RFC-010):** All JSON-producing commands now support `--json-envelope standard`, which wraps payloads in `{ schemaVersion, command, toolVersion, success, exitCode, data }`. Default is `legacy` (existing behavior) for the `0.15.x` line.
- **Markdown split/extract workflows (RFC-011):** `md split plan` provides a non-mutating analysis of candidate section splits for large Markdown files. `md edit extract-section` is a preview/apply operation that extracts a named section into a new target file, with optional link replacement and managed-region ownership enforcement.
- **`validation.severity_overrides` implemented:** The `severity_overrides` config field was previously modeled and validated but never applied at runtime. Diagnostics are now rewritten to the configured severity level after all rules run, and pass/fail computation reflects the overridden severities.
- **`explain path` family-applicability fixes:** Family classification now runs for all files including those in `artifacts[]` (explicit artifacts were previously excluded). STWD-014/015/016 now show as applicable only when the file matches a configured artifact family, not for every file.
- **Markdown anchor selectors:** `md query` now accepts Markdown-fragment selectors such as `#who-is-steward-for` and combined CLI tokens such as `README.md#who-is-steward-for`.
- **STWD-017 unique heading validation:** Heading text must now be unique within a Markdown file after anchor-style normalization so fragment selectors stay deterministic.
- **Generated decision-index automation:** This repo now dogfoods steward-managed decision-index sections powered by `directory-index`, with mandatory child-document `description` frontmatter and generated `Status` columns.
- **Local-change frontmatter date refresh:** `governance.frontmatter.auto_fields` now synthesizes `frontmatter-auto` maintenance that updates existing fields like `last_updated` to today's date when `git diff --name-only HEAD` reports a local change.
- **Package/release alignment:** The published tool package is now `Steward`, and the release workflow pushes tagged packages to nuget.org using `NUGET_ORG_API_KEY`.

## What Is New In v0.16.0

- **Tested first-hour onboarding path:** `README.md` now includes a repo-independent "First 15 Minutes" flow, explicit `global.json` hazard guidance, and clearer source-build/install instructions for using Steward against another repository.
- **Agent-safe JSON contract baseline (CC-01 through CC-10):** Standard envelope mode, structured errors, process/domain success separation, normalized `md query` JSON, richer `refactor move` output, and contract tests are now on the release line.
- **`config suggest` trust improvements:** Suggestions now respect path-override-style exclusions, emit `confidence` hints, and mark conservative inferences so mature repos can treat the command as a safer bootstrap surface.
- **Help and text UX polish:** Runtime help now presents the public command name `steward`, option placeholders are restored, `md`/`md edit` help is more operational, `check` distinguishes warning-bearing passes, and `orient --signals` clarifies that it is a cheap/non-exhaustive signal surface.
- **Explicit-artifact/family coherence:** Explicit artifacts now inherit family frontmatter, sections, naming, and min-count governance, while path-scoped frontmatter overlays can still express intentional local exceptions such as `type: prd`.
- **Repo-self-stewardship alignment:** The repo policy, generated structure file, README, changelog, and active planning docs now align on the `0.16.0` release line.

## What Is New In v0.17.0

- **Documentation overhaul:** Steward now ships dedicated maintainer, contributor, configuration, and AI-agent guides plus docs indexes, so the README no longer has to carry every audience alone.
- **Standard-envelope-only JSON mode:** The legacy `--json-envelope` compatibility path is gone; `--output json` now always targets the standard envelope contract on the main command surface.
- **Deeper handoff/provenance fields:** `explain path`, `refs`, and `search` now emit richer provenance, concrete link-instance, and section-context data to improve agent and tooling handoff.
- **Governed skill metadata:** `.agents/skills/**/SKILL.md` files are now covered by a `skill` artifact family that enforces the shared `name` and `description` frontmatter floor.
- **Runtime resilience hardening:** Discovery now skips inaccessible directories and unreadable nested `.gitignore` files, and top-level CLI failures now return stable access-denied/internal-error output instead of raw stack traces.
- **Still deferred to `v0.18.0`:** the remaining universal JSON expected-failure cleanup, the first narrow RFC-009 typed-address slice, and the adoption-oriented config-model follow-on decision.

## Remaining Before First Stable Shipment

The detailed categorized list lives in [Pre-1.0 Readiness Plan](planning/pre-1-0-readiness-plan.md). At a high level, the remaining work is now concentrated in a smaller set of release-hardening items:

### Required

- ~~Hosted green cross-platform CI evidence~~ — confirmed: v0.17.0 CI matrix ran green on Windows, Linux, and macOS (2026-04-19)
- ~~Hosted GitHub Release / nuget.org publication evidence~~ — confirmed: tag-driven release workflow ran green through v0.17.0; NuGet package published
- Explicit stable-release authorization for `v1.0.0` per ADR-013

### Strongly Recommended

- Keep the changelog, release-process doc, and versioned planning/status artifacts in sync whenever the next intentional `0.x` release is prepared

### Optional / Later Pre-1.0

- Universal JSON envelope guarantees on every JSON-capable success and expected-failure path
- Heading selector fuzzy matching in MdPath
- Typed resource addresses (RFC-009, deferred)
- Re-enabling deferred profiles (`mixed`, `knowledge`) when their contracts are enriched (see [ADR-014](decisions/adrs/ADR-014-non-software-profile-scope.md))
- Workflow/session modeling (RFC-008 Phase 3, `v0.18.0+`)

## Manual Follow-Up Outside The Repo

This repository contains no local git tags, but if any remote `v1.0.0` tag, release entry, or published public package exists, it must be corrected manually because that cannot be fixed from inside the workspace alone.
