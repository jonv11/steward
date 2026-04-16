# Release Governance Conformance Review — 2026-04-16

- **Status:** Complete
- **Reviewer role:** Principal-engineering release-governance pass
- **Source-of-truth order used:** Accepted ADRs/RFCs, accepted requirements artifacts, active planning/readiness docs, current code/tests/config, then backlog-style notes

---

## 1. Executive Summary

Repository governance health is materially better than in the earlier 2026-04-14 and 2026-04-15 audits, but the repo is **not release-clear yet**.

Accepted PRD, RFC, and ADR artifacts now form a substantially more coherent product and architecture story after this pass, especially around the implemented CLI command surface and the current configuration model. The largest trust issue found during this review was drift inside accepted RFCs: several decision docs no longer described the actual shipped CLI/config contract. That drift has been corrected in-place where the intended behavior was already clear from code, tests, and active planning artifacts.

**Release readiness assessment:** **FAIL**

The repository is **not coherent enough for a first stable release**, and it is also **not yet clear for a first meaningful public release** under the repo's own current blockers. The top remaining risks are:

- no hosted green evidence yet for the new cross-platform CI matrix
- no explicit keep-or-narrow release decision yet for non-software `init --profile` offerings
- ADR-013 stable-release authorization criteria still remain unmet for a true `1.0.0` shipment

## 2. Method And Scope

This pass reviewed:

- requirements artifacts in `docs/requirements/`
- accepted RFCs and ADRs in `docs/decisions/`
- active planning/readiness docs in `docs/planning/`
- public/operator guidance in `README.md`
- CLI command implementations, configuration loading, policy/explainability surfaces, and representative tests
- current release-readiness and profile-readiness audit records

Repository conventions used:

- `docs/requirements/` contains accepted product and constraint artifacts
- `docs/decisions/rfcs/` and `docs/decisions/adrs/` contain accepted binding decisions, indexed by `docs/decisions/decision-index.md`
- `docs/planning/` contains active readiness and sequencing artifacts; these are authoritative for current release gating but do not override accepted product/architecture decisions
- `docs/audits/` preserves durable review records and evidence
- accepted status is binding for implementation; active planning artifacts are binding for current release readiness; historical audits are evidence, not authoritative current truth

## 3. Artifact Inventory Summary

### Requirements And Product Artifacts Reviewed

| ID | Path | Status | Binding |
|----|------|--------|---------|
| PRD-0001 | `docs/requirements/PRD.md` | Accepted | Yes |
| ACD-0001 | `docs/requirements/assumptions-constraints.md` | Accepted | Yes |
| TRACE-0001 | `docs/requirements/requirements-traceability.md` | Accepted | Yes |

### RFCs Reviewed

| Range | Status | Binding |
|-------|--------|---------|
| RFC-001 through RFC-007 | Accepted | Yes |

### ADRs Reviewed

| Range | Status | Binding |
|-------|--------|---------|
| ADR-001 through ADR-013 | Accepted | Yes |

### Related Active Docs Reviewed

- `README.md`
- `docs/planning-index.md`
- `docs/implementation-status.md`
- `docs/planning/delivery-strategy.md`
- `docs/planning/milestone-plan.md`
- `docs/planning/implementation-instructions.md`
- `docs/planning/pre-1-0-readiness-plan.md`
- `docs/planning/pre-release-blockers.md`
- `docs/planning/rfc-007-governance-enhancements-backlog.md`
- `docs/audits/release-readiness-assessment-2026-04-15.md`
- `docs/audits/repo-actionability-pass-2026-04-16.md`
- `docs/audits/profile-readiness-review-2026-04-16.md`

## 4. Findings By Severity

### Release Blocker

#### RG-001 — Cross-platform CI evidence trail is still incomplete

- **Severity:** Release blocker
- **Affected artifacts/code paths:** `docs/planning/pre-release-blockers.md`, `docs/implementation-status.md`, `.github/workflows/ci.yml`, ADR-013 stable-release authorization criteria
- **Evidence:** The repo now contains a Windows/macOS/Linux GitHub Actions matrix, but the blocker document still correctly marks hosted green evidence as pending.
- **Why it matters:** Multi-platform support is part of the accepted product contract. Until a hosted matrix run is green, the repo lacks the release-gate evidence needed to trust that claim.
- **Recommended resolution:** Run the hosted workflow and keep the resulting green run as release evidence before any public release tag.
- **Auto-fixed?:** No. This requires execution outside the local workspace.

#### RG-002 — Non-software profile offering still lacks a final release decision

- **Severity:** Release blocker
- **Affected artifacts/code paths:** `docs/planning/pre-release-blockers.md`, `docs/audits/profile-readiness-review-2026-04-16.md`, `README.md`, `src/Steward.Cli/Commands/InitCommand.cs`, profile defaults and profile-readiness tests
- **Evidence:** Fixture-backed coverage now exists, but current planning still correctly records that the remaining step is an explicit keep-or-narrow decision for `docs`, `mixed`, `knowledge`, and `minimal`.
- **Why it matters:** The accepted product direction supports multiple repository archetypes, but release-facing profile availability should not outrun demonstrated usefulness. Leaving the offered profile set undecided creates avoidable ambiguity for adopters.
- **Recommended resolution:** Make an explicit release decision per profile and either keep only the profiles with demonstrated value or narrow the offered `init --profile` set.
- **Auto-fixed?:** No. This is a product/release decision, not a safe unilateral implementation change.

### High

#### RG-003 — Accepted RFC command and config contracts had drifted materially from implementation

- **Severity:** High
- **Affected artifacts/code paths:** `docs/decisions/rfcs/RFC-001-cli-command-structure.md`, `RFC-002-configuration-model.md`, `RFC-003-validation-and-diagnostics.md`, `RFC-004-markdown-structural-model.md`, `RFC-005-orientation-search-outline.md`, `RFC-006-maintenance-and-memory.md`
- **Evidence:** Accepted RFCs still described outdated command names/flags, outdated JSON field names, an obsolete `.steward/profiles/` layout, an obsolete config field (`output.color`), and old policy examples that no longer matched `RepositoryPolicy`, CLI help, or tests.
- **Why it matters:** Accepted decision docs are binding implementation guidance. When those artifacts drift from code, new contributors and agents can make incorrect changes even if tests still pass.
- **Recommended resolution:** Update accepted RFCs to describe the implemented contract where current behavior is already clearly settled by code/tests and current planning.
- **Auto-fixed?:** Yes. The accepted RFC text was updated in-place to match the current delivered CLI/config contract.

### Medium

#### RG-004 — `steward explain path` did not fully reflect the validator's effective frontmatter contract

- **Severity:** Medium
- **Affected artifacts/code paths:** `src/Steward.Cli/Commands/ExplainCommand.cs`, `tests/Steward.Cli.Tests/ExplainCommandTests.cs`
- **Evidence:** The validation rule accepts both `validation.required_frontmatter_fields` and `governance.frontmatter.required_fields`, but `ResolveEffectivePolicy` only surfaced the governance path.
- **Why it matters:** RFC-007 and the explainability requirements depend on path explanation showing what actually applies. A path-level explain surface that hides part of the enforced contract undermines trust.
- **Recommended resolution:** Merge both global frontmatter sources when computing effective path policy and cover the behavior with a regression test.
- **Auto-fixed?:** Yes.

#### RG-005 — Overlapping global frontmatter declarations were valid but unnecessarily ambiguous

- **Severity:** Medium
- **Affected artifacts/code paths:** `src/Steward.Cli/Commands/ConfigCommand.cs`, `tests/Steward.Cli.Tests/ConfigCommandTests.cs`, `README.md`
- **Evidence:** The repo already carried an audit finding that `validation.required_frontmatter_fields` and `governance.frontmatter.required_fields` were additive in practice but undocumented. Before this pass, `config doctor` did not surface the overlap.
- **Why it matters:** A valid-but-confusing policy contract is a governance risk. Maintainers can unintentionally create stricter frontmatter requirements than they realize.
- **Recommended resolution:** Warn in `config doctor` when both global declaration paths are used and document the canonical location.
- **Auto-fixed?:** Yes.

### Low

#### RG-006 — Accepted RFC-007 still looked like a draft in its filename

- **Severity:** Low
- **Affected artifacts/code paths:** `docs/decisions/decision-index.md`, `docs/planning/rfc-007-governance-enhancements-backlog.md`, `docs/decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md`
- **Evidence:** The decision index marked RFC-007 as accepted, but its path still ended with `-draft.md`.
- **Why it matters:** Lifecycle naming should not contradict artifact status. A lingering `-draft` suffix makes the decision inventory less trustworthy than it should be.
- **Recommended resolution:** Rename the file to drop the `-draft` suffix and update references.
- **Auto-fixed?:** Yes.

### Informational

#### RG-007 — Stable-release authorization remains intentionally unmet

- **Severity:** Informational
- **Affected artifacts/code paths:** `docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md`, `docs/planning/pre-1-0-readiness-plan.md`, `docs/implementation-status.md`
- **Evidence:** Active planning docs consistently keep the repo on `0.x.y`, and ADR-013 requires explicit approval plus green readiness evidence before `1.0.0`.
- **Why it matters:** This is not drift; it is the current intended release posture. It should, however, frame the verdict: a true stable-release gate cannot pass until those criteria are intentionally met.
- **Recommended resolution:** Keep the pre-1.0 stance explicit and treat stable-release authorization as a separate human decision after the required readiness items are closed.
- **Auto-fixed?:** No change required beyond maintaining consistency.

## 5. Traceability Summary

| Major PRD area | RFC / ADR coverage | Implementation status | Notes |
|----------------|--------------------|-----------------------|-------|
| Config and policy separation, profiles, precedence | RFC-002, ADR-003, ADR-011 | Implemented | Accepted config docs were updated to match the real schema and merge semantics; non-software profile release decision remains open |
| Validation, diagnostics, exit codes, workflow completeness | RFC-001, RFC-003, ADR-005, ADR-006, ADR-013 | Implemented with open release-gate evidence | CLI behavior and JSON/text contracts are implemented; hosted CI evidence still missing |
| Orientation, search, outline boundaries | RFC-001, RFC-005, ADR-010 | Implemented | Accepted command docs now match current command names and flags |
| Markdown structural query/editing and ownership | RFC-004, ADR-004 | Implemented with later partials tracked | Future split/extract work remains explicitly deferred in requirements traceability |
| Deterministic maintenance, memory/state artifacts, anti-drift | RFC-006, RFC-007, ADR-012 | Implemented for current baseline | Current planning correctly treats larger artifact-type schema work as later pre-1.0 scope |
| Explainability and governance inspection | RFC-007, PRD explainability requirements | Implemented and tightened | `explain path` now reflects both global frontmatter declaration paths |
| Distribution and release governance | ADR-009, ADR-013, active readiness plans | Not release-clear | Pre-1.0 posture is coherent, but stable/public release gates remain open |

## 6. Applied Fixes

| File(s) changed | Why the change was safe |
|-----------------|-------------------------|
| `docs/decisions/rfcs/RFC-001-cli-command-structure.md` through `RFC-006-maintenance-and-memory.md` | These were documentation-alignment fixes only. The accepted intent was already settled by current code, tests, and command help. |
| `docs/decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md`, `docs/decisions/decision-index.md`, `docs/planning/rfc-007-governance-enhancements-backlog.md` | Lifecycle/path cleanup only; no product or architecture semantics changed. |
| `src/Steward.Cli/Commands/ConfigCommand.cs`, `tests/Steward.Cli.Tests/ConfigCommandTests.cs` | Added a low-risk doctor warning for an already-supported but confusing config pattern. |
| `src/Steward.Cli/Commands/ExplainCommand.cs`, `tests/Steward.Cli.Tests/ExplainCommandTests.cs` | Brought path explainability into line with the validator's existing enforced behavior. |
| `README.md` | Public docs were tightened to reflect current additive frontmatter semantics and the current pre-1.0 contract more honestly. |

## 7. Remaining Open Issues

- Hosted green evidence is still required for the Windows/macOS/Linux CI workflow before any public release gate can pass.
- A human release decision is still required on which non-software profiles remain publicly offered.
- A true stable-release gate remains blocked until ADR-013 authorization conditions are intentionally met, including the broader stable-readiness items in `docs/planning/pre-1-0-readiness-plan.md`.

## 8. Final Release-Gate Verdict

**FAIL**

The repository now tells a much clearer and more internally consistent product/decision story than it did at the start of this pass, and the accepted artifact chain is materially cleaner. However, the repo's own release-governance artifacts still leave blocking work open:

- hosted cross-platform CI evidence is still missing
- non-software profile offering still lacks an explicit release decision
- stable-release authorization remains intentionally unmet under ADR-013

Those are real release-gate conditions, not editorial imperfections, so the conservative verdict remains **FAIL**.
