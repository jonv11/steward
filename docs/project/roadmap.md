---
type: project
status: Active
summary: Current v0.18.0 working-line scope and the decision boundary for the following milestone
last_updated: 2026-06-06
review_after: 2026-07-06
---

# Roadmap

## Current Milestone: `v0.18.0`

The working line is focused on stronger policy schemas, CI handoff, and validation reliability.

### Landed

See [docs/project/status.md](status.md) for the authoritative list of implemented capabilities on the unreleased working line.

### Before Release

- keep README, guides, changelog, and project status aligned with the 21-rule baseline
- maintain a warning-free self-governance baseline
- decide whether remaining JSON expected-failure cleanup belongs in `v0.18.0` or the next milestone
- run the release process only when explicitly authorized

## Next Milestone

No version after `v0.18.0` is committed. The next milestone should be selected after the `v0.18.0` release boundary is reviewed.

Candidate themes:

- a narrow RFC-009 typed-resource-address slice
- adoption-oriented config transparency and phase-in support
- deferred Markdown refactors or governed suppressions when prerequisites are satisfied

Unscheduled items belong in the [backlog](backlog.md). `v1.0.0` remains unscheduled until a separate decision explicitly authorizes it under [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
