# 07 — `config validate` and `config doctor` check the baseline

Status: done
Type: task
Category: enhancement
Blocked by: 02
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

`config validate` fails when `baseline.enabled: true` and the file is missing or malformed.
`config doctor` reports baseline entries whose rule ID no longer exists, or whose path no
longer exists, as dead baseline entries, parallel to the existing dead-config findings.

## Acceptance criteria

- [x] `config validate` reports a missing or malformed baseline as a usage error.
- [x] `config doctor` reports entries for unknown rules and missing paths.
- [x] CLI tests cover each finding (the checks live in the CLI `ConfigCommand`, over core `BaselineSettings`, `BaselineDocument` and `BaselineMatcher`).

## Answer

`config validate` resolves `BaselineSettings` against the config directory's parent and
reports a required-but-missing or malformed baseline as a usage error. `config doctor` adds
`dead-baseline-entry` findings for unknown rule IDs, and for entries whose path no longer
exists **and** that have drifted. A missing path alone is not dead: STWD-001 grandfathers a
required file that does not exist yet, and that entry still matches. Doctor only runs
validation when a baseline is active. Covered by `BaselineCommandTests`.
