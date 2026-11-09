# 01 — Shared or inheritable policy

Status: needs-triage
Type: research
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/policy-expressiveness/map.md`

## The question

No `extends`/import exists, so multi-repo governance means copying `.steward/` and
hand-syncing drift. Can shared policy be supported without breaking the offline-first
constraint?

## Acceptance criteria

- [ ] A recommendation evaluates at least a local-path `extends` and a vendored-copy approach against the offline-first constraint.
- [ ] The recommendation states merge semantics for families, overrides and path rules.
- [ ] An RFC is drafted if the recommendation is to build it.
