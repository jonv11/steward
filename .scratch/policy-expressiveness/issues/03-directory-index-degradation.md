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

## Comments

- 2026-11-14 (agent): Second data point from `mpes`. The index's real value there is the description and `status` columns, which only `directory-index` provides; `type: index` writes titles only, so it cannot replace it. The hard block also hit two specification files that were meant to stay byte-identical, and `source` has no exclude, so the choice was all-or-nothing: they were given front matter. An exclude list on `source` (or the H1 fallback this ticket proposes) would have avoided that. Related: generated index fails the repository's own front-matter requirement, `adoption-trial-defects/10`.
