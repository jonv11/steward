# Milestone Plan — Pre-1.0 Mainline

- **Document ID:** PLAN-0002
- **Version:** 0.11.0
- **Status:** Active
- **Last updated:** 2026-04-16

---

## Framing

Steward is still on a pre-stable SemVer line. The active roadmap continues on `0.x` milestones until an explicit release-authorization decision schedules `1.0.0`.

## Delivered Milestones

| Version | Theme | State |
|---------|-------|-------|
| `v0.1.0` | Foundations | Delivered |
| `v0.2.0` | Orientation and repository discovery | Delivered |
| `v0.3.0` | Configuration, profiles, and path policy | Delivered |
| `v0.4.0` | Validation engine and diagnostics | Delivered |
| `v0.5.0` | Markdown query and address foundations | Delivered |
| `v0.6.0` | Search | Delivered |
| `v0.7.0` | Structural Markdown editing and ownership | Delivered |
| `v0.8.0` | Deterministic maintenance | Delivered |
| `v0.9.0` | Workflow completeness | Delivered |
| `v0.10.0` | Pre-1.0 governance hardening and roadmap correction | Delivered |

## Planned Pre-1.0 Milestones

| Version | Theme | Primary outcome |
|---------|-------|-----------------|
| `v0.11.0` | Stable-release hardening and trust fixes | B5 profile scope (ADR-014), B6 scoped validation fix, B7 status JSON coverage, exit-code regression tests, stable-surface contract tests, dependency stabilization, publication checklist |
| `v0.12.0` | Workflow polish and depth improvements | See detailed v0.12.0 scope below |
| `v0.13.0` | Artifact type schema RFC and base implementation | Begin the accepted ADR-012 direction on the pre-1.0 line |
| `v0.14.0` | Type-aware validation expansion | Field constraints, section expectations, and controlled vocabulary enforcement |
| `v0.15.0` | Later pre-1.0 requirement families | Typed resource-address follow-on work, search/address alignment, split-extract evaluation |
| `v0.16.0` | Optional pre-stable extensions | Host-integration exploration and remaining later-scope items if still justified |

## First Stable Release

| Version | State | Condition |
|---------|-------|-----------|
| `v1.0.0` | Not scheduled | Requires explicit authorization per ADR-013 plus green evidence from the active readiness plan |

## Notes

- Former `v1.1.0` through `v1.6.0` planning has been logically retargeted to `v0.11.0` through `v0.16.0`.
- The exact boundary between later pre-1.0 milestones may continue to move as stable-release criteria are clarified, but the roadmap must stay on the `0.x` line until that decision is made.

---

## v0.12.0 Detailed Scope

The following items are planned for v0.12.0. Each addresses a known workflow gap or depth deficiency identified in the 2026-04-16 CLI review cycle.

| # | Item | Source | Scope description |
|---|------|--------|-------------------|
| 1 | Standardize preview/apply flag conventions | F2 | Unify the three mutation patterns (`--fix`/`--dry-run` on check, `--apply` default-preview on maintain, `--preview`+`--apply` on md edit) into a consistent convention across all mutation commands. |
| 2 | Fix `md query --pattern` batch mode | F4 | Resolve argument parsing ambiguity between positional `file` arg and `--pattern` option so multi-file structural queries work. |
| 3 | Fix init scaffolding immediate-failure experience | F5 | `init --profile software` + immediate `check` should not fail for required artifacts that were just scaffolded as part of the profile contract. Either scaffold placeholder files or adjust first-run check behavior. |
| 4 | Deepen `explain path` provenance and filtering | F7, G7-05/06 | `explain path` should filter rules by actual governance applicability for the given file and show precedence/provenance detail, not list all 13 rules for any path. |
| 5 | Deepen `config suggest` for mature repos | F6, G7-20 | `config suggest` currently detects only 3 artifacts on the 19-artifact steward repo. Improve heuristics for decisions, planning, indexes, roles, and other common structures. |
| 6 | Deepen `config doctor` beyond baseline checks | F6, G7-07 | Add deeper checks: shadowed rules, dead suppressions, no-effect declarations, unreachable path-policy patterns. |
| 7 | Add `fm-validate` to Markdown edit subsystem | EF-005, REQ-MD-004 | RFC-004 specifies `fm-validate` but `md edit` does not implement it. Add frontmatter validation as an edit operation. |
| 8 | Exclude test fixtures from governance coverage | F16 | `status --coverage` counts test-fixture Markdown as ungoverned, diluting the governance signal. Add a configurable exclusion mechanism. |
| 9 | Enrich deferred profiles (mixed, knowledge) | ADR-014 | If archetype-specific governance contracts can be designed (e.g., knowledge-specific structure rules, mixed-repo boundary detection), re-enable these profiles in `init`. |
