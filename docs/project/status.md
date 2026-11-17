---
type: project
status: Active
summary: Current repository facts, implemented capability baseline, and known health gaps
last_updated: 2026-11-17
review_after: 2026-12-09
---

# Project Status

## Current Baseline

Steward's latest published version is **`v0.18.1`**, tagged and released to GitHub Releases and nuget.org. It was the first release published through nuget.org trusted publishing. **`v0.19.0`** is prepared and pending release; it is not published until its GitHub Release and nuget.org package are verified.

| Area | Current truth |
|------|---------------|
| Runtime | .NET 10 |
| Tests | 984 passing: 646 core and 338 CLI |
| Validation rules | 24 (`STWD-001` through `STWD-024`) |
| Auto-fix rules | 4 (`STWD-003`, `STWD-007`, `STWD-012`, `STWD-018`) |
| Built-in init profiles | `software`, `docs`, `minimal` |
| Release posture | `v0.18.1` published; `v0.19.0` pending release; public `0.x` releases allowed; `1.0.0` requires explicit authorization under ADR-013 |
| Generated repo artifacts | `STRUCTURE.md` and `docs/decisions/README.md` |

## Delivered In v0.18.0

- SARIF 2.1.0 output for `steward check`
- merge-base-aware `check --since <ref>` scope
- deterministic fragment-link auto-fix for STWD-018
- closed artifact-family frontmatter schemas and H1 title patterns (STWD-019)
- H2 section-heading pattern enforcement (STWD-020)
- ordered document section schemas (STWD-021)
- check-only SARIF enforcement, with repository output defaults limited to text or JSON

## Delivered In v0.18.1

A 2026-08-24 adoption trial ran Steward against two external repositories (`jvcode`, `mdrule`) and logged real friction; see the [maintainer configuration experience audit](../history/audits/maintainer-configuration-experience-audit-2026-08-24.md). Three defects it surfaced are already fixed:

- `steward maintain` now reports unmaintainable artifacts as `BLOCKED` instead of `OK`.
- `steward status --coverage` now counts `artifact_families[]` matches as governed.
- `config doctor` no longer flags `forbidden`/`reserved` path rules that match nothing as dead config.

One related defect from the same trial is still open: the same dead-config false positive extends to anticipatory `artifact_families` and `validation.path_overrides` (`.scratch/adoption-trial-defects/issues/01-doctor-accepts-anticipatory-config.md`). The rest of the trial's findings became tickets in the `.scratch/` efforts for configuration transparency, policy expressiveness and configuration documentation gaps — nothing further from it is scheduled yet.

The project was also relicensed under Apache-2.0, and routine dependency bumps have landed.

Release publication moved to nuget.org trusted publishing: the release workflow exchanges a GitHub OIDC token for a one-hour API key instead of reading a stored `NUGET_ORG_API_KEY` secret.

## Delivered In v0.19.0 (pending release)

The release notes are the `[0.19.0]` section of the [CHANGELOG](../../CHANGELOG.md):

- A bare relative `--config` directory no longer crashes discovery, and `init` honours `--config`.
- Rule phase-in through a baseline (RFC-017): `steward baseline generate` and `baseline status`, `check` suppressing grandfathered violations and reporting drift, and baseline checks in `config validate` and `config doctor`. The opt-in `--fail-on-baseline-growth` gate is deferred past this release.

Agent guidance and issue tracking:

- A user-invoked [setup-steward](../../.agents/skills/setup-steward/SKILL.md) skill guides first-time Steward setup in any repository. On releases newer than `0.18.1` it grandfathers existing violations with a baseline; on `0.18.1` and earlier it gates a rule in CI only when the repository has zero existing violations of it.
- The domain glossary convention was renamed from `CONTEXT.md` to `GLOSSARY.md` to match the installed engineering skills.
- Two `--config` defects found while building that skill were recorded as tickets in `.scratch/adoption-trial-defects/` and are now fixed.
- Issue tracking moved from `docs/project/backlog.md` to the local-markdown [issue tracker](../agents/issue-tracker.md) under `.scratch/`, using the same convention as the sibling `graphs-everywhere` and `jvcode` repositories; every backlog item became a ticket, and the old file is kept as a [snapshot](../history/plans/backlog-2026-11-09.md).

### Document contracts

Owner-authorized MPES adoption adds opt-in [document contracts](../guide/document-contracts.md):
budgets, identities/relationships, frozen source collections and bounded topic-index coverage.
Strict family vocabularies and plan-ordered shared-file maintenance ship with them.

## Current Health

- Build and all 984 tests pass locally (Release configuration, re-verified 2026-11-17).
- Markdown lint passes.
- The [test quality assessment](../history/audits/unit-test-quality-audit-2026-11-14.md) strengthened redaction and Git-scoping oracles and removed silent successful Git-test exits. Remaining risks include UTC-midnight date assertions and untested search I/O-failure handling.
- Latest inspected default-branch CI (2026-11-12, `88f36e5`) passes Linux/macOS but fails six Windows schema/completion tests at upstream commit `88f36e5`; local macOS results do not verify those Windows failures.
- Hosted build, release, and NuGet publication evidence is established through `v0.18.1`.
- The repository remains pre-1.0; no stable-release authorization has been accepted.
- The current milestone, rule phase-in and baseline, ships in `v0.19.0`; the baseline-growth gate is deferred — see the [roadmap](roadmap.md).

## Known Gaps

- A few expected-failure JSON paths still need contract review before the machine-facing surface can be called universal.
- The first typed-resource-address implementation slice from RFC-009 is not scheduled as delivered work.
- `mixed` and `knowledge` profiles remain intentionally unavailable through `init`.
- `search --role` still matches explicit artifact declarations, not all family-classified files.
- Nothing yet stops a regenerated baseline from absorbing new debt; the opt-in `--fail-on-baseline-growth` gate is still `needs-triage` (`.scratch/rule-phase-in-baseline/issues/08-fail-on-baseline-growth.md`).

Delivered release details belong in [CHANGELOG.md](../../CHANGELOG.md). Current and next work belongs in the [roadmap](roadmap.md); individual work items belong in the [issue tracker](../agents/issue-tracker.md).
