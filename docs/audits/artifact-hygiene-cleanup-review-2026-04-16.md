# Artifact Hygiene Cleanup Review — 2026-04-16

- **Status:** Complete
- **Scope:** Audit, review, status, readiness, and progress-tracking artifact hygiene
- **Primary convention used:** Active truth lives in current-state and planning artifacts; historical audits are preserved for evidence and context

---

## 1. Executive Summary

This cleanup pass reduced ambiguity in the repository's audit and review surface without erasing intentional history.

The repository already had a clear convention that active truth lives in `docs/planning/` and `docs/implementation-status.md`, while `docs/audits/` preserves historical evidence. The main hygiene issue was not excessive raw file count; it was presentation and scoping. Several older review artifacts could still be mistaken for current truth, and the planning index mixed current release-gate reviews, historical review records, and domain-input documents in one flat list.

This pass therefore focused on:

- clarifying which records are current release/readiness evidence
- marking older review artifacts as historical-scope evidence
- reclassifying domain-input files so they read as source material rather than active audits

No audit/status artifact met a clear, evidence-based deletion bar under the repo's stated preservation convention, so this pass made **clarifying and de-emphasizing changes**, not blind removals.

## 2. Repository Conventions Used

The following repo conventions drove the cleanup decisions:

- [planning-index.md](../planning-index.md) explicitly states that historical audits are preserved for evidence and context.
- [implementation-instructions.md](../planning/implementation-instructions.md) explicitly says to keep historical audit documents historical, fix links and active references, and avoid treating stale audit wording as current truth.
- [curation-notes.md](../planning/curation-notes.md) states that historical audit documents remain historical while active planning artifacts carry the current pre-stable story.
- [pre-1-0-readiness-plan.md](../planning/pre-1-0-readiness-plan.md) is the authoritative remaining-work list before a first stable shipment.

Applied lifecycle model for this pass:

- **Active:** current-state docs and active planning/readiness artifacts
- **Current review evidence:** latest release/readiness review artifacts still used by active planning
- **Historical evidence:** earlier audits and review records preserved for traceability
- **Research/input evidence:** domain analyses and idea inventories preserved as source material, not as current-state truth

## 3. Inventory Summary

Candidates reviewed: **18**

- Kept unchanged: **9**
- Kept but clarified/reclassified: **9**
- Archived: **0**
- Removed: **0**
- Uncertain: **0**

Reviewed set included:

- all `12` files in `docs/audits/`
- `docs/planning-index.md`
- `docs/implementation-status.md`
- `docs/planning/pre-1-0-readiness-plan.md`
- `docs/planning/pre-release-blockers.md`
- `docs/planning/implementation-instructions.md`
- `docs/planning/curation-notes.md`

## 4. Decision Log

### AH-001

- **Path:** `docs/planning-index.md`
- **Classification:** Active index / navigation artifact
- **Action taken:** Clarified and reclassified
- **Rationale:** A flat “Audits” list made current release-gate reviews, older historical reviews, and domain-input documents look equally current. The index now separates:
  - current release/readiness reviews
  - historical reviews and closed pass records
  - research/input evidence
- **Evidence:** Repo conventions explicitly preserve historical audits but keep active truth in planning/current-state artifacts.
- **Replacement/canonical successor:** Not a replacement; a clearer classification of the existing retained set.

### AH-002

- **Path:** `docs/audits/repository-audit-2026-04-14.md`
- **Classification:** Historical audit
- **Action taken:** Kept but clarified with a historical-scope note
- **Rationale:** The file remains useful as traceability evidence, but without a scope note it can read as current contract truth.
- **Evidence:** Later active truth now lives in `docs/implementation-status.md` and active planning docs.
- **Replacement/canonical successor:** Current state docs and later reviews, especially `release-governance-conformance-review-2026-04-16.md`.

### AH-003

- **Path:** `docs/audits/review-requirements.md`
- **Classification:** Historical review artifact with outdated version posture
- **Action taken:** Kept but clarified with a historical-scope note
- **Rationale:** The document still has traceability value, but it explicitly reflects an earlier declared version posture and earlier repo snapshot.
- **Evidence:** The file itself already carries an accuracy note; current truth is maintained elsewhere.
- **Replacement/canonical successor:** `docs/implementation-status.md`, `docs/planning/pre-1-0-readiness-plan.md`, and later review artifacts.

### AH-004

- **Path:** `docs/audits/assessment-coding-agent-usefulness.md`
- **Classification:** Historical review evidence
- **Action taken:** Kept but clarified with a historical-scope note
- **Rationale:** Still useful as input to ADR-010, but it should not be mistaken for a live operator guide or current-state artifact.
- **Evidence:** ADR-010 still cites it; current state and readiness live elsewhere.
- **Replacement/canonical successor:** No single replacement; preserved as supporting evidence.

### AH-005

- **Path:** `docs/audits/maintainer-review.md`
- **Classification:** Historical review evidence
- **Action taken:** Kept but clarified with a historical-scope note
- **Rationale:** Still useful as input to later governance work, but not authoritative current truth.
- **Evidence:** The repo's own planning docs now distinguish active truth from historical audits.
- **Replacement/canonical successor:** Later governance/readiness artifacts, especially the active planning docs and later review records.

### AH-006

- **Path:** `docs/audits/usability-review-2026-04-15.md`
- **Classification:** Historical review evidence
- **Action taken:** Kept but clarified with a historical-scope note
- **Rationale:** This file documents why certain ergonomics fixes landed, but it should not read like the current command contract.
- **Evidence:** Accepted RFCs and current implementation now hold the canonical command/config contract.
- **Replacement/canonical successor:** Current docs plus the updated accepted RFCs.

### AH-007

- **Path:** `docs/audits/release-readiness-assessment-2026-04-15.md`
- **Classification:** Current evidence with historical scope boundaries
- **Action taken:** Kept but clarified
- **Rationale:** This remains the durable end-user review that fed the blocker list, but active blocker status and the latest release-gate verdict now live elsewhere.
- **Evidence:** `pre-release-blockers.md` and `release-governance-conformance-review-2026-04-16.md`.
- **Replacement/canonical successor:** No full replacement; active status is now split between the blocker tracker and the latest conformance review.

### AH-008

- **Path:** `docs/audits/repo-actionability-pass-2026-04-16.md`
- **Classification:** Closed pass record
- **Action taken:** Kept but clarified
- **Rationale:** The file is a useful implementation trail, but it is not a current-state artifact.
- **Evidence:** Current state and remaining release questions are now tracked in active planning/current-state docs and the later release-governance review.
- **Replacement/canonical successor:** `docs/implementation-status.md`, `docs/planning/pre-release-blockers.md`, and `release-governance-conformance-review-2026-04-16.md`.

### AH-009

- **Path:** `docs/audits/usecase-consolidation-proposal.md`
- **Classification:** Research/input evidence
- **Action taken:** Kept but clarified
- **Rationale:** This file still materially supports ADR-011 and ADR-012, so removal would harm traceability. The issue was classification, not usefulness.
- **Evidence:** ADR-011 and ADR-012 still cite it directly.
- **Replacement/canonical successor:** No replacement; preserved as source analysis.

## 5. Remaining Uncertain Items

None required immediate human resolution.

One longer-term optional improvement remains possible but was not necessary for this pass: if the repo ever introduces a dedicated research/input location distinct from `docs/audits/`, the domain input files could move there. Current repo conventions do not justify inventing that structure now.

## 6. Final Repository Hygiene Assessment

Repository artifact hygiene is now **acceptable and coherent**.

The remaining audit/status/review set is easier to interpret because:

- current release/readiness reviews are distinct from older historical reviews
- older audits now announce their historical scope instead of passively looking current
- domain-input documents no longer read like active audit authorities in the planning index

The repository still keeps its intentional evidence trail, but the active state is less noisy and less misleading for both maintainers and coding agents.
