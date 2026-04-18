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
| [Implementation Status](implementation-status.md) | Current runtime/package baseline, landed working-line scope, and remaining pre-stable gaps |
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
| [Milestone Plan](planning/milestone-plan.md) | Delivered lineage plus the active `v0.16.0` and `v0.17.0` pre-1.0 scope |
| [Review Synthesis Action Plan — 2026-04-18](reviews/review-synthesis-action-plan.md) | Canonical synthesis of the April 18 review cycle into validated backlog, priorities, and planning destinations |
| [Pre-Release Blockers](planning/pre-release-blockers.md) | Critical items that must be resolved before a first meaningful public pre-1.0 release |
| [Implementation Instructions](planning/implementation-instructions.md) | Contributor execution guide for the active pre-1.0 roadmap |
| [v0.15.0 Draft Preparation](planning/v0-15-draft-preparation.md) | Historical preparation memo preserved as evidence for the shipped `v0.15.0` milestone |
| [RFC-007 Governance Enhancements Backlog](planning/rfc-007-governance-enhancements-backlog.md) | Status ledger for accepted governance-enhancement items |
| [Curation Notes](planning/curation-notes.md) | Provenance, superseded framing, and roadmap retargeting notes |
| [Release Process](planning/release-process.md) | Authoritative operator guide for intentional public pre-1.0 releases, labels, changelog policy, tagging, and GitHub Releases |
| [Release Publication Checklist](planning/release-publication-checklist.md) | Local verification, tagging, NuGet publication, and self-contained binary steps |

## Deferred RFCs

| Document | Purpose |
|----------|---------|
| [RFC-009 Typed Resource Addresses and Search Alignment](decisions/rfcs/RFC-009-typed-resource-addresses-and-search-alignment.md) | Deferred typed-address model for file and Markdown resources — revisit in a later pre-1.0 milestone |

## Config and Policy Reviews

| Document | Purpose |
| -------- | ------- |
| [AI-Agent Contract Review](reviews/ai-agent-contract-review.md) | Deep contract audit of Steward's JSON outputs, diagnostics, selectors, error handling, and mutation flows for autonomous agent use |
| [Config Expressiveness Stress Test — 2026-04-18](reviews/config-expressiveness-stress-test.md) | Multi-repo adoption stress test of Steward's policy/config model: what expresses well, what is awkward, and what is not credibly expressible |
| [Rule-System Completeness Audit](reviews/rule-system-completeness-audit.md) | Systematic per-rule and system-wide review of all 17 validation rules: intent clarity, diagnostic quality, remediation quality, false-positive/negative risks, coverage gaps, and prioritized governance improvements |

## Reviews And Audit Evidence

Historical audits are preserved for evidence and context. Active repository truth lives in the current-state and planning artifacts above. The latest release-gate view is captured by the current release/readiness review records below; earlier reviews remain useful as historical evidence, not as current truth.

### Current Release And Readiness Reviews

| Document | Purpose |
| -------- | ------- |
| [Maintainer Remarks Implementation Summary — 2026-04-18](audits/maintainer-remarks-implementation-summary-2026-04-18.md) | Repo-grounded implementation summary for the 2026-04-18 maintainer pass: code, tests, docs, package naming, and release automation outcomes |
| [Rule-System Completeness Audit — 2026-04-18](audits/rule-system-completeness-audit-2026-04-18.md) | Systematic per-rule and system-wide review of the validation rule set as of 2026-04-18: intent clarity, diagnostic quality, remediation quality, coverage gaps, and highest-value governance improvements |
| [AI-Agent Contract Review - 2026-04-18](audits/ai-agent-contract-review-2026-04-18.md) | Focused contract audit of Steward as an autonomous coding-agent tool: JSON stability, handoff quality, recovery, and mutation safety |
| [Fresh-Eyes Onboarding Audit - 2026-04-18](audits/fresh-eyes-onboarding-audit-2026-04-18.md) | Fresh-clone, README-only onboarding audit of whether a skeptical newcomer can reach meaningful first value on a real repository without internal knowledge |
| [Fresh-Eyes Re-Audit — 2026-04-18](audits/fresh-eyes-reaudit-onboarding-2026-04-18.md) | Follow-up onboarding re-audit after remediation: actionable findings and remaining gaps from a second fresh-clone pass |
| [CLI Expectation Fidelity Assessment - 2026-04-17](audits/cli-expectation-fidelity-assessment-2026-04-17.md) | Current evidence-based assessment of whether Steward-on-Steward fulfills the repo's stated CLI promise, workflows, and trust expectations |
| [Pre-1.0 Release Process Pass — 2026-04-17](audits/pre-1-0-release-process-pass-2026-04-17.md) | Release-governance and operator-process pass: labels, changelog, GitHub Release workflow, asset publication, and remaining deferred items |
| [End-User Documentation Path Audit — 2026-04-17](audits/end-user-documentation-path-audit-2026-04-17.md) | Persona-based audit and remediation of the end-user documentation path for maintainers and contributors |
| [Release-Readiness Assessment — 2026-04-15](audits/release-readiness-assessment-2026-04-15.md) | End-user product review that originated the active pre-release blocker list |
| [Profile Readiness Review — 2026-04-16](audits/profile-readiness-review-2026-04-16.md) | Command-level release checklist and fixture-backed evidence for non-software profile readiness; cited by ADR-014 |

### Historical Reviews And Closed Pass Records

| Document | Purpose |
| -------- | ------- |
| [Historical Audit Synthesis — 2026-04-14 through 2026-04-16](audits/historical-audit-synthesis.md) | Consolidated durable lessons from the early-development review wave: architecture, ergonomics, governance, and release conventions; canonical successor for the reduced files below |
| [Audit Curation Decision Log — 2026-04-18](audits/audit-curation-decision-log-2026-04-18.md) | File-by-file classification, action taken, and rationale for the 2026-04-18 `docs/audits/` curation pass |
| [Code Quality Pass — 2026-04-16](audits/code-quality-pass-2026-04-16.md) | Closed correctness and consistency pass: `goto` refactor, STWD-009 double-report fix, `AllRules` field, `IndexMaintainer` type consistency |
| [CLI Full Assessment — 2026-04-16](audits/cli-full-assessment-2026-04-16.md) | Stub — superseded by 2026-04-17/18 reviews; finding labels F1–F5 preserved for traceability |
| [CLI Expectation Fidelity Reassessment — 2026-04-16](audits/cli-expectation-fidelity-reassessment-2026-04-16.md) | Stub — superseded by 2026-04-17 assessment; finding labels F-01/F-02 preserved for traceability |
| [CLI Expectation Fidelity Review — 2026-04-16](audits/cli-expectation-fidelity-review-2026-04-16.md) | Stub — superseded by 2026-04-17 assessment; finding labels EF-001/EF-002 preserved for traceability |
| [Release Governance Conformance Review — 2026-04-16](audits/release-governance-conformance-review-2026-04-16.md) | Stub — superseded by 2026-04-17/18 release evidence; key decisions (ADR-014, RFC corrections) recorded in successor artifacts |
| [Repository Audit — 2026-04-14](audits/repository-audit-2026-04-14.md) | Stub — early requirement-driven audit; changes and key findings recorded in stub and synthesis |
| [Requirements Implementation Review — 2026-04-14](audits/review-requirements.md) | Stub — early per-requirement pass; remaining open items tracked in readiness plan |
| [Coding-Agent Usefulness Assessment — 2026-04-14](audits/assessment-coding-agent-usefulness.md) | Historical agent-workflow review that informed ADR-010 |
| [Maintainer Review — 2026-04-14](audits/maintainer-review.md) | Historical maintainer-perspective gap analysis that informed later governance work |
| [CLI Usability and Configurability Review — 2026-04-15](audits/usability-review-2026-04-15.md) | Historical Steward-on-Steward ergonomics review that informed subsequent fixes |
| [Repo Actionability Pass — 2026-04-16](audits/repo-actionability-pass-2026-04-16.md) | Closed implementation pass record: CI matrix, `config show --effective` text output, README tightening |
| [Artifact Hygiene Cleanup Review — 2026-04-16](audits/artifact-hygiene-cleanup-review-2026-04-16.md) | Stub — prior classification-only cleanup pass; superseded by the 2026-04-18 curation pass |

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
