# Pre-Release Blockers

- **Source:** [Release-Readiness Assessment 2026-04-15](../audits/release-readiness-assessment-2026-04-15.md)
- **Status:** Active
- **Last updated:** 2026-04-16 (B6, B7 added from CLI review normalization)

---

## Purpose

This document captures the specific items that must be resolved before a first meaningful public release of Steward. These were identified in the release-readiness assessment as items where the current product either overpromises, has a visible functional gap, or lacks minimum-viable validation for its primary use case.

These are not nice-to-haves. Each item, if left unresolved, would materially weaken the first impression or undermine the product's core promises.

---

## Blocking Items

### B1: Tighten README claims to match delivered reality

- **Status:** Completed (2026-04-16)
- **Category:** Docs
- **Promise affected:** "Full explainability", profile readiness, feature breadth claims
- **Resolution evidence:** `README.md`
- **What must change:**
  - Change "Full explainability" to "Rule explainability" in the README features list (or equivalent honest phrasing)
  - Add a note to the profiles table indicating which profiles are battle-tested vs. starting-point defaults
  - Review the features list for any other claims that exceed current delivery
- **Acceptance criteria:** A first-time user reading the README would not encounter a claim that the product cannot fulfill within their first 10 minutes of use.
- **Effort estimate:** Small (documentation edit)

### B2: `config suggest` must support `--output json`

- **Status:** Completed (already implemented; regression-tested on 2026-04-16)
- **Category:** Implementation
- **Promise affected:** Dual-audience output (humans and AI agents)
- **Resolution evidence:** `src/Steward.Cli/Commands/ConfigCommand.cs`, `tests/Steward.Cli.Tests/ConfigCommandTests.cs`
- **What must change:**
  - `config suggest` must honour the global `--output json` flag
  - The `BootstrapSuggestion` result object must be serialized through the standard formatter
- **Acceptance criteria:** `steward config suggest --output json` produces valid JSON containing the same suggestion data as the text output.
- **Effort estimate:** Small (the typed result already exists; needs JSON serialization path)

### B3: Cross-platform CI pipeline

- **Status:** In progress (workflow added 2026-04-16; hosted green run still pending)
- **Category:** Workflow / Release
- **Promise affected:** "Offline and portable", CI validation-gate use case
- **Resolution evidence:** `.github/workflows/ci.yml`
- **What must change:**
  - Add a CI configuration (e.g., GitHub Actions) that runs `dotnet build`, `dotnet test`, and `dotnet pack` on Windows, macOS, and Linux
  - All tests must pass on all three platforms
- **Acceptance criteria:** A green CI run on all three platforms before the release tag is created.
- **Effort estimate:** Medium (CI authoring + any cross-platform bug fixes discovered)

### B4: Document or mitigate System.CommandLine beta dependency risk

- **Status:** Completed for option (a) on 2026-04-16
- **Category:** Dependency / Release
- **Promise affected:** Product reliability
- **Resolution evidence:** `README.md`, `Directory.Packages.props`
- **What must change:**
  - Either (a) pin the specific System.CommandLine version in `Directory.Packages.props` and add a note in README acknowledging the beta dependency, or (b) evaluate migration to a GA alternative if timing permits
  - The dependency posture must be a conscious, documented decision rather than an implicit acceptance
- **Acceptance criteria:** A maintainer or adopter asking "is this dependency safe?" can find a clear answer in the repo.
- **Effort estimate:** Small for option (a), large for option (b)

### B5: Validate or reduce non-software profile offerings

- **Status:** In progress (fixture-backed validation added on 2026-04-16; keep/narrow decision still pending)
- **Category:** Product / Config
- **Promise affected:** "Works across repository archetypes"
- **Current note:** README now presents non-software profiles as conservative starting-point defaults rather than battle-tested curated experiences, and the repo now has representative fixture-backed CLI coverage for `docs`, `mixed`, `knowledge`, and `minimal`. The remaining release decision is whether the observed behavior is strong enough to keep each profile enabled, or whether the offered `init --profile` set should be narrowed.
- **Detailed breakdown:** [Profile Readiness Review — 2026-04-16](../audits/profile-readiness-review-2026-04-16.md)
- **What must change:**
  - Use the current representative validation evidence to make an explicit release decision for each non-software profile (`docs`, `mixed`, `knowledge`, `minimal`)
  - Either (a) keep only the profiles whose resulting contracts are clearly useful and document any important semantics, or (b) reduce the `init --profile` options to only the profiles that meet that bar and document that others are coming later
- **Command-level release checklist:**
  - `init --profile <name>` must scaffold an intentional-looking policy for each kept profile
  - `config show --effective` must reveal a meaningfully archetype-specific merged policy for each kept profile
  - `status` must show a useful required/recommended artifact contract for each kept profile
  - `check` must have representative passing and failing fixture coverage for each kept profile
  - `config doctor` should not raise confusing false positives immediately after scaffolding a representative kept profile
- **Acceptance criteria:** Every profile offered in `init --profile` either produces demonstrated value or is not offered.
- **Effort estimate:** Medium for option (a), small for option (b)

### B6: Fix scoped validation false positives on clean tree

- **Status:** Open
- **Category:** Implementation — critical workflow trust defect
- **Promise affected:** Scoped pre-commit/CI validation (PRD UC-02, REQ-VALIDATE-002/003, RFC-003)
- **Evidence sources:** [CLI Expectation Fidelity Review — 2026-04-16](../audits/cli-expectation-fidelity-review-2026-04-16.md) EF-001, [CLI Expectation Fidelity Reassessment — 2026-04-16](../audits/cli-expectation-fidelity-reassessment-2026-04-16.md) F-01, [CLI Full Assessment — 2026-04-16](../audits/cli-full-assessment-2026-04-16.md) F1
- **What is broken:**
  - `steward check --scope changed` and `--scope staged` on a clean tree produce false missing-artifact, broken-reference, and stale-artifact diagnostics (`STWD-001`, `STWD-007`, `STWD-009`) while reporting `Files checked: 0`.
  - Root cause: `RequiredArtifactRule`, `BrokenArtifactReferenceRule`, and `StaleArtifactRule` evaluate repository-wide obligations against `context.TargetFiles` instead of a full-repo file set.
- **What must change:**
  - Add an `AllDiscoveredFiles` (or equivalent) property to `ValidationContext` that represents the full repository file set regardless of scope.
  - Repo-wide obligation rules (`STWD-001`, `STWD-009`, and the existence check in `STWD-007`) must check file existence against `AllDiscoveredFiles`, not `TargetFiles`.
  - Content-scanning rules continue to use `TargetFiles` for scope sensitivity.
  - Add regression tests: scoped check on a governed repo with zero changed files must produce zero false positives; scoped check with a single changed file must validate only that file's content rules.
- **Acceptance criteria:** `steward check --scope changed` and `--scope staged` on a clean governed repo return clean PASS with no false diagnostics.
- **Effort estimate:** Small-medium (validation context split, rule adjustments, regression tests)

### B7: Include governance coverage in status JSON output

- **Status:** Open
- **Category:** Implementation — agent-facing parity gap
- **Promise affected:** Dual-audience coverage reporting (PRD §3, REQ-WORKFLOW-005)
- **Evidence sources:** [CLI Expectation Fidelity Review — 2026-04-16](../audits/cli-expectation-fidelity-review-2026-04-16.md) EF-002, [CLI Expectation Fidelity Reassessment — 2026-04-16](../audits/cli-expectation-fidelity-reassessment-2026-04-16.md) F-02, [CLI Full Assessment — 2026-04-16](../audits/cli-full-assessment-2026-04-16.md) F3
- **What is broken:**
  - `steward status --coverage --output json` returns the same JSON as `status --output json` — no `coverage` field is included even when `--coverage` is requested.
- **What must change:**
  - Include a `coverage` object in JSON output when `--coverage` is requested, containing at minimum: governed count, total Markdown count, percentage, and list of ungoverned paths.
  - Add a contract test for `status --coverage --output json`.
- **Acceptance criteria:** `steward status --coverage --output json` includes a `coverage` object with accurate governance-coverage data.
- **Effort estimate:** Small

---

## Relationship to Other Planning Artifacts

- The [Pre-1.0 Readiness Plan](pre-1-0-readiness-plan.md) captures broader stable-release requirements. Items B3 and B4 overlap with that plan's required items.
- The [Milestone Plan](milestone-plan.md) sequences future work. These blockers should be resolved before or as part of `v0.11.0`.
- The [Release-Readiness Assessment](../audits/release-readiness-assessment-2026-04-15.md) provides the full rationale for each blocker.

## Resolution Protocol

1. Address each blocker and update its status in this document.
2. When all blockers are resolved, update the release-readiness assessment with a re-assessment note.
3. Proceed with the release-authorization decision per ADR-013.
