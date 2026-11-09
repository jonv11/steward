# 06 — Give `path-policy` `kind` a meaning or reject it

Status: needs-triage
Type: grilling
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/config-transparency/map.md`

## The question

`PathRule.Kind` is parsed and never read, and `config validate` does not flag it. Should the
field gain a meaning, or should `config validate` reject it?

## Why a human decides this

Rejecting it is a breaking change for any policy that sets it; giving it meaning is product
design. Both are maintainer calls under the pre-1.0 versioning policy (ADR-013).

## What is needed to close this

A decision — keep and define, deprecate with a warning, or reject — recorded under `## Answer`,
with a follow-up task ticket for the chosen change.
