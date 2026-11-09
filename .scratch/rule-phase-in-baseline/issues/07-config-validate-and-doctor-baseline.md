# 07 — `config validate` and `config doctor` check the baseline

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: 02
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

`config validate` fails when `baseline.enabled: true` and the file is missing or malformed.
`config doctor` reports baseline entries whose rule ID no longer exists, or whose path no
longer exists, as dead baseline entries, parallel to the existing dead-config findings.

## Acceptance criteria

- [ ] `config validate` reports a missing or malformed baseline as a usage error.
- [ ] `config doctor` reports entries for unknown rules and missing paths.
- [ ] Core and CLI tests cover each finding.
