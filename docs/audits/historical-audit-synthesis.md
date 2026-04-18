---
type: audit
status: Historical
last_updated: 2026-04-18
---

# Historical Audit Synthesis — 2026-04-14 through 2026-04-16

**Scope:** Consolidated durable lessons from the early-development audit wave (2025-07-23, 2026-04-14, 2026-04-15, 2026-04-16) covering code quality, requirement coverage, usability, release readiness, and governance conformance.

**What this is not:** This is not a current-state document. The findings below describe what was learned during the review wave that shaped design decisions and governance conventions. Current state lives in [implementation-status.md](../implementation-status.md), the active planning docs, and the 2026-04-17/2026-04-18 review artifacts.

**Original artifacts synthesized:**
- Code Quality Review — 2025-07-23 → removed; all fixes applied
- [Repository Audit — 2026-04-14](repository-audit-2026-04-14.md) → reduced to stub
- [Requirements Implementation Review — 2026-04-14](review-requirements.md) → reduced to stub
- [CLI Usability and Configurability Review — 2026-04-15](usability-review-2026-04-15.md) → historical evidence
- [Release-Readiness Assessment — 2026-04-15](release-readiness-assessment-2026-04-15.md) → historical evidence; still linked by pre-release-blockers.md
- [CLI Full Assessment — 2026-04-16](cli-full-assessment-2026-04-16.md) → reduced to stub
- [CLI Expectation Fidelity Review — 2026-04-16](cli-expectation-fidelity-review-2026-04-16.md) → reduced to stub
- [CLI Expectation Fidelity Reassessment — 2026-04-16](cli-expectation-fidelity-reassessment-2026-04-16.md) → reduced to stub
- [Release Governance Conformance Review — 2026-04-16](release-governance-conformance-review-2026-04-16.md) → reduced to stub
- [Repo Actionability Pass — 2026-04-16](repo-actionability-pass-2026-04-16.md) → closed pass record; historical
- [Artifact Hygiene Cleanup Review — 2026-04-16](artifact-hygiene-cleanup-review-2026-04-16.md) → closed cleanup record; historical

---

## 1. Durable Architecture and Design Lessons

### The two-file config split is correct
Separating `config.yaml` (runtime behavior) from `policy.yaml` (repository contract) proved correct in practice. The separation kept policy portable, testable, and inspectable without embedding execution preferences. This convention should not be collapsed even as the policy model grows.

### Profile defaults must be merged into effective policy, not merely label-stamped
The early codebase made profiles a display label only. Every review cycle flagged this as the primary reason "archetype-aware behavior" felt hollow. The fix — merging profile defaults in `CommandSetup.Build()` via `ProfileMerger` — was the correct shape. Future profile additions must follow the same pattern; no new profile should ship as label-only.

### Managed regions are the right ownership primitive; roles must eventually have behavioral weight
Managed region markers (ownership, stale-artifact detection) proved durable and trustworthy in practice. By contrast, artifact roles (`state-document`, `audit`, `reference`) functioned only as display taxonomy. The maintainer review correctly noted that roles must eventually carry behavioral defaults (freshness signals, special orient prominence) to deliver on the policy-as-contract promise. This remains open and is tracked in the readiness plan.

### Policy schema should stay close to accepted RFCs; drift creates agent confusion
By the 2026-04-14 review, `RepositoryPolicy` had already drifted from RFC-002's `governance.frontmatter` / `governance.managed_regions` / `governance.completion_policy` structure. This required multiple reconciliation passes. The lesson: when the RFC specifies a schema, align implementation to it; document intentional divergences as ADR amendments rather than letting silent drift accumulate.

### Config-semantic validation (`config validate`) must catch more than YAML parse errors
Every review wave flagged `config validate` as giving false confidence because it only caught parse errors. A semantic validation pass (unknown maintainer types, dead `disabled_rules` entries, invalid `severity_overrides` keys) was eventually added in v0.12.0. This should be the standard going forward: every new config surface should add a corresponding `config validate` check.

---

## 2. Durable Ergonomics and UX Lessons

### Preview-first is non-negotiable for write operations
The preview-before-apply model on `md edit` and `maintain` was validated across every review cycle as the correct safety model for both humans and agents. No regression on this is acceptable. All future write-capable operations should default to preview and require `--apply` to commit.

### Three inconsistent preview/apply conventions erode trust
By mid-2026-04-16 reviews, `check --fix/--dry-run`, `maintain --apply` (default preview), and `refactor move --preview+--apply` used three different patterns for the same conceptual workflow. This inconsistency was flagged as a meaningful trust issue, especially for agents. The canonical pattern is: default to preview (show diff), require `--apply` to commit.

### Scoped validation (`--scope changed|staged`) must cover repository-wide obligation rules
The most critical bug in the 2026-04-16 review cycle was scoped validation producing false positives (missing required artifacts) on clean trees when checking only the changed file set. The root cause was that obligation rules (STWD-001, STWD-008, STWD-009, STWD-011) need access to `AllDiscoveredFiles` even when validation is scoped. This was fixed in v0.11.0 by adding `AllDiscoveredFiles` to `ValidationContext`. Any new obligation rule must follow this pattern.

### JSON output on failure must be JSON
The 2026-04-16 through 2026-04-17 review cycles repeatedly found that `--output json` could fall back to plain-text error output when the command failed before producing output. This breaks any consumer that parses JSON. The fix is structured error envelopes regardless of whether the command succeeds or fails.

### Orient verbosity hurts agents; compact mode is a clear need
Orient listing 100+ entries was identified as a consistent agent pain point. `--compact` / summary mode that surfaces only start-here entries, classified directories, and required artifacts was implemented. This remains the preferred default for agent-oriented orient usage.

---

## 3. Durable Governance and Release Lessons

### Release notes must be changelog-backed, not generated summaries
The 2026-04-17 release process pass established that GitHub Release notes should come from curated `CHANGELOG.md` entries, not from generated git-log summaries. This was a deliberate choice: generated summaries carry too much noise and too little product context. The `release.yml` workflow enforces this by reading the changelog entry for the tagged version.

### Pre-1.0 public releases are allowed; `v1.0.0` requires explicit authorization
ADR-013 records this explicitly. The 2026-04-16 governance conformance review confirmed that the repo's own earlier drafts blurred this line — some artifacts implied `1.0.0` was imminent when it was not. After that review, all public-facing docs describe the pre-1.0 line honestly and the `1.0.0` gate is explicit and separate.

### Self-dogfooding on a non-trivial policy is the most credible release signal
By 2026-04-16, the Steward repo itself used start-here entries, completion policy, artifact families, coverage exclusions, and maintained artifacts meaningfully. Every review cycle noted this as a strong positive signal. The dogfooding quality should be maintained; cosmetic dogfooding (trivial policy files that only declare the README) does not provide the same confidence.

### Non-software profiles (`mixed`, `knowledge`) shipped thin; narrowing them was correct
The profile readiness review established that `mixed` and `knowledge` profiles produced contracts indistinguishable from a minimal README-only policy. ADR-014 recorded the correct response: narrow the public profile set to `software`, `docs`, and `minimal` until the artifact type schema system can make the other profiles meaningfully archetype-specific.

---

## 4. Durable Research Input Captured Elsewhere

The following research input remains preserved in its source documents and is cited by accepted ADRs:

| Research input | Cited by | Location |
|----------------|---------|---------|
| Story/worldbuilding maintainer use-case analysis | ADR-011, ADR-012 | [maintainer-usecase-expectations.md](maintainer-usecase-expectations.md), [maintainer-usecase-ideas.md](maintainer-usecase-ideas.md), [usecase-consolidation-proposal.md](usecase-consolidation-proposal.md) |
| Coding-agent workflow usability gaps | ADR-010 | [assessment-coding-agent-usefulness.md](assessment-coding-agent-usefulness.md) |
| Maintainer governance gap analysis | governance work record | [maintainer-review.md](maintainer-review.md) |
| Non-software profile readiness evidence | ADR-014 | [profile-readiness-review-2026-04-16.md](profile-readiness-review-2026-04-16.md) |

---

## 5. What Has Since Been Fixed

The following significant findings from this review wave were resolved and are recorded in delivered milestone notes:

| Finding | Resolved in | Location |
|---------|-------------|---------|
| Scoped validation false positives on clean trees | v0.11.0 | `implementation-status.md` §v0.11.0 |
| `status --coverage --output json` drops coverage object | v0.11.0 | `implementation-status.md` §v0.11.0 |
| Profile defaults not merged into effective policy | v0.10.0 | `implementation-status.md` §v0.10.0 |
| `config validate` only caught YAML parse errors | v0.12.0 | `implementation-status.md` §v0.12.0 |
| `md query --pattern` argument parsing ambiguity | v0.12.0 | `implementation-status.md` §v0.12.0 |
| STWD-009 double-reporting for `importance: required` artifacts | v0.11.0 | `code-quality-pass-2026-04-16.md` |
| `goto` labels in `ConfigLoader.ValidateConfig` | v0.11.0 | `code-quality-pass-2026-04-16.md` |
| `validation.severity_overrides` validated but not applied | v0.15.0 | `implementation-status.md` §v0.15.0 |
| `refactor move --apply --output json` reporting success without acting | v0.16.0 CC-04 | `ai-agent-contract-review-2026-04-18.md` |
| JSON envelope not universal across all commands | v0.16.0 CC-01 through CC-10 | `ai-agent-contract-review-2026-04-18.md` |

---

## 6. What Remains Open

Open items from this review wave that are still tracked in active planning:

- Role-linked behavioral defaults (freshness for state-document, prominence for requirements) — tracked in readiness plan
- `explain path` family-applicability accuracy — partially improved in v0.15.0; further in v0.16.0
- Artifact type schema system (ADR-012) — post-1.0 milestone
- `mixed` / `knowledge` profile enrichment (ADR-014) — deferred until type schema system exists
- Workflow/session modeling (RFC-008 Phase 3) — v0.16.0+

For current status of all open items, see [pre-1-0-readiness-plan.md](../planning/pre-1-0-readiness-plan.md).
