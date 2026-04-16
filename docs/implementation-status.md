# Implementation Status

Last updated: 2026-04-16

## Current Baseline

Steward is currently on **`v0.13.0`**. The repository is **still pre-1.0**: `v1.0.0` is reserved for a future stable release and is not authorized yet. Versioning governance is recorded in [ADR-013](decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

| Area | Current state |
|------|---------------|
| Version line | `0.x.y` only until explicit stable-release approval |
| Current repo version | `0.13.0` |
| Tests | 598 passing (`407` core, `191` CLI) |
| Validation rules | 13 (`STWD-001` through `STWD-013`) |
| Artifact families | `artifact_families` section now supported in `policy.yaml`; ADRs and RFCs governed by families in this repo |
| Maintainer types | 6 (`structure-document`, `index`, `directory-index`, `managed-section`, `frontmatter-auto`, `manifest`) |
| Packaging | `dotnet pack` succeeds cleanly for `Steward.Cli.0.13.0.nupkg` |
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

## What Was Established In `v0.10.0`

- Shared version metadata now comes from `Directory.Build.props`, and `steward version` now reports the current repo version.
- The repo no longer needs per-project version duplication.
- `config validate` now catches semantic problems such as unknown rule ids, invalid maintainer types, bad `depends_on` references, and invalid path-policy regex/glob declarations.
- `check` completion summaries now follow repository policy instead of hardcoded rule assumptions.
- `status` now surfaces required artifacts, recommended artifacts, and state-document coverage instead of only hard-required entries.
- `config show --effective` now surfaces the merged effective policy in text mode as well as JSON.
- The repo now includes a GitHub Actions matrix for build/test/pack on Windows, macOS, and Linux; the first hosted green run remains part of the stable-release evidence trail.
- Public-facing docs now describe explainability, profile readiness, and prerelease dependency posture more conservatively and explicitly.
- Accepted RFC command/config artifacts now align more closely with the current CLI and policy model, reducing decision drift for contributors and coding agents.
- Non-software profile behavior now has fixture-backed CLI coverage across `init`, `config validate`, `config show --effective`, `status`, `orient`, `check`, and `config doctor`; the remaining B5 work is an explicit keep/narrow release decision, not missing execution evidence.
- The accepted RFC-007 governance-enhancement work is materially present in the codebase and should be treated as part of the delivered pre-1.0 baseline, not as a hypothetical post-`1.0.0` future.

## What Is New In The v0.11.0 Work

- **B6 resolved:** Scoped validation (`check --scope changed/staged`) no longer produces false diagnostics on clean trees. `ValidationContext` now includes `AllDiscoveredFiles` for repo-wide obligation rules.
- **B7 resolved:** `status --coverage --output json` now includes a `coverage` object with `governedCount`, `totalMarkdownFiles`, `percentage`, and `ungoverned` list.
- **B5 resolved (ADR-014):** Non-software profile scope decision recorded. `init --profile` now offers `software`, `docs`, `minimal`. `mixed` and `knowledge` deferred until contracts are enriched.
- **Exit-code regression tests:** 7 tests covering all 4 exit codes (Success, ValidationFailure, UsageError, InternalError).
- **Stable-surface contract tests:** 10 tests covering check/status/orient JSON shapes, text output contracts, version output, and scoped-check regression.
- **Dependency stabilization:** `Microsoft.Extensions.DependencyInjection.Abstractions` upgraded from preview to GA 10.0.6. Only `System.CommandLine` beta remains (documented and intentional).
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
- **RFC-008 accepted:** RFC-008 accepted with §8 narrowing the v0.13.0 scope. Deferred: `required_sections`, `min_count`, `naming_pattern` enforcement, workflow modeling.

## Incremental Pass (post-v0.13.0, in-progress)

### Required-sections enforcement

ADRs, RFCs, and PRDs have established section conventions (Context / Decision / Consequences for ADRs; numbered sections for RFCs and PRDs). These conventions are consistent across all existing documents and are documented in policy comments and the planning gap tracker. Current Steward has no mechanism to enforce section presence at the family level — RFC-008 §3.3 explicitly marks `required_sections` per family as a future capability dependent on the artifact type schema work (ADR-012, v0.13.0+). The gap is tracked in [pre-1-0-readiness-plan.md](planning/pre-1-0-readiness-plan.md) under "Later Pre-1.0 Candidates".

### Session-start and text-UX coherence

The text-mode entry surfaces are now more deliberate and trustworthy. `orient` is compact by default in text mode, `--full` restores the full classified inventory, and `--tree` renders actual hierarchy instead of mixing indentation with full paths. Compact tree views now preserve real ancestors rather than implying false parent-child relationships. `outline` now renders an actual tree by default, adds recursive folder counts via `--counts`, and shows aggregate directory sizes when `--sizes` is requested. `status`, `orient`, and `outline` now use semantic text styling for headings and key status/classification tokens, so `--no-color` has meaningful scope without changing machine-oriented JSON behavior.

## Maintainer Review Pass (post-v0.13.0, in-progress)

A comprehensive maintainer review pass addressed several cross-cutting concerns:

- **Culture/locale fix:** `Program.Main()` now sets `InvariantCulture` before command parsing, ensuring deterministic output across all environments.
- **JSON/text output parity:** `steward check --output json` now includes impact signals and staged completeness, matching the text output.
- **Document frontmatter migration:** All ADR, RFC, PRD, planning, and requirements documents migrated from markdown-bullet metadata to YAML frontmatter blocks.
- **Policy scalability:** `policy.yaml` refactored from 19 per-file artifact entries to 11 structurally essential ones, with convention-based `frontmatter_requirements` for document families.
- **RFC-008 accepted and partially delivered:** [Convention-Based Discovery and Workflow Modeling](decisions/rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md) now governs the current artifact-family baseline and captures the deferred follow-on work.

## Remaining Before First Stable Shipment

The detailed categorized list lives in [Pre-1.0 Readiness Plan](planning/pre-1-0-readiness-plan.md). At a high level, the remaining work is now concentrated in a smaller set of release-hardening items:

### Required

- First hosted green runs from the cross-platform build/test/pack automation

### Strongly Recommended

- An explicit later pre-1.0 roadmap ordering (v0.12.0 scope now detailed in [milestone-plan.md](planning/milestone-plan.md))

### Optional / Later Pre-1.0

- **v0.14.0+:** `required_sections` per family, `min_count` directory expectations, `naming_pattern` regex enforcement (all deferred from v0.13.0 per RFC-008 §8)
- Re-enabling deferred profiles (`mixed`, `knowledge`) when their contracts are enriched (see [ADR-014](decisions/adrs/ADR-014-non-software-profile-scope.md))
- Workflow/session modeling (RFC-008 Phase 3, v0.15.0+)

## Manual Follow-Up Outside The Repo

This repository contains no local git tags, but if any remote `v1.0.0` tag, release entry, or published public package exists, it must be corrected manually because that cannot be fixed from inside the workspace alone.
