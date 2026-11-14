# 10 — A generated `directory-index` fails the repository's own front-matter requirement

Status: needs-triage
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

With `frontmatter_requirements` on `docs/**/*.md` and a `directory-index` maintenance artifact writing `docs/INDEX.md`, the file steward generates has no front matter, so `steward check` reports STWD-003 against steward's own output. The only way out is a hand-written `path_overrides` entry disabling STWD-003 for the generated path. Found in `mpes` on 2026-11-14.

## Acceptance criteria

- [ ] Files written by a maintenance artifact are exempt from front-matter requirements by default (or the artifact can emit configured front matter).
- [ ] `config doctor` does not need a manual override to report a clean setup for this layout.
- [ ] Tests cover a `directory-index` inside a directory with a front-matter requirement.
