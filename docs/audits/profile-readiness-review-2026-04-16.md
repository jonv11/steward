# Profile Readiness Review — 2026-04-16

- **Scope:** Convert B5 ("Validate or reduce non-software profile offerings") into a command-level pre-release checklist grounded in the current repo.
- **Primary source:** [Pre-Release Blockers](../planning/pre-release-blockers.md)
- **Supporting sources:** [RFC-002 Configuration Model](../decisions/rfcs/RFC-002-configuration-model.md), [PRD](../requirements/PRD.md), [Release-Readiness Assessment 2026-04-15](release-readiness-assessment-2026-04-15.md), `ProfileDefaults.cs`, `CommandSetup.cs`, current CLI tests

## Current Repo Facts

- Built-in profiles currently ship from `src/Steward.Core/Configuration/ProfileDefaults.cs`: `software`, `docs`, `mixed`, `knowledge`, `minimal`.
- Profile defaults are merged into effective policy in `CommandSetup.Build()`, so release readiness is not blocked on the profile mechanism itself.
- Representative fixture repos now exist under `tests/Steward.TestFixtures/Repos/` for `docs`, `mixed`, `knowledge`, and `minimal`.
- CLI integration coverage now exercises those fixture repos across:
  - `init --profile`
  - `config validate`
  - `config show --effective`
  - `status`
  - `orient`
  - `check` pass/fail paths
  - `config doctor`
- Those representative fixtures now pass `check` cleanly in their healthy state and have explicit failing cases for missing key artifacts.
- The new tests establish behavioral evidence for all four non-software profiles, but they do not by themselves prove that every profile is product-strong enough to keep enabled for release.

## Representative Sample Observations

These observations came from running the current CLI on small temporary repositories shaped for each non-software profile.

| Profile | Observed effective behavior | Judgment |
|---------|-----------------------------|----------|
| `docs` | `config show --effective` resolves repository type `documentation`; `status` reports `README.md` and `docs/` as required; `check` passes cleanly on a docs-only sample. | Strongest non-software candidate to keep enabled. |
| `mixed` | Effective type becomes `mixed`, but `status` only treats `README.md` as required and reports no recommended artifacts in the sampled repo. | Fixture-backed validation now exists, but the resulting contract still looks too thin to market confidently without richer defaults or a narrower promise. |
| `knowledge` | Effective type becomes `knowledge`, but the shipped defaults only require `README.md`; no knowledge-specific structure is demonstrated. | Fixture-backed validation now exists, but the resulting contract still looks too generic to justify a distinct archetype claim. |
| `minimal` | `init --profile minimal` looks sparse, but after role-default resolution `status` still treats `README.md` as required because `authoritative` implies required importance. | Release messaging must either accept "README-first minimal" as the real meaning, or implementation/docs must be changed before calling it "bare minimum." |

## Command-Level Release Checklist

These are the commands whose user-visible behavior is materially affected by profile quality. This is the minimum evidence needed before a public release keeps each non-software profile enabled.

| Command | Why it matters for B5 | What must be true before release | Current gap |
|---------|------------------------|----------------------------------|-------------|
| `steward init --profile <name>` | First-run promise surface; creates the policy users will judge immediately. | For each shipped profile, scaffolded `policy.yaml` must look intentional for a representative repo, not generic or misleading. | Fixture-backed CLI coverage now exists; remaining gap is product judgment, not missing execution evidence. |
| `steward config validate` | Confirms a scaffolded profile config is valid and stable. | Each shipped profile must round-trip through `init` + `config validate` with no surprises. | Fixture-backed CLI coverage now exists. |
| `steward config show --effective` | Reveals the actual merged policy; this is the best truth surface for what a profile really means. | For each shipped profile, the effective policy should show a meaningful archetype-specific contract that a maintainer would recognize as useful. | `docs` is plausible; `mixed` and `knowledge` currently look too generic; `minimal` reveals a README-required baseline that should be documented explicitly if kept. |
| `steward orient` | Session-start understanding is part of the repo-archetype promise. | On a representative repo, `orient` should surface a believable context and useful top-level classification for the profile. | Fixture-backed evidence now exists; remaining gap is that some profiles still do not feel archetype-specific enough. |
| `steward status` | Shows the profile-implied contract at a glance: required/recommended artifacts, start-here entries, completeness. | Required/recommended artifact sets should be meaningfully different by archetype and should help a user understand what "healthy" means for that profile. | `mixed` and `knowledge` currently collapse to "README only" in sample runs; `minimal` also effectively becomes README-required. |
| `steward check` | This is the real enforcement surface; profile usefulness lives or dies here. | Each shipped profile needs at least one representative passing fixture and one representative failing fixture showing sensible diagnostics from profile-implied rules. | Fixture-backed pass/fail coverage now exists; the remaining question is whether the resulting contracts are distinct enough to justify keeping every profile. |
| `steward config doctor` | Protects adopters from silent bad scaffolding or ineffective defaults. | Representative repos for shipped profiles should not produce confusing false positives immediately after `init`. | No current execution gap was found in fixture-backed coverage; only the keep/narrow release decision remains. |

## Suggested Release Decision Logic

Use this decision rule for B5:

1. Keep a profile enabled only if the command checklist above has representative evidence and the resulting policy feels meaningfully archetype-specific.
2. If the evidence does not exist in time, narrow the public `init --profile` set rather than shipping weak archetype claims.

## Current Conservative Recommendation

If release happened from the current repo state without further profile work:

- Keep `software` enabled.
- `docs` is the strongest candidate to keep.
- `mixed` and `knowledge` should be considered the first profiles to narrow or defer unless maintainers believe the current README-first contracts are sufficient for a first release.
- `minimal` needs an explicit decision:
  - either document it as a README-first baseline and validate that meaning end-to-end
  - or adjust the implementation so it truly behaves like "almost no default rules"

## Smallest Sufficient Pre-Release Work

The smallest credible closure path for B5 was:

1. Add representative fixture repos for `docs`, `mixed`, `knowledge`, and `minimal`.
2. Add black-box or CLI integration coverage for:
   - `init --profile`
   - `config show --effective`
   - `status`
   - `check`
3. Review the resulting behavior:
   - keep profiles that show clear value
   - narrow the offered set for the first public release if any profile still looks generic or misleading

Items 1 and 2 are now implemented. Item 3 remains the active release decision.

## If Narrowing Is Chosen

If time is too short for full validation, the repo should prefer a smaller honest profile set over a broader speculative one. Based on the current evidence, the first narrowing candidates are `mixed` and `knowledge`. `minimal` should only remain if its README-first semantics are made explicit.
