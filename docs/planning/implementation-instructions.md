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

### `v0.11.0` — Stable-Release Hardening

Implement the smallest set of work that materially improves confidence in a first stable shipment:

- Confirm the new cross-platform build/test/pack workflow goes green on Windows, macOS, and Linux.
- Stabilize release-critical dependencies away from preview/beta packages where feasible.
- Expand stable-surface contract tests for the most user-visible command/output contracts.
- Write the publication and verification steps that would be used for an intentional stable release.

### `v0.12.0` — Workflow And Explainability Polish

Use this milestone for remaining operator-facing improvements that strengthen day-to-day stewardship loops without changing the release-governance story:

- fit-and-finish for readiness/status/reporting views
- any still-open ergonomics improvements that stay within the accepted product direction

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
