# 04 — `check` suppresses baselined diagnostics and reports baseline debt

Status: ready-for-agent
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

- [ ] With a baseline generated from the current state, `check` passes and reports the suppressed count.
- [ ] A new violation added afterwards fails `check` while the baselined ones stay suppressed.
- [ ] Without a baseline file, or with `baseline.enabled: false`, output is identical to today (snapshot tests unchanged).
- [ ] JSON and SARIF behaviour is covered by CLI tests, and the JSON contract docs describe the new summary field.
