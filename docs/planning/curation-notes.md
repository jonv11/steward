---
type: planning
status: Active
version: 0.10.0
last_updated: 2026-06-05
---

# Curation Notes

---

## Source Material

| Source | Disposition |
|--------|------------|
| `repository-steward-master-requirements.md` (MRD-0001) | Analyzed and fully mapped to PRD, RFCs, and milestone plan. Preserved as original source material. |

## Artifacts Created

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
| `docs/planning/milestone-plan.md` | Delivered and planned pre-1.0 milestone sequencing |
| `docs/planning/implementation-instructions.md` | Per-milestone execution guide |
| `docs/planning/pre-1-0-readiness-plan.md` | Active readiness and remaining-work register before first stable shipment |

## Conflicts Resolved

No competing artifacts existed. The repository was empty except for the master requirements document.

## Key Decisions During Curation

1. **Scope prioritization:** ~125 traced requirements remain organized into incremental milestones, but the active roadmap now stays on the `0.x` line until explicit `1.0.0` criteria are approved.

2. **Implicit requirements added:** The PRD includes requirements not explicitly in MRD but necessary for a professional CLI: help system, version command, config initialization, color control, verbosity control.

3. **Terminology normalization:** MRD used varied phrasing for the same concepts. PRD standardizes on: "policy" (not "contract" or "ruleset"), "diagnostics" (not "findings" or "results"), "managed region" (not "managed block" or "managed section" interchangeably).

4. **Config model design:** MRD described config abstractly. RFC-002 concretized it into `.steward/config.yaml` + `policy.yaml` + `path-policy.yaml` with documented layering.

5. **Command hierarchy design:** MRD specified capabilities but not command structure. RFC-001 designed the full command tree with options and exit codes.

6. **mdpath selector syntax:** MRD referenced "mdpath-style or equivalent" without specifying syntax. RFC-004 designed a concrete selector syntax.

## Roadmap Notes

- The active roadmap remains on the `0.x` line until the repository is explicitly authorized for `1.0.0`.
- Historical audit documents remain historical, but active planning artifacts describe the current pre-stable roadmap.

## Items Intentionally Deferred Within The Pre-1.0 Line

| Item | Reason |
|------|--------|
| Typed resource-address model (REQ-ADDR-002–005) | Larger architectural change; keep on a later pre-1.0 milestone until the simpler path model is fully hardened |
| Split/extract workflows (REQ-MD-012) | Higher-risk Markdown mutation work than the current stable-readiness set |
| Canonical addresses in search (REQ-SEARCH-012) | Depends on the resource-address model |
| Git hosting integrations (REQ-DIST-002) | Optional integration point, not part of the current offline-first core |
| Native AOT compilation | The current dependency stack has not been hardened for AOT |
| Plugin/extensibility system | Internal rules remain sufficient on the current roadmap |
| Protocol integration (MCP, LSP) | Future enhancement, but still part of the broader pre-1.0 exploration line rather than a shipped stable promise |
