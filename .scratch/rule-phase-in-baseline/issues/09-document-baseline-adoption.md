# 09 — Document baseline adoption and update `setup-steward`

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: 04, 05
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Document the baseline workflow in the maintainer guide and configuration reference
(the `baseline:` block, activation rule, regeneration, drift), and update the
`setup-steward` skill so a rule with existing violations can be gated through a baseline
instead of being downgraded. Update `docs/project/status.md` known gaps and the roadmap.

## Acceptance criteria

- [ ] The configuration reference documents the `baseline:` block and activation rule.
- [ ] The maintainer guide has an adoption walkthrough using `baseline generate`.
- [ ] `setup-steward` uses a baseline for rules with existing debt, noting the minimum Steward version.
- [ ] Status and roadmap reflect the delivered capability.
