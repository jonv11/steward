# 03 — `steward baseline generate` snapshots current diagnostics

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: 02
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Add the `baseline` command group with `generate`, which runs the same full validation as
`check` (same config, profile merge, overrides) and writes every diagnostic's identity to the
baseline path. Supports `--output json` with the standard envelope and honours `--config`.

## Acceptance criteria

- [ ] `steward baseline generate` writes the baseline file and reports the entry count per rule.
- [ ] Running it twice on an unchanged repository produces an identical file.
- [ ] JSON output uses the standard envelope; CLI tests cover text and JSON.
- [ ] The CHANGELOG `[Unreleased]` section records the command.
