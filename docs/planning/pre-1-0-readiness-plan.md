---
type: planning
source_baseline: v0.14.0
status: Active
last_updated: 2026-04-17
---

# Pre-1.0 Readiness Plan

---

## Purpose

This document is the authoritative list of remaining work that is still useful before the real first stable shipment. Intentional public `0.x` releases may happen earlier under ADR-013 and the documented release process, but they do not authorize `1.0.0`. Until explicit `1.0.0` criteria are approved, all future work remains on the pre-1.0 `0.x` line.

## Required Before First Stable Shipment

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| Cross-platform build/test/pack automation | A stable release needs reproducible validation on Windows, macOS, and Linux; local-only verification is not enough. | `.github/workflows/ci.yml` now runs build/test/pack on Windows, macOS, and Linux, but the first hosted green run is still pending; multi-platform support is a stated constraint in [ACD-0001](../requirements/assumptions-constraints.md). | Yes — workflow authored and local Windows build/test/pack works | Workflow, tests, release | Direct implementation task on the active `0.x` line until hosted validation is green |
| Hosted GitHub Release evidence | A credible public pre-1.0 release path needs at least one green hosted execution of the tag-driven release workflow before maintainers rely on it. | `.github/workflows/release.yml` now publishes changelog-backed GitHub Releases with attached `.nupkg`, self-contained bundles, and checksums, but no hosted run exists yet. | Yes — workflow authored and locally verifiable via scripts | Workflow, release | Direct implementation task on the active `0.x` line before the first intentional public `0.x` tag |
| ~~Dependency stabilization for stable release~~ | ~~Stable release posture should not depend on beta/preview packages where avoidable.~~ | `Directory.Packages.props` now has only `System.CommandLine` beta (documented, intentional). DI Abstractions upgraded to GA 10.0.6. | **Completed in v0.11.0** | Dependency | Milestone `v0.11.0` |
| ~~Distribution/publication hardening~~ | ~~Packaging now works, but stable-release publication steps and verification should be explicit and repeatable.~~ | [Release process](release-process.md), [release publication checklist](release-publication-checklist.md), `CHANGELOG.md`, `.github/workflows/release.yml`, and release helper scripts now define explicit pre-1.0 release preparation, GitHub Release asset publication, and optional NuGet publication. | **Completed in v0.14.0** | Docs, release | Milestone `v0.14.0` |
| ~~Fix scoped validation false positives (B6)~~ | ~~`check --scope changed\|staged` produces false diagnostics on clean trees.~~ | `AllDiscoveredFiles` added to `ValidationContext`; `STWD-001`, `STWD-007`, `STWD-009` updated; 4 regression tests + 1 contract test. | **Completed in v0.11.0** | Code, tests | [Pre-release blocker B6](pre-release-blockers.md) |
| ~~Include coverage in status JSON output (B7)~~ | ~~`status --coverage --output json` omits governance-coverage data.~~ | `RepositoryStatusWithCoverage` and `CoverageResponse` classes; 2 contract tests. | **Completed in v0.11.0** | Code, tests | [Pre-release blocker B7](pre-release-blockers.md) |

## Strongly Recommended Before First Stable Shipment

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| ~~Broaden stable contract tests~~ | ~~Stable surfaces should have stronger command/output regression coverage.~~ | 10 stable-surface contract tests added in `StableSurfaceContractTests.cs` covering check JSON/text, status JSON/text, orient JSON, version, and B6 scoped regression. | **Completed in v0.11.0** | Tests | Milestone `v0.11.0` |
| Decide the later pre-1.0 roadmap ordering explicitly | The repo now correctly stays on `0.x`, but later pre-stable scope still needs explicit sequencing as stable criteria are defined. | User guidance now places all future work on pre-`1.0.0` milestones; [milestone-plan.md](milestone-plan.md) now captures delivered lineage through `v0.14.0` plus the planned `v0.15.0+` work. | Yes | Planning, governance | Milestone planning update as criteria evolve |
| ~~Standardize preview/apply flag conventions~~ | ~~Three different preview/apply patterns across mutation commands (`--fix`/`--dry-run`, `--apply` default-preview, `--preview`+`--apply` required) erode CLI coherence.~~ | `check --fix` now previews by default, `--fix --apply` commits, and deprecated `--dry-run` remains hidden for compatibility. | **Completed in v0.12.0** | Code, docs | Delivered |
| ~~Fix `md query --pattern` batch mode~~ | ~~Argument parsing ambiguity between positional `file` arg and `--pattern` option prevents multi-file structural queries.~~ | Batch-mode parsing now works correctly for multi-file structural queries. | **Completed in v0.12.0** | Code, tests | Delivered |
| ~~Fix init scaffolding immediate-failure experience~~ | ~~Fresh `init --profile software` followed by `check` produces immediate errors for missing artifacts the scaffolded policy declares as required.~~ | `init --profile software` now scaffolds placeholder files for required artifacts so the first `check` does not fail immediately on STWD-001. | **Completed in v0.12.0** | Code | Delivered |
| ~~Add exit code regression tests~~ | ~~Only 2 exit code tests existed.~~ | 7 exit-code regression tests added in `ExitCodeTests.cs` covering all 4 exit codes. | **Completed in v0.11.0** | Tests | Milestone `v0.11.0` |

## Optional Polish Before First Stable Shipment

No currently tracked optional-polish items remain after `config show --effective` gained merged-policy surfacing in text mode on 2026-04-16. Add new optional items here only when they are clearly below the stable-release bar.

## Later Pre-1.0 Candidates

These remain valid future scope, but they are not current stable-release blockers.

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| ~~Artifact type schema system and dependent validation features~~ | ~~Accepted direction with strong evidence from the use-case analysis.~~ | RFC-008 accepted (§8 narrows v0.13.0 scope); `artifact_families` implemented in v0.13.0. | **Completed in v0.13.0** (Phase 1) | ADR/RFC, code, tests, docs | Milestone `v0.13.0` |
| ~~Required-sections enforcement per document family~~ | ~~ADRs, RFCs, and PRDs have well-established section conventions that could reduce drift if enforced.~~ | STWD-014 (RequiredSectionsRule) implemented and tested. | **Completed** | Code, tests | Delivered |
| ~~`naming_pattern` regex enforcement per family~~ | ~~Stored in `ArtifactFamilyDefinition.NamingPattern` but not enforced.~~ | STWD-016 (FamilyNamingPatternRule) implemented and tested. | **Completed** | Code, tests | Delivered |
| ~~`directory_expectations.min_count` per family~~ | ~~Allows asserting that a family directory contains at least N files.~~ | STWD-015 (FamilyMinCountRule) implemented and tested. | **Completed** | Code, tests | Delivered |
| Typed resource-address follow-on work | Still valuable, but depends on stronger pre-1.0 foundations and clearer type/address design. | Deferred requirement family in [requirements-traceability.md](../requirements/requirements-traceability.md) | Partial | Design, code, docs | Later pre-1.0 milestone |
| Markdown split/extract workflows | Useful but higher-risk than the current stable-readiness set. | Deferred requirement family in [PRD](../requirements/PRD.md) and [requirements-traceability.md](../requirements/requirements-traceability.md) | No | Code, tests, docs | Later pre-1.0 milestone |
| Optional host-specific integrations | Valid future extension, but intentionally outside the current offline-first core. | `REQ-DIST-002` in [PRD](../requirements/PRD.md) and [requirements-traceability.md](../requirements/requirements-traceability.md) | No | Integration, release | Later pre-1.0 milestone |
| Deepen `explain path` provenance and applicability filtering | `explain path` currently shows all 13 rules for any file without filtering by actual governance applicability or showing precedence/provenance detail. | CLI reviews 2026-04-16 (EF-006, F-04, F7); RFC-007 G7-05/G7-06 depth gap | **Done (v0.12.0)** — per-rule applicability filtering | Code | Delivered |
| Deepen `config suggest` for mature repos | `config suggest` only detects 3 artifacts on this 19-artifact repo. Heuristics need richer inference for decisions, planning, indexes, roles. | CLI reviews 2026-04-16 (EF-003, F-03, F6); RFC-007 G7-20 depth gap | **Done (v0.12.0)** — detects decisions, planning, state docs, indexes | Code | Delivered |
| Deepen `config doctor` beyond baseline checks | Doctor checks are useful but narrow; deeper checks for shadowed rules, dead suppressions, and no-effect declarations are needed. | CLI reviews 2026-04-16 (EF-008, F-06); RFC-007 G7-07 depth gap | **Done (v0.12.0)** — dead suppressions, unreachable overrides/patterns | Code | Delivered |
| Add `fm-validate` to Markdown edit subsystem | RFC-004 specifies `fm-validate` but `md edit` does not implement it. | CLI Expectation Fidelity Review 2026-04-16 EF-005; PRD REQ-MD-004 | **Done (v0.12.0)** — `md edit fm-validate <file>` | Code, tests | Delivered |
| Exclude test fixtures from governance coverage | `status --coverage` counts test-fixture Markdown as ungoverned, diluting the repo's governance signal. | CLI reviews 2026-04-16 (EF-007, F-05, F16) | **Done (v0.12.0)** — `coverage.exclude` config section | Code | Delivered |
| Heading selector substring/fuzzy matching in MdPath | `md query` requires exact heading text; no contains/substring match mode. | CLI Full Assessment 2026-04-16 F8 | No | Code | Later pre-1.0 milestone |
| JSON output envelope consistency | Commands use different JSON shapes with no common envelope. | CLI Full Assessment 2026-04-16 F18 | No | Design, code | Later pre-1.0 milestone |

## No Longer Relevant / Superseded

| Item | Why it is superseded |
|------|----------------------|
| Any active planning artifact that assumes `1.0.0` already shipped | The repo is explicitly pre-1.0 and governed by ADR-013 |
| “Post-v1” roadmap framing | All future scope is now retargeted to the pre-1.0 `0.x` line until explicit stable criteria exist |
| RFC-007 items still marked as future-only when code already implements them | The accepted governance-enhancement work is part of the delivered `0.10.0` baseline |

## External Manual Follow-Up

- If a remote `v1.0.0` tag, GitHub release, or public package already exists, remove or supersede it manually. That cannot be corrected from inside the repository alone.
