# 04 — `check` suppresses baselined diagnostics and reports baseline debt

Status: done
Type: task
Category: enhancement
Blocked by: 02, 03
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

When a baseline is active, `check` removes baselined diagnostics from pass/fail and the
error/warning/info counts, and reports them separately as baseline debt: a count in text
output and a `baseline` object in the JSON summary (`active`, `path`, `suppressed`). New
diagnostics are scored at full severity as today. SARIF output omits baselined results.
`--fix` still fixes baselined violations. Scoped runs (`--since`, `--staged`, paths) apply
the baseline to the diagnostics they produce.

## Acceptance criteria

- [x] With a baseline generated from the current state, `check` passes and reports the suppressed count.
- [x] A new violation added afterwards fails `check` while the baselined ones stay suppressed.
- [x] Without a baseline file, or with `baseline.enabled: false`, output is identical to today (snapshot tests unchanged).
- [x] JSON and SARIF behaviour is covered by CLI tests, and the agent integration guide describes the new summary field.

## Answer

`check` resolves `BaselineSettings`, parses the file (a malformed baseline is a usage error,
exit `2`) and calls `BaselineMatcher.Apply`, which drops baselined diagnostics and recomputes
counts and pass/fail before any output, so text, JSON, SARIF, `--quiet` and the completion
summary all see the same filtered list. JSON gains `data.summary.baseline` (`path`,
`suppressed`) only when a baseline is active. Scoped runs filter what they produce. `--fix`
is unchanged and still fixes baselined violations. Drift is not reported yet (ticket 05).
Covered by `BaselineCommandTests`.
