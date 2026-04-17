---
type: planning
version: 0.14.0
status: Active
last_updated: 2026-04-17
---

# Implementation Instructions

---

## Purpose

This guide tells contributors what to do next from the current `v0.14.0` baseline. The release-process milestone is now delivered, and the active question is: what should still land on the pre-1.0 line before stable-release criteria are approved and then satisfied?

## Immediate Execution Order

1. Keep versioning and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) and [release-process.md](release-process.md).
2. Treat the first hosted green cross-platform CI run and the first hosted green GitHub Release run in [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md) as the current operational evidence gaps before the next public `0.x` tag.
3. Keep active planning and state artifacts synchronized with the real repository state whenever milestone scope, release process, or delivered capability changes.

## Current Priority Stack

### Current Release-Hardening Work

Keep the current pre-1.0 line honest, stable, and reviewable:

- Keep versioning, packaging, and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
- Keep the pre-1.0 release process trustworthy: release-intent labels, `CHANGELOG.md`, tag naming, and GitHub Release assets should remain deterministic and reviewable.
- Preserve trust in the session-start and reporting surfaces: `orient`, `outline`, `status`, and `check` should stay deterministic, legible, and review-friendly.
- Treat documentation/state drift as a real defect: update active planning and status artifacts in the same change when current repo truth changes.
- Keep the release-publication checklist and readiness evidence believable; do not imply that stable publication already happened.

### `v0.15.0` — Later Pre-1.0 Requirement Families

The next planned milestone should return to product-surface expansion on the now-stabilized pre-1.0 release line:

- **Typed resource-address follow-on work.** Continue the later pre-1.0 address/search alignment tracked in requirements and the milestone plan.
- **Split/extract evaluation.** Revisit the deferred Markdown split/extract workflow work.
- **JSON envelope consistency.** Improve machine-facing output consistency without destabilizing current consumers.

### `v0.16.0+` — Later Pre-1.0 Expansion

Treat the following as later pre-stable scope unless stable criteria explicitly pull them forward:

- workflow/session modeling
- heading selector fuzzy matching in MdPath
- optional host-specific integrations

## Contributor Rules While Executing The Plan

- Do not introduce new `1.x` version targets in active docs or metadata.
- Do not bump the repo version casually; follow ADR-013.
- When a milestone or version target changes, update the active planning/state artifacts in the same change.
- Keep historical audit documents historical; fix links and active references, but do not rely on stale audit wording as current truth.
