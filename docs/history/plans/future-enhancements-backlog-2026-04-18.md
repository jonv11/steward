---
type: planning
status: Superseded
last_updated: 2026-04-18
standalone: true
---

# Future Enhancements Backlog

---

## Purpose

This document records validated enhancement concepts that emerged from the April 2026 draft-RFC review cycle. Each item was reviewed against current code, accepted decisions, and the active roadmap, then classified as a future enhancement rather than a current-milestone commitment or RFC-grade decision.

Items here are not scheduled. They represent well-scoped concepts that may become implementation targets in later pre-1.0 milestones or post-1.0 work, depending on product priorities and adoption evidence.

## Source

These items originate from seven draft proposals reviewed in the [draft RFC disposition review](../reviews/draft-rfc-disposition-review-2026-04-18.md). Two of the seven became deferred RFCs (RFC-012, RFC-013). The remaining five are captured here as backlog items.

---

## Enhancement Items

### E-01: Repository Adoption Flow

**Origin:** RFC-DRAFT-001 (adopt flow)

**Concept:** A dedicated `steward adopt` command that orchestrates existing setup primitives (`init`, `config suggest`, `config validate`, `config doctor`, `check`) into a single staged, confidence-scored adoption workflow for existing repositories.

**Why not an RFC now:** The individual primitives exist and continue to improve. `config suggest` gained confidence scoring and exclusion-aware inference in v0.16.0. The adoption workflow is a product-level orchestration layer, not a design-authority question. It should be revisited when adoption friction evidence from real external repositories justifies the investment.

**Current coverage:**

- `steward init --profile` scaffolds base files.
- `steward config suggest` proposes policy and config content with confidence hints.
- `steward config validate` and `config doctor` catch configuration problems.
- README "First 15 Minutes" path provides a tested onboarding sequence.

**Remaining gap:** No single command composes these into one staged plan with preview/apply semantics. Agent setup workflows must orchestrate multiple commands manually.

**Prerequisites:** Broader external adoption evidence. Stable config model. RFC-013 (suppression governance) may influence adoption-time exception handling.

**Priority signal:** Medium-low for pre-1.0. Higher if external adoption friction reports indicate that the current multi-command path is a real barrier.

---

### E-02: Governance Gap Explainer

**Origin:** RFC-DRAFT-002 (governance gap explainer and coverage guidance)

**Concept:** A governance-gap explanation surface that decomposes coverage into actionable dimensions (artifact coverage, path-policy coverage, frontmatter coverage, structural coverage, maintenance coverage, ownership coverage) and recommends specific policy changes to improve governance.

**Why not an RFC now:** The core idea overlaps substantially with existing surfaces. `status --coverage` reports coverage metrics. `explain path` describes what governance applies to a specific file. `config doctor` identifies ineffective declarations. `config suggest` proposes new policy. The incremental value is in decomposition and path-specific gap recommendations, which can be added to existing surfaces without a new command family.

**Current coverage:**

- `steward status --coverage` shows governed vs ungoverned file counts and lists ungoverned files.
- `steward explain path` describes classification, matched family, required frontmatter, and applicable rules.
- `steward config doctor` reports dead suppressions, unreachable patterns, and unmatched rules.
- `steward config suggest` proposes missing artifact and family declarations.

**Remaining gap:** No multi-dimensional coverage breakdown. No path-specific "what is missing and what should I add" explanation. No explicit support for marking paths as intentionally ungoverned.

**Implementation approach when ready:** Enhance `explain path` with a `--gaps` or `--coverage` flag rather than creating a new top-level command. Add `intentional: true` or equivalent suppression metadata when RFC-013 lands.

**Priority signal:** Medium. Becomes more valuable after RFC-013 (suppression governance) provides the intentional-ungoverned mechanism.

---

### E-03: Policy Evaluation Playground

**Origin:** RFC-DRAFT-005 (policy and rule playground)

**Concept:** A `steward config eval <path>` command that shows the full effective governance result for a path (all matching declarations, precedence, effective frontmatter/structure expectations, suppressions) and a `steward rule test <rule-id> --path <path>` command for single-rule evaluation.

**Why not an RFC now:** Most of the proposed output is already available through `explain path`. The incremental value is in precedence-trace detail and hypothetical evaluation (`--with temp-policy.yaml`). The precedence trace can be added as a `--verbose` enhancement to `explain path`. Hypothetical evaluation is genuinely novel but speculative — no user or agent has reported needing it.

**Current coverage:**

- `steward explain path` shows classification, family match, path-policy pattern, required frontmatter, allowed values, and applicable rules.
- `steward check --paths <path>` tests rules against specific paths.
- `steward config show --effective` displays the merged runtime policy.

**Remaining gap:** No precedence-trace detail showing why a specific rule won over alternatives. No hypothetical-config evaluation mode.

**Implementation approach when ready:** Add `--verbose` to `explain path` for precedence trace. Consider `--with <overlay>` for hypothetical evaluation as a separate feature.

**Priority signal:** Low. Nice-to-have for policy authors. Not blocking any current workflow.

---

### E-04: Indexed Mode for Large Repositories

**Origin:** RFC-DRAFT-006 (first-class indexed mode)

**Concept:** An explicit indexed operating mode where Steward maintains local deterministic inventory artifacts (file manifest, heading index, reference graph, coverage inventory) and uses them for faster repeated operations on large repositories.

**Why not an RFC now:** This is a speculative performance/architecture concern. No evidence of real performance problems with Steward's live-scan model on actual repositories. The complexity cost (freshness tracking, dual execution modes, index schema management) is high relative to current needs. Steward's maintained-artifact model (STRUCTURE.md, decision indexes) already provides limited indexing conceptually.

**Current coverage:**

- Live-scan mode works correctly and truthfully on all tested repository sizes.
- `steward maintain` can refresh generated artifacts deterministically.
- Maintained structure and index artifacts serve as passive accelerators for orientation.

**Remaining gap:** No explicit performance-aware execution mode. No maintained search index or reference graph artifact.

**Prerequisites:** Evidence of actual performance problems on medium-to-large repositories. Clear user or agent demand for faster repeated operations.

**Priority signal:** Low. Revisit only when performance evidence justifies the investment.

---

### E-05: Consolidated Change Impact Surface

**Origin:** RFC-DRAFT-007 (consolidated change impact surface)

**Concept:** A unified `steward impact <target>` command that aggregates governance, reference, maintenance, workflow-significance, and risk information into a single pre-change or post-change impact summary.

**Why not an RFC now:** The building blocks exist: `check` includes impact signals (G7-15), `refs` provides reference analysis, `explain path` describes governance context, `maintain --preview` shows pending maintenance work. The proposal is an aggregation layer over these existing surfaces, not a new capability. The aggregation can be built incrementally without RFC-level design authority.

**Current coverage:**

- `steward check` includes `ComputeImpactSignals` and `ComputeStagedCompleteness`.
- `steward refs <path>` shows inbound and outbound references.
- `steward explain path` describes full governance context.
- `steward maintain --preview` shows what maintenance would change.

**Remaining gap:** No single command aggregates all impact dimensions. Contributors must run multiple commands and synthesize results. Agents need extra orchestration.

**Implementation approach when ready:** Add a `steward impact` command as a thin aggregation layer. Design should focus on concise default summary with per-dimension drill-down via existing commands.

**Priority signal:** Medium-low. Useful for agents. Can be built incrementally after the trust floor stabilizes.

---

## Relationship to Other Planning Artifacts

- **Pre-1.0 readiness plan:** Items here are not readiness blockers. They are enhancement scope.
- **Milestone plan:** Items here are not assigned to any milestone. They become candidates when product priorities and adoption evidence justify scheduling.
- **RFC-007 backlog:** Tracks already-accepted governance enhancement items. Items here are newer concepts not covered by RFC-007.
- **RFC-012 (heading refactors):** The accepted-deferred RFC for heading-level Markdown refactoring. Related to E-05 (impact surface) for cross-reference impact.
- **RFC-013 (suppression governance):** The accepted-deferred RFC for structured suppression metadata. Prerequisite for E-02 (governance gap explainer) intentional-ungoverned support.
