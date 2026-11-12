# 05 — Config discovery climbs out of a linked worktree

Status: done
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

`ConfigLoader.FindConfigDirectory` stops its upward walk only at a `.git` *directory*. In a
linked worktree or a submodule `.git` is a *file*, so a worktree without its own `.steward/`
keeps climbing and adopts the enclosing repository's `.steward/` and root. Coding agents work
in worktrees nested inside the main checkout (for example `.claude/worktrees/<name>/`), so
`steward check` there validated the wrong tree. Found on 2026-11-12 while comparing repository
discovery with the sibling `tracker` CLI, which already treats a `.git` file as the root.

## Acceptance criteria

- [x] A `.git` file stops the walk exactly as a `.git` directory does.
- [x] A regression test reproduces a worktree nested inside a configured repository.
- [x] The CHANGELOG `[Unreleased]` section records the fix.

## Answer

`FindConfigDirectory` now treats `.git` as the root marker whether it is a directory or a file.
Covered by `ConfigLoaderTests.FindConfigDirectory_GitFileMarksRoot_DoesNotEscapeWorktree`.
