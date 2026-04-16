# Implementation Status

Last updated: 2026-04-17

## Current Baseline

Steward is currently on **`v0.12.0`**. The repository is **still pre-1.0**: `v1.0.0` is reserved for a future stable release and is not authorized yet. Versioning governance is recorded in [ADR-013](decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

| Area | Current state |
|------|---------------|
| Version line | `0.x.y` only until explicit stable-release approval |
| Current repo version | `0.12.0` |
| Tests | 539 passing (`375` core, `164` CLI) |
| Validation rules | 13 (`STWD-001` through `STWD-013`) |
| Maintainer types | 6 (`structure-document`, `index`, `directory-index`, `managed-section`, `frontmatter-auto`, `manifest`) |
| Packaging | `dotnet pack` succeeds cleanly for `Steward.Cli.0.12.0.nupkg` |
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

## What Is Already True In `v0.10.0`

- Shared version metadata now comes from `Directory.Build.props`, and `steward version` reports `0.10.0`.
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

## Remaining Before First Stable Shipment

The detailed categorized list lives in [Pre-1.0 Readiness Plan](planning/pre-1-0-readiness-plan.md). At a high level, the remaining work is now concentrated in a smaller set of release-hardening items:

### Required

- First hosted green runs from the cross-platform build/test/pack automation

### Strongly Recommended

- An explicit later pre-1.0 roadmap ordering (v0.12.0 scope now detailed in [milestone-plan.md](planning/milestone-plan.md))

### Optional / Later Pre-1.0

- Artifact type schema work and other deferred requirement families on later `0.x` milestones
- Re-enabling deferred profiles (`mixed`, `knowledge`) when their contracts are enriched (see [ADR-014](decisions/adrs/ADR-014-non-software-profile-scope.md))

## Manual Follow-Up Outside The Repo

This repository contains no local git tags, but if any remote `v1.0.0` tag, release entry, or published public package exists, it must be corrected manually because that cannot be fixed from inside the workspace alone.
