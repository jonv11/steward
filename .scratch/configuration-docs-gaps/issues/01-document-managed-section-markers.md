# 01 — Document managed-section marker syntax

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/configuration-docs-gaps/map.md`

## What to build

The `steward:begin`/`steward:end` marker form required by `maintenance` type `index` is
documented nowhere. The markers are also asymmetric: begin carries `id` and `owner`, end must
be bare `<!-- steward:end -->`, and an `id` on the end marker fails silently.

## Acceptance criteria

- [ ] The configuration reference documents both markers with a working example.
- [ ] The asymmetry and the silent failure on an `id`-bearing end marker are called out.
