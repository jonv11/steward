# 02 — Compose maintenance edits sharing one document

Status: done
Type: task
Category: bug
Blocked by: None — can start immediately
Spec: `.scratch/bounded-documentation/spec.md`

## What to build

MPES's bounded indexes require several generated regions per page. Maintenance plans each from
the original file and later writes overwrite earlier regions. Metadata auto-edits can overwrite
them too. Compose updates in memory, retaining deterministic preview and an idempotent apply.

## Acceptance criteria

- [x] A minimal real-CLI regression fails before the fix for two regions sharing one file.
- [x] One apply preserves both regions and metadata; a second apply makes no changes.
- [x] Full tests, lint and Steward checks pass; update the pinned MPES source afterward.

## Answer

The real CLI regression failed before the fix in both metadata orders, then passed. Maintenance evaluates against a read-through in-memory overlay and applies actions in plan order. Full Release suite: 645 core and 324 CLI tests passed; lint and Steward checks passed. MPES will vendor the resulting committed source.
