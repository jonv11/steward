# Implementation Instructions

- **Version:** 0.10.0
- **Status:** Active
- **Last updated:** 2026-04-16

---

## Purpose

This guide tells contributors what to do next from the current `v0.10.0` baseline. It intentionally avoids the old “work toward a shipped `v1.0.0`” framing. The active question is: what should land on the pre-1.0 line before stable-release criteria are approved and then satisfied?

## Immediate Execution Order

1. Keep versioning and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
2. Prioritize the required items in [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md).
3. Treat later pre-1.0 candidates as roadmap work, not as implicit stable blockers.

## Current Priority Stack

### `v0.11.0` — Stable-Release Hardening And Trust Fixes

Implement the smallest set of work that materially improves confidence in a first stable shipment, plus the critical trust fixes identified in the 2026-04-16 CLI review cycle:

- **Fix scoped validation false positives (B6).** Add `AllDiscoveredFiles` to `ValidationContext`. Repo-wide obligation rules (`STWD-001`, `STWD-007`, `STWD-009`) must check file existence against the full discovered set, not `TargetFiles`. Add regression tests for changed/staged scope on clean and single-file-changed repos. This is the single most critical product defect.
- **Include governance coverage in status JSON output (B7).** Add a `coverage` object to JSON when `--coverage` is requested. Add a contract test.
- **Add exit code regression tests.** Explicit tests for check-pass (0), check-fail (1), and bad-input (2) exit codes.
- Confirm the new cross-platform build/test/pack workflow goes green on Windows, macOS, and Linux.
- Stabilize release-critical dependencies away from preview/beta packages where feasible.
- Expand stable-surface contract tests for the most user-visible command/output contracts.
- Write the publication and verification steps that would be used for an intentional stable release.

### `v0.12.0` — Workflow Polish And Depth Improvements

Use this milestone for remaining operator-facing improvements that strengthen day-to-day stewardship loops without changing the release-governance story:

- **Standardize preview/apply flag conventions.** Align mutation commands toward a common `--apply` (default preview) pattern.
- **Fix `md query --pattern` batch mode.** Resolve argument parsing ambiguity so multi-file structural queries work.
- **Fix init scaffolding immediate-failure experience.** Scaffold `required: false` or equivalent for artifacts that don't exist yet.
- **Improve `explain path` applicability filtering.** Filter to rules that can actually fire on the given file's governance context.
- **Deepen `config suggest` heuristics.** Detect decisions, planning docs, indexes, state docs, and roles for mature-repo bootstrap.
- **Deepen `config doctor` checks.** Add shadowed rules, dead suppressions, and no-effect declaration detection.
- **Add `fm-validate` to `md edit`.** Implement the accepted RFC-004 frontmatter-validate operation.
- **Exclude test fixtures from governance coverage.** Add coverage scoping or repo-zone ignore controls.
- Fit-and-finish for readiness/status/reporting views.

### `v0.13.0+` — Later Pre-1.0 Expansion

Treat the following as later pre-stable scope unless stable criteria explicitly pull them forward:

- artifact type schema system work (ADR-012)
- typed resource-address follow-on work
- split/extract workflows
- optional host-specific integrations

## Contributor Rules While Executing The Plan

- Do not introduce new `1.x` version targets in active docs or metadata.
- Do not bump the repo version casually; follow ADR-013.
- When a milestone or version target changes, update the active planning/state artifacts in the same change.
- Keep historical audit documents historical; fix links and active references, but do not rely on stale audit wording as current truth.
