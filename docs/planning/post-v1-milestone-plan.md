# Post-v1.0.0 Milestone Plan — RFC-007 Governance Enhancements

- **Source:** [RFC-007 Governance Enhancements Backlog](rfc-007-governance-enhancements-backlog.md)
- **RFC:** [RFC-007 Maintainer Governance and Stewardship Enhancements](../decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements-draft.md) (Accepted)
- **Created:** 2026-04-15

---

## v1.1.0 — Policy Expressiveness and Validation Feedback

**Theme:** Fill day-to-day maintainer gaps with targeted extensions to the existing policy and validation engine.

| Item | Summary | Effort |
|------|---------|--------|
| G7-01 | Per-path rule suppression (`validation.path_overrides`) | Low |
| G7-02 | Scoped frontmatter requirements per path pattern | Medium |
| G7-03 | Naming convention enforcement in path-policy (`must_match` regex) | Medium |
| G7-04 | Post-fix and maintain diff output | Low |
| G7-05 | Rule scope transparency in `explain --verbosity verbose` | Low |

**Exit criteria:** All five items implemented, tested, and passing. `steward check` validates the new policy schema. Existing repos without new keys work unchanged.

---

## v1.2.0 — Governance Inspection and Explainability

**Theme:** Inspect, explain, and diagnose governance configuration and coverage.

| Item | Summary | Effort |
|------|---------|--------|
| G7-06 | Effective policy explanation (`steward explain path <path>`) | Medium |
| G7-07 | Configuration doctor (`steward config doctor`) | Medium |
| G7-08 | Index-completeness validation rule (STWD-010) | Medium |
| G7-09 | State-document freshness signaling (STWD-011) | Medium |

**Exit criteria:** All four items implemented. `explain path` shows complete effective governance for any file. `config doctor` detects at least three classes of ineffective configuration.

---

## v1.3.0 — Maintenance Evolution

**Theme:** Extend the maintenance engine with directory-index generation, dependency modeling, and richer artifact classification.

| Item | Summary | Effort |
|------|---------|--------|
| G7-10 | Directory-index generator for maintained sections | High |
| G7-11 | Maintenance dependency modeling (`depends_on`) | Medium |
| G7-12 | Three-level artifact classification (required/recommended/optional) | Medium |
| G7-13 | Role-linked behavioral defaults | Medium |

**Exit criteria:** `steward maintain --apply` generates directory indexes deterministically. Dependency ordering works. Three-level classification changes STWD-001 severity correctly.

---

## v1.4.0 — Discoverability and Impact Analysis

**Theme:** Proactive detection of discoverability gaps, change-impact signaling, and staged completeness.

| Item | Summary | Effort |
|------|---------|--------|
| G7-14 | Orphaned-but-valid document detection | Medium |
| G7-15 | Change-impact output in `steward check` | Medium |
| G7-16 | Governance coverage reporting (`steward status --coverage`) | Medium |
| G7-17 | Staged-scope completeness checks | Medium |

**Exit criteria:** Orphan detection flags unreachable docs. Change-impact signals downstream refresh needs. Coverage report identifies ungoverned areas.

---

## v1.5.0 — Workflow Operations and Onboarding

**Theme:** Safe move/rename workflows, reference graph queries, and bootstrap-by-analysis.

| Item | Summary | Effort |
|------|---------|--------|
| G7-18 | Reference graph queries (`steward refs <path>`) | Medium |
| G7-19 | Safe move/rename (`steward refactor move`) | High |
| G7-20 | Bootstrap-by-analysis (`steward init --analyze`) | High |

**Exit criteria:** `refs` shows inbound/outbound links. `refactor move` updates all references with preview. `init --analyze` produces reviewable suggestions for mature repos.
