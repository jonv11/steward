# 06 — `steward baseline status` reports baseline debt

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: 02
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Add `steward baseline status`, which reports entry counts by rule and the baseline's
age (`generated_at`), plus how many entries currently match and how many have drifted, so
baseline debt is visible without opening the JSON file. Standard JSON envelope.

## Acceptance criteria

- [ ] Text and JSON output show counts by rule, age, matched and drifted.
- [ ] With no baseline it reports that none is active and exits `0`.
- [ ] CLI tests cover both cases.
