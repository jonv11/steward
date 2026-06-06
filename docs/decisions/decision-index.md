# Decision Index

Index of the current decision artifacts for the Repository Steward project. The RFC and ADR tables below are steward-managed from decision-record frontmatter. Each indexed document must declare a non-empty `description` field, and lifecycle state is shown in the generated `Status` column.

Deferred RFCs remain in the RFC table with `Status: Deferred`; accepted ADRs and RFCs remain the authoritative decision record.

## RFCs

<!-- steward:begin id="decision-rfc-index" owner="steward" -->
| Title | Path | Status | Description |
| --- | --- | --- | --- |
| RFC-001: CLI Command Structure | [RFC-001-cli-command-structure.md](rfcs/RFC-001-cli-command-structure.md) | Accepted | Defines the CLI command hierarchy, naming, global options, and interaction conventions |
| RFC-002: Configuration Model | [RFC-002-configuration-model.md](rfcs/RFC-002-configuration-model.md) | Accepted | Defines the config and policy model, built-in profiles, layering rules, and precedence |
| RFC-003: Validation and Diagnostics | [RFC-003-validation-and-diagnostics.md](rfcs/RFC-003-validation-and-diagnostics.md) | Accepted | Defines repository validation behavior, diagnostic structure, severities, remediation, and scoping |
| RFC-004: Markdown Structural Model | [RFC-004-markdown-structural-model.md](rfcs/RFC-004-markdown-structural-model.md) | Accepted | Defines Markdown structural selectors, anchor-compatible heading addressing, managed regions, and preview-first edit operations |
| RFC-005: Orientation, Search, and Outline Boundaries | [RFC-005-orientation-search-outline.md](rfcs/RFC-005-orientation-search-outline.md) | Accepted | Defines the boundaries and overlap rules for orient, outline, and search surfaces |
| RFC-006: Maintenance and Memory Artifacts | [RFC-006-maintenance-and-memory.md](rfcs/RFC-006-maintenance-and-memory.md) | Accepted | Defines deterministic maintenance artifacts, managed sections, frontmatter auto-maintenance, and anti-drift behavior |
| RFC-007: Maintainer Governance and Repository Stewardship Enhancements | [RFC-007-maintainer-governance-and-stewardship-enhancements.md](rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md) | Accepted | Defines maintainer-focused governance, explainability, and stewardship workflow enhancements |
| RFC-008: Convention-Based Artifact Discovery and Workflow Modeling | [RFC-008-convention-based-discovery-and-workflow-modeling.md](rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md) | Accepted | Defines convention-based artifact discovery, frontmatter-driven classification, and related workflow-modeling direction |
| RFC-009: Typed Resource Addresses and Search Alignment | [RFC-009-typed-resource-addresses-and-search-alignment.md](rfcs/RFC-009-typed-resource-addresses-and-search-alignment.md) | Deferred | Proposes a typed resource-address model aligned across file, Markdown, search, and reference surfaces |
| RFC-010: Consistent JSON Output Envelope | [RFC-010-consistent-json-output-envelope.md](rfcs/RFC-010-consistent-json-output-envelope.md) | Accepted | Defines an additive standard JSON envelope for machine-facing CLI consistency |
| RFC-011: Markdown Split and Extract Workflows | [RFC-011-markdown-split-and-extract-workflows.md](rfcs/RFC-011-markdown-split-and-extract-workflows.md) | Accepted | Defines preview-first Markdown split planning and extract-section workflows |
| RFC-012: Heading-Level Markdown Refactors | [RFC-012-heading-level-markdown-refactors.md](rfcs/RFC-012-heading-level-markdown-refactors.md) | Deferred | Defines heading-level Markdown refactor operations starting with safe, reference-aware heading rename |
| RFC-013: Governed Suppressions and Expiring Debt | [RFC-013-governed-suppressions-and-expiring-debt.md](rfcs/RFC-013-governed-suppressions-and-expiring-debt.md) | Deferred | Defines structured, metadata-bearing suppression governance with optional expiry, ownership, and auditability for policy exceptions |
| RFC-014: Closed Artifact Family Schema and Title Convention Enforcement | [RFC-014-closed-family-schema-and-title-pattern.md](rfcs/RFC-014-closed-family-schema-and-title-pattern.md) | Proposed | Extends artifact family schemas with closed-field validation, deprecated-field migration, and per-family H1 title-pattern enforcement to make family schemas authoritative rather than merely additive |

<!-- steward:end -->

## ADRs

<!-- steward:begin id="decision-adr-index" owner="steward" -->
| Title | Path | Status | Description |
| --- | --- | --- | --- |
| ADR-001: .NET 10 CLI Architecture | [ADR-001-dotnet10-cli-architecture.md](adrs/ADR-001-dotnet10-cli-architecture.md) | Accepted | Defines the .NET 10 LTS baseline, CLI hosting model, and layered architecture for Steward |
| ADR-002: Project Structure | [ADR-002-project-structure.md](adrs/ADR-002-project-structure.md) | Accepted | Defines the solution layout, project boundaries, and test-project structure for Steward |
| ADR-003: Configuration Format — YAML | [ADR-003-configuration-format-yaml.md](adrs/ADR-003-configuration-format-yaml.md) | Accepted | Selects YAML and YamlDotNet as the configuration and policy format for Steward |
| ADR-004: Markdown Parser — Markdig | [ADR-004-markdown-parser-markdig.md](adrs/ADR-004-markdown-parser-markdig.md) | Accepted | Selects Markdig as the Markdown parser and preserves raw-text editing for minimal diffs |
| ADR-005: Validation Engine Design | [ADR-005-validation-engine-design.md](adrs/ADR-005-validation-engine-design.md) | Accepted | Defines the validation engine, rule registry, diagnostics model, and fixable-rule contract |
| ADR-006: Output Formatting Strategy | [ADR-006-output-formatting-strategy.md](adrs/ADR-006-output-formatting-strategy.md) | Accepted | Defines the text and JSON output strategy, stdout or stderr contract, and color handling |
| ADR-007: Test Strategy | [ADR-007-test-strategy.md](adrs/ADR-007-test-strategy.md) | Accepted | Defines Steward's unit, integration, fixture, and snapshot testing strategy |
| ADR-008: .gitignore Handling | [ADR-008-gitignore-handling.md](adrs/ADR-008-gitignore-handling.md) | Accepted | Defines Steward's custom .gitignore handling and early-pruning discovery behavior |
| ADR-009: Packaging and Distribution | [ADR-009-packaging-distribution.md](adrs/ADR-009-packaging-distribution.md) | Accepted | Defines the .NET tool packaging model, package identity, GitHub Release assets, and NuGet publication flow for Steward |
| ADR-010: Agent-Usefulness Improvements | [ADR-010-agent-usefulness-improvements.md](adrs/ADR-010-agent-usefulness-improvements.md) | Accepted | Records the first bundle of agent-focused CLI improvements for navigation, query, and maintenance workflows |
| ADR-011: Domain-Specific Stewardship Through Generic Configuration | [ADR-011-domain-stewardship-through-generic-configuration.md](adrs/ADR-011-domain-stewardship-through-generic-configuration.md) | Accepted | Keeps domain-specific stewardship needs in generic repository configuration instead of hardcoded logic |
| ADR-012: Artifact Type Schema System Direction | [ADR-012-artifact-type-schema-direction.md](adrs/ADR-012-artifact-type-schema-direction.md) | Accepted | Defines the direction for per-type artifact schemas in policy-driven governance |
| ADR-013: Pre-1.0 Versioning and Release Authorization | [ADR-013-pre-1-0-versioning-and-release-authorization.md](adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) | Accepted | Keeps Steward on the 0.x line until an explicit stable-release decision authorizes 1.0.0 |
| ADR-014: Non-Software Profile Scope for First Public Release | [ADR-014-non-software-profile-scope.md](adrs/ADR-014-non-software-profile-scope.md) | Accepted | Narrows the first public profile set and defers mixed and knowledge profiles until contracts are richer |

<!-- steward:end -->
