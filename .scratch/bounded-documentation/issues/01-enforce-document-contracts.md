# 01 — Enforce bounded documentation contracts

Status: done
Type: task
Category: enhancement
Blocked by: None — can start immediately
Spec: `.scratch/bounded-documentation/spec.md`

## What to build

Implement the opt-in contract described in the spec for MPES's approved documentation migration.

## Acceptance criteria

- [x] Budgets, identities/relations and source collection rules registered and documented.
- [x] Boundary and CLI tests reject violations and preserve valid Markdown/source text.
- [x] Build, tests, Markdown lint and Steward checks pass.

## Answer

Implemented in `5839311`: three registered opt-in rules, glob index coverage, configuration validation/doctor, explain guidance and documentation. Release build tests: 645 core + 322 CLI passed, no skips. Markdown lint and full Steward check passed with zero diagnostics. CLI tests exercise valid and planted-invalid corpora, including scoped global obligations. No version bump or release.
