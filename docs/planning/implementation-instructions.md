---
type: planning
version: 0.16.0
status: Active
last_updated: 2026-04-18
---

# Implementation Instructions

---

## Purpose

This guide tells contributors what to do next from the current `v0.16.0` baseline. The adoption-readiness and runtime-coherence pass is now delivered locally, and the active question is: what should still land on the pre-1.0 line before stable-release criteria are approved and then satisfied?

For the canonical reference on *how* to perform each type of work, see [workflow-guide.md](workflow-guide.md).

## Immediate Execution Order

1. Keep versioning and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) and [release-process.md](release-process.md).
2. Capture the first hosted green cross-platform CI run and the first hosted green GitHub Release run in [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md); those are now the main remaining operational evidence gaps.
3. Keep active planning and state artifacts synchronized with the real repository state whenever milestone scope, release process, or delivered capability changes.

## Current Priority Stack

### Current Release-Hardening Work

Keep the current pre-1.0 line honest, stable, and reviewable:

- Keep versioning, packaging, and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
- Keep the pre-1.0 release process trustworthy: release-intent labels, `CHANGELOG.md`, tag naming, and GitHub Release assets should remain deterministic and reviewable.
- Capture hosted CI/release evidence rather than implying local-only validation is enough for stable confidence.
- Preserve trust in the session-start and reporting surfaces: `orient`, `outline`, `status`, and `check` should stay deterministic, legible, and review-friendly.
- Treat documentation/state drift as a real defect: update active planning and status artifacts in the same change when current repo truth changes.
- Keep the release-publication checklist and readiness evidence believable; do not imply that stable publication already happened.

### `v0.16.0` — Delivered

The current `v0.16.0` line delivered the adoption-readiness and runtime-coherence pass:

- **First-hour onboarding path:** `README.md` now includes a tested “First 15 Minutes” flow and repo-independent source-build guidance, including the `global.json` hazard.
- **Agent-safe JSON contract baseline:** The CC-01 through CC-10 contract hardening work is now present on the mainline, including standard envelopes, structured errors, and JSON-mode mutation fixes.
- **Rule/runtime coherence:** Family validation now applies consistently to explicit artifacts and frontmatter-sensitive family counts/reporting stay aligned across validation and status surfaces.
- **Help and UX polish:** Runtime help now uses `steward`, value placeholders are restored, `config suggest` exposes confidence/conservative hints, and the Markdown subsystem has clearer operational help/examples.

RFC-009 (typed resource addresses) remains deferred to a later pre-1.0 milestone.

### `v0.17.0+` — Later Pre-1.0 Expansion

Treat the following as later pre-stable scope unless stable criteria explicitly pull them forward:

- universal JSON envelope guarantees across all expected failure paths
- hosted CI/release evidence capture and any follow-on publication hardening
- deeper machine handoff/provenance surfaces
- workflow/session modeling
- heading selector fuzzy matching in MdPath
- optional host-specific integrations

## Contributor Rules While Executing The Plan

- Do not introduce new `1.x` version targets in active docs or metadata.
- Do not bump the repo version casually; follow ADR-013.
- When a milestone or version target changes, update the active planning/state artifacts in the same change.
- Keep historical audit documents historical; fix links and active references, but do not rely on stale audit wording as current truth.
