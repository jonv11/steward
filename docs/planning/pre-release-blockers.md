# Pre-Release Blockers

- **Source:** [Release-Readiness Assessment 2026-04-15](../audits/release-readiness-assessment-2026-04-15.md)
- **Status:** Active
- **Last updated:** 2026-04-16

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

---

## Relationship to Other Planning Artifacts

- The [Pre-1.0 Readiness Plan](pre-1-0-readiness-plan.md) captures broader stable-release requirements. Items B3 and B4 overlap with that plan's required items.
- The [Milestone Plan](milestone-plan.md) sequences future work. These blockers should be resolved before or as part of `v0.11.0`.
- The [Release-Readiness Assessment](../audits/release-readiness-assessment-2026-04-15.md) provides the full rationale for each blocker.

## Resolution Protocol

1. Address each blocker and update its status in this document.
2. When all blockers are resolved, update the release-readiness assessment with a re-assessment note.
3. Proceed with the release-authorization decision per ADR-013.
