---
type: project
status: Active
summary: Rule phase-in and baseline is the committed next milestone after v0.18.1
last_updated: 2026-08-27
review_after: 2026-09-25
---

# Roadmap

## Current Milestone: Rule Phase-In And Baseline

Committed as the next milestone after `v0.18.1` (see [docs/project/status.md](status.md) for the current release posture). Selected over the other backlog candidates because it's the one item both external repositories in the 2026-08-24 adoption trial hit and both flagged as blocking, not just annoying — see the [maintainer configuration experience audit](../history/audits/maintainer-configuration-experience-audit-2026-08-24.md) and the [backlog](backlog.md), which called it the "highest-impact adoption gap."

### Problem

Enabling a rule today applies it to all existing content at once. A repository that already has history and turns a rule on gets an immediate wall of errors across every pre-existing file, with no way to say "only enforce this going forward." That makes adopting a new rule into an established repository effectively all-or-nothing.

### Scope

Defined in [RFC-017](../decisions/rfcs/RFC-017-rule-phase-in-and-baseline.md) (Accepted): a generated `.steward/baseline.json` snapshots current violations at adoption time so `check` suppresses them while still catching new ones, with a drift signal to show when grandfathered debt gets fixed. Implementation is not yet started or scoped into slices.

### Explicit Deferral (carried from v0.18.0)

The remaining universal JSON expected-failure cleanup was deferred from `v0.18.0`. The standard envelope is already the only supported JSON mode and the remaining paths did not block that release's policy-schema, SARIF, scoped-validation, and reliability scope. Tracked in the [backlog](backlog.md).

## Next Milestone

Not yet selected. Once rule phase-in ships, revisit the backlog's remaining validated enhancements — adoption-oriented config transparency (`--fail-on <severity>`, policy impact preview, policy evaluation trace, governance-gap explanation) is the next-strongest candidate on current evidence, since several of those items are workarounds people reach for in the absence of phase-in. `v1.0.0` remains unscheduled until a separate decision explicitly authorizes it under [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
