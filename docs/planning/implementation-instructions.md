---
type: planning
version: 0.17.0
status: Active
last_updated: 2026-06-05
---

# Implementation Instructions

---

## Purpose

This guide tells contributors what to do next from the current `v0.17.0` baseline. The documentation overhaul, contract-surface deepening, compatibility-shim cleanup, and runtime exception hardening are now delivered locally, and the active question is: what should still land on the pre-1.0 line before stable-release criteria are approved and then satisfied?

For the canonical reference on *how* to perform each type of work, see [workflow-guide.md](workflow-guide.md).

## Immediate Execution Order

1. Keep versioning and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) and [release-process.md](release-process.md).
2. Note that hosted cross-platform CI evidence and hosted tag-driven release evidence are now captured through v0.17.0; [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md) reflects this. The remaining operational evidence gap is explicit stable-release authorization per ADR-013.
3. Keep active planning and state artifacts synchronized with the real repository state whenever milestone scope, release process, or delivered capability changes.

## Current Priority Stack

### Current Release-Hardening Work

Keep the current pre-1.0 line honest, stable, and reviewable:

- Keep versioning, packaging, and release messaging aligned with [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
- Keep the pre-1.0 release process trustworthy: release-intent labels, `CHANGELOG.md`, tag naming, and GitHub Release assets should remain deterministic and reviewable.
- Hosted cross-platform CI and tag-driven release evidence are now confirmed through v0.17.0; maintain that evidence record as new releases land.
- Preserve trust in the session-start and reporting surfaces: `orient`, `outline`, `status`, and `check` should stay deterministic, legible, and review-friendly.
- Treat documentation/state drift as a real defect: update active planning and status artifacts in the same change when current repo truth changes.
- Keep the release-publication checklist and readiness evidence believable; do not imply that stable publication already happened.

### `v0.17.0` — Delivered

The current `v0.17.0` line delivered the work that actually landed after `v0.16.0`:

- **Documentation and onboarding overhaul:** the repo now ships dedicated guides for maintainers, contributors, AI agents, and configuration authors, plus clearer docs navigation and a less overloaded README.
- **JSON contract and handoff hardening:** the standard envelope is now the only JSON mode, `explain path`/`refs`/`search` emit richer machine handoff data, and contract tests cover more success and failure paths.
- **Governance refinement:** `.agents/skills/**/SKILL.md` files are now governed by a `skill` artifact family that enforces the shared `name`/`description` frontmatter floor.
- **Runtime resilience:** discovery now skips inaccessible paths, and top-level CLI exceptions produce stable access-denied/internal-error output instead of raw stack traces.

Hosted cross-platform CI and tag-driven release evidence are now confirmed through v0.17.0. The remaining universal-JSON cleanup and typed-address/config-model follow-ons remain deferred.

### `v0.18.0` — Active Next Scope

Treat the following as the active next pre-stable scope:

- finish routing the remaining expected-failure JSON paths through the standard envelope
- scope and implement the first narrow RFC-009 typed-resource-address slice
- decide the adoption-oriented config-model follow-on path from the stress-test review
- carry forward workflow/session modeling and heading-selector fuzzy matching if the trust-floor items above finish cleanly
- revisit deferred `mixed`/`knowledge` profile work only if their contracts become meaningfully distinct

## Contributor Rules While Executing The Plan

- Do not introduce new `1.x` version targets in active docs or metadata.
- Do not bump the repo version casually; follow ADR-013.
- When a milestone or version target changes, update the active planning/state artifacts in the same change.
- Keep historical audit documents historical; fix links and active references, but do not rely on stale audit wording as current truth.
