# Decision Index

Index of the current decision artifacts for the Repository Steward project. Accepted RFCs and ADRs remain the authoritative decision record; draft RFCs under review are listed separately below.

## RFCs — Product and Requirement Decisions

| ID | Title | Status | Summary |
|----|-------|--------|---------|
| [RFC-001](rfcs/RFC-001-cli-command-structure.md) | CLI Command Structure | Accepted | Command hierarchy, naming, global options, exit codes |
| [RFC-002](rfcs/RFC-002-configuration-model.md) | Configuration Model | Accepted | Config/policy separation, profiles, layering, precedence |
| [RFC-003](rfcs/RFC-003-validation-and-diagnostics.md) | Validation and Diagnostics | Accepted | Check behavior, diagnostic schema, scoping, fix/dry-run |
| [RFC-004](rfcs/RFC-004-markdown-structural-model.md) | Markdown Structural Model | Accepted | Selectors (mdpath), managed regions, edit ops, preview/apply |
| [RFC-005](rfcs/RFC-005-orientation-search-outline.md) | Orientation, Search, and Outline | Accepted | Surface boundaries and responsibilities |
| [RFC-006](rfcs/RFC-006-maintenance-and-memory.md) | Maintenance and Memory | Accepted | Maintenance flows, memory artifacts, anti-drift |
| [RFC-007](rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md) | Maintainer Governance and Stewardship Enhancements | Accepted | Policy expressiveness, governance inspection, maintenance evolution, stewardship workflows |
| [RFC-008](rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md) | Convention-Based Discovery and Workflow Modeling | Accepted | Artifact families, path-based discovery, frontmatter-driven classification, workflow/session modeling; v0.13.0 scope narrowed in §8 |

## Draft RFCs Under Review

These draft RFCs are design inputs for the next pre-1.0 milestone. They are not accepted decisions yet.

| ID | Title | Status | Summary |
|----|-------|--------|---------|
| [RFC-009](rfcs/RFC-009-typed-resource-addresses-and-search-alignment.md) | Typed Resource Addresses and Search Alignment | Draft | Additive typed-address model for file and Markdown resources plus initial search/alignment consumers |
| [RFC-010](rfcs/RFC-010-consistent-json-output-envelope.md) | Consistent JSON Output Envelope | Draft | Additive standard JSON envelope for machine-facing consistency while preserving legacy payloads |
| [RFC-011](rfcs/RFC-011-markdown-split-and-extract-workflows.md) | Markdown Split and Extract Workflows | Draft | Narrow split-planning and extract-section workflow for the Markdown subsystem |

## ADRs — Technical and Architectural Decisions

| ID | Title | Status | Summary |
|----|-------|--------|---------|
| [ADR-001](adrs/ADR-001-dotnet10-cli-architecture.md) | .NET 10 CLI Architecture | Accepted | Runtime, System.CommandLine, three-layer architecture |
| [ADR-002](adrs/ADR-002-project-structure.md) | Project Structure | Accepted | Solution layout, Cli/Core split, test projects |
| [ADR-003](adrs/ADR-003-configuration-format-yaml.md) | Configuration Format — YAML | Accepted | YAML + YamlDotNet for all config/policy files |
| [ADR-004](adrs/ADR-004-markdown-parser-markdig.md) | Markdown Parser — Markdig | Accepted | Markdig for parsing, structural facade, raw-text editing |
| [ADR-005](adrs/ADR-005-validation-engine-design.md) | Validation Engine Design | Accepted | Rule interface, registry, scope resolution, fix support |
| [ADR-006](adrs/ADR-006-output-formatting-strategy.md) | Output Formatting Strategy | Accepted | Text/JSON formatters, stdout/stderr contract, color handling |
| [ADR-007](adrs/ADR-007-test-strategy.md) | Test Strategy | Accepted | xUnit, FluentAssertions, Verify, fixture repos |
| [ADR-008](adrs/ADR-008-gitignore-handling.md) | .gitignore Handling | Accepted | Custom implementation, IIgnoreFilter, early pruning |
| [ADR-009](adrs/ADR-009-packaging-distribution.md) | Packaging and Distribution | Accepted | dotnet tool, self-contained single-file, NuGet |
| [ADR-010](adrs/ADR-010-agent-usefulness-improvements.md) | Agent-Usefulness Improvements | Accepted | --compact orient, --regex search, --quiet check, stdin content, maintain diff, batch query |
| [ADR-011](adrs/ADR-011-domain-stewardship-through-generic-configuration.md) | Domain-Specific Stewardship Through Generic Configuration | Accepted | Domain needs served through generic policy mechanisms, not hardcoded domain logic |
| [ADR-012](adrs/ADR-012-artifact-type-schema-direction.md) | Artifact Type Schema System Direction | Accepted | Per-type artifact definitions in policy.yaml for frontmatter, sections, naming, lifecycle |
| [ADR-013](adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) | Pre-1.0 Versioning and Release Authorization | Accepted | Keeps Steward on `0.x.y` until explicit stable-release approval and defines version-bump governance |
| [ADR-014](adrs/ADR-014-non-software-profile-scope.md) | Non-Software Profile Scope for First Public Release | Accepted | Keep `software`/`docs`/`minimal`; defer `mixed`/`knowledge` until contracts are enriched |
