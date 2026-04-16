---
type: planning
version: 0.13.0
status: Active
last_updated: 2026-04-16
---

# Implementation Instructions

---

## Purpose

This guide tells contributors what to do next from the current `v0.13.0` baseline. The `v0.11.0` through `v0.13.0` milestone work is already delivered. The active question is: what should still land on the pre-1.0 line before stable-release criteria are approved and then satisfied?

## Immediate Execution Order

1. Keep versioning and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
2. Treat the first hosted green cross-platform CI run in [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md) as the only current required stable-shipment blocker.
3. Keep active planning and state artifacts synchronized with the real repository state whenever milestone scope or delivered capability changes.

## Current Priority Stack

### Current Release-Hardening Work

Keep the current pre-1.0 line honest, stable, and reviewable:

- Keep versioning, packaging, and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
- Preserve trust in the session-start and reporting surfaces: `orient`, `outline`, `status`, and `check` should stay deterministic, legible, and review-friendly.
- Treat documentation/state drift as a real defect: update active planning and status artifacts in the same change when current repo truth changes.
- Keep the release-publication checklist and readiness evidence believable; do not imply that stable publication already happened.

### `v0.14.0` — Type-Aware Validation Expansion

The next planned milestone is still the type-aware validation expansion captured in the milestone and readiness plans:

- **Required sections per family.** Add `required_sections` enforcement for ADRs, RFCs, PRDs, and similar document families.
- **Naming-pattern enforcement per family.** Turn stored `naming_pattern` fields into actual validation rather than documentation-only intent.
- **Directory minimum-count expectations.** Add `directory_expectations.min_count` enforcement for directories where presence/coverage matters.
- Keep the scope narrow enough that it strengthens repository trust without blurring the current release story.

### `v0.15.0+` — Later Pre-1.0 Expansion

Treat the following as later pre-stable scope unless stable criteria explicitly pull them forward:

- typed resource-address follow-on work
- split/extract workflows
- workflow/session modeling
- optional host-specific integrations

## Contributor Rules While Executing The Plan

- Do not introduce new `1.x` version targets in active docs or metadata.
- Do not bump the repo version casually; follow ADR-013.
- When a milestone or version target changes, update the active planning/state artifacts in the same change.
- Keep historical audit documents historical; fix links and active references, but do not rely on stale audit wording as current truth.
