# 01 — Document managed-section marker syntax

Status: done
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/configuration-docs-gaps/map.md`

## What to build

The `steward:begin`/`steward:end` marker form required by `maintenance` type `index` is
documented nowhere. The markers are also asymmetric: begin carries `id` and `owner`, end must
be bare `<!-- steward:end -->`, and an `id` on the end marker fails silently.

## Acceptance criteria

- [x] The configuration reference documents both markers with a working example.
- [x] The asymmetry and the silent failure on an `id`-bearing end marker are called out.

## Answer

The configuration reference has a "Managed region markers" section with a working example, the begin/end asymmetry, and the silent failure on an `id`-bearing end marker (verified: STWD-005 accepts it, `ManagedRegionRewriter` requires the bare `<!-- steward:end -->`). The `managed_section` field row now says it names the marker `id`.
