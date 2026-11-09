# 05 — Report `baseline-drift` for entries that no longer match

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: 04
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

A baseline entry that matches no current diagnostic is reported as an info-level
`baseline-drift` note naming the entry, with a remediation to run `steward baseline generate`.
Drift never fails `check`. In scoped runs, only entries for files in scope can drift, so a
`--since` run does not report the whole repository's entries as fixed.

## Acceptance criteria

- [ ] Fixing a baselined violation produces one `baseline-drift` info diagnostic and `check` still passes.
- [ ] Scoped runs only report drift for in-scope files.
- [ ] `explain rule` or the rules reference documents `baseline-drift`.
