# 03 — Accept narrower family vocabularies in doctor

Status: claimed
Type: task
Category: bug
Blocked by: None — can start immediately
Spec: `.scratch/bounded-documentation/spec.md`

## What to build

MPES's ADR family permits only historical lifecycle status, within the general living/historical
vocabulary. Doctor reports the valid restriction as a conflict. Accept family subsets while
retaining warnings for family values outside the scoped general vocabulary.

## Acceptance criteria

- [ ] A real doctor test rejects the false warning before the fix.
- [ ] Narrower and equal vocabularies pass; incompatible or expanding vocabularies still warn.
- [ ] Tests, Markdown lint and Steward check pass.

## Answer

Pending.
