# 04 — Managed-region markers inside code fences are treated as live

Status: needs-triage
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

A `<!-- steward:begin ... -->` / `<!-- steward:end -->` pair shown as an example inside a
fenced code block is parsed as a real managed region. Found on 2026-11-09 while documenting
the marker syntax: an empty example region in `docs/guide/configuration-reference.md`
raised STWD-006 ("Managed region ... is empty"). STWD-005, STWD-006, the Markdown parser's
region detection and `ManagedRegionRewriter` all scan raw lines. Any documentation that shows
the marker syntax can trip the rules, or in the rewriter's case could have its example
overwritten if the `id` matches a configured `managed_section`.

## Acceptance criteria

- [ ] Markers inside fenced code blocks (backtick and tilde fences) are ignored by STWD-005, STWD-006, region parsing and the rewriter.
- [ ] Tests cover an example region inside a fence alongside a real region in the same file.
- [ ] The CHANGELOG `[Unreleased]` section records the fix.
