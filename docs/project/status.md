---
type: project
status: Active
summary: Current repository facts, implemented capability baseline, and known health gaps
last_updated: 2026-08-27
review_after: 2026-09-25
---

# Project Status

## Current Baseline

Steward's latest published version is **`v0.18.0`**. **`v0.18.1`** is prepared as a release candidate: its version metadata and changelog section are in place, but the tag, GitHub Release, and NuGet publication are still pending.

| Area | Current truth |
|------|---------------|
| Runtime | .NET 10 |
| Tests | 838 passing: 585 core and 253 CLI |
| Validation rules | 21 (`STWD-001` through `STWD-021`) |
| Auto-fix rules | 4 (`STWD-003`, `STWD-007`, `STWD-012`, `STWD-018`) |
| Built-in init profiles | `software`, `docs`, `minimal` |
| Release posture | `v0.18.0` published, `v0.18.1` pending publication; public `0.x` releases allowed; `1.0.0` requires explicit authorization under ADR-013 |
| Generated repo artifacts | `STRUCTURE.md` and `docs/decisions/README.md` |

## Delivered In v0.18.0

- SARIF 2.1.0 output for `steward check`
- merge-base-aware `check --since <ref>` scope
- deterministic fragment-link auto-fix for STWD-018
- closed artifact-family frontmatter schemas and H1 title patterns (STWD-019)
- H2 section-heading pattern enforcement (STWD-020)
- ordered document section schemas (STWD-021)
- check-only SARIF enforcement, with repository output defaults limited to text or JSON

## Prepared For v0.18.1 (Pending Publication)

A 2026-08-24 adoption trial ran Steward against two external repositories (`jvcode`, `mdrule`) and logged real friction; see the [maintainer configuration experience audit](../history/audits/maintainer-configuration-experience-audit-2026-08-24.md). Three defects it surfaced are already fixed:

- `steward maintain` now reports unmaintainable artifacts as `BLOCKED` instead of `OK`.
- `steward status --coverage` now counts `artifact_families[]` matches as governed.
- `config doctor` no longer flags `forbidden`/`reserved` path rules that match nothing as dead config.

One related defect from the same trial is still open: the same dead-config false positive extends to anticipatory `artifact_families` and `validation.path_overrides` (see [backlog](backlog.md)). The rest of the trial's findings became the [backlog](backlog.md)'s validated-enhancements and documentation-gap entries — nothing further from it is scheduled yet.

The project was also relicensed under Apache-2.0, and routine dependency bumps have landed.

Release publication moved to nuget.org trusted publishing: the release workflow exchanges a GitHub OIDC token for a one-hour API key instead of reading a stored `NUGET_ORG_API_KEY` secret.

## Current Health

- Build and tests pass locally.
- Markdown lint passes.
- Hosted build, release, and NuGet publication evidence is established through `v0.18.0`.
- The repository remains pre-1.0; no stable-release authorization has been accepted.
- The next milestone after `v0.18.1` is rule phase-in and baseline — see the [roadmap](roadmap.md).

## Known Gaps

- A few expected-failure JSON paths still need contract review before the machine-facing surface can be called universal.
- The first typed-resource-address implementation slice from RFC-009 is not scheduled as delivered work.
- `mixed` and `knowledge` profiles remain intentionally unavailable through `init`.
- `search --role` still matches explicit artifact declarations, not all family-classified files.
- Rule phase-in has no baseline/warn-on-existing mode, which the 2026-08-24 trial flagged as the highest-impact adoption blocker (see [backlog](backlog.md)).

Delivered release details belong in [CHANGELOG.md](../../CHANGELOG.md). Current and next work belongs in the [roadmap](roadmap.md).
