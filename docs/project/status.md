---
type: project
status: Active
summary: Current repository facts, implemented capability baseline, and known health gaps
last_updated: 2026-06-06
review_after: 2026-07-06
---

# Project Status

## Current Baseline

Steward's released version is **`v0.17.0`**. The repository is on an unreleased `v0.18.0` working line, while `Directory.Build.props` intentionally remains at `0.17.0` until release preparation is explicitly authorized.

| Area | Current truth |
|------|---------------|
| Runtime | .NET 10 |
| Tests | 826 passing: 578 core and 248 CLI |
| Validation rules | 21 (`STWD-001` through `STWD-021`) |
| Auto-fix rules | 4 (`STWD-003`, `STWD-007`, `STWD-012`, `STWD-018`) |
| Built-in init profiles | `software`, `docs`, `minimal` |
| Release posture | Public `0.x` releases allowed; `1.0.0` requires explicit authorization under ADR-013 |
| Generated repo artifacts | `STRUCTURE.md` and `docs/decisions/README.md` |

## Implemented On The Unreleased Line

- SARIF 2.1.0 output for `steward check`
- merge-base-aware `check --since <ref>` scope
- deterministic fragment-link auto-fix for STWD-018
- closed artifact-family frontmatter schemas and H1 title patterns (STWD-019)
- H2 section-heading pattern enforcement (STWD-020)
- ordered document section schemas (STWD-021)
- review-driven fixes for subdirectory execution, config validation, orphan detection, and changed-file resolution

These capabilities are not a released `v0.18.0` until the release workflow is intentionally completed.

## Current Health

- Build and tests pass locally.
- Markdown lint passes.
- Hosted build, release, and NuGet publication evidence is established through `v0.17.0`.
- The repository remains pre-1.0; no stable-release authorization has been accepted.

## Known Gaps

- A few expected-failure JSON paths still need contract review before the machine-facing surface can be called universal.
- The first typed-resource-address implementation slice from RFC-009 is not scheduled as delivered work.
- `mixed` and `knowledge` profiles remain intentionally unavailable through `init`.
- `search --role` still matches explicit artifact declarations, not all family-classified files.

Delivered release details belong in [CHANGELOG.md](../../CHANGELOG.md). Current and next work belongs in the [roadmap](roadmap.md).
