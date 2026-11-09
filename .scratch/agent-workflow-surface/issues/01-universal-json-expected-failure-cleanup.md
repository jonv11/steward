# 01 — Route remaining expected failures through the JSON envelope

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/agent-workflow-surface/map.md`

## What to build

Route the remaining expected-failure paths through the standard envelope before claiming
universal JSON coverage. Deferred from `v0.18.0`: the standard envelope is already the only
supported JSON mode, and the remaining paths did not block that release.

## Acceptance criteria

- [ ] Every command's expected-failure paths emit the standard envelope under `--json`.
- [ ] A test enumerates the expected-failure paths and asserts the envelope for each.
- [ ] The agent integration guide claims universal JSON coverage only once this lands.
