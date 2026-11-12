# 08 — Publish JSON Schemas for the JSON output

Status: done
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/agent-workflow-surface/map.md`

## What to build

Agents parse `--output json`, but the contract lived only in prose (RFC-010, the agent
integration guide), and the guide had already drifted from the code (`filePath` for `path`,
`warning` for `warn`). The sibling `tracker` CLI ships a schema per command and prints it with
`tracker schema`. Publish steward's envelope, error shape and `check` data the same way, so an
agent can validate output against the binary it runs.

## Acceptance criteria

- [x] `steward schema` lists the schemas; `steward schema <command>` prints one; an unknown name is a usage error with a structured JSON error.
- [x] `common` covers the envelope, `data.error` and the diagnostic; `check` covers summary, completion, diagnostics, impact signals, staged completeness and the baseline fields.
- [x] Tests validate real failing, passing, baseline-drift and configuration-error `check` output, a usage error and `version` against the schemas, and show an undocumented field is rejected.
- [x] README, the agent integration guide and the CHANGELOG `[Unreleased]` section document the command.

## Answer

`SchemaCommand` serves draft 2020-12 schemas embedded from `src/Steward.Cli/Schemas/v1/`. Objects
are closed, so a field added without its schema fails `SchemaCommandTests`. Other commands' `data`
is not yet described; the envelope schema still applies to them.
