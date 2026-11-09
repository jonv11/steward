# 03 — `directory-index` degrades without descriptions

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/policy-expressiveness/map.md`

## What to build

`directory-index` hard-blocks unless every source file has non-empty
`frontmatter.description`, which rules it out for any repository being adopted into.
`type: index` degrades to an H1-derived link list and worked; consider the same fallback.

## Acceptance criteria

- [ ] `directory-index` falls back to an H1-derived entry for a source file without a description.
- [ ] Existing indexes with descriptions are byte-identical after the change.
- [ ] Tests cover a mixed set of sources.
