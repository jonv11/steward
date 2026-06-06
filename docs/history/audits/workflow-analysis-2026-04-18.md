---
type: audit
status: Historical
standalone: true
---
# Workflow Model Analysis

Date: 2026-04-18

## Purpose

This document records the analysis behind the [Workflow Guide](../../project/workflow-guide.md). It captures what was learned from git history, which historical patterns were kept or corrected, and why the final workflow model was chosen.

This is a supporting artifact — the canonical process reference is `docs/planning/workflow-guide.md`.

## Methodology

- Full git history analysis (82 commits across 5 days of active development, 2 release tags).
- Conventional commit type categorization.
- File co-change pattern analysis.
- Review of all existing process documentation.
- Cross-referencing current docs for contradictions and drift.

## Git History Findings

### Commit Type Distribution

| Type | Count | % |
|------|-------|---|
| chore | 21 | 26% |
| feat | 16 | 20% |
| fix | 3 | 4% |
| refactor | 3 | 4% |
| docs | 2 | 2% |
| test | 1 | 1% |
| non-conventional | 36 | 44% |

### Non-Conventional Commit Sources

The 36 non-conventional commits break down as:

- **G7-xx prefixed** (~20): Rapid backlog execution of RFC-007 governance enhancements. Used a custom prefix format instead of conventional commits. Effective for tracking but breaks tooling expectations.
- **Dependabot merges** (~12): Auto-generated merge commits and bump messages. Expected and unavoidable.
- **Bare descriptive messages** (~4): Release preparation, orientation improvements. Missing the `type:` prefix.

### Work Pattern Categories Observed

1. **Feature sprints** — G7-01 through G7-20 show rapid serial feature delivery against a backlog. Each commit added a governance enhancement with tests. Pattern: `src/ + tests/ + sometimes .steward/policy.yaml`.

2. **Review/remediation cycles** — Six numbered review passes (review-01 through review-06) each produced audit findings followed by remediation commits. Pattern: `docs/audits/ + docs/planning-index.md`, then separate `src/ + tests/` remediation commits.

3. **Release execution** — Two tagged releases (v0.14.0, v0.15.0). Pattern: `Directory.Build.props + CHANGELOG.md + docs/implementation-status.md + tag`. Both followed a similar sequence.

4. **Audit curation** — Consolidating, synthesizing, and retiring historical audit documents. Pattern: `docs/audits/ + docs/planning-index.md + STRUCTURE.md`.

5. **Dependency updates** — Dependabot PRs for NuGet packages and GitHub Actions. Pattern: automated PRs with merge commits.

6. **Agent guidance** — AGENTS.md creation and skill file updates. Pattern: `AGENTS.md + .agents/skills/ + CONTRIBUTING.md + .steward/policy.yaml`.

7. **CI/CD changes** — Workflow additions and modifications. Pattern: `.github/workflows/ + sometimes CONTRIBUTING.md or README.md`.

8. **Governance evolution** — `.steward/policy.yaml` changed in 15 of 82 commits (18%), often bundled with feature work.

### Co-Change Patterns

| Files that frequently change together | Implication |
|---------------------------------------|-------------|
| `src/**` + `tests/**` | Code changes always need test changes |
| `CHANGELOG.md` + feature/fix commits | Changelog discipline is mostly followed |
| `STRUCTURE.md` + structural changes | Generated artifact refresh is practiced |
| `docs/planning-index.md` + new docs | Navigation linking is practiced |
| `.steward/policy.yaml` + feature commits | Governance evolves with features in this self-dogfooded repo |
| `docs/implementation-status.md` + release commits | State doc updates at release time |

### Gaps Identified

1. **No canonical workflow guide.** Process knowledge was distributed across AGENTS.md (agent-focused), CONTRIBUTING.md (setup-focused), release-process.md (release-focused), and implementation-instructions.md (priority-focused). No single document answered "how do I do X correctly?"

2. **Post-change consistency steps were implicit.** The finalization sequence (`steward maintain` → `npm run lint:md` → `steward check`) was documented in AGENTS.md and CONTRIBUTING.md but not consolidated into a single reusable checklist.

3. **Version drift.** `docs/planning/implementation-instructions.md` still claims `v0.14.0` as the baseline while `docs/implementation-status.md` reports `v0.15.0`.

4. **Vague commit messages.** Review commits used `chore: review-01` through `chore: review-06` without describing what was reviewed or remediated. Several `chore: maintenance` commits provide no insight into what was maintained.

5. **No explicit review/audit workflow.** Reviews happened frequently but the two-phase pattern (conduct review → implement remediations) was not documented anywhere.

6. **No governance config change workflow.** `.steward/policy.yaml` changed in 18% of commits but the expected validation sequence (`config validate` → `config doctor` → `config show --effective` → `check`) was only documented in the CLI skill file, not in a contributor workflow.

## Decisions Made

### Patterns Kept and Formalized

| Historical pattern | Decision |
|-------------------|----------|
| Conventional Commits format | **Kept.** Formalized with quality rules requiring specificity. |
| CHANGELOG maintenance with [Unreleased] section | **Kept.** Already well-documented in release-process.md. |
| `steward check` as final validation gate | **Kept.** Made explicit in shared finalization checklist. |
| `steward maintain` for generated artifacts | **Kept.** Made explicit in shared finalization checklist. |
| Release process with tag-driven automation | **Kept.** Already well-documented; workflow guide links to release-process.md. |
| `docs/planning-index.md` as navigation hub | **Kept.** Workflow guide requires linking new docs there. |
| Two-phase review/remediation pattern | **Kept and formalized.** Was practiced but undocumented. |
| Test co-changes with code changes | **Kept.** Made explicit in feature, bug fix, and refactoring workflows. |

### Patterns Corrected

| Historical pattern | Correction | Rationale |
|-------------------|------------|-----------|
| G7-xx commit prefix format | **Retired.** All commits must use Conventional Commits. | Custom prefixes break tooling and are not discoverable by contributors unfamiliar with the repo's history. Backlog item IDs can go in commit body or PR description. |
| Vague `chore: review-NN` messages | **Corrected.** Review commits must describe what was reviewed. | Sequential numbers provide no information about content. |
| Vague `chore: maintenance` messages | **Corrected.** All commits must describe what was done. | Generic labels make history useless for debugging and auditing. |
| Implicit finalization steps | **Consolidated.** Single shared checklist in workflow guide. | Steps were scattered across 3+ documents. |
| Governance config changes bundled with features | **Acceptable when necessary** but called out as a distinct workflow when the config change is the primary intent. | Self-dogfooded repos naturally evolve governance with features, but standalone config changes need their own validation sequence. |

### Patterns Not Preserved

| Historical pattern | Why not preserved |
|-------------------|-------------------|
| Mega-commits bundling review + remediation + feature work | Violates one-logical-change-per-commit principle. Review records and remediation fixes should be separate commits. |
| Non-conventional commit messages on non-Dependabot commits | Non-standard format provides no benefit and breaks changelog generation and history scanning. |
| Bare "Prepare vX.Y.Z release" commit messages | Should use `chore(release): prepare vX.Y.Z` for consistency. |

### Workflow Categories Evaluated But Not Included

| Category | Decision | Rationale |
|----------|----------|-----------|
| Performance optimization | Not included as separate workflow | Follows feature or refactoring workflow depending on whether behavior changes. Too rare in this repo to justify its own section. |
| Security fix | Not included as separate workflow | Follows bug fix workflow with urgency priority. The repo has no runtime attack surface that would require a distinct security workflow. |
| Database migration | Not applicable | No database in this project. |

## Workflow Model Design Rationale

### Why a single canonical guide

The repo had process knowledge in at least 5 documents (AGENTS.md, CONTRIBUTING.md, release-process.md, implementation-instructions.md, SKILL.md). Each served a different audience or purpose, but the overlap created contradiction risk and made it hard to answer "what's the right way to do X?"

The workflow guide is the single process authority. Other documents should link to it rather than restate process steps. AGENTS.md and CONTRIBUTING.md retain their audience-specific framing but delegate process detail to the workflow guide.

### Why a shared finalization checklist

Every workflow ended with some variation of "build, test, lint, check." Rather than repeating this in each workflow, a single checklist at the top of the guide reduces duplication and ensures consistency. Workflows reference it by name.

### Why 11 workflows

The initial analysis identified 12+ potential categories. After consolidation:

- Feature, bug fix, and refactoring are distinct because they have different commit types, different testing expectations, and different CHANGELOG requirements.
- Documentation, planning, and agent guidance are distinct because they have different frontmatter requirements and different navigation obligations.
- Review/audit is distinct because it has a unique two-phase pattern.
- Release is distinct because it has its own authoritative operator guide.
- Governance config is distinct because it has a unique validation sequence.
- Dependency update and CI/CD are distinct because they have minimal process overhead but clear boundaries.

ADR/RFC creation is handled as a sub-section of documentation because the steps overlap heavily, with only frontmatter and naming differences.

## Document Relationship Changes

| Document | Change | Rationale |
|----------|--------|-----------|
| `docs/planning/workflow-guide.md` | **Created.** Canonical workflow reference. | Central gap in the repo. |
| `docs/audits/workflow-analysis-2026-04-18.md` | **Created.** This document. Supporting evidence. | Records the reasoning behind the workflow model. |
| `AGENTS.md` | **Updated.** Links to workflow guide. Process details replaced with link. | Avoid duplication; single source of truth. |
| `CONTRIBUTING.md` | **Updated.** Links to workflow guide. | Contributor workflow details now live in the guide. |
| `docs/planning-index.md` | **Updated.** Links to workflow guide and this analysis. | Discoverability. |
| `docs/planning/implementation-instructions.md` | **Updated.** Fixed stale version baseline; links to workflow guide. | Version drift was a real gap. |
