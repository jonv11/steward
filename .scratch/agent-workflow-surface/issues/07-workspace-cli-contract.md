# 07 — Align the JSON envelope and exit codes with the sibling CLIs

Status: ready-for-human
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/agent-workflow-surface/map.md`

## What to build

An agent that chains `steward`, `tracker` and `jvcode` meets three output contracts. The two
siblings share one; steward differs on every axis:

| | steward | `tracker` and `jvcode` |
|---|---|---|
| Format flag | `--output text\|json\|sarif` | `--output human\|plain\|json`, `--json`, `<TOOL>_OUTPUT` |
| Envelope | `schemaVersion: "steward-json/v1"`, `success`, `exitCode`, error in `data.error` | `schemaVersion: 1`, exactly one of `data` or `error`, `diagnostics[]` |
| Usage error | exit `2` | exit `255` |
| Configuration error | exit `2` | exit `2` |
| Internal error | exit `3` | exit `70` |

`tracker-cli` ADR 0013 already warns callers not to read steward's `2` as a usage error. Pick
one workspace contract, record it as an ADR here, and migrate. The siblings' table is the
cheaper target: two tools already publish it and steward is pre-`1.0`.

## Acceptance criteria

- [ ] An ADR records the workspace contract (envelope, format flag, exit codes) and the migration path.
- [ ] steward emits the chosen contract, with snapshot tests updated and the change called out as breaking in the CHANGELOG.
- [ ] `docs/guide/agent-integration.md` and the README exit-code tables match.

## Out of scope

- SARIF output, which stays on `check`.

## Comments

- HITL: this breaks every existing caller of steward's JSON and exit codes, so the maintainer
  decides whether and when, per ADR-013.
