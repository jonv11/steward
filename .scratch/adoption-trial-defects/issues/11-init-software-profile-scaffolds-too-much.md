# 11 — `init --profile software` writes files and requirements a repository did not ask for

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

Run on a Unity project with no LICENSE, CHANGELOG or CONTRIBUTING (`mpes`, 2026-11-14), `steward init --profile software` creates `CHANGELOG.md` and `CONTRIBUTING.md` stubs, marks `LICENSE` required (an immediate STWD-001 error on a private repo), and has no `--dry-run` to show this first. The `setup-steward` skill already works around it by starting from `minimal`. The default profile should not create content or impose a licence.

## Acceptance criteria

- [ ] `steward init --dry-run` prints the files and policy it would write, writing nothing.
- [ ] The `software` profile does not create stub documents unless asked (for example `--scaffold`), and does not require `LICENSE` by default.
- [ ] The maintainer guide states what each profile writes.
