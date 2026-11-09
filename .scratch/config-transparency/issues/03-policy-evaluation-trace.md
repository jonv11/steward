# 03 — Policy evaluation trace in `explain path`

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/config-transparency/map.md`

## What to build

Add precedence and hypothetical-policy detail to `explain path` rather than creating a broad
new command family. `explain path` currently lists applicable rule IDs with no severity and
no config provenance, which leaves the configuration surface undebuggable.

## Acceptance criteria

- [ ] `explain path` reports, per applicable rule, its effective severity and the config entry it came from.
- [ ] Overrides that changed or disabled a rule for the path are shown with their source.
- [ ] The JSON envelope carries the same detail, and tests cover it.
