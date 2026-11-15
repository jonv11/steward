# 03 — Accept narrower family vocabularies in doctor

Status: done
Type: task
Category: bug
Blocked by: None — can start immediately
Spec: `.scratch/bounded-documentation/spec.md`

## What to build

MPES's ADR family permits only historical lifecycle status, within the general living/historical
vocabulary. Doctor reports the valid restriction as a conflict. Accept family subsets while
retaining warnings for family values outside the scoped general vocabulary.

## Acceptance criteria

- [x] A real doctor test rejects the false warning before the fix.
- [x] Narrower and equal vocabularies pass; incompatible or expanding vocabularies still warn.
- [x] Tests, Markdown lint and Steward check pass.

## Answer

Observed the two narrower-vocabulary cases fail before the fix. Doctor now uses case-insensitive subset compatibility; equal, narrower, incompatible and expanding cases are covered. Full Release tests: 645 core + 329 CLI passed, no skips. Markdown lint and Steward check passed.
