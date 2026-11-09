# 08 — `check --fail-on-baseline-growth` blocks new grandfathered debt

Status: needs-triage
Type: task
Category: enhancement
Blocked by: 04
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Add the opt-in `--fail-on-baseline-growth` flag: `check` fails when the baseline file has
more entries than its committed version at `HEAD` (or the `--since` base when given), so the
baseline cannot quietly absorb new debt. Without git history the flag reports that it could
not compare rather than passing silently.

## Acceptance criteria

- [ ] Adding an entry to a committed baseline fails `check` with the flag and passes without it.
- [ ] Shrinking the baseline never fails.
- [ ] Missing git history yields a clear diagnostic, covered by a test.
