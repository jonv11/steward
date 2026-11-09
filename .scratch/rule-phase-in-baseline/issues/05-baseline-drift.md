# 05 — Report `baseline-drift` for entries that no longer match

Status: done
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

- [x] Fixing a baselined violation produces one `baseline-drift` info entry and `check` still passes.
- [x] Scoped runs only report drift for in-scope files.
- [x] ~~`explain rule` or the rules reference documents `baseline-drift`~~ — not applicable: drift is not a rule (see Answer); the agent integration guide and CHANGELOG document it.

## Answer

Drift is reported as its own section of `check` output, not as a diagnostic under a
pseudo-rule ID: a `baseline-drift` diagnostic would need a SARIF rule descriptor, would count
toward `Info` and the completion summary, and would appear in `explain` as a rule that cannot
be configured. Text output lists `[info] baseline-drift <rule> <path>: <selector>` lines under
"Baseline drift"; JSON adds `data.baselineDrift[]` and `data.summary.baseline.drift`. Full
runs report every unmatched entry; scoped runs only entries whose path is in scope, and never
pathless entries. Covered by `BaselineCommandTests`.
