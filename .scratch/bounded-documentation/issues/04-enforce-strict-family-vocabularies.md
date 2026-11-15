# 04 — Enforce explicit strict family vocabularies

Status: claimed
Type: task
Category: bug
Blocked by: None — can start immediately
Spec: `.scratch/bounded-documentation/spec.md`

## What to build

MPES's integration probe found that STWD-003 unions family values with scoped values, while
explain displays the family alone. Doctor subset compatibility does not itself narrow validation.
Add explicit family `allowed_values_mode: replace`, retain default union for scoped exceptions,
and make explain report the same effective values as validation.

## Acceptance criteria

- [ ] Real CLI tests show union accepts the scoped value while replace rejects it.
- [ ] Explain agrees with validation in both modes; invalid modes are configuration errors.
- [ ] Full tests, lint and Steward checks pass; MPES uses replace for strict lifecycle families.

## Answer

Pending.
