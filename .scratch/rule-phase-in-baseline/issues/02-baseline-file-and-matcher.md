# 02 — Baseline file model and matcher in core

Status: done
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

- [x] Round-trip serialization is byte-stable and entries are sorted by path, rule, selector.
- [x] The matcher partitions diagnostics into new, baselined and drift, with multiset semantics for repeated identities.
- [x] A malformed baseline file raises a `StewardConfigException` naming the file.
- [x] The `baseline:` policy block is accepted by the loader and rejected with unknown fields, like other blocks.

## Answer

Landed `Steward.Core.Baseline`: `BaselineDocument` (sorted, LF-terminated, byte-stable
serialization; `Parse` rejects malformed files and unknown fields with a
`StewardConfigException` naming the file), `BaselineMatcher` (multiset partition into new,
baselined and drift), and `BaselineSettings` (path resolution and the activation rule from the
spec). `RepositoryPolicy.Baseline` holds the `baseline:` block and survives profile merging.
Covered by `BaselineTests`.
