---
type: review
status: Completed
last_updated: 2026-04-18
---

# Draft RFC Disposition Review — 2026-04-18

---

## Purpose

This review documents the disposition of seven draft RFC proposals found in `docs/drafts/` during the April 2026 governance pass. Each draft was assessed against current code, accepted decisions, the active roadmap, and existing coverage surfaces. The outcome is a validated placement decision for each, with all artifacts moved or absorbed into their canonical locations.

## Review Criteria

Each draft was evaluated on:

1. **Does this address a real gap or a solved problem?** Overlap with existing commands and delivered features disqualifies standalone RFC status.
2. **Does this require design-authority-grade decision-making?** Only concepts with cross-cutting design trade-offs, schema changes, or irreversible commitments warrant RFC treatment.
3. **Is this timely or deferred?** Pre-1.0 scope discipline per ADR-013 means speculative features are deferred, not rejected.
4. **Where does this belong?** Each concept must live in exactly one canonical location: an RFC, a planning backlog item, or nowhere.

## Dispositions

### DRAFT-001: Adopt Flow

**Disposition:** Absorbed into backlog item E-01.

**Rationale:** The `steward adopt` orchestration concept is valid but premature. The individual primitives it would compose (`init --profile`, `config suggest`, `config validate`, `config doctor`, `check`) already exist and continue to improve. `config suggest` gained confidence scoring and exclusion-aware inference in v0.16.0. The adoption workflow is a product-level orchestration concern, not a design-authority question requiring RFC treatment. No external adoption friction evidence justifies the investment now.

**Canonical location:** [future-enhancements-backlog.md](../planning/future-enhancements-backlog.md) § E-01

---

### DRAFT-002: Governance Gap Explainer

**Disposition:** Absorbed into backlog item E-02.

**Rationale:** The proposed governance-gap explanation surface overlaps substantially with `status --coverage`, `explain path`, `config doctor`, and `config suggest`. The incremental value is in multi-dimensional coverage decomposition and path-specific gap recommendations, which can be added to existing surfaces without a new command family or RFC-grade design authority.

**Canonical location:** [future-enhancements-backlog.md](../planning/future-enhancements-backlog.md) § E-02

---

### DRAFT-003: Heading Refactors

**Disposition:** Accepted as RFC-012, status Deferred.

**Rationale:** Heading-level Markdown refactoring with cross-file reference updating is a genuine design-authority decision. It extends the Markdown subsystem into structural mutation territory that has irreversible reference-graph implications. The concept does not overlap with existing capabilities — `md edit` operates at the content level, not at the heading-structure level. However, it depends on selector infrastructure maturity and proven Markdown refactoring surfaces, making it premature for scheduling.

**Canonical location:** [RFC-012-heading-level-markdown-refactors.md](../decisions/rfcs/RFC-012-heading-level-markdown-refactors.md)

---

### DRAFT-004: Suppression Governance

**Disposition:** Accepted as RFC-013, status Deferred.

**Rationale:** Structured suppression metadata with lifecycle, ownership, and auditability is a policy-schema-extending design decision with cross-cutting implications for `check`, `config doctor`, `explain path`, and the config model. This clearly requires RFC-grade design authority. However, the current pre-1.0 trust floor does not yet need this sophistication, and adoption evidence for structured exception management does not exist yet.

**Canonical location:** [RFC-013-governed-suppressions-and-expiring-debt.md](../decisions/rfcs/RFC-013-governed-suppressions-and-expiring-debt.md)

---

### DRAFT-005: Policy Playground

**Disposition:** Absorbed into backlog item E-03.

**Rationale:** Most of the proposed output is already available through `explain path`, `check --paths`, and `config show --effective`. The incremental value is in precedence-trace detail and hypothetical config evaluation. Precedence trace can be added as a `--verbose` enhancement to `explain path`. Hypothetical evaluation is genuinely novel but speculative — no user or agent has reported needing it. This does not warrant RFC treatment.

**Canonical location:** [future-enhancements-backlog.md](../planning/future-enhancements-backlog.md) § E-03

---

### DRAFT-006: Indexed Mode

**Disposition:** Absorbed into backlog item E-04.

**Rationale:** Speculative performance/architecture concern with no evidence of real performance problems on actual repositories. The complexity cost (freshness tracking, dual execution modes, index schema management) is high relative to current needs. Steward's live-scan model works correctly and truthfully on all tested repository sizes. This is a future exploration, not a current design decision.

**Canonical location:** [future-enhancements-backlog.md](../planning/future-enhancements-backlog.md) § E-04

---

### DRAFT-007: Change Impact Surface

**Disposition:** Absorbed into backlog item E-05.

**Rationale:** The building blocks exist: `check` includes impact signals (G7-15), `refs` provides reference analysis, `explain path` describes governance context, `maintain --preview` shows pending maintenance work. The proposal is an aggregation layer over existing surfaces, not a new capability requiring design-authority treatment. Can be built incrementally without RFC-level decision-making.

**Canonical location:** [future-enhancements-backlog.md](../planning/future-enhancements-backlog.md) § E-05

---

## Summary Table

| Draft | Disposition | Artifact Type | Final Location | Status |
|-------|-------------|---------------|----------------|--------|
| DRAFT-001 (Adopt Flow) | Backlog | Planning | future-enhancements-backlog.md § E-01 | Active |
| DRAFT-002 (Governance Gap) | Backlog | Planning | future-enhancements-backlog.md § E-02 | Active |
| DRAFT-003 (Heading Refactors) | RFC-012 | RFC | RFC-012-heading-level-markdown-refactors.md | Deferred |
| DRAFT-004 (Suppressions) | RFC-013 | RFC | RFC-013-governed-suppressions-and-expiring-debt.md | Deferred |
| DRAFT-005 (Policy Playground) | Backlog | Planning | future-enhancements-backlog.md § E-03 | Active |
| DRAFT-006 (Indexed Mode) | Backlog | Planning | future-enhancements-backlog.md § E-04 | Active |
| DRAFT-007 (Change Impact) | Backlog | Planning | future-enhancements-backlog.md § E-05 | Active |

## Cleanup Actions Taken

1. Created [RFC-012](../decisions/rfcs/RFC-012-heading-level-markdown-refactors.md) with full RFC structure and Deferred status.
2. Created [RFC-013](../decisions/rfcs/RFC-013-governed-suppressions-and-expiring-debt.md) with full RFC structure and Deferred status.
3. Created [future-enhancements-backlog.md](../planning/future-enhancements-backlog.md) absorbing 5 draft concepts as tracked enhancement items.
4. Deleted all 7 draft files from `docs/drafts/`.
5. Removed the empty `docs/drafts/` directory.
6. Updated [milestone-plan.md](../planning/milestone-plan.md) with a Deferred RFCs section listing RFC-009, RFC-012, and RFC-013.
7. Updated [pre-1-0-readiness-plan.md](../planning/pre-1-0-readiness-plan.md) Later Candidates with RFC-012 and RFC-013.
8. Fixed stale v0.14.0 baseline in [delivery-strategy.md](../planning/delivery-strategy.md) to v0.16.0.
9. Updated [planning-index.md](../planning-index.md) to reference new artifacts and remove draft references.
10. Ran `steward maintain --apply` to regenerate decision indexes and structure.

## Priority Ordering

For future scheduling purposes, the deferred and backlog items are ordered by estimated value and prerequisite readiness:

1. **RFC-013 (Suppressions)** — highest governance-depth value; prerequisite for E-02 intentional-ungoverned support.
2. **RFC-012 (Heading Refactors)** — genuine capability gap; depends on selector maturity.
3. **E-02 (Governance Gap Explainer)** — medium value; builds on RFC-013 and existing surfaces.
4. **E-05 (Change Impact Surface)** — medium-low value; thin aggregation layer.
5. **E-01 (Adopt Flow)** — medium-low; needs external adoption evidence.
6. **E-03 (Policy Playground)** — low priority; nice-to-have for policy authors.
7. **E-04 (Indexed Mode)** — low priority; speculative performance concern.

## Open Questions

- Should the empty `docs/drafts/` path be excluded in `.steward/policy.yaml` discovery, or is its removal sufficient?
- Should RFC-012 and RFC-013 be explicitly targeted to a named milestone (e.g., v0.18.0), or should they remain unscheduled until adoption evidence justifies them?
