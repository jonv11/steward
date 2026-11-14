# 06 — `maintain --apply` re-dates and re-serialises front matter on mechanical edits

Status: needs-triage
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

Found on 2026-11-14 adopting a front-matter contract in `mpes` (31 documents, `governance.frontmatter.auto_fields.updated: true`).

1. **Every locally changed file is re-dated to today**, including changes that are not content: adding the front matter block itself set `updated` to the adoption day on all 31 files and erased the real last-change dates. The maintainer had to restore them from `git log -1 --format=%cs` by hand, after which STWD-007 reported 18 "stale" errors until the commit, because the restored date differs from today on a locally changed file. There is no way to say "this change is mechanical" (a flag such as `maintain --apply --skip auto-fields`, or a per-run `--auto-fields-date <date|git>`), nor an option to take the date from git history.
2. **The whole front-matter block is re-serialised** when one auto field is written. Double-quoted scalars came back single-quoted, and quotes the YAML did not need were dropped, so 19 of 31 files in one repository changed quoting style without any request. Only the auto field's line should change.

## Acceptance criteria

- [ ] Writing an auto field rewrites that field's line only; other lines, their quoting and their order are byte-identical.
- [ ] A documented way to apply maintenance without bumping auto date fields (or to take them from git history), so a mechanical bulk edit keeps real dates.
- [ ] STWD-007 does not report a date that was deliberately kept on a mechanical change (or the docs say how to express that).
- [ ] Tests cover quoted, unquoted and multi-field front matter.
