# 02 — Document that `title_pattern` matches normalized text

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/configuration-docs-gaps/map.md`

## What to build

Patterns are applied to the normalized H1 text, not the raw Markdown, so `` # `jvcode issues` ``
matches as `jvcode issues` and a pattern anchored on the backtick never matches.

## Acceptance criteria

- [ ] The `title_pattern` entry in the configuration reference states that inline formatting is stripped before matching, with an example.
