# 09 — `config doctor` reports a family that narrows global allowed values as a conflict

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

`validation.frontmatter_requirements` sets the shared vocabulary for `docs/**/*.md` (`kind` in 10 values, `status` in `living | historical`), and a family narrows it for its own files (`engineering-issue`: `kind: [issue]`; `review`: `status: [historical]`). That is the natural way to say "every document has a kind, and these documents have this kind". `steward config doctor` reports each narrowing as `conflicting-allowed-values` (6 findings in `mpes`), and the doctor output is what an agent loop reads to decide the config is healthy. The maintainer removed the family restrictions to quiet it and lost the stricter check.

## Acceptance criteria

- [ ] When a family's allowed values are a subset of the overlapping requirement's, doctor reports nothing (or an info-level note), and STWD-003 applies the family's narrower set.
- [ ] Values outside the requirement's set are still reported as a conflict.
- [ ] The configuration reference documents narrowing as supported.
