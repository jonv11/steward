# 02 — Baseline file model and matcher in core

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: 01
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Add the `.steward/baseline.json` model (`generated_at`, `steward_version`, `entries[]` of
`rule`, `path`, `selector`), deterministic serialization (sorted entries, stable formatting,
so regenerating an unchanged repository produces no diff), loading with clear errors, and a
matcher that partitions a diagnostic list into *new*, *baselined* and *drift* (entries that
no longer match anything) using multiset semantics. Add the `baseline:` policy block
(`enabled`, `path`) to the policy model and schema.

## Acceptance criteria

- [ ] Round-trip serialization is byte-stable and entries are sorted by path, rule, selector.
- [ ] The matcher partitions diagnostics into new, baselined and drift, with multiset semantics for repeated identities.
- [ ] A malformed baseline file raises a `StewardConfigException` naming the file.
- [ ] The `baseline:` policy block is accepted by the loader and rejected with unknown fields, like other blocks.
