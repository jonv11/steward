# Pre-1.0 Readiness Plan

- **Source baseline:** `v0.10.0`
- **Status:** Active
- **Last updated:** 2026-04-15

---

## Purpose

This document is the authoritative list of remaining work that is still useful before the real first stable shipment. Until explicit `1.0.0` criteria are approved, all future work remains on the pre-1.0 `0.x` line.

## Required Before First Stable Shipment

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| Cross-platform build/test/pack automation | A stable release needs reproducible validation on Windows, macOS, and Linux; local-only verification is not enough. | No `.github/` workflows are present; multi-platform support is a stated constraint in [ACD-0001](../requirements/assumptions-constraints.md). | Yes — local build/test/pack works | Workflow, tests, release | Direct implementation task on the active `0.x` line |
| Dependency stabilization for stable release | Stable release posture should not depend on beta/preview packages where avoidable. | `Directory.Packages.props` still pins `System.CommandLine` beta and preview DI packages. | Yes — the current stack is functional | Dependency, code, release | Milestone item (`v0.11.0`) |
| Distribution/publication hardening | Packaging now works, but stable-release publication steps and verification should be explicit and repeatable before any public ship. | `dotnet pack` succeeds after this cleanup, but no publication workflow or stable-release checklist exists in-repo. | Yes — local package creation is clean | Docs, release, workflow | Milestone item (`v0.11.0`) |

## Strongly Recommended Before First Stable Shipment

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| Broaden stable contract tests | Stable surfaces should have stronger command/output regression coverage than the current targeted set. | The repo has strong unit/integration coverage, but active planning and audits still call for more stable-surface contract coverage. | Yes | Tests | Milestone item (`v0.11.0`) |
| Decide the later pre-1.0 roadmap ordering explicitly | The repo now correctly stays on `0.x`, but later pre-stable scope still needs explicit sequencing as stable criteria are defined. | User guidance now places all future work on pre-`1.0.0` milestones; [milestone-plan.md](milestone-plan.md) captures the coarse retargeting. | Yes | Planning, governance | Milestone planning update as criteria evolve |

## Optional Polish Before First Stable Shipment

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| Richer merged-policy surfacing in `config show --effective` | Would make the effective governance model easier to inspect without opening multiple files. | JSON already includes merged policy objects; text mode still focuses on raw files plus runtime defaults. | Yes | Code, docs | Direct implementation task if time permits |

## Later Pre-1.0 Candidates

These remain valid future scope, but they are not current stable-release blockers.

| Item | Rationale | Evidence / source | Partly implemented? | Work type | Home |
|------|-----------|-------------------|---------------------|-----------|------|
| Artifact type schema system and dependent validation features | Accepted direction with strong evidence from the use-case analysis, but larger than the current release-hardening set. | [ADR-012](../decisions/adrs/ADR-012-artifact-type-schema-direction.md), [Use-Case Consolidation Proposal](../audits/usecase-consolidation-proposal.md) | No | ADR/RFC, code, tests, docs | Later pre-1.0 milestone (`v0.13.0+`) |
| Typed resource-address follow-on work | Still valuable, but depends on stronger pre-1.0 foundations and clearer type/address design. | Deferred requirement family in [requirements-traceability.md](../requirements/requirements-traceability.md) | Partial | Design, code, docs | Later pre-1.0 milestone |
| Markdown split/extract workflows | Useful but higher-risk than the current stable-readiness set. | Deferred requirement family in [PRD](../requirements/PRD.md) and [requirements-traceability.md](../requirements/requirements-traceability.md) | No | Code, tests, docs | Later pre-1.0 milestone |
| Optional host-specific integrations | Valid future extension, but intentionally outside the current offline-first core. | `REQ-DIST-002` in [PRD](../requirements/PRD.md) and [requirements-traceability.md](../requirements/requirements-traceability.md) | No | Integration, release | Later pre-1.0 milestone |

## No Longer Relevant / Superseded

| Item | Why it is superseded |
|------|----------------------|
| Any active planning artifact that assumes `1.0.0` already shipped | The repo is explicitly pre-1.0 and governed by ADR-013 |
| “Post-v1” roadmap framing | All future scope is now retargeted to the pre-1.0 `0.x` line until explicit stable criteria exist |
| RFC-007 items still marked as future-only when code already implements them | The accepted governance-enhancement work is part of the delivered `0.10.0` baseline |

## External Manual Follow-Up

- If a remote `v1.0.0` tag, GitHub release, or public package already exists, remove or supersede it manually. That cannot be corrected from inside the repository alone.
