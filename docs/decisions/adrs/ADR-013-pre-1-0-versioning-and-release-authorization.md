---
type: adr
status: Accepted
category: Release Governance
description: Keeps Steward on the 0.x line until an explicit stable-release decision authorizes 1.0.0
decision_date: 2026-04-15
last_updated: 2026-06-06
---

# ADR-013: Pre-1.0 Versioning and Release Authorization

---

## Context

The repository drifted into a premature `1.0.0` posture even though the project has not yet been explicitly approved for a first stable shipment. That created inconsistent metadata, planning artifacts, and release messaging.

Steward already has an active pre-stable lineage through `v0.9.0`, plus additional implemented scope that justifies a new pre-1.0 baseline. What was missing was an explicit governance rule describing:

- when the project may leave the `0.x.y` line
- when intentional public releases are allowed before `1.0.0`
- where the canonical current version lives
- which changes justify patch vs minor bumps before `1.0.0`
- which artifacts must be updated whenever the version changes

## Decision

Steward remains on the `0.x.y` line until a separate, explicit release-authorization decision approves `1.0.0`.

### 1. Current baseline

- The corrected current version is `0.14.0`.
- `Directory.Build.props` is the source of truth for the repository version.
- Assembly/package/version-command output must derive from that shared MSBuild property set.

### 2. Who authorizes `1.0.0`

`1.0.0` may be created only after both conditions are met:

1. A dedicated accepted ADR, or an accepted amendment to this ADR, explicitly authorizes the stable release.
2. The active release-readiness artifacts show green evidence for the agreed stable-release criteria.

No README wording, package metadata, changelog entry, workflow, or tag reference may imply that `1.0.0` has happened before that decision exists.

### 3. Public pre-1.0 releases

Intentional public releases on the `0.x.y` line are allowed before `1.0.0` provided that:

1. The version bump follows the patch/minor rules in this ADR.
2. The active readiness and release-process artifacts show green evidence for the intended public release.
3. Release notes, release assets, and README wording describe the release as pre-stable rather than implying `1.0.0`.

Public `0.x` publication does not authorize `1.0.0`, weaken the separate stable-release gate, or imply stable-support guarantees that the repo has not claimed.

### 4. Pre-1.0 patch bumps

Use `0.x.(y+1)` only for scoped corrections on the current pre-1.0 minor line, such as:

- bug fixes
- packaging and installation corrections
- documentation fixes
- compatibility fixes
- test-only hardening that does not materially expand product scope

Patch bumps must not be used to silently introduce new roadmap scope.

### 5. Pre-1.0 minor bumps

Use `0.(x+1).0` for intentional scope advancement on the pre-stable roadmap.

Pre-1.0 minor bumps require written rationale in the active planning/state artifacts and should correspond to a meaningful delivered slice, not an arbitrary calendar tick.

### 6. No casual bumping

No one should silently advance the repo to a new pre-1.0 minor just because additional work landed. The planning artifacts must explain why the new baseline is the most coherent representation of delivered scope.

### 7. Versioning and release readiness

Release readiness gates the right to publish or advertise a version, not just the mechanical ability to build or pack it.

- Packaging success alone does not authorize `1.0.0`.
- A future stable-release decision may still choose to ship from a later pre-1.0 baseline than `0.14.0`.
- Until explicit stable criteria are accepted, outstanding future work remains planned on the `0.x` line rather than a `1.x` line.

### 8. Required updates when the version changes

Every intentional version change must update, at minimum:

- `Directory.Build.props`
- `CHANGELOG.md`
- version and release wording in `README.md`
- version wording in `AGENTS.md`
- `docs/project/status.md`
- `docs/project/roadmap.md`

The release-preparation commit must describe the target as pending and must not claim that publication has completed. After the tag-driven workflow and NuGet publication are verified, a post-release state commit updates public and project wording from pending to shipped and closes the milestone.

The operator must also review `docs/README.md`, user and contributor guides, and `docs/requirements/requirements-traceability.md`, updating them only when the version change alters their navigation, behavior, milestone targets, or status summaries. Release readiness evidence is owned by `docs/project/status.md`, `docs/project/roadmap.md`, and `docs/project/release-publication-checklist.md`; no separate active readiness plan is required.

## Consequences

- The repository has one authoritative source of truth for version metadata.
- Premature stable-release messaging becomes a governance violation, not just a documentation mistake.
- Future work continues on a coherent pre-1.0 roadmap until explicit stable criteria are approved.
- Version changes become deliberate release-governance actions rather than casual metadata edits.
