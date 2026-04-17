---
type: planning
status: Active
source: RFC-007 Maintainer Governance and Stewardship Enhancements
last_updated: 2026-04-18
---

# RFC-007 Governance Enhancements — Status Ledger

---

## Framing

This artifact is no longer a “post-v1” backlog. It is now a ledger showing which accepted RFC-007 items are already present in the codebase and which follow-on work, if any, still belongs on later pre-1.0 milestones.

## Item Status

| Item | Summary | Current status | Evidence |
|------|---------|----------------|----------|
| G7-01 | Per-path rule suppression (`validation.path_overrides`) | Implemented | Policy schema and config validation support present in `RepositoryPolicy` and `ConfigLoader` |
| G7-02 | Scoped frontmatter requirements per path pattern | Implemented | `validation.frontmatter_requirements` exists in schema and validation flow |
| G7-03 | Naming convention enforcement in path-policy | Implemented | `NamingConventionRule` / `STWD-010` |
| G7-04 | Post-fix and maintain diff output | Implemented | `MaintainCommand` supports `--diff`; fix/apply flows report concrete changes |
| G7-05 | Rule scope transparency in explain | Implemented | `explain path` now filters rules by actual governance applicability (type, artifact status, family match); STWD-014/015/016 gated on matched family; explicit artifacts classified by family too |
| G7-06 | Effective policy explanation for a path | Implemented | `steward explain path <path>` |
| G7-07 | Configuration doctor for ineffective governance | Implemented (baseline) | `steward config doctor` covers dead start-here, missing artifacts, unmatched rules/sources, overlapping globals; deeper checks for shadowed rules, dead suppressions, and no-effect declarations remain for later depth work |
| G7-08 | Index-completeness validation | Implemented | `IndexCompletenessRule` / `STWD-011` |
| G7-09 | State-document freshness signaling | Implemented | `FreshnessRule` / `STWD-012` |
| G7-10 | Directory-index generator | Implemented | `DirectoryIndexMaintainer` |
| G7-11 | Maintenance dependency modeling | Implemented | `depends_on` handling in `MaintenanceEngine` |
| G7-12 | Three-level artifact classification | Implemented | `importance`, role defaults, and status surfacing |
| G7-13 | Role-linked behavioral defaults | Implemented | `RoleDefaults` / `WellKnownRoles` |
| G7-14 | Orphaned-but-valid document detection | Implemented | `OrphanedDocumentRule` / `STWD-013` |
| G7-15 | Change-impact output in `check` | Implemented | `CheckCommand.ComputeImpactSignals` |
| G7-16 | Governance coverage reporting | Implemented | `steward status --coverage` |
| G7-17 | Staged-scope completeness checks | Implemented | `CheckCommand.ComputeStagedCompleteness` |
| G7-18 | Reference graph queries | Implemented | `steward refs <path>` |
| G7-19 | Safe move/rename workflow | Implemented | `steward refactor move` |
| G7-20 | Bootstrap-by-analysis | Implemented (baseline) | `BootstrapAnalyzer` and `config suggest` provide heuristic suggestions including decisions, planning, state docs, and subdirectory indexes; precision on mature repos is still weak (fixture/sample files proposed as artifacts); confidence scoring and path-override exclusion planned for v0.16.0 |

## Remaining Follow-On Work

RFC-007 itself no longer carries the main unfinished stable-readiness load. The one item still marked "Implemented (baseline)" has a known depth gap:

- **G7-20 (config suggest):** Precision on mature repos is weak — `BootstrapAnalyzer` proposes test-fixture and sample files as real artifact candidates. Confidence scoring and `validation.path_overrides`-aware exclusion are planned for `v0.16.0`.

Remaining stable-readiness work is tracked in [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md) and [milestone-plan.md](milestone-plan.md).
