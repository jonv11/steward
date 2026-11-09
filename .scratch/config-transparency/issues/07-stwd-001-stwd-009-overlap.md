# 07 — Confirm the STWD-001 and STWD-009 overlap

Status: needs-triage
Type: research
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/config-transparency/map.md`

## The question

Do STWD-001 and STWD-009 emit verbatim duplicate diagnostics for declared artifacts?
STWD-009 appears to add value only for `importance: optional`, which STWD-001 skips. Confirm
before scoping a fix.

## Acceptance criteria

- [ ] A reproduction shows whether duplicate diagnostics occur, and for which importance levels.
- [ ] If they do, a task ticket proposes the narrowest change that removes the duplicate without losing the optional-artifact signal.
