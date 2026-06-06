---
type: project
status: Active
summary: Pending v0.18.0 publication and the decision boundary for the following milestone
last_updated: 2026-06-06
review_after: 2026-07-06
---

# Roadmap

## Current Milestone: Publish `v0.18.0`

The `v0.18.0` release candidate is prepared with stronger policy schemas, CI handoff, and validation reliability.

### Landed

See [docs/project/status.md](status.md) for the authoritative release-candidate capability list.

### Before Release

- run the full local publication checklist and obtain green hosted CI for the release commit
- run the release process only when explicitly authorized
- verify the GitHub Release assets and NuGet package, then commit the post-release state update

### Explicit Deferral

The remaining universal JSON expected-failure cleanup is deferred from `v0.18.0`. The standard envelope is already the only supported JSON mode and the remaining paths do not block the policy-schema, SARIF, scoped-validation, and reliability scope of this release. The follow-on is tracked in the [backlog](backlog.md).

## Next Milestone

No version after `v0.18.0` is committed. The next milestone should be selected after the `v0.18.0` release boundary is reviewed.

Candidate themes:

- a narrow RFC-009 typed-resource-address slice
- adoption-oriented config transparency and phase-in support
- deferred Markdown refactors or governed suppressions when prerequisites are satisfied

Unscheduled items belong in the [backlog](backlog.md). `v1.0.0` remains unscheduled until a separate decision explicitly authorizes it under [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
