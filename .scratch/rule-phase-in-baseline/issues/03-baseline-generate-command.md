# 03 — `steward baseline generate` snapshots current diagnostics

Status: done
Type: task
Category: enhancement
Blocked by: 02
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Add the `baseline` command group with `generate`, which runs the same full validation as
`check` (same config, profile merge, overrides) and writes every diagnostic's identity to the
baseline path. Supports `--output json` with the standard envelope and honours `--config`.

## Acceptance criteria

- [x] `steward baseline generate` writes the baseline file and reports the entry count per rule.
- [x] Running it twice on an unchanged repository produces an identical file.
- [x] JSON output uses the standard envelope; CLI tests cover text and JSON.
- [x] The CHANGELOG `[Unreleased]` section records the command.

## Answer

Landed `BaselineCommand` with `generate`. It runs the same `CheckCommand.Validate` entry point
`check` uses (extracted in this change), writes through `BaselineSettings`, and leaves an
existing file untouched when its entries already match, so regenerating on a later day yields
no diff. It requires a configuration directory. Covered by `BaselineCommandTests`.
