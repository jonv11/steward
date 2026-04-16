# Repository Steward — Planning Index

Central navigation for the active product, planning, readiness, and decision artifacts.

## Requirements And Product

| Document | Purpose |
|----------|---------|
| [PRD](requirements/PRD.md) | Canonical product requirements document |
| [Assumptions, Constraints, and Dependencies](requirements/assumptions-constraints.md) | Explicit assumptions, constraints, dependencies, and risks |
| [Requirements Traceability](requirements/requirements-traceability.md) | Requirement-to-milestone and status mapping for the current pre-1.0 roadmap |

## Current State

| Document | Purpose |
|----------|---------|
| [Implementation Status](implementation-status.md) | Current `0.10.0` baseline, delivered scope, and remaining pre-stable gaps |
| [Pre-1.0 Readiness Plan](planning/pre-1-0-readiness-plan.md) | Categorized remaining work before the first stable shipment |

## Decisions

| Document | Purpose |
|----------|---------|
| [Decision Index](decisions/decision-index.md) | Index of accepted RFCs and ADRs |
| [ADR-013 Pre-1.0 Versioning and Release Authorization](decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) | Authoritative versioning policy and the rule that `1.0.0` needs explicit approval |

## Planning

| Document | Purpose |
|----------|---------|
| [Delivery Strategy](planning/delivery-strategy.md) | Sequencing principles and versioning strategy |
| [Milestone Plan](planning/milestone-plan.md) | Delivered lineage through `v0.10.0` plus planned pre-1.0 milestones |
| [Pre-Release Blockers](planning/pre-release-blockers.md) | Critical items that must be resolved before a first meaningful public release |
| [Implementation Instructions](planning/implementation-instructions.md) | Contributor execution guide for the active pre-1.0 roadmap |
| [RFC-007 Governance Enhancements Backlog](planning/rfc-007-governance-enhancements-backlog.md) | Status ledger for accepted governance-enhancement items |
| [Curation Notes](planning/curation-notes.md) | Provenance, superseded framing, and roadmap retargeting notes |
| [Release Publication Checklist](planning/release-publication-checklist.md) | Local verification, tagging, NuGet publication, and self-contained binary steps |

## Reviews And Audit Evidence

Historical audits are preserved for evidence and context. Active repository truth lives in the current-state and planning artifacts above. The latest release-gate view is captured by the current release/readiness review records below; earlier reviews remain useful as historical evidence, not as current truth.

### Current Release And Readiness Reviews

| Document | Purpose |
| -------- | ------- |
| [CLI Full Assessment — 2026-04-16](audits/cli-full-assessment-2026-04-16.md) | Comprehensive end-to-end product assessment: build, exercise, cross-reference, diagnose, score |
| [CLI Expectation Fidelity Reassessment — 2026-04-16](audits/cli-expectation-fidelity-reassessment-2026-04-16.md) | Deep evidence-based reassessment of Steward-on-Steward promise fidelity, workflow trust, and release-line credibility |
| [CLI Expectation Fidelity Review — 2026-04-16](audits/cli-expectation-fidelity-review-2026-04-16.md) | Principal-level assessment of whether Steward-on-Steward currently fulfills the repo's own CLI promise, workflows, and trust expectations |
| [Release-Readiness Assessment — 2026-04-15](audits/release-readiness-assessment-2026-04-15.md) | End-user product review that feeds the active pre-release blocker list |
| [Profile Readiness Review — 2026-04-16](audits/profile-readiness-review-2026-04-16.md) | Command-level release checklist and current evidence for non-software profile readiness |
| [Release Governance Conformance Review — 2026-04-16](audits/release-governance-conformance-review-2026-04-16.md) | Current principal-engineering release-gate pass across accepted product/architecture artifacts and implementation |

### Historical Reviews And Closed Pass Records

| Document | Purpose |
| -------- | ------- |
| [Code Quality Pass — 2026-04-16](audits/code-quality-pass-2026-04-16.md) | Closed correctness and consistency pass: `goto` refactor, STWD-009 double-report fix, `AllRules` field, `IndexMaintainer` type consistency |
| [Repository Audit — 2026-04-14](audits/repository-audit-2026-04-14.md) | Historical requirement-driven audit and contract-alignment review against the earlier repo state |
| [Requirements Implementation Review — 2026-04-14](audits/review-requirements.md) | Historical per-requirement review against the earlier repo state and earlier version posture |
| [Coding-Agent Usefulness Assessment — 2026-04-14](audits/assessment-coding-agent-usefulness.md) | Historical agent-workflow review that informed later ergonomics decisions |
| [Maintainer Review — 2026-04-14](audits/maintainer-review.md) | Historical maintainer-perspective gap analysis that informed later governance work |
| [CLI Usability and Configurability Review — 2026-04-15](audits/usability-review-2026-04-15.md) | Historical Steward-on-Steward ergonomics review that informed subsequent fixes |
| [Repo Actionability Pass — 2026-04-16](audits/repo-actionability-pass-2026-04-16.md) | Closed implementation pass record preserved as evidence of why specific repo-grounded changes landed |
| [Artifact Hygiene Cleanup Review — 2026-04-16](audits/artifact-hygiene-cleanup-review-2026-04-16.md) | Cleanup record for audit/status/review artifact classification and scoping hygiene |

### Research And Input Evidence

| Document | Purpose |
| -------- | ------- |
| [Use-Case Consolidation Proposal — 2026-04-15](audits/usecase-consolidation-proposal.md) | Cross-reference of domain use cases against current and later pre-1.0 scope; feeds ADR-011/ADR-012 and later roadmap work |
| [Maintainer Use-Case Expectations](audits/maintainer-usecase-expectations.md) | Detailed domain use-case expectations preserved as source input evidence |
| [Maintainer Use-Case Ideas](audits/maintainer-usecase-ideas.md) | Additional domain-specific idea inventory preserved as source input evidence |

## Source Material

| Document | Role |
|----------|------|
| [Master Requirements](../repository-steward-master-requirements.md) | Original requirements source (MRD-0001) |
