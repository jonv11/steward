# Post-v1.0.0 Milestone Plan — RFC-007 Governance Enhancements

- **Source:** [RFC-007 Governance Enhancements Backlog](rfc-007-governance-enhancements-backlog.md)
- **RFC:** [RFC-007 Maintainer Governance and Stewardship Enhancements](../decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements-draft.md) (Accepted)
- **Created:** 2026-04-15
- **Last updated:** 2026-04-15

---

## v1.1.0 — Policy Expressiveness and Validation Feedback

**Theme:** Fill day-to-day maintainer gaps with targeted extensions to the existing policy and validation engine.

| Item | Summary | Effort | Status |
|------|---------|--------|--------|
| G7-01 | Per-path rule suppression (`validation.path_overrides`) | Low | Planned |
| G7-02 | Scoped frontmatter requirements per path pattern | Medium | Planned |
| G7-03 | Naming convention enforcement in path-policy (`must_match` regex) | Medium | ✅ Implemented (STWD-010) |
| G7-04 | Post-fix and maintain diff output | Low | Planned |
| G7-05 | Rule scope transparency in `explain --verbosity verbose` | Low | Planned |

**Exit criteria:** All five items implemented, tested, and passing. `steward check` validates the new policy schema. Existing repos without new keys work unchanged.

**Current status:** 1 of 5 items implemented (G7-03). Remaining 4 items are planned.

---

## v1.2.0 — Governance Inspection and Explainability

**Theme:** Inspect, explain, and diagnose governance configuration and coverage.

| Item | Summary | Effort | Status |
|------|---------|--------|--------|
| G7-06 | Effective policy explanation (`steward explain path <path>`) | Medium | Planned |
| G7-07 | Configuration doctor (`steward config doctor`) | Medium | Planned |
| G7-08 | Index-completeness validation rule (STWD-011) | Medium | ✅ Implemented |
| G7-09 | State-document freshness signaling (STWD-012) | Medium | ✅ Implemented |

**Exit criteria:** All four items implemented. `explain path` shows complete effective governance for any file. `config doctor` detects at least three classes of ineffective configuration.

**Current status:** 2 of 4 items implemented (G7-08 as STWD-011, G7-09 as STWD-012). G7-06 and G7-07 require follow-up ADRs.

---

## v1.3.0 — Maintenance Evolution

**Theme:** Extend the maintenance engine with directory-index generation, dependency modeling, and richer artifact classification.

| Item | Summary | Effort | Status |
|------|---------|--------|--------|
| G7-10 | Directory-index generator for maintained sections | High | ✅ Implemented |
| G7-11 | Maintenance dependency modeling (`depends_on`) | Medium | ✅ Implemented |
| G7-12 | Three-level artifact classification (required/recommended/optional) | Medium | Planned |
| G7-13 | Role-linked behavioral defaults | Medium | ✅ Implemented |

**Exit criteria:** `steward maintain --apply` generates directory indexes deterministically. Dependency ordering works. Three-level classification changes STWD-001 severity correctly.

**Current status:** 3 of 4 items implemented (G7-10, G7-11, G7-13). G7-12 (three-level classification) requires follow-up ADR.

---

## v1.4.0 — Discoverability and Impact Analysis

**Theme:** Proactive detection of discoverability gaps, change-impact signaling, and staged completeness.

| Item | Summary | Effort | Status |
|------|---------|--------|--------|
| G7-14 | Orphaned-but-valid document detection | Medium | ✅ Implemented (STWD-013) |
| G7-15 | Change-impact output in `steward check` | Medium | ✅ Implemented |
| G7-16 | Governance coverage reporting (`steward status --coverage`) | Medium | ✅ Implemented |
| G7-17 | Staged-scope completeness checks | Medium | ✅ Implemented |

**Exit criteria:** Orphan detection flags unreachable docs. Change-impact signals downstream refresh needs. Coverage report identifies ungoverned areas.

**Current status:** All 4 items implemented (G7-14 as STWD-013, G7-15, G7-16, G7-17).

---

## v1.5.0 — Workflow Operations and Onboarding

**Theme:** Safe move/rename workflows, reference graph queries, and bootstrap-by-analysis.

| Item | Summary | Effort | Status |
|------|---------|--------|--------|
| G7-18 | Reference graph queries (`steward refs <path>`) | Medium | ✅ Implemented |
| G7-19 | Safe move/rename (`steward refactor move`) | High | ✅ Implemented |
| G7-20 | Bootstrap-by-analysis (`steward init --analyze`) | High | ✅ Implemented |

**Exit criteria:** `refs` shows inbound/outbound links. `refactor move` updates all references with preview. `init --analyze` produces reviewable suggestions for mature repos.

**Current status:** All 3 items implemented (G7-18, G7-19, G7-20).

---

## v1.6.0 — Artifact Type Schemas and Domain Configuration

**Theme:** Deliver the artifact type schema system that enables domain-specific governance through generic configuration.

**Source:** [Use-Case Consolidation Proposal](../audits/usecase-consolidation-proposal.md), [ADR-012](../decisions/adrs/ADR-012-artifact-type-schema-direction.md)

| Item | Summary | Effort |
|------|---------|--------|
| T6-01 | Artifact type definition schema in policy.yaml (`artifact_types:`) | High |
| T6-02 | Type-to-file matching (by path pattern and/or frontmatter `type` field) | Medium |
| T6-03 | Per-type frontmatter field requirements with value constraints (enum, regex, data type) | High |
| T6-04 | Per-type required/optional section declarations and section-presence validation rule | Medium |
| T6-05 | Controlled vocabulary enforcement via `allowed_values` field constraints | Medium |
| T6-06 | Story/worldbuilding built-in profile leveraging artifact type schemas | Low |
| T6-07 | Design RFC for artifact type schema system (prerequisite for T6-01) | Medium |

**Prerequisites:** v1.1.0 (G7-02 scoped frontmatter, G7-03 naming enforcement provide foundations). G7-12 (three-level classification) and G7-13 (role-linked defaults) from v1.3.0 are complementary.

**Exit criteria:** A repository can declare artifact types in policy.yaml with per-type frontmatter requirements, field value constraints, required sections, and naming patterns. `steward check` validates against declared type schemas. Existing repos without `artifact_types:` work unchanged. Story/worldbuilding profile provides a curated starting configuration.

**Requires follow-up RFC:** Yes — T6-07 must produce the design RFC before implementation of T6-01 through T6-05.

---

## Future Direction (v2.0+)

The following capabilities are identified by the use-case analysis as valuable but deferred beyond the v1.x roadmap. They require the artifact type schema system (v1.6.0) as a foundation.

| Item | Summary | Depends on |
|------|---------|------------|
| Scaffold/template command | Guided artifact creation from per-type templates | v1.6.0 (type definitions, naming patterns) |
| Status lifecycle and transition rules | Configurable allowed status transitions per type | v1.6.0 (per-type status enum) |
| Plan/next computed recommendations | Computed next-action suggestions based on policy | v1.6.0 + lifecycle rules |
| Relationship type declarations | Declare and validate allowed references between types | v1.6.0 (type awareness) |
| Backlink generation | Deterministic backlink sections in managed regions | v1.5.0 (reference graph) |
