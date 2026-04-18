---
type: planning
document_id: PLAN-0002
version: 0.15.0
status: Active
last_updated: 2026-04-18
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
| `v0.15.0` | JSON envelope, Markdown split/extract, and config/explainability trust | Delivered |

## Planned Pre-1.0 Milestones

| Version   | Theme                                                          | Primary outcome                                                                                              |
|-----------|----------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------|
| `v0.16.0` | Adoption readiness, onboarding trust, and rule/runtime coherence | Tested first-hour onboarding, version-story alignment, rule/runtime trust fixes, `config suggest` precision, and UX/help polish |
| `v0.17.0` | Machine contract hardening and targeted pre-stable extensions  | Universal JSON contract behavior, safe JSON mutation/reporting, typed-address handoff follow-on, and scoped config-model/RFC follow-ups if justified |

## First Stable Release

| Version | State | Condition |
|---------|-------|-----------|
| `v1.0.0` | Not scheduled | Requires explicit authorization per ADR-013 plus green evidence from the active readiness plan |

## Notes

- Former `v1.1.0` through `v1.6.0` planning has been logically retargeted to `v0.11.0` through `v0.16.0`.
- The release-process completion work now lives on the active pre-1.0 line; it does not imply that `1.0.0` is scheduled or authorized.
- The exact boundary between later pre-1.0 milestones may continue to move as stable-release criteria are clarified, but the roadmap must stay on the `0.x` line until that decision is made.

---

## v0.16.0 Planned Scope

Primary outcome: adoption readiness, onboarding trust, and rule/runtime coherence. Refreshed by the 2026-04-18 review synthesis so the next milestone reflects validated current gaps rather than older partially stale carryover items.

| # | Item | Source | Notes |
|---|------|--------|--------|
| 1 | Add a tested README "First 15 Minutes" path | Fresh-eyes onboarding audit, synthesis `SYN-01` | Make the first value path explicit: install or repo-independent executable path -> target repo -> `orient` -> `init` -> `status --coverage` -> `check` |
| 2 | Document repo-independent source-build usage and the `global.json` hazard | Fresh-eyes onboarding audit, synthesis `SYN-01` | Prefer a built executable or local `--tool-path` install; state that `dotnet run --project ...` can fail when the target repo controls SDK selection |
| 3 | Reconcile runtime/package/version-story drift | Fresh-eyes onboarding audit, synthesis `SYN-05` | README, changelog, `steward version`, and planning/status artifacts must tell the same version/baseline story before the next release-oriented doc pass |
| 4 | Fix rule-system trust defects in scoped and family-based validation | Rule-system completeness audit, synthesis `SYN-04` | Correct STWD-008, STWD-011, and STWD-015 behavior; make STWD-012 diagnostics self-contained; narrow STWD-006 if the fix stays bounded |
| 5 | Fix `refactor move --apply --output json` and add postcondition tests | AI-agent contract review, synthesis `SYN-02` | JSON mode must execute the move, rewrite references, and prove what changed |
| 6 | Improve `config suggest` precision for mature repos | RFC-007 G7-20, synthesis `SYN-07` | Honor `validation.path_overrides`-style exclusions, add confidence/heuristic scoring, and label conservative suggestions as such |
| 7 | Fix help text: public command identity and value placeholders | Fresh-eyes onboarding audit, synthesis `SYN-06` | Pin command display name to `steward` instead of `Steward.Cli`; restore value placeholders on `--config`, `--artifact`, `--role`, `--max` |
| 8 | Tighten status, summary, and diagnostic language coherence | Fresh-eyes onboarding audit, rule-system completeness audit, synthesis `SYN-06` | Reword warning-heavy `check` summaries, clarify `orient --signals`, and clean up the highest-noise messages |
| 9 | Resolve explicit-artifact / family-schema inheritance | Assessment finding 4, RFC-008 | Explicit artifacts currently do not inherit family `frontmatter_schema`, forcing duplicate `frontmatter_requirements` in policy; design and implement a clean inheritance or merge model |
| 10 | Improve Markdown subsystem help and examples | Assessment finding, RFC-004 | Add operational selector examples to `md query` / `md edit` help; reduce "already know MdPath" assumption in subcommand descriptions |

## v0.17.0 Planned Scope

Primary outcome: machine contract hardening and targeted pre-stable extensions. This milestone should finish the machine-facing trust floor before Steward takes on broader config-model growth.

| # | Item | Source | Notes |
|---|------|--------|--------|
| 1 | Standardize the JSON envelope on every JSON-capable command and failure path | AI-agent contract review, synthesis `SYN-03` | `--output json --json-envelope standard` must guarantee standard-envelope JSON on stdout for both success and expected failure |
| 2 | Separate process success from domain result and add structured error kinds | AI-agent contract review, synthesis `SYN-03` | Keep validation/result booleans in payload data; add typed error envelopes with recovery hints |
| 3 | Deepen `explain path`, `refs`, and `search` handoff surfaces | AI-agent contract review, synthesis `SYN-03` | Add `exists`/provenance fields and improve machine handoff fidelity ahead of typed-address work |
| 4 | Scope and implement the first narrow RFC-009 slice | RFC-009, AI-agent contract review | Focus on reusable address handoff across `search`, `refs`, `check`, and `explain path` rather than a broad address model all at once |
| 5 | Publish machine-facing contract docs and broaden contract tests | AI-agent contract review | Add coverage for standard-envelope success/failure paths and mutation postconditions |
| 6 | Decide whether to open an adoption-oriented config-model RFC | Config expressiveness stress test, synthesis `SYN-08` | Only after the trust floor above lands; constrain scope to non-Markdown transparency, intentionally ungoverned zones, and grandfathering/new-files-only |

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
