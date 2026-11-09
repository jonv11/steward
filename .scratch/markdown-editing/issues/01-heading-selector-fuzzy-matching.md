# 01 — Conservative fuzzy heading selector

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/markdown-editing/map.md`

## What to build

Add a conservative non-exact selector mode for heading selectors.

## Acceptance criteria

- [ ] An opt-in selector mode matches headings case- and punctuation-insensitively.
- [ ] An ambiguous match fails with the candidate headings listed, rather than picking one.
- [ ] Exact matching stays the default, and tests cover both modes.
