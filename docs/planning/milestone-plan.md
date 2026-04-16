---
type: planning
document_id: PLAN-0002
version: 0.11.0
status: Active
last_updated: 2026-04-16
---

# Milestone Plan — Pre-1.0 Mainline

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
| `v0.11.0` | Stable-release hardening and trust fixes | Delivered |
| `v0.12.0` | CLI fidelity, governance deepening, and Markdown subsystem completion | Delivered |

## Planned Pre-1.0 Milestones

| Version | Theme | Primary outcome |
|---------|-------|-----------------|
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

## v0.12.0 Delivered Scope

All items planned for v0.12.0 have been implemented and tested. Each addresses a known workflow gap or depth deficiency identified in the 2026-04-16 CLI review cycle.

| # | Item | Source | Status |
|---|------|--------|--------|
| 1 | Standardize preview/apply flag conventions | F2 | Done — `check --fix` previews, `--fix --apply` commits, `--dry-run` hidden deprecated |
| 2 | Fix `md query --pattern` batch mode | F4 | Done — argument parsing disambiguation |
| 3 | Fix init scaffolding immediate-failure experience | F5 | Done — placeholder files scaffolded for required artifacts |
| 4 | Deepen `explain path` provenance and filtering | F7, G7-05/06 | Done — per-rule applicability filtering |
| 5 | Deepen `config suggest` for mature repos | F6, G7-20 | Done — detects decisions, planning, state docs, subdirectory indexes |
| 6 | Deepen `config doctor` beyond baseline checks | F6, G7-07 | Done — dead suppressions, unreachable overrides, unreachable frontmatter patterns |
| 7 | Add `fm-validate` to Markdown edit subsystem | EF-005, REQ-MD-004 | Done — `md edit fm-validate <file>` validates against policy requirements |
| 8 | Exclude test fixtures from governance coverage | F16 | Done — `coverage.exclude` config section with glob patterns |
| 9 | Enrich deferred profiles (mixed, knowledge) | ADR-014 | Deferred — ADR-014 explicitly defers until contracts are enriched |
