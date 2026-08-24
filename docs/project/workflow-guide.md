---
type: project
status: Active
summary: Canonical contributor and agent workflow for all categories of repository work
last_updated: 2026-08-24
---

# Workflow Guide

---

## Purpose

This document is the canonical reference for how work should be performed in the Steward repository. It defines the expected workflow for each category of change, including entry criteria, required steps, validation, and definition of done.

All contributors — human and agent — should follow these workflows. When other documents describe process, this guide takes precedence. When in doubt, follow the workflow here and flag any contradiction.

## How to Use This Guide

1. Identify which workflow applies to your change.
2. Follow its steps in order.
3. Run the [shared finalization checklist](#shared-finalization-checklist) before finishing.
4. Commit using [Conventional Commits](#commit-conventions).

Most changes follow one primary workflow. If your change spans multiple categories (e.g., a feature that also requires a planning doc update), follow the primary workflow and incorporate the relevant steps from the secondary workflow.

## Shared Finalization Checklist

Every change — regardless of workflow — must pass this checklist before it is considered done.

### After any change

```bash
steward check                  # must exit 0
```

### After Markdown or structural changes

```bash
steward maintain --artifact structure --apply   # refresh STRUCTURE.md
steward maintain --apply                        # refresh decision indexes if ADRs/RFCs changed
npm run lint:md                                 # Markdown lint must pass
steward check                                   # re-check after maintenance
```

### After C# source changes

```bash
dotnet build steward.sln       # must compile clean
dotnet test steward.sln        # all tests must pass
steward check                  # must exit 0
```

### After .steward/ config changes

```bash
steward config validate        # no syntax or semantic errors
steward config doctor          # no ineffective declarations
steward config show --effective # review merged runtime policy
steward check                  # must exit 0
```

### Cross-cutting expectations

- Link active Markdown from `docs/README.md`, `docs/project/README.md`, or the generated decision index. Historical evidence must declare `standalone: true`.
- Never hand-edit `STRUCTURE.md` or the managed sections in `docs/decisions/README.md`; run `steward maintain`.
- When a change affects current facts or committed scope, update `docs/project/status.md` or `docs/project/roadmap.md` in the same change.

---

## Commit Conventions

Use [Conventional Commits](https://www.conventionalcommits.org/) for all commits:

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

| Type | When to use |
|------|------------|
| `feat` | New feature or capability |
| `fix` | Bug fix |
| `docs` | Documentation-only change |
| `test` | Test-only change (new tests or fixing existing tests) |
| `refactor` | Code restructuring with no behavior change |
| `chore` | Maintenance, dependency updates, CI config, tooling |
| `ci` | CI/CD pipeline changes |

Breaking changes use a `!` suffix (e.g., `feat!:`) or a `BREAKING CHANGE:` footer.

**Commit message quality rules:**

- The description must be specific enough to understand the change without reading the diff.
- Avoid vague messages like `chore: maintenance` or `chore: updates`. State what was maintained or updated.
- Scope tags are encouraged for targeted changes: `feat(validation):`, `fix(orient):`, `chore(deps):`.
- One logical change per commit. Do not bundle unrelated work.

---

## Workflows

### 1. Feature Implementation

**Objective:** Add a new capability, command, validation rule, or behavioral enhancement to the Steward CLI or core library.

**When to use:** Any change that adds or extends product functionality.

**Prerequisites:**

- The feature is aligned with `docs/project/roadmap.md` or an accepted RFC/ADR.
- You have read `docs/project/status.md` and `docs/project/roadmap.md`.
- You have reviewed any relevant RFC or ADR for design authority.

**Steps:**

1. **Orient.** Run `steward orient --signals` and read the project status and roadmap.
2. **Branch or work on dev.** This repo works on the `dev` branch. Create a feature branch if the change is large or exploratory.
3. **Implement.** Write the feature code under `src/`. Follow existing patterns in the codebase.
4. **Test.** Add or update tests under `tests/`. All existing tests must continue to pass.
5. **Update CHANGELOG.** Add an entry under `## [Unreleased]` in `CHANGELOG.md` describing the feature.
6. **Update docs.** If the feature changes user-visible behavior, update `README.md`. If it changes current capability or committed scope, update `docs/project/status.md` or `docs/project/roadmap.md`.
7. **Run finalization checklist.** Build, test, lint, `steward check`.

**Commit type:** `feat:` or `feat(scope):`

**Definition of done:**

- Code compiles and all tests pass.
- CHANGELOG entry exists under `[Unreleased]`.
- `steward check` exits 0.
- Affected docs are updated in the same change.
- Commit message clearly describes the feature.

**Anti-patterns:**

- Implementing without checking if an RFC/ADR governs the design.
- Skipping CHANGELOG entry ("I'll add it later").
- Leaving project status or roadmap stale after changing capability or scope.
- Bundling unrelated fixes or refactoring into a feature commit.

---

### 2. Bug Fix

**Objective:** Correct defective behavior in the CLI, core library, or governance configuration.

**When to use:** The system produces incorrect output, crashes, validates incorrectly, or behaves contrary to documented intent.

**Steps:**

1. **Reproduce.** Confirm the bug exists. Write a failing test if possible.
2. **Fix.** Make the minimal change to correct the behavior.
3. **Test.** Ensure the new or updated test passes. All existing tests must still pass.
4. **Update CHANGELOG.** Add an entry under `## [Unreleased]`.
5. **Run finalization checklist.**

**Commit type:** `fix:` or `fix(scope):`

**Definition of done:**

- The defect is corrected and covered by a test.
- CHANGELOG entry exists.
- `steward check` exits 0.
- No unrelated changes bundled.

**Anti-patterns:**

- Fixing a bug and also refactoring surrounding code in the same commit.
- Fixing without adding a regression test.

---

### 3. Refactoring and Cleanup

**Objective:** Improve code structure, reduce duplication, or address technical debt without changing external behavior.

**When to use:** Code quality improvements, extract-method, rename, reorganize modules, address static analysis findings.

**Steps:**

1. **Scope the refactoring.** Define what you are changing and confirm it does not alter behavior.
2. **Refactor.** Make the structural changes.
3. **Test.** All existing tests must pass without modification (unless test structure itself needs updating to match the refactoring).
4. **Run finalization checklist.**

**Commit type:** `refactor:` or `refactor(scope):`

**Definition of done:**

- All tests pass.
- No behavioral change.
- `steward check` exits 0.
- Commit message explains what was restructured and why.

**Anti-patterns:**

- Sneaking behavior changes into a refactoring commit.
- Refactoring areas unrelated to your current task.
- Skipping tests because "it's just a refactor."

---

### 4. Documentation Update

**Objective:** Improve, correct, or extend repository documentation without changing code.

**When to use:** Fixing stale docs, improving clarity, adding missing docs, correcting links, updating planning artifacts.

**Prerequisites:**

- For governed docs under `docs/project/`, `docs/requirements/`, `docs/decisions/`, or `docs/history/`, run `steward explain path <file>` before editing.

**Steps:**

1. **Edit the document.** Follow existing Markdown conventions and required frontmatter schemas.
2. **Declare navigation intent.** Link active documents from `docs/README.md` or `docs/project/README.md`; decision records enter the generated index; historical records declare `standalone: true`.
3. **Run finalization checklist.** Include `steward maintain --artifact structure --apply` and `npm run lint:md`.

**Commit type:** `docs:`

**Definition of done:**

- `npm run lint:md` passes.
- `steward check` exits 0.
- New documents are linked from a governed navigation surface.
- Frontmatter is valid for the document's family.

**Anti-patterns:**

- Creating a document without linking it (orphan — violates STWD-013).
- Hand-editing `STRUCTURE.md` or managed sections in `docs/decisions/README.md`.
- Leaving `last_updated` stale in frontmatter after significant edits (steward auto-maintains this when configured).

---

### 5. Review and Audit Remediation

**Objective:** Conduct a review or audit of repo state, record findings, and implement remediations.

**When to use:** Scheduled quality passes, onboarding audits, release readiness assessments, architecture reviews, or responding to external review feedback.

This workflow has two phases: the review itself and the remediation.

#### Phase A — Conduct the Review

1. **Conduct the review.** Keep working notes local to the branch until findings are stable.
2. **Record findings** with clear, actionable items. Distinguish between errors (must fix), warnings (should fix), and observations (consider).
3. **Promote current truth.** Move accepted actions into status, roadmap, backlog, a decision record, or an issue before archiving the review.
4. **Archive the evidence.** Put broad repository/system assessments in `docs/history/audits/`; put targeted contract/config/artifact reviews in `docs/history/reviews/`. Use `status: Historical` and `standalone: true`.
5. **Run finalization checklist.**
6. **Commit** with `chore:` or `docs:` and a descriptive message.

#### Phase B — Implement Remediations

1. **Triage findings.** Not every finding requires immediate action. Prioritize errors, then warnings.
2. **Implement fixes** using the appropriate workflow for each fix type (feature, bug fix, docs, etc.).
3. **Do not turn the archived review into an active tracker.** Track live work in roadmap, backlog, issues, or implementation changes.
4. **Commit remediations separately** from the review itself. Each remediation should be its own commit with the appropriate type (`fix:`, `feat:`, `docs:`, etc.) and a message that references the review (e.g., `fix: correct STWD-009 false positive identified in release-readiness review`).

**Definition of done:**

- Review evidence is archived with lifecycle metadata after current actions are promoted.
- All error-level findings are remediated or have a tracked deferral.
- Remediation commits reference the review they address.
- `steward check` exits 0.

**Anti-patterns:**

- Bundling the review and all remediations into a single massive commit.
- Using vague commit messages like `chore: review-03` — state what the review covered and what was remediated.
- Treating audit docs as current truth without verifying against live code and docs.

---

### 6. Release Execution

**Objective:** Cut and publish an intentional pre-1.0 release.

**When to use:** When accumulated changes justify a public release and the release criteria in [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) are met.

**Prerequisites:**

- All intended changes are merged and passing.
- The current release boundary in [roadmap.md](roadmap.md) is satisfied or explicitly deferred.
- The authoritative release process is documented in [release-process.md](release-process.md).
- The full operator checklist is in [release-publication-checklist.md](release-publication-checklist.md).

**Steps (summary — defer to release-process.md for full detail):**

1. **Decide the version bump** based on merged release-intent labels and ADR-013.
2. **Update `Directory.Build.props`** to the target version.
3. **Update `CHANGELOG.md`** — move `[Unreleased]` entries to a new dated version section.
4. **Update current truth** — `docs/project/status.md`, `docs/project/roadmap.md`, and `README.md` if the version baseline changes.
5. **Run local verification** per [release-publication-checklist.md](release-publication-checklist.md).
6. **Create and push an annotated tag** matching the version (e.g., `v0.15.0`).
7. **Verify post-release** — GitHub Release assets, nuget.org appearance, checksums.
8. **Refresh generated artifacts** — `steward maintain --apply`.

**Commit type:** The release commit message should be `chore(release): prepare v<VERSION>`.

**Definition of done:**

- Tag matches `Directory.Build.props` version.
- CHANGELOG has a dated section for the release.
- GitHub Release exists with all expected assets.
- State docs reflect the new version baseline.
- `steward check` exits 0 on the tagged commit.

**Anti-patterns:**

- Bumping the version without following ADR-013.
- Tagging before local verification passes.
- Forgetting to update project status and roadmap post-release.
- Introducing `1.x` version references without stable-release authorization.

---

### 7. Governance Configuration Update

**Objective:** Modify the `.steward/` governance configuration — policy rules, path policies, artifact declarations, maintenance definitions, or family schemas.

**When to use:** Adding governed artifacts, changing frontmatter requirements, adjusting validation rules, updating maintenance targets, or evolving the governance model.

**Prerequisites:**

- Understand the current effective policy: `steward config show --effective`.
- Review `.steward/policy.yaml`, `.steward/path-policy.yaml`, and `.steward/config.yaml` for the scope of your change.

**Steps:**

1. **Edit the config files** under `.steward/`.
2. **Validate the config.** Run `steward config validate` and `steward config doctor`.
3. **Check the effective result.** Run `steward config show --effective` and verify the merged policy is what you intended.
4. **Run finalization checklist.** `steward check` must exit 0 — your config change must not break existing governance.

**Commit type:** `chore:` with a descriptive scope, e.g., `chore(governance): add freshness window for pre-1-0-readiness-plan`.

**Definition of done:**

- `steward config validate` passes.
- `steward config doctor` reports no new ineffective declarations.
- `steward check` exits 0.
- The change is intentional and justified — governance config is a shared contract.

**Anti-patterns:**

- Disabling rules to make `steward check` pass instead of fixing the underlying issue.
- Adding artifacts without linking them from a navigation surface.
- Making governance changes without testing the effective result.

---

### 8. Agent Guidance Update

**Objective:** Update guidance that coding agents use to navigate and work in this repository.

**When to use:** Changing `AGENTS.md`, `.agents/skills/steward-self-cli/SKILL.md`, `CONTRIBUTING.md`, or other agent-facing documentation.

**Prerequisites:**

- Understand the current agent entry point hierarchy: `AGENTS.md` → `CONTRIBUTING.md` → `README.md`.

**Steps:**

1. **Edit the guidance.** Keep instructions prescriptive, concrete, and consistent with this workflow guide.
2. **Cross-check consistency.** Ensure `AGENTS.md`, `CONTRIBUTING.md`, and `README.md` do not contradict each other or this workflow guide.
3. **Run finalization checklist.** Include `npm run lint:md` and `steward check`.

**Commit type:** `docs:` if purely content; `chore:` if changing structure or agent tooling.

**Definition of done:**

- Agent-facing docs are consistent with each other and with this workflow guide.
- `npm run lint:md` passes.
- `steward check` exits 0.

**Anti-patterns:**

- Duplicating process details that belong in this workflow guide. Agent docs should link here, not restate steps.
- Writing vague guidance like "follow best practices" — be specific.
- Changing agent guidance without testing that agents can actually follow it.

---

### 9. Project Status, Roadmap, and Backlog Update

**Objective:** Update one canonical owner: status for current facts, roadmap for committed scope, or backlog for unscheduled work.

**When to use:** After scope changes, milestone completions, priority shifts, or when planning artifacts drift from reality.

**Prerequisites:**

- Read `docs/project/README.md` and avoid creating a new active authority when an existing owner fits.

**Steps:**

1. **Choose the owner.** Current facts go to status; current/next scope goes to roadmap; unscheduled work goes to backlog.
2. **Edit one authority.** Avoid copying the same volatile fact into multiple project documents.
3. **Archive displaced evidence** under `docs/history/plans/` when a substantive prior plan still has traceability value.
4. **Run finalization checklist.**

**Commit type:** `docs:` for content updates; `chore:` for structural reorganization.

**Definition of done:**

- Project authorities reflect current truth without duplication.
- No orphaned docs.
- `steward check` exits 0.
- Cross-references are consistent.

---

### 10. Dependency Update

**Objective:** Update NuGet packages, GitHub Actions versions, or npm dependencies.

**When to use:** Dependabot PRs, manual dependency bumps, runtime or SDK upgrades.

**Steps:**

1. **Apply the update.** Accept the dependabot PR or manually edit the package reference.
2. **Build and test.** `dotnet build steward.sln` and `dotnet test steward.sln` must pass.
3. **Verify no behavioral regressions.** `steward check` must exit 0.
4. **Update CHANGELOG** only if the dependency change is user-visible (e.g., a breaking change in a key dependency).

**Commit type:** `chore(deps):` for automated bumps; `chore:` or `feat:` for intentional upgrades that change behavior.

**Definition of done:**

- Build and tests pass.
- `steward check` exits 0.
- No new warnings or behavioral regressions.

---

### 11. CI/CD Pipeline Change

**Objective:** Modify GitHub Actions workflows, CI matrix, release automation, or related infrastructure.

**When to use:** Adding CI steps, fixing workflow bugs, updating action versions, changing the release pipeline.

**Steps:**

1. **Edit the workflow files** under `.github/workflows/`.
2. **Test locally** where possible (e.g., validate YAML syntax, run equivalent commands locally).
3. **Run finalization checklist.**
4. **Update `docs/project/release-process.md`** if the change affects the release pipeline.

**Commit type:** `ci:` for pure pipeline changes; `chore:` if bundled with other infrastructure.

**Definition of done:**

- Workflow YAML is valid.
- `steward check` exits 0.
- Release process docs are consistent with pipeline behavior.

---

## ADR and RFC Workflow

ADRs and RFCs follow the [documentation update](#4-documentation-update) workflow with additional requirements:

**Creating an ADR or RFC:**

1. Use the naming pattern `ADR-NNN-lower-kebab.md` or `RFC-NNN-lower-kebab.md`.
2. Run `steward explain path <file>` to see required frontmatter.
3. Include required frontmatter fields:
   - ADRs: `type: adr`, `status:` (Draft/Proposed/Accepted/Superseded/Deprecated), `category:`
   - RFCs: `type: rfc`, `status:` (Draft/Proposed/Accepted/Superseded/Deprecated/Deferred), `resolves:`
4. Run `steward maintain --apply` to refresh the generated decision index.

**Commit type:** `docs:` for new decisions; the implementing work uses its own appropriate type.

---

## Workflow Selection Guide

| If your change involves... | Primary workflow | Also consider |
|---------------------------|-----------------|---------------|
| New CLI command or feature | [Feature Implementation](#1-feature-implementation) | [Project Update](#9-project-status-roadmap-and-backlog-update) |
| Fixing broken behavior | [Bug Fix](#2-bug-fix) | |
| Code restructuring | [Refactoring](#3-refactoring-and-cleanup) | |
| Updating or creating docs | [Documentation Update](#4-documentation-update) | [ADR/RFC Workflow](#adr-and-rfc-workflow) |
| Responding to review findings | [Review Remediation](#5-review-and-audit-remediation) | Varies by finding type |
| Cutting a release | [Release Execution](#6-release-execution) | |
| Changing `.steward/` config | [Governance Config](#7-governance-configuration-update) | |
| Updating agent docs | [Agent Guidance](#8-agent-guidance-update) | |
| Updating status, roadmap, or backlog | [Project Update](#9-project-status-roadmap-and-backlog-update) | |
| Bumping dependencies | [Dependency Update](#10-dependency-update) | |
| Changing CI/CD workflows | [CI/CD Pipeline](#11-cicd-pipeline-change) | [Release Execution](#6-release-execution) |

---

## Notes for Agents vs Humans

**Agents should:**

- Read [AGENTS.md](../../AGENTS.md) and [.agents/skills/steward-self-cli/SKILL.md](../../.agents/skills/steward-self-cli/SKILL.md) for repo-specific tooling guidance.
- Run `steward orient --signals` at session start for context.
- Follow this workflow guide for process decisions.
- Run the finalization checklist before presenting work as complete.

**Humans should:**

- Use this guide as the authoritative process reference.
- Follow `CONTRIBUTING.md` for development setup.
- Use `steward explain path <file>` before editing governed docs.
- Consult `docs/project/release-process.md` for the full release operator flow.
