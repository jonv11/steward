# Curation Notes

- **Version:** 1.0.0

---

## Source material

| Source | Disposition |
|--------|------------|
| `repository-steward-master-requirements.md` (MRD-0001) | Analyzed and fully mapped to PRD, RFCs, and milestone plan. Preserved as original source material. |

## Artifacts created

The following canonical artifacts were created from the master requirements:

| Artifact | Purpose |
|----------|---------|
| `docs/planning-index.md` | Central navigation for all planning documents |
| `docs/requirements/PRD.md` | Canonical Product Requirements Document |
| `docs/requirements/assumptions-constraints.md` | Assumptions, constraints, dependencies, risks |
| `docs/requirements/requirements-traceability.md` | Full mapping from MRD requirements to milestones |
| `docs/decisions/decision-index.md` | Index of all RFCs and ADRs |
| `docs/decisions/rfcs/RFC-001` through `RFC-006` | Product and requirement decisions |
| `docs/decisions/adrs/ADR-001` through `ADR-009` | Technical and architectural decisions |
| `docs/planning/delivery-strategy.md` | Sequencing rationale |
| `docs/planning/milestone-plan.md` | Milestones v0.1.0 through v1.0.0 |
| `docs/planning/implementation-instructions.md` | Per-milestone execution guide |

## Conflicts resolved

No competing artifacts existed. The repository was empty except for the master requirements document.

## Key decisions during curation

1. **Scope prioritization:** ~130 requirements were mapped to 10 milestones. 5 requirements were explicitly deferred beyond v1.0.0 (typed resource addresses, split/extract workflows, search resource addresses, hosting integrations).

2. **Implicit requirements added:** The PRD includes requirements not explicitly in MRD but necessary for a professional CLI: help system, version command, config initialization, color control, verbosity control.

3. **Terminology normalization:** MRD used varied phrasing for the same concepts. PRD standardizes on: "policy" (not "contract" or "ruleset"), "diagnostics" (not "findings" or "results"), "managed region" (not "managed block" or "managed section" interchangeably).

4. **Config model design:** MRD described config abstractly. RFC-002 concretized it into `.steward/config.yaml` + `policy.yaml` + `path-policy.yaml` with documented layering.

5. **Command hierarchy design:** MRD specified capabilities but not command structure. RFC-001 designed the full command tree with options and exit codes.

6. **mdpath selector syntax:** MRD referenced "mdpath-style or equivalent" without specifying syntax. RFC-004 designed a concrete selector syntax.

## Items intentionally deferred

| Item | Reason |
|------|--------|
| Typed resource-address model (REQ-ADDR-002–005) | Architectural evolution beyond v1.0.0; path-based model is sufficient |
| Split/extract workflows (REQ-MD-012) | High-risk, low-priority for first release |
| Canonical addresses in search (REQ-SEARCH-012) | Depends on resource-address model |
| Git hosting integrations (REQ-DIST-002) | Explicitly out of v1.0.0 scope per requirements |
| Native AOT compilation | System.CommandLine and YAML parsing may have AOT limitations |
| Plugin/extensibility system | Internal rules are sufficient for v1.0.0 |
| Protocol integration (MCP, LSP) | Future enhancement beyond CLI-first scope |
