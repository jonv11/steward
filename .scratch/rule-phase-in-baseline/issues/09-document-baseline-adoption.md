# 09 — Document baseline adoption and update `setup-steward`

Status: done
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

- [x] The configuration reference documents the `baseline:` block and activation rule.
- [x] The maintainer guide has an adoption walkthrough using `baseline generate`.
- [x] `setup-steward` uses a baseline for rules with existing debt, noting the minimum Steward version.
- [x] Status and roadmap reflect the delivered capability.

## Answer

The configuration reference has a `baseline` section (fields, activation, matching, drift),
a file-overview row and command rows. The maintainer guide has an "Adopting rules on an
existing repository" walkthrough and its limitation now names the missing growth gate. The
`setup-steward` and `steward-cli-maintainer` skills use a baseline for existing debt and fall
back to the old advice on `0.18.1` and earlier. Status and roadmap reflect the delivered
slices.
