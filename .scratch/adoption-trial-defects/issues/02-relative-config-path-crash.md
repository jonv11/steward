# 02 — A relative `--config` path crashes discovery

Status: ready-for-agent
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

- [ ] `check` and `config doctor` resolve a bare relative `--config` directory against the current directory and run normally.
- [ ] A regression test reproduces the original crash and passes after the fix.
- [ ] The CHANGELOG `[Unreleased]` section records the fix.
