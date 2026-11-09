# 06 — `steward baseline status` reports baseline debt

Status: done
Type: task
Category: enhancement
Blocked by: 02
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Add `steward baseline status`, which reports entry counts by rule and the baseline's
age (`generated_at`), plus how many entries currently match and how many have drifted, so
baseline debt is visible without opening the JSON file. Standard JSON envelope.

## Acceptance criteria

- [x] Text and JSON output show counts by rule, age, matched and drifted.
- [x] With no baseline it reports that none is active and exits `0`.
- [x] CLI tests cover both cases.

## Answer

Landed `steward baseline status` in `BaselineCommand`. It runs the shared
`CheckCommand.Validate` over the full repository, matches with `BaselineMatcher`, and reports
`generatedAt`, `ageDays`, `entries`, `matched`, `drifted` and a per-rule breakdown (text and
JSON). With no file, or `baseline.enabled: false`, it reports the baseline as inactive with a
`reason` and exits `0`. Covered by `BaselineCommandTests`.
