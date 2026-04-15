# Implementation Status

Last updated: 2026-04-15

## Current Baseline

Steward is currently on **`v0.10.0`**. The repository is **still pre-1.0**: `v1.0.0` is reserved for a future stable release and is not authorized yet. Versioning governance is recorded in [ADR-013](decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

| Area | Current state |
|------|---------------|
| Version line | `0.x.y` only until explicit stable-release approval |
| Current repo version | `0.10.0` |
| Tests | 487 passing (`366` core, `121` CLI) |
| Validation rules | 13 (`STWD-001` through `STWD-013`) |
| Maintainer types | 6 (`structure-document`, `index`, `directory-index`, `managed-section`, `frontmatter-auto`, `manifest`) |
| Packaging | `dotnet pack` succeeds cleanly for `Steward.Cli.0.10.0.nupkg` |
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

## What Is Already True In `v0.10.0`

- Shared version metadata now comes from `Directory.Build.props`, and `steward version` reports `0.10.0`.
- The repo no longer needs per-project version duplication.
- `config validate` now catches semantic problems such as unknown rule ids, invalid maintainer types, bad `depends_on` references, and invalid path-policy regex/glob declarations.
- `check` completion summaries now follow repository policy instead of hardcoded rule assumptions.
- `status` now surfaces required artifacts, recommended artifacts, and state-document coverage instead of only hard-required entries.
- The accepted RFC-007 governance-enhancement work is materially present in the codebase and should be treated as part of the delivered pre-1.0 baseline, not as a hypothetical post-`1.0.0` future.

## Remaining Before First Stable Shipment

The detailed categorized list lives in [Pre-1.0 Readiness Plan](planning/pre-1-0-readiness-plan.md). At a high level, the remaining work is now concentrated in a smaller set of release-hardening items:

### Required

- Cross-platform build/test/pack automation
- Dependency stabilization away from preview/beta release-critical packages
- Final distribution/publication hardening for a real stable release process

### Strongly Recommended

- Broader contract-style command/output coverage for the stable surface
- Explicit handling of later pre-1.0 roadmap candidates that are still valuable but not stable blockers

### Optional / Later Pre-1.0

- Additional `config show --effective` policy surfacing
- Artifact type schema work and other deferred requirement families on later `0.x` milestones

## Manual Follow-Up Outside The Repo

This repository contains no local git tags, but if any remote `v1.0.0` tag, release entry, or published public package exists, it must be corrected manually because that cannot be fixed from inside the workspace alone.
