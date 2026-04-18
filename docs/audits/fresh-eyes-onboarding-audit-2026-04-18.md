---
type: audit
status: Active
last_updated: 2026-04-18
---

# Fresh-Eyes Onboarding Audit - 2026-04-18

## Executive Summary

### Verdict

**Final verdict: not ready**

Steward's core product value is real. Once it was running against a real repository, `orient`, `status --coverage`, `check`, `refs`, and `search` all delivered meaningful signal quickly. The problem is that a stranger following only the README does not have a reliable path from clone to that first value.

Two onboarding failures are decisive:

1. The README's documented global install command did not work as written.
2. The README's source-build run path does not explain how to use Steward on another repository, and the obvious `dotnet run --project ...` workaround breaks in a common real-world case where the target repo pins a different SDK via `global.json`.

Because of that, the README currently underserves the exact user it claims to help: a technically strong newcomer trying to apply Steward to a real repository in the first hour without reading internal docs or source code.

### Bottom line

- Core CLI value after workaround: credible
- README-led first-hour onboarding: not credible enough yet
- Main failure mode: install/run path, not command quality

## Environment Used

| Item | Value |
|------|-------|
| Audit date | 2026-04-18 |
| Host OS | Windows 10.0.26200 x64 |
| Shell | PowerShell |
| Steward repo path | `d:\git\steward` |
| Steward README baseline seen by newcomer | `0.14.0` |
| `dotnet --version` in Steward repo | `10.0.202` |
| Steward runtime reported by CLI | `.NET 10.0.6` |

### Candidate target repos checked

- `d:\git\datascope`
- `d:\git\docflux`
- `d:\git\advanced-repo-spine`

### Environment-specific notes

- `datascope` and `docflux` were rejected as primary test targets because local Git ownership on this machine triggered `safe.directory` errors. That was an environment-specific Git issue, not a Steward issue.
- `advanced-repo-spine` was chosen because it is a real, non-trivial, clean local repository and did not require environment repair before Steward could be exercised.

## Repo Chosen For Real Usage And Why

**Chosen repo:** `d:\git\advanced-repo-spine`

**Why this repo was the fairest test:**

- It is non-trivial: code, docs, workflows, templates, and governance artifacts are all present.
- It was clean and immediately usable.
- It was not already configured for Steward.
- It is structurally rich enough that `orient`, `check`, `status`, and search/navigation commands have a real job to do.

## README Promise Summary

### What the README claims Steward is for

Steward is presented as a configurable repository stewardship CLI for humans and AI agents. The README promises help with repository orientation, policy-driven validation, document governance, deterministic maintenance, Markdown structural editing, broken-link detection, explainability, and machine-readable output.

### Intended user

The README explicitly splits the audience into two roles:

- **Maintainer**: sets up `.steward/` policy and repository governance
- **Contributor**: works inside an already-governed repo and uses Steward to orient, validate, and remediate

That role split is one of the README's stronger qualities. It is clear that the project is not only for maintainers.

### Immediate problems it claims to solve

- Understanding what matters in a repository
- Detecting broken links, missing artifacts, and policy drift
- Establishing document and path conventions
- Keeping generated structure artifacts in sync
- Helping humans and agents navigate and edit Markdown structurally

### Fresh-eyes assessment of the value proposition

The value proposition is directionally strong but overloaded. "Repository stewardship" is more concrete than generic "repo tooling," but the README introduces a lot of domain terms early: artifacts, artifact families, governed files, maintenance artifacts, managed sections, start-here entries, and path policy. A technically strong newcomer can parse it, but the first 10 minutes are heavier than they need to be.

### Terminology clarity

**Worked well**

- `maintainer` and `contributor`
- `orient`, `check`, `maintain`, `refs`, `search`
- `required artifacts`, `broken links`, `frontmatter`

**More internal than intuitive**

- `artifact families`
- `governance coverage`
- `path policy`
- `managed region integrity`

## Exact First-Hour Journey Timeline

| Time | Action | Outcome | Classification | Evidence / notes |
|------|--------|---------|----------------|------------------|
| T+00 | Read `README.md` only | Clear high-level intent, long and dense README | Worked but confusing | Good role split, weak golden path |
| T+08 | `dotnet --version` | `10.0.202` | Worked as expected | Prerequisite present |
| T+09 | `dotnet build` in Steward repo | Build succeeded | Worked as expected | No hidden restore or build blockers |
| T+11 | `dotnet run --project src/Steward.Cli -- version` | Reported `steward 0.14.0` | Worked as expected | Good first sanity check |
| T+12 | `dotnet run --project src/Steward.Cli -- --help` | Help rendered | Worked but confusing | Usage banner says `Steward.Cli`, not `steward`; some option placeholders missing |
| T+15 | `dotnet pack src/Steward.Cli -c Release` | Produced `Steward.Cli.0.14.0.nupkg` | Worked as expected | Packaging step itself succeeded |
| T+16 | `dotnet tool install --global --add-source ./src/Steward.Cli/bin/Release Steward.Cli --version 0.14.0` | Failed: package version not found | Failed due to documentation | Exact README command did not work |
| T+21 | Choose real target repo | `advanced-repo-spine` selected | Worked as expected | Real repo, clean, not preconfigured |
| T+22 | `dotnet run --project d:\git\steward\src\Steward.Cli -- orient` from target repo | Failed with `NETSDK1045` | Failed due to product/UX/design issue | Target repo's `global.json` forced `.NET 8` SDK during build/evaluation |
| T+26 | Workaround: `dotnet d:\git\steward\src\Steward.Cli\bin\Debug\net10.0\Steward.Cli.dll orient` | Orientation output succeeded | Worked but confusing | Useful, but undocumented workaround |
| T+27 | `... status --coverage` before init | Clear failure: no `.steward` directory found | Worked as expected | Good error message |
| T+28 | `... init --profile software` | Created `.steward/config.yaml` and `.steward/policy.yaml` | Worked as expected | Good next-step guidance |
| T+30 | `... config suggest` | Suggested a small artifact set | Worked but confusing | Output useful but manual and shallower than expected on this repo |
| T+31 | `... config validate` | Valid | Worked as expected | Fast feedback |
| T+32 | `... config doctor` | No issues | Worked as expected | Fast feedback |
| T+33 | `... check` | 8 broken-link warnings, 5 discoverability infos, `PASS` | Worked but confusing | Signal is real, status wording is too reassuring |
| T+35 | `... status --coverage` after init | 40% governance coverage | Worked as expected | Strongest first-value moment |
| T+37 | `... orient --signals` | `Signals none` | Worked but confusing | Conflicts with recent warning-heavy `check` impression |
| T+39 | `... explain STWD-008` | Short rule summary | Worked as expected | Useful but minimal |
| T+40 | `... check --fix` | `No automatic fixes available` | Worked but confusing | Honest, but remediation loop remains manual |
| T+42 | `... refs docs/adr/ADR-0009-add-ars-outline-command.md` | Inbound and outbound links shown | Worked as expected | Strong navigation value |
| T+44 | `... search outline --mode headings --max 10` | Useful heading matches returned | Worked as expected | Good secondary discovery surface |

## What Worked Well

- The README establishes a real product, not just a bag of commands. The project has a coherent thesis.
- The maintainer/contributor split is clear and worth keeping.
- Build from source worked cleanly with the declared prerequisite.
- `orient` produced immediate value on an unconfigured repo. This is a legitimate first-value command.
- `init --profile software` had good next-step guidance and did not overwhelm the user.
- `config validate` and `config doctor` were quick, legible, and confidence-building.
- `status --coverage` was excellent. Seeing `20/50 Markdown files (40%)` governed is instantly understandable and actionable.
- `check` surfaced real broken links on the target repo without requiring policy authoring beyond `init`.
- `refs` and `search --mode headings` felt like serious repository tools, not filler commands.

## All Friction Points And Blockers

| Severity | Issue | Classification | Why it matters |
|----------|-------|----------------|----------------|
| Critical | README global install command failed exactly as written | Failed due to documentation | A newcomer loses trust immediately when the canonical install command fails after a successful build/pack |
| Critical | README does not explain how a source-built Steward instance should be used on another repository | Failed due to documentation | The README tells you how to build Steward, not how to apply that build to a real repo |
| Critical | The obvious cross-repo workaround, `dotnet run --project ...`, breaks in target repos that pin another SDK with `global.json` | Failed due to product/UX/design issue | This is a common real-world setup, not an edge case |
| High | No copy-paste-safe first 10-15 minute golden path is called out | Worked but confusing | A long README is acceptable; a missing short path is not |
| High | `status --coverage` is the best first-value command after `init`, but the README does not elevate it as the primary payoff | Worked but confusing | The strongest onboarding moment is buried |
| High | `config suggest` requires manual YAML editing and suggested less than a newcomer would expect on a docs-heavy repo | Worked but confusing | New users have to guess how much to trust or extend the suggestion set |
| Medium | `check` reports visible problems and still ends with `Result: PASS` | Worked but confusing | "Pass" reads cleaner than the actual repo state feels |
| Medium | `orient --signals` reported `none` shortly after `check` found 13 diagnostics | Worked but confusing | The semantic boundary between "signals" and "diagnostics" is not obvious from the command name |
| Medium | Root help exposes implementation identity (`Steward.Cli`) instead of the user-facing command name (`steward`) | Worked but confusing | It makes the product feel less finished than the README |
| Medium | Several help options omit the expected value placeholder | Worked but confusing | Example: `--config` in help vs `--config <path>` in README |
| Low | `check --scope changed` after `init` checked `0` files because `.steward/` was untracked | Worked but confusing | Probably defensible, but surprising right after init |
| Low | Repo self-story drift exists once a newcomer follows the planning index | Failed due to documentation | README and runtime say `0.14.0`; `implementation-status.md` says `0.15.0` delivered |

## Credibility Leaks Ranked By Severity

### 1. Broken documented install path

This is the single biggest leak. A user who successfully builds and packs the tool, then watches the README install command fail, has no reason to assume the next section will be more reliable.

### 2. No reliable README-only cross-repo execution path

The README never makes the crucial transition from "you built Steward" to "you are now using Steward on a different repository." That gap is fatal to the primary audit scenario.

### 3. Common `global.json` target-repo scenario breaks the obvious workaround

This is not a niche case. Many serious repositories pin SDK versions. If Steward's easiest source-built usage path breaks there, onboarding is brittle by default.

### 4. The product's strongest first-value surface is not presented as the golden path

`status --coverage` on a real repo was the first moment that felt immediately valuable. The README makes the user work too hard to find that.

### 5. `PASS` plus warnings plus `orient --signals: none` weakens confidence in status language

The tool is finding real issues, but two nearby surfaces make the repo feel cleaner than it is.

### 6. Help text still feels like framework output in places

The root help is not terrible, but `Steward.Cli` instead of `steward` and missing value placeholders are small paper cuts that accumulate into a "pre-1.0 roughness" vibe.

### 7. Version/story drift in supporting docs

A fresh user who leaves the README and checks current-state docs can encounter conflicting version claims. That is avoidable trust loss.

## Top 10 Actionable Improvements, Prioritized

1. Fix the README installation story so that the first documented install path succeeds exactly as written.
2. Add a fully explicit "use a source build against another repo" section with a copy-paste-safe command sequence.
3. Provide a first-hour golden path section near the top of the README with a real target-repo flow: `build -> install or executable path -> orient -> init -> status --coverage -> check`.
4. Add a local `--tool-path` installation option to the README and call out `--ignore-failed-sources` if local package source resolution requires it in practice.
5. Ship a runnable binary or documented executable path that does not depend on the target repo's SDK selection.
6. Elevate `orient` and `status --coverage` as the first meaningful-value commands for a new maintainer.
7. Improve `config suggest` so it either detects more of the obvious structure on real repos or clearly labels itself as intentionally conservative.
8. Reword `check` summary language so a warning-heavy run does not read as fully healthy.
9. Make `orient --signals` semantics clearer or broaden it so it reflects the most important recent health issues.
10. Remove help-text rough edges that make the CLI feel less polished than the README claims.

## Must Fix Before Broader Adoption

- The README install command must work without improvisation.
- The README must document a reliable way to apply a source-built Steward binary to a different repository.
- Cross-repo usage must not collapse when the target repo pins a different SDK via `global.json`, or the docs must route users around that entirely.
- The README needs a short golden path that reaches a clearly meaningful outcome in 10-15 minutes.

## Nice To Improve

- Make `config suggest` feel smarter and less manual on mature repositories.
- Tighten status semantics so `PASS`, `signals`, and warning-heavy output feel mutually coherent.
- Polish help output to consistently use user-facing command names and option placeholders.
- Reduce supporting-doc version drift so deeper docs reinforce, rather than dilute, first impressions.

## Concrete Doc Changes Recommended

1. Add a new README section near Installation called **First 15 Minutes** with one exact flow:
   - build from source
   - install locally with `--tool-path` or use the built executable directly
   - `cd` into the target repo
   - run `orient`
   - run `init --profile software`
   - run `status --coverage`
   - run `check`
2. Add a README subsection called **Using a source build on another repository** with an explicit note that the current working directory is the repository being analyzed.
3. Replace or augment the current global tool install instructions with a tested command that succeeds from a local package source.
4. Add one short sentence warning that `dotnet run --project ...` can be affected by the target repo's `global.json`, and point users to the safer executable path.
5. Show one realistic sample output snippet from `status --coverage` so the payoff is visible before the user commits to policy authoring.
6. Clarify what `orient --signals` does and does not include.
7. Clarify that `check --scope changed` is Git-scoped and may ignore newly untracked files until they are staged or tracked.
8. Reconcile README version/baseline language with `implementation-status.md`.

## Concrete Product / UX Changes Recommended

1. Make local package installation robust enough that the documented command works without extra flags.
2. Produce a first-class runnable artifact path after `dotnet build` or `dotnet publish` that is explicitly designed for cross-repo use.
3. Consider a dedicated `steward doctor onboarding` or `steward quickstart` surface that validates prerequisites and prints the next command sequence.
4. Adjust `check` completion wording so warning-heavy runs are not summarized as a clean pass.
5. Revisit `orient --signals` so it better matches user expectation after running `check`.
6. Improve `config suggest` heuristics or confidence reporting for real repos with decision logs, RFCs, PRDs, and docs subtrees.
7. Make root help consistently show the public command identity and option argument placeholders.

## README Standalone Quality

### Does the README stand on its own for initial onboarding?

**Not yet.**

It stands on its own for understanding the product concept and major command families. It does not yet stand on its own for reliably getting a stranger from clone to real-repo usage without improvisation.

### Are deeper docs discoverable when needed?

Yes. The repo has a strong planning index and a substantial documentation set. Discoverability is not the main issue.

### Are links, examples, and command references consistent with actual behavior?

Only partially.

- The core command inventory broadly matches reality.
- The install/run path is not reliable enough.
- Help text does not fully align with the README's user-facing command presentation.
- Supporting docs expose version drift.

### Is there a clean separation between maintainer-facing and contributor-facing guidance?

Yes. This part of the README is better than average. The weakness is not audience separation; it is the missing newcomer golden path that bridges setup and first value.

## Power Versus Fragility In Real Repo Usage

### Where Steward felt powerful

- Fast, useful orientation on a repository with no prior Steward config
- Immediate quantification of governance coverage after `init`
- Real broken-link detection without deep setup
- Navigation support through `refs` and heading search
- Quick config sanity checks via `config validate` and `config doctor`

### Where Steward felt fragile or overly manual

- Installation from local package output
- Cross-repo execution from a source build
- Manual policy editing after `config suggest`
- Remediation flow when `check --fix` has nothing it can actually fix
- Semantic mismatch between nearby health surfaces (`check`, `status`, `orient --signals`)

## Final Verdict

**Not ready**

### Rationale

The question for this audit was not whether Steward has useful commands. It does.

The question was whether a stranger could go from clone to meaningful first value using only the README and obvious entry points. In this audit, the answer was **no**:

- the documented global install path failed
- the README did not explain cross-repo usage for a source build
- the obvious source-build workaround broke in a realistic target repository

Meaningful first value was reached only after undocumented workarounds. That is enough to disqualify the current onboarding path for broader adoption, even though the underlying product already shows real promise once running.

## Prioritized Action Checklist

- [ ] P0: Replace the README install command with a tested command that succeeds from a local package source.
- [ ] P0: Add a README section for using Steward on another repository after a source build.
- [ ] P0: Document and prefer a runnable executable path that is immune to the target repo's `global.json`.
- [ ] P0: Add a 10-15 minute golden path near the top of the README.
- [ ] P1: Promote `status --coverage` as the primary first-value payoff after `init`.
- [ ] P1: Reword `check` summary output so warning-heavy runs do not read as fully healthy.
- [ ] P1: Clarify or improve `orient --signals` semantics.
- [ ] P1: Improve `config suggest` guidance and/or heuristic depth for mature repositories.
- [ ] P2: Fix root help to consistently use `steward` and show option value placeholders.
- [ ] P2: Reconcile README, runtime, and implementation-status version claims.
