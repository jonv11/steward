---
type: audit
status: Historical
standalone: true
---
# Repo Actionability Pass — 2026-04-16

- **Scope:** Determine which repo-grounded improvements were already sufficiently defined, implement the clearest ones, and leave active planning/state artifacts more accurate than before the pass.
- **Source-of-truth order used:** accepted ADRs/RFCs, current requirements/roadmap, active readiness/planning docs, current code/tests, then backlog-style notes.

> Historical scope note (2026-04-16): This is a closed implementation-pass record preserved as evidence of what changed during that cleanup. Current repo state and remaining release questions now live in [implementation-status.md](../plans/implementation-status-2026-06-06.md), [pre-release-blockers.md](../plans/pre-release-blockers.md), and [release-governance-conformance-review-2026-04-16.md](../stubs/release-governance-conformance-review-2026-04-16.md).

## What Was Inspected

- `README.md`
- `docs/implementation-status.md`
- `docs/planning-index.md`
- `docs/requirements/PRD.md`
- `docs/requirements/requirements-traceability.md`
- `docs/planning/pre-1-0-readiness-plan.md`
- `docs/planning/pre-release-blockers.md`
- `docs/planning/implementation-instructions.md`
- `docs/planning/milestone-plan.md`
- `docs/decisions/decision-index.md`
- `docs/decisions/adrs/ADR-010-agent-usefulness-improvements.md`
- `docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md`
- `docs/audits/release-readiness-assessment-2026-04-15.md`
- `docs/audits/usability-review-2026-04-15.md`
- `src/Steward.Cli/Commands/ConfigCommand.cs`
- `src/Steward.Cli/Commands/InitCommand.cs`
- `tests/Steward.Cli.Tests/ConfigCommandTests.cs`
- `Directory.Packages.props`

## Candidate Inventory

| Candidate | Source artifact(s) | Why it matters | Defined enough? | Scope / risk | Kind | Classification |
|-----------|--------------------|----------------|-----------------|--------------|------|----------------|
| Tighten README promises and profile/dependency wording | `pre-release-blockers.md` B1/B4/B5, `release-readiness-assessment-2026-04-15.md` | Reduces promise/reality mismatch in the first-run experience | Yes for docs tightening | Small / low | Docs alignment | Implement now |
| Cross-platform build/test/pack automation | `pre-1-0-readiness-plan.md`, `pre-release-blockers.md` B3, `implementation-instructions.md` | Converts a release-hardening requirement into concrete repo workflow | Yes | Medium / low | CI / release | Implement now |
| Surface merged policy in `config show --effective` text output | `pre-1-0-readiness-plan.md`, `usability-review-2026-04-15.md` item 2 | Makes profile-injected governance inspectable without reading source code | Yes | Small / low | CLI / docs | Implement now only if small and low-risk |
| `config suggest --output json` blocker | `pre-release-blockers.md` B2, `release-readiness-assessment-2026-04-15.md` | Critical for agent bootstrap flows | Already implemented in code; missing regression/status alignment | Small / low | Tests / planning hygiene | Already done in code; tighten docs/test |
| Non-software profile readiness | `pre-release-blockers.md` B5, `release-readiness-assessment-2026-04-15.md` | Avoids overstating archetype readiness | Partly; fixture-backed validation now exists, but keep/narrow decision is still missing | Medium / decision-bearing | Product / config | Not ready yet because release-scope decision is still missing |
| Migrate away from prerelease CLI dependencies | `pre-1-0-readiness-plan.md`, `assumptions-constraints.md` | Stable-release hardening remains important | Partly | Medium-high / broader than this pass | Dependency / release | Not ready yet because the repo only authorizes documentation and pinning in this pass |

## What Changed

- Added a GitHub Actions matrix at `.github/workflows/ci.yml` that runs `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet pack` on Windows, macOS, and Linux.
- Extended `steward config show --effective` text output so it now prints the merged effective policy, not only raw files plus runtime defaults.
- Added CLI regression coverage for `config suggest --output json` and for the new `config show --effective` text behavior.
- Tightened public-facing wording in `README.md`:
  - "Full explainability" became "Rule explainability"
  - prerelease dependency posture is now explicit
  - built-in profiles are described as starting-point defaults, with `software` called out as the exercised profile in this repo
  - `config show --effective` and CI behavior are now discoverable
- Added maintainer-facing dependency notes in `Directory.Packages.props`.
- Updated active planning/current-state artifacts so they no longer claim `config suggest --output json` is missing and no longer treat merged-policy surfacing as unfinished.

## Deferred Items

- Non-software profile breadth remains open. README language is now honest and representative fixture-backed validation now exists for `docs` / `mixed` / `knowledge` / `minimal`, but the repo still needs a deliberate keep/narrow decision for the first public release.
- Dependency migration away from prerelease packages remains open. This pass only documented the current posture and exact pins; it did not change package selection.
- Cross-platform CI still needs its first hosted green run before the release-hardening requirement is truly complete.

## Validation

- `dotnet build steward.sln -c Release`
- `dotnet test steward.sln -c Release --no-build`
- `dotnet test tests/Steward.Cli.Tests/Steward.Cli.Tests.csproj --filter ProfileReadinessTests -c Release --no-build`
- `dotnet pack src/Steward.Cli/Steward.Cli.csproj -c Release --no-build`
- `dotnet run --project src/Steward.Cli --no-build -- config show --effective`
- `dotnet run --project src/Steward.Cli --no-build -- config suggest --output json`
- `dotnet run --project src/Steward.Cli -c Release --no-build -- check`

## Validation Outcome

- Build succeeded.
- Full tests passed: `502` total (`366` core, `136` CLI).
- Pack succeeded and produced `src/Steward.Cli/bin/Release/Steward.Cli.0.10.0.nupkg`.
- `config show --effective` displayed the merged effective policy in text output.
- `config suggest --output json` returned valid JSON suggestion output.
- The profile-readiness fixture slice passed for all four non-software profiles.
- `steward check` initially reported one warning: `STRUCTURE.md` was stale after the documentation additions. The structure artifact was refreshed in the same pass, and the final `steward check` rerun passed cleanly (`266` files, `0` warnings).
