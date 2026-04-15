# Repository Steward — Planning Index

Central navigation for all planning and decision artifacts.

## Requirements and Product

| Document | Purpose |
|----------|---------|
| [PRD](requirements/PRD.md) | Canonical Product Requirements Document |
| [Assumptions, Constraints, and Dependencies](requirements/assumptions-constraints.md) | Explicit assumptions, constraints, dependencies, and risks |
| [Requirements Traceability](requirements/requirements-traceability.md) | Mapping from master requirements to milestones |

## Decisions

| Document | Purpose |
|----------|---------|
| [Decision Index](decisions/decision-index.md) | Index of all RFCs and ADRs |
| [RFC-001 CLI Command Structure](decisions/rfcs/RFC-001-cli-command-structure.md) | Command hierarchy, naming, global options |
| [RFC-002 Configuration Model](decisions/rfcs/RFC-002-configuration-model.md) | Config format, layering, profiles, policy |
| [RFC-003 Validation and Diagnostics](decisions/rfcs/RFC-003-validation-and-diagnostics.md) | Check behavior, diagnostics, exit codes |
| [RFC-004 Markdown Structural Model](decisions/rfcs/RFC-004-markdown-structural-model.md) | Selectors, edit ops, managed regions |
| [RFC-005 Orientation, Search, and Outline](decisions/rfcs/RFC-005-orientation-search-outline.md) | Surface boundaries and responsibilities |
| [RFC-006 Maintenance and Memory](decisions/rfcs/RFC-006-maintenance-and-memory.md) | Maintenance flows, memory artifacts |
| [RFC-007 Maintainer Governance and Stewardship Enhancements](decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements-draft.md) | Policy expressiveness, governance inspection, stewardship workflows **(Accepted)** |
| [ADR-001 .NET 10 CLI Architecture](decisions/adrs/ADR-001-dotnet10-cli-architecture.md) | Runtime, CLI framework, project shape |
| [ADR-002 Project Structure](decisions/adrs/ADR-002-project-structure.md) | Solution layout and assembly boundaries |
| [ADR-003 Configuration Format](decisions/adrs/ADR-003-configuration-format-yaml.md) | YAML choice and library |
| [ADR-004 Markdown Parser](decisions/adrs/ADR-004-markdown-parser-markdig.md) | Markdig choice and structural model |
| [ADR-005 Validation Engine Design](decisions/adrs/ADR-005-validation-engine-design.md) | Rule-based engine architecture |
| [ADR-006 Output Formatting](decisions/adrs/ADR-006-output-formatting-strategy.md) | Human and machine output strategy |
| [ADR-007 Test Strategy](decisions/adrs/ADR-007-test-strategy.md) | Frameworks, coverage, test types |
| [ADR-008 .gitignore Handling](decisions/adrs/ADR-008-gitignore-handling.md) | Ignore-file parsing approach |
| [ADR-009 Packaging and Distribution](decisions/adrs/ADR-009-packaging-distribution.md) | dotnet tool, single-file publish |
| [ADR-010 Agent Usefulness Improvements](decisions/adrs/ADR-010-agent-usefulness-improvements.md) | Targeted improvements for coding-agent ergonomics |
| [ADR-011 Domain-Specific Stewardship](decisions/adrs/ADR-011-domain-stewardship-through-generic-configuration.md) | Domain needs through generic configuration, not hardcoded logic |
| [ADR-012 Artifact Type Schema Direction](decisions/adrs/ADR-012-artifact-type-schema-direction.md) | Per-type artifact definitions in policy.yaml |

## Implementation Planning

| Document | Purpose |
|----------|---------|
| [Delivery Strategy](planning/delivery-strategy.md) | Sequencing rationale and approach |
| [Milestone Plan](planning/milestone-plan.md) | Milestones v0.1.0 through v1.0.0 |
| [Post-v1.0.0 Milestone Plan](planning/post-v1-milestone-plan.md) | Milestones v1.1.0 through v1.6.0 (RFC-007 + use-case analysis) |
| [Implementation Instructions](planning/implementation-instructions.md) | Per-milestone execution guide |
| [Curation Notes](planning/curation-notes.md) | What was merged, superseded, or deferred |
| [RFC-007 Governance Enhancements Backlog](planning/rfc-007-governance-enhancements-backlog.md) | Actionable backlog derived from RFC-007 **(Accepted)** |

## Audits

| Document | Purpose |
| -------- | ------- |
| [Repository Audit — 2026-04-14](audits/repository-audit-2026-04-14.md) | Requirement-driven release audit and contract-alignment review |
| [Coding-Agent Usefulness Assessment — 2026-04-14](audits/assessment-coding-agent-usefulness.md) | Per-command assessment of Steward's value in the agent terminal workflow |
| [Maintainer Review — 2026-04-14](audits/maintainer-review.md) | Maintainer-perspective gaps and improvement requests for rule enforcement |
| [Use-Case Consolidation Proposal — 2026-04-15](audits/usecase-consolidation-proposal.md) | Exhaustive cross-reference of maintainer use-case files against CLI capabilities and roadmap |
| [Maintainer Use-Case Expectations](audits/maintainer-usecase-expectations.md) | Detailed maintainer expectations for story/worldbuilding repository stewardship |
| [Maintainer Use-Case Ideas](audits/maintainer-usecase-ideas.md) | Steward-aware improvement proposals for story/worldbuilding domain |

## Source Materials

| Document | Role |
|----------|------|
| [Master Requirements](../repository-steward-master-requirements.md) | Original requirements source (MRD-0001) |
