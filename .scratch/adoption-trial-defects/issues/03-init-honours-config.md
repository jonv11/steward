# 03 — `init` ignores `--config`

Status: done
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

`steward init -c <dir>` always writes `.steward/` in the current directory, so a draft
configuration cannot be scaffolded beside an existing one — the workflow the
`setup-steward` skill needs.

## Acceptance criteria

- [x] `steward init -c <dir>` writes its scaffold into `<dir>`.
- [x] `init` without `--config` behaves exactly as before.
- [x] A CLI test covers both paths, and the CHANGELOG `[Unreleased]` section records the fix.

## Answer

`init` chooses its scaffold directory through the same `GlobalOptionsSetup.GetConfigDirectoryOverride` helper, falling back to `.steward/`. Covered by `ConfigPathResolutionTests`.
