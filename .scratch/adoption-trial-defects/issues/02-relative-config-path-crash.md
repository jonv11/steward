# 02 — A relative `--config` path crashes discovery

Status: done
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

`steward check -c .steward-draft` throws an unhandled `ArgumentException` (exit `1`) because
the repository root is derived as the config directory's parent, which is empty for a bare
relative name. An absolute path works. `config validate` is unaffected; `check` and
`config doctor` crash.

## Acceptance criteria

- [x] `check` and `config doctor` resolve a bare relative `--config` directory against the current directory and run normally.
- [x] A regression test reproduces the original crash and passes after the fix.
- [x] The CHANGELOG `[Unreleased]` section records the fix.

## Answer

Every command now reads `--config` through `GlobalOptionsSetup.GetConfigDirectoryOverride`, which resolves it to an absolute path against the current directory before the repository root is derived. Covered by `ConfigPathResolutionTests`.
