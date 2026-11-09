# 05 — Per-path rule severity

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/config-transparency/map.md`

## What to build

`path_overrides` can only disable rules and `severity_overrides` is global, so phasing in a
rule declared for a single family forces a repository-wide downgrade. A
`path_overrides[].severity_overrides` would express the real intent and partly addresses the
phase-in gap.

## Acceptance criteria

- [ ] `validation.path_overrides[]` accepts `severity_overrides`, applied after global overrides.
- [ ] `config validate` rejects unknown rule IDs and severities in the new field.
- [ ] The configuration reference documents precedence, and tests cover it.
