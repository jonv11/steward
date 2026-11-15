# 02 — Compose maintenance edits sharing one document

Status: claimed
Type: task
Category: bug
Blocked by: None — can start immediately
Spec: `.scratch/bounded-documentation/spec.md`

## What to build

MPES's bounded indexes require several generated regions per page. Maintenance plans each from
the original file and later writes overwrite earlier regions. Metadata auto-edits can overwrite
them too. Compose updates in memory, retaining deterministic preview and an idempotent apply.

## Acceptance criteria

- [ ] A minimal real-CLI regression fails before the fix for two regions sharing one file.
- [ ] One apply preserves both regions and metadata; a second apply makes no changes.
- [ ] Full tests, lint and Steward checks pass; update the pinned MPES source afterward.

## Answer

Pending.
