---
type: project
status: Active
summary: Rule phase-in and baseline is the committed next milestone after v0.18.1
last_updated: 2026-11-09
review_after: 2026-12-24
---

# Roadmap

Living plan, not a commitment. The roadmap names the current and next milestone and why they
were chosen; the work itself lives in the local-markdown issue tracker under `.scratch/` (see
the [issue tracker guide](../agents/issue-tracker.md)). Validated but unscheduled work is every
effort under `.scratch/` that this page does not name.

## Current Milestone: Rule Phase-In And Baseline

Effort: `.scratch/rule-phase-in-baseline/spec.md`.

Committed as the next milestone after `v0.18.1` (see [status](status.md) for the current release posture). Selected over the other candidates because it's the one item both external repositories in the 2026-08-24 adoption trial hit and both flagged as blocking, not just annoying — see the [maintainer configuration experience audit](../history/audits/maintainer-configuration-experience-audit-2026-08-24.md), which called it the "highest-impact adoption gap."

The design authority is [RFC-017](../decisions/rfcs/RFC-017-rule-phase-in-and-baseline.md) (Accepted). As of 2026-11-09 there is no baseline code and the spec is not yet sliced into tickets. Until it ships, the [setup-steward](../../.agents/skills/setup-steward/SKILL.md) skill works around the gap by gating rules only where a repository has zero existing violations.

### Explicit Deferral (carried from v0.18.0)

The remaining universal JSON expected-failure cleanup was deferred from `v0.18.0`. The standard envelope is already the only supported JSON mode and the remaining paths did not block that release's policy-schema, SARIF, scoped-validation, and reliability scope. Tracked as `.scratch/agent-workflow-surface/issues/01-universal-json-expected-failure-cleanup.md`.

## Next Milestone

Not yet selected. Once rule phase-in ships, the strongest candidate on current evidence is the configuration transparency effort (`.scratch/config-transparency/map.md`: `--fail-on <severity>`, policy impact preview, policy evaluation trace, governance-gap explanation, per-path severity), since several of those items are workarounds people reach for in the absence of phase-in. `v1.0.0` remains unscheduled until a separate decision explicitly authorizes it under [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

## Open efforts

| Effort | Driven by | What it holds |
| --- | --- | --- |
| `rule-phase-in-baseline` | spec | Current milestone (RFC-017) |
| `adoption-trial-defects` | map | Open defects from the 2026-08-24 trial and the `setup-steward` work |
| `config-transparency` | map | Next-milestone candidate; configuration debuggability |
| `policy-expressiveness` | map | Rules maintainers cannot express in `.steward/` today |
| `agent-workflow-surface` | map | JSON coverage, impact view, adoption flow, integrations |
| `markdown-editing` | map | Structural editing follow-ons, including deferred RFC-012 |
| `configuration-docs-gaps` | map | Configuration behaviour that is undocumented |

The previous `docs/project/backlog.md` is preserved as a [snapshot](../history/plans/backlog-2026-11-09.md).
