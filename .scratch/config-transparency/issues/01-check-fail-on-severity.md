# 01 — `check --fail-on <severity>`

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/config-transparency/map.md`

## What to build

Let CI gate on warnings without rewriting rule severities in `policy.yaml`. Only `error`
affects the exit code today, so most rules are non-blocking by default.

## Acceptance criteria

- [ ] `steward check --fail-on warning` exits non-zero when any warning is reported; `--fail-on error` matches today's behaviour.
- [ ] The exit-code table in the README and the CLI help document the option.
- [ ] Tests cover each accepted severity and an invalid value.
