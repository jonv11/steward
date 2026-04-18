---
type: audit
status: Active
last_updated: 2026-04-18
---

# Audit Curation Decision Log — 2026-04-18

**Scope:** Full curation pass over `docs/audits/` to reduce clutter, consolidate durable insights, and clearly separate current truth from historical evidence.

**Context:** This pass follows the 2026-04-16 artifact hygiene cleanup, which classified files without reducing or removing them. The current pass applies actual content reduction and removal where the evidence for doing so is strong.

**Governing principle:** A historical document should remain in full only if its full body still carries durable value not already captured elsewhere. If only a small subset remains relevant, extract and centralize it, then reduce the old document.

---

## File-by-File Decisions

### Files Kept Substantially As-Is (Outcome A)

| File | Rationale |
|------|-----------|
| `maintainer-remarks-implementation-summary-2026-04-18.md` | Active current-milestone record. Full body remains relevant. |
| `rule-system-completeness-audit-2026-04-18.md` | Active; cited by review-synthesis-action-plan. Full body still relevant for v0.16.0+ rule work. |
| `ai-agent-contract-review-2026-04-18.md` | Active; cited by review-synthesis-action-plan. Full body still relevant for CC-01–CC-10 work. |
| `fresh-eyes-onboarding-audit-2026-04-18.md` | Active; cited by review-synthesis-action-plan. Findings still open. |
| `fresh-eyes-reaudit-onboarding-2026-04-18.md` | Active follow-up to onboarding audit. Remaining gaps still open. |
| `cli-expectation-fidelity-assessment-2026-04-17.md` | Most recent fidelity view; open findings feed active planning. |
| `pre-1-0-release-process-pass-2026-04-17.md` | Release process record; decisions still in force. |
| `end-user-documentation-path-audit-2026-04-17.md` | Documentation audit with open items. |
| `release-readiness-assessment-2026-04-15.md` | Still cited as the originating source for `pre-release-blockers.md`. Preserved. |
| `profile-readiness-review-2026-04-16.md` | Directly cited by ADR-014 for fixture-backed evidence. Must remain. |
| `code-quality-pass-2026-04-16.md` | Closed but distinct pass record; describes specific correctness and consistency fixes. |
| `repo-actionability-pass-2026-04-16.md` | Closed implementation pass record; explains why specific CI/docs changes landed. |
| `assessment-coding-agent-usefulness.md` | Directly cited by ADR-010. Research/input evidence. |
| `maintainer-review.md` | Cited by governance work; contains durable product-intent evidence from maintainer perspective. |
| `usability-review-2026-04-15.md` | Historical ergonomics evidence for why specific renames and fixes landed. |
| `usecase-consolidation-proposal.md` | Directly cited by ADR-011 and ADR-012. Research/input evidence. |
| `maintainer-usecase-expectations.md` | Directly cited by ADR-011. Source input evidence. |
| `maintainer-usecase-ideas.md` | Directly cited by ADR-011. Source input evidence. |

### Files Removed (Outcome D)

| File | Rationale | Canonical successor |
|------|-----------|---------------------|
| `code-quality-review-2025-07-23.md` | Fully superseded by `code-quality-pass-2026-04-16.md` which redid all the same work with a fresh baseline. All 6 original fixes applied; 2 deferred items either resolved or permanently deferred with documented rationale. Only live effect was causing a STWD-013 orphan warning. No live citations in ADRs, RFCs, or active planning. | `code-quality-pass-2026-04-16.md`; durable lessons in `historical-audit-synthesis.md` |

### Files Reduced to Historical Stubs (Outcome B)

| File | Rationale | What the stub preserves | Canonical successor |
|------|-----------|------------------------|---------------------|
| `cli-full-assessment-2026-04-16.md` | Superseded by 2026-04-17 and 2026-04-18 reviews. All critical findings (F1–F5) resolved in v0.11.0–v0.12.0. Pre-release-blockers.md cites finding labels F1 and F3 — labels preserved in stub. | Finding labels F1–F5 with resolution status | `cli-expectation-fidelity-assessment-2026-04-17.md`; `historical-audit-synthesis.md` |
| `cli-expectation-fidelity-review-2026-04-16.md` | Superseded by 2026-04-17 assessment. Pre-release-blockers.md cites EF-001 and EF-002 — labels preserved in stub. Both resolved in v0.11.0. | Finding labels EF-001/EF-002 with resolution status | `cli-expectation-fidelity-assessment-2026-04-17.md`; `historical-audit-synthesis.md` |
| `cli-expectation-fidelity-reassessment-2026-04-16.md` | Superseded by 2026-04-17 assessment. Pre-release-blockers.md cites F-01 and F-02 — labels preserved in stub. Both resolved in v0.11.0. | Finding labels F-01/F-02 with resolution status | `cli-expectation-fidelity-assessment-2026-04-17.md`; `historical-audit-synthesis.md` |
| `release-governance-conformance-review-2026-04-16.md` | Superseded by 2026-04-17/18 release evidence. Three blockers resolved (CI green run, ADR-014 decision, ADR-013 still governs 1.0.0). RFC corrections applied in-place during the original pass. | Three top blockers and their resolution status | `maintainer-remarks-implementation-summary-2026-04-18.md`; `ADR-014`; `historical-audit-synthesis.md` |
| `repository-audit-2026-04-14.md` | Early repo snapshot, all in-pass fixes applied and recorded in implementation-status. Long body is mostly superseded point-in-time assessment. | What the pass changed; key findings that shaped later work | `implementation-status.md`; `historical-audit-synthesis.md` |
| `review-requirements.md` | Early per-requirement pass, superseded by current requirements-traceability and implementation-status. Long body is mostly stale assessment against v0.10.0 era. Accuracy note already embedded. | Key findings that shaped later work; accuracy note about pre-existing inaccuracies | `requirements-traceability.md`; `implementation-status.md`; `historical-audit-synthesis.md` |
| `artifact-hygiene-cleanup-review-2026-04-16.md` | A meta-record about a prior classification-only cleanup pass. Its decision log is superseded by this pass. Conventions it established are still in use. | What the pass did; conventions established | This document (2026-04-18 curation pass) |

### New Artifact Created (Outcome C — Consolidation)

| File | Rationale |
|------|-----------|
| `historical-audit-synthesis.md` | Consolidates the durable, future-relevant lessons from the 2025-07-23 through 2026-04-16 review wave into a single coherent artifact. Replaces the need to read multiple stale full-body documents to extract architectural, ergonomic, governance, and release lessons. Does not duplicate active planning content; explicitly points to current truth for current status. |

---

## Navigation Impact

- `docs/planning-index.md` updated: the "Current Release And Readiness Reviews" section now contains only 10 current/durable entries (down from 17). The "Historical Reviews" section now contains 13 entries structured around the synthesis, closed pass records, stubs, and research/input evidence.
- `STRUCTURE.md` updated to reflect the removed file and the new synthesis.
- No ADR, RFC, or active planning document required content changes — all citations that existed before this pass still resolve to existing files.

---

## Validation Checklist

- [x] No broken links introduced — all stubs retain their original paths; `pre-release-blockers.md` finding-label citations still resolve
- [x] Planning-index reflects the new reality clearly — current vs historical distinction is explicit
- [x] Surviving `docs/audits/` surface is intentional, not cluttered — 25 files down to 24 (one removed), with 6 reduced to stubs and one synthesis added
- [x] STWD-013 orphan warning for `code-quality-review-2025-07-23.md` is resolved by removal
- [x] ADR-010, ADR-011, ADR-012, ADR-014 citations to audit files all still resolve
- [x] `pre-release-blockers.md` citations (EF-001, EF-002, F-01, F-02, F1, F3) all preserved in stubs
- [x] No active truth was moved into historical artifacts or stubs
- [x] `historical-audit-synthesis.md` synthesizes durable content without duplicating implementation-status or planning docs
