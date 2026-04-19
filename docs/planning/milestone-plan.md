---
type: planning
document_id: PLAN-0002
version: 0.16.0
status: Active
last_updated: 2026-04-19
---

# Milestone Plan — Pre-1.0 Mainline

---

## Framing

Steward is still on a pre-stable SemVer line. The active roadmap continues on `0.x` milestones until an explicit release-authorization decision schedules `1.0.0`.

## Delivered Milestones

| Version | Theme | State |
|---------|-------|-------|
| `v0.1.0` | Foundations | Delivered |
| `v0.2.0` | Orientation and repository discovery | Delivered |
| `v0.3.0` | Configuration, profiles, and path policy | Delivered |
| `v0.4.0` | Validation engine and diagnostics | Delivered |
| `v0.5.0` | Markdown query and address foundations | Delivered |
| `v0.6.0` | Search | Delivered |
| `v0.7.0` | Structural Markdown editing and ownership | Delivered |
| `v0.8.0` | Deterministic maintenance | Delivered |
| `v0.9.0` | Workflow completeness | Delivered |
| `v0.10.0` | Pre-1.0 governance hardening and roadmap correction | Delivered |
| `v0.11.0` | Stable-release hardening and trust fixes | Delivered |
| `v0.12.0` | CLI fidelity, governance deepening, and Markdown subsystem completion | Delivered |
| `v0.13.0` | Artifact type schema RFC and base implementation | Delivered |
| `v0.14.0` | Release automation and public pre-1.0 distribution discipline | Delivered |
| `v0.15.0` | JSON envelope, Markdown split/extract, release alignment, and config/explainability trust | Delivered |
| `v0.16.0` | Adoption readiness, onboarding trust, contract hardening, and rule/runtime coherence | Delivered |

## Planned Pre-1.0 Milestones

| Version   | Theme                                                          | Primary outcome                                                                                              |
|-----------|----------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| `v0.17.0` | Hosted release evidence and machine contract hardening | First hosted CI/release evidence, universal JSON contract guarantees, deeper machine handoff fidelity, and targeted pre-stable follow-ons |

## First Stable Release

| Version | State | Condition |
|---------|-------|-----------|
| `v1.0.0` | Not scheduled | Requires explicit authorization per ADR-013 plus green evidence from the active readiness plan |

## Deferred RFCs (Not Scheduled)

The following RFCs are accepted in principle but deferred until the pre-1.0 trust floor is established and adoption evidence justifies scheduling:

| RFC | Topic | Prerequisites |
|-----|-------|---------------|
| [RFC-009](../decisions/rfcs/RFC-009-typed-resource-addresses-and-search-alignment.md) | Typed resource addresses and search alignment | Stronger pre-1.0 foundations and clearer type/address design |
| [RFC-012](../decisions/rfcs/RFC-012-heading-level-markdown-refactors.md) | Heading-level Markdown refactors | Stable selector infrastructure, proven Markdown refactoring surfaces |
| [RFC-013](../decisions/rfcs/RFC-013-governed-suppressions-and-expiring-debt.md) | Governed suppressions and expiring debt | Stable policy schema, adoption evidence for structured exception management |

Additional future enhancement concepts that do not yet warrant RFC-grade design authority are tracked in [future-enhancements-backlog.md](future-enhancements-backlog.md).

## Notes

- The release-process completion work now lives on the active pre-1.0 line; it does not imply that `1.0.0` is scheduled or authorized.
- The exact boundary between later pre-1.0 milestones may continue to move as stable-release criteria are clarified, but the roadmap must stay on the `0.x` line until that decision is made.

---

## v0.16.0 Delivered Scope

All locally finishable items planned for `v0.16.0` are now implemented and verified. The primary outcome: Steward now has a tested first-hour onboarding path, agent-safer JSON surfaces, clearer help/runtime behavior, deeper `config suggest` trust, and family-governance behavior that stays coherent for explicit artifacts and frontmatter-sensitive reporting.

| # | Item | Source | Status |
|---|------|--------|--------|
| 1 | Add a tested README "First 15 Minutes" path | Fresh-eyes onboarding audit, synthesis `SYN-01` | Done — README now provides a repo-independent first-value sequence: install/build -> target repo -> `orient` -> `init` -> `status --coverage` -> `check` |
| 2 | Document repo-independent source-build usage and the `global.json` hazard | Fresh-eyes onboarding audit, synthesis `SYN-01` | Done — source-build guidance now prefers a built executable or local `--tool-path` install and warns against target-repo SDK pinning surprises |
| 3 | Fix rule-system trust defects in scoped and family-based validation | Rule-system completeness audit, synthesis `SYN-04` | Done — STWD-008/STWD-011/STWD-015 behavior hardened, STWD-012 is self-contained, and STWD-006 remains narrowed to managed-scope anomalies |
| 4 | Fix `refactor move --apply --output json` and add postcondition tests | AI-agent contract review, synthesis `SYN-02` | Done — JSON mode now performs the move, rewrites references, and reports the postcondition shape |
| 5 | Improve `config suggest` precision for mature repos | RFC-007 G7-20, synthesis `SYN-07` | Done — suggestions now honor path-override-style exclusions, emit confidence hints, and mark conservative inferences |
| 6 | Fix help text: public command identity and value placeholders | Fresh-eyes onboarding audit, synthesis `SYN-06` | Done — runtime help uses `steward`, and value placeholders are restored on `--config`, `--artifact`, `--role`, and `--max` |
| 7 | Tighten status, summary, and diagnostic language coherence | Fresh-eyes onboarding audit, rule-system completeness audit, synthesis `SYN-06` | Done — `check` now distinguishes warning-bearing passes, `orient --signals` explains its cheap/non-exhaustive semantics, and noisy diagnostics were tightened |
| 8 | Resolve explicit-artifact / family-schema inheritance | Assessment finding 4, RFC-008 | Done — explicit artifacts now inherit family frontmatter/section/naming/min-count governance while scoped path rules can still provide local allowed-value exceptions |
| 9 | Improve Markdown subsystem help and examples | Assessment finding, RFC-004 | Done — `md`/`md edit` help and README examples now show practical selector and edit flows instead of assuming prior MdPath knowledge |

Hosted CI and tag-driven release evidence remain open in the active readiness plan because they can only close once the pushed `v0.16.0` tag runs remotely.

## v0.17.0 Planned Scope

Primary outcome: hosted release evidence and machine contract hardening. This milestone should finish the remaining pre-stable trust floor after the local `v0.16.0` adoption/runtime pass.

| # | Item | Source | Notes |
|---|------|--------|--------|
| 1 | Capture the first hosted green CI and tag-driven release runs | Readiness tracker, release process | Close the remaining operational evidence gap with a real hosted matrix pass, GitHub Release, and NuGet publication record |
| 2 | Standardize the JSON envelope on every JSON-capable command and expected failure path | AI-agent contract review, synthesis `SYN-03` | `--output json` must become a guaranteed contract, not a mostly-complete convention |
| 3 | Deepen `explain path`, `refs`, and `search` handoff surfaces | AI-agent contract review, synthesis `SYN-03` | Add richer provenance/exists fields and improve machine handoff fidelity ahead of typed-address work |
| 4 | Publish machine-facing contract docs and broaden contract tests | AI-agent contract review | Add coverage for standard-envelope success/failure paths and mutation postconditions |
| 5 | Scope and implement the first narrow RFC-009 slice | RFC-009, AI-agent contract review | Focus on reusable address handoff across `search`, `refs`, `check`, and `explain path` rather than a broad address model all at once |
| 6 | Decide whether to open an adoption-oriented config-model RFC | Config expressiveness stress test, synthesis `SYN-08` | Only after the trust floor above lands; constrain scope to non-Markdown transparency, intentionally ungoverned zones, and grandfathering/new-files-only |

---

## v0.15.0 Delivered Scope

All items planned for v0.15.0 have been implemented and aligned into a coherent pre-1.0 release-ready baseline. The primary outcome: Steward now has the intended `0.15.0` version story, machine-facing envelope work, Markdown split/extract support, anchor-aware Markdown selectors, stricter generated-index governance, and an activated NuGet publication path.

| # | Item | Source | Status |
|---|------|--------|--------|
| 1 | Standard JSON output envelope support | RFC-010 | Done — `--json-envelope standard` supported on JSON-producing commands |
| 2 | Markdown split planning and extract-section workflows | RFC-011 | Done — `md split plan` and `md edit extract-section` implemented with preview/apply safety |
| 3 | Apply `validation.severity_overrides` at runtime | Config/runtime trust pass | Done — configured severities now affect emitted diagnostics and pass/fail outcomes |
| 4 | Fix `explain path` family applicability | Explainability trust pass | Done — family-only rules now surface only when the file actually matches a family |
| 5 | Support Markdown anchor-style selectors | Maintainer remarks pass | Done — `#anchor-slug` and `README.md#anchor-slug` work in `md query` |
| 6 | Enforce unique normalized heading text per file | Maintainer remarks pass | Done — STWD-017 warns when anchor-normalized headings collide |
| 7 | Require description frontmatter for generated directory indexes | Maintainer remarks pass | Done — generated indexes block on missing descriptions and the repo now dogfoods them in `decision-index.md` |
| 8 | Update existing frontmatter date fields on local change | Maintainer remarks pass | Done — `governance.frontmatter.auto_fields` synthesizes `today-if-local-change` maintenance |
| 9 | Align package identity to `Steward` and activate NuGet publication | ADR-009, maintainer remarks pass | Done — package id renamed before first public publish and tag workflow now pushes to nuget.org |
| 10 | Reconcile repo version/package/release story to `0.15.0` | Fresh-eyes onboarding audit, synthesis `SYN-05` | Done — shared version metadata, changelog, README, release docs, and current-state docs now agree |

---

## v0.14.0 Delivered Scope

All items planned for v0.14.0 have been implemented and are releaseable on the pre-1.0 line. The primary outcome: Steward now has a professional, documented, downloadable public `0.x` release path.

| # | Item | Source | Status |
|---|------|--------|--------|
| 1 | Clarify ADR-013 to distinguish public `0.x` releases from separately gated `1.0.0` | ADR-013 | Done — public pre-1.0 release governance made explicit |
| 2 | Add repo-managed release-intent labels | Release-process pass | Done — `release:none`, `release:patch`, `release:minor` codified and synchronized |
| 3 | Enforce release-intent labels on non-draft PRs | Release-process pass | Done — `pr-release-intent.yml` |
| 4 | Add tag-driven GitHub Release workflow | Release-process pass | Done — builds/tests, exports changelog-backed notes, uploads assets |
| 5 | Attach useful downloadable release assets | ADR-009, release-process pass | Done — `.nupkg`, curated self-contained bundles, checksums |
| 6 | Create canonical changelog policy and file | Release-process pass | Done — `CHANGELOG.md` introduced as release-notes source |
| 7 | Add operator documentation for release execution | Release-process pass | Done — `release-process.md` and updated checklist/docs |
| 8 | Harden package metadata for publication surfaces | Release-process pass | Done — license, repo URL, tags, and publication metadata added to `Steward.Cli.csproj` |

---

## v0.13.0 Delivered Scope

All items planned for v0.13.0 have been implemented and tested. The primary outcome: policy can declare reusable artifact families, discovery classifies files deterministically, `steward check` enforces type-aware frontmatter, and `status`/`orient`/`explain path` surface those classifications.

| # | Item | Source | Status |
|---|------|--------|--------|
| 1 | Accept RFC-008; narrow §8 scope for v0.13.0 | ADR-012, RFC-008 | Done — RFC-008 status: Accepted; §8 added with explicit IN/NOT IN contract |
| 2 | `artifact_families` model classes in `RepositoryPolicy` | RFC-008 §8 | Done — `ArtifactFamilyDefinition`, `ArtifactFamilyMatch`, `ArtifactFamilyFrontmatterSchema` |
| 3 | `ArtifactFamilyClassifier` shared classification engine | RFC-008 §8 | Done — declaration-order first-match, AND semantics, path+frontmatter criteria |
| 4 | `ConfigLoader` validation for `artifact_families` | RFC-008 §8 | Done — validates glob syntax, duplicate names, blank fields, invalid importance |
| 5 | Extend STWD-003 with family-level schema enforcement | RFC-008 §8 | Done — required fields + allowed_values per family; `[family: name]` in diagnostics |
| 6 | `ProfileMerger` fix: preserve `ArtifactFamilies` | Bug | Done — was silently dropped through merge |
| 7 | `explain path` family awareness | RFC-008 §8 | Done — `Family: name (DisplayName)`, family-level required fields/values |
| 8 | `status` family summary | RFC-008 §8 | Done — text and JSON `Artifact Families:` / `artifactFamilies` section |
| 9 | `orient` family classification | RFC-008 §8 | Done — `family:{name}` classification in orientation tree |
| 10 | `config doctor` unreachable family patterns | RFC-008 §8 | Done — `unreachable-family-pattern` finding |
| 11 | Test fixture: `artifact-families` | Plan | Done — ADR and RFC fixture files with valid/invalid documents |
| 12 | Unit tests: classifier, validation, config loading | Plan | Done — 52 new tests across 3 test files |
| 13 | CLI integration tests: all artifact-family commands | Plan | Done — 14 tests in `ArtifactFamiliesCommandTests` |
| 14 | Dogfooding migration: `.steward/policy.yaml` | Plan | Done — ADR/RFC governance migrated from `frontmatter_requirements` to `artifact_families` |
| 15 | Deferred: workflow/session modeling follow-on work | RFC-008 §8 | Deferred to v0.15.0+ |

## v0.12.0 Delivered Scope

All items planned for v0.12.0 have been implemented and tested. Each addresses a known workflow gap or depth deficiency identified in the 2026-04-16 CLI review cycle.

| # | Item | Source | Status |
|---|------|--------|--------|
| 1 | Standardize preview/apply flag conventions | F2 | Done — `check --fix` previews, `--fix --apply` commits, `--dry-run` hidden deprecated |
| 2 | Fix `md query --pattern` batch mode | F4 | Done — argument parsing disambiguation |
| 3 | Fix init scaffolding immediate-failure experience | F5 | Done — placeholder files scaffolded for required artifacts |
| 4 | Deepen `explain path` provenance and filtering | F7, G7-05/06 | Done — per-rule applicability filtering |
| 5 | Deepen `config suggest` for mature repos | F6, G7-20 | Done — detects decisions, planning, state docs, subdirectory indexes |
| 6 | Deepen `config doctor` beyond baseline checks | F6, G7-07 | Done — dead suppressions, unreachable overrides, unreachable frontmatter patterns |
| 7 | Add `fm-validate` to Markdown edit subsystem | EF-005, REQ-MD-004 | Done — `md edit fm-validate <file>` validates against policy requirements |
| 8 | Exclude test fixtures from governance coverage | F16 | Done — `coverage.exclude` config section with glob patterns |
| 9 | Enrich deferred profiles (mixed, knowledge) | ADR-014 | Deferred — ADR-014 explicitly defers until contracts are enriched |
