# 04 — Per-directory `directory_expectations`

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/policy-expressiveness/map.md`

## What to build

`min_count` is repository-wide, so "every effort directory contains a `spec.md`" is
inexpressible for multi-instance layouts — only "at least one exists anywhere." This
repository's tracker wants exactly that rule ("every effort has a `spec.md` or a `map.md`,
never both").

## Acceptance criteria

- [ ] An expectation can be scoped per matched directory.
- [ ] The rule can express "exactly one of these files" for each matched directory.
- [ ] The configuration reference documents it, and tests cover it.
