# 03 — Document glob character classes in patterns

Status: done
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/configuration-docs-gaps/map.md`

## What to build

Character classes such as `docs/adr/[0-9]*.md` work and are the practical way to exclude an
index `README.md` from a family glob, but are not mentioned.

## Acceptance criteria

- [x] The configuration reference documents glob syntax, including character classes, with the index-exclusion example.

## Answer

The configuration reference has a "Glob pattern syntax" section listing `*`, `**`, `?`, `[...]` and `[!...]` (each verified against `explain path`), the fields that use it, and the index-exclusion example.
