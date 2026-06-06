---
type: review
status: Historical
last_updated: 2026-04-19
source_review_cycle: 2026-04-18
standalone: true
---

# Review Synthesis Action Plan - 2026-04-18

## Executive Summary

This synthesis reviewed and validated all four expected April 18 review artifacts:

1. [Fresh-Eyes Onboarding Audit](../audits/fresh-eyes-onboarding-audit-2026-04-18.md)
2. [AI-Agent Contract Review](../audits/ai-agent-contract-review-2026-04-18.md)
3. [Rule-System Completeness Audit](../audits/rule-system-completeness-audit-2026-04-18.md)
4. [Config Expressiveness Stress Test](config-expressiveness-stress-test.md)

All four artifacts were present. The highest-value retained work consolidates into eight non-overlapping items:

- 3 x `P0` credibility/usefulness items
- 4 x `P1` near-term trust and correctness items
- 1 x `P2` follow-on design item

The dominant conclusion is that Steward's core value is already visible, but its adoption-ready story is still being undercut by three things:

- onboarding is not yet a tested, copy-paste-safe path from clone to first value
- agent-facing JSON behavior is not yet a trustworthy contract
- several rule/runtime/planning inconsistencies still leak trust in ways that are easy for newcomers and agents to notice

The recommended sequencing is:

1. fix first-hour onboarding and version-story drift
2. fix the confirmed correctness/safety defects
3. finish the machine-contract floor
4. only then expand the config model

## Review Artifacts Found And Analyzed

| Artifact | Path | Status | Role in synthesis |
|----------|------|--------|-------------------|
| Fresh-eyes onboarding audit | `docs/audits/fresh-eyes-onboarding-audit-2026-04-18.md` | Found | Primary source for newcomer trust, install/run path, first-value flow, and doc credibility |
| AI-agent contract review | `docs/audits/ai-agent-contract-review-2026-04-18.md` | Found | Primary source for JSON stability, mutation safety, error semantics, and machine handoff quality |
| Rule-system completeness audit | `docs/audits/rule-system-completeness-audit-2026-04-18.md` | Found | Primary source for rule correctness, diagnostic quality, missing governance seams, and auto-fix gaps |
| Config expressiveness stress test | `docs/reviews/config-expressiveness-stress-test.md` | Found | Primary source for adoption-boundary analysis and config-model follow-on prioritization |

## Validation Notes Against Current Repo State

The review artifacts were not accepted blindly. The following checks were validated against the current workspace, current runtime behavior, or current source:

| Finding cluster | Current repo evidence | Synthesis disposition |
|-----------------|-----------------------|-----------------------|
| README still lacks a newcomer golden path and repo-independent execution guidance | `README.md` still documents `dotnet run --project src/Steward.Cli -- <command>` and a global install flow, but does not provide a tested "First 15 Minutes" sequence, a "use a source build on another repository" section, or a warning that target-repo `global.json` can break `dotnet run --project ...`. A disposable target repo with `global.json` was used to reproduce the cross-repo failure. | Retained as `P0` onboarding work |
| The install path is broken as a product capability | A safer local-tool install validation succeeded with `dotnet tool install --tool-path ... --add-source ./src/Steward.Cli/bin/Release Steward.Cli --version 0.14.0`. The exact `--global` path was not re-run to avoid mutating the host environment. | Downgraded from confirmed product defect to docs/tested-command issue |
| Help/output still expose implementation roughness | `dotnet run --project src/Steward.Cli -- --help` still shows `Usage: Steward.Cli`, and `--config` still omits a value placeholder. | Retained as `P1` UX/help polish |
| Runtime/story drift is still real | `Directory.Build.props`, `README.md`, `CHANGELOG.md`, and `steward version` all report `0.14.0`, while planning/status artifacts describe `v0.15.0` as delivered. | Retained as `P1` planning/doc consistency work |
| Standard JSON envelope is not universal | `explain path README.md --output json --json-envelope standard` and `version --output json --json-envelope standard` both returned raw payloads. Source confirms `ExplainCommand`, `VersionCommand`, `OutlineCommand`, and `RefactorCommand` bypass `JsonEnvelopeWriter`. | Retained as `P0` contract work |
| JSON failure behavior is inconsistent | Confirmed plain-text failures under JSON mode for `status` without `.steward/`, `search '(' --regex`, and `md query ... 'heading['`. | Retained as `P0` contract work |
| `refactor move --apply --output json` is unsafe | Confirmed on a disposable repo: the command returned JSON plan output and exit code `0`, but the file was not moved and references were not rewritten. Source confirms side effects are only executed in the text-output branch. | Retained as `P0` bug fix |
| Scoped/family rule correctness gaps are still present | `BrokenInternalLinkRule` still uses `TargetFiles`; `IndexCompletenessRule` still uses `TargetFiles`; `FamilyMinCountRule` still classifies with `frontmatterFields: null`; `FreshnessRule` message still omits the path. | Retained as `P1` correctness work |
| New `depends_on` rule is required immediately | `ConfigLoader.ValidatePolicy()` already rejects unknown `depends_on` references during `config validate`. | Downgraded; no new standalone rule needed now |
| Config-model adoption gaps are real but not all immediate | The stress test gaps remain credible, but the broadest proposals are design work rather than ready backlog implementation. | Consolidated into one scoped `P2` RFC candidate |

## Items Discarded Or Downgraded

### Downgraded

- **Local package install failure as a product defect**
  - The review artifact reported a failed README install attempt.
  - Current validation showed that a local-tool installation from the packed `.nupkg` succeeds with `--tool-path`.
  - Retained problem: the README still does not present a safer, tested path for local installation and cross-repo use.

- **New rule for dangling `depends_on` references**
  - The audit gap is real only if the expectation is "surface this during `check`."
  - `config validate` already catches the underlying semantic error.
  - Retained problem: config/runtime coherence, not absence of validation.

### Already implemented or already tracked at baseline

- **"Make `init` scaffold a clean first-check state"**
  - Already recorded as delivered in `v0.12.0`.
  - It should not stay on the active milestone as an open item.

- **"Make `config suggest` detect decisions/planning/indexes at all"**
  - Already delivered at baseline.
  - The remaining problem is precision and transparency on mature repos.

### Deferred instead of committed scope

- **Full non-Markdown content governance**
  - Important, but broader than the immediate trust floor.
  - Near-term need is transparency when maintainers declare unsupported content governance, not full RST parsing.

- **Large config-model expansion bundle**
  - Co-presence constraints, OR-logic, changelog-entry validation, lifecycle transitions, and human-authored index integrity are individually credible.
  - Collectively they are too broad for direct backlog insertion before trust and contract issues are fixed.

## Consolidated Actionable Item List

### SYN-01

- **Title:** Restore a tested first-hour onboarding path
- **Source review artifact(s):** Fresh-Eyes Onboarding Audit
- **Problem summary:** The README still explains how to build Steward, but not how to use that build safely on another repository. It does not present a tested 10-15 minute flow, and the obvious `dotnet run --project ...` cross-repo path still fails when the target repository's `global.json` controls SDK resolution.
- **Why it matters:** Steward's value shows up quickly once it is running. Failing before that first value moment is the highest-leverage adoption and credibility loss in the current repo.
- **Affected audience:** contributor-facing, maintainer-facing, general product usefulness
- **Category:** documentation improvement, UX improvement
- **Priority:** `P0`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.16.0`), `docs/planning/pre-1-0-readiness-plan.md`
- **Suggested next action:** Add a tested "First 15 Minutes" README path that uses a repo-independent executable path or local tool-path install, explicitly states that the current working directory is the target repository, elevates `orient`, `init`, `status --coverage`, and `check`, and warns against the `global.json` trap.
- **Rationale:** This is the shortest path from "interesting repo" to "credible tool."

### SYN-02

- **Title:** Fix JSON-mode mutation safety for `refactor move`
- **Source review artifact(s):** AI-Agent Contract Review
- **Problem summary:** `refactor move --apply --output json` reports success and emits a plan payload, but does not move the file or rewrite references in current code.
- **Why it matters:** A mutation surface that no-ops under a successful machine-facing contract is a trust-breaking defect for both agents and maintainers.
- **Affected audience:** agent-facing, maintainer-facing, general product usefulness
- **Category:** bug fix, CLI contract improvement
- **Priority:** `P0`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.16.0`), `docs/planning/rfc-007-governance-enhancements-backlog.md`
- **Suggested next action:** Move side effects out of the text-only branch, return explicit `preview` and `applied` fields plus postcondition data in JSON, and add CLI tests that fail if the file move or reference rewrites do not occur.
- **Rationale:** This is a concrete correctness defect with a bounded fix and disproportionately high trust impact.

### SYN-03

- **Title:** Make JSON mode a reliable contract on success and expected failure
- **Source review artifact(s):** AI-Agent Contract Review
- **Problem summary:** `--output json` does not yet guarantee standard-envelope output, and common failure paths still return plain text instead of JSON.
- **Why it matters:** Steward explicitly targets AI-agent use. Agents cannot reliably automate it until JSON mode means "parseable JSON on stdout" for both success and expected failure.
- **Affected audience:** agent-facing, general product usefulness
- **Category:** CLI contract improvement
- **Priority:** `P0`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.17.0`), `docs/planning/pre-1-0-readiness-plan.md`, RFC-010 follow-on
- **Suggested next action:** Route remaining commands through `JsonEnvelopeWriter`, add structured error envelopes for usage/config/selector/regex/file-not-found failures, and stop overloading envelope `success` with domain-validation results.
- **Rationale:** This is the core machine-contract floor. Without it, "agent-ready" remains overstated.

### SYN-04

- **Title:** Correct the confirmed rule-system trust defects
- **Source review artifact(s):** Rule-System Completeness Audit
- **Problem summary:** Several validated rule issues still undermine correctness: STWD-008 and STWD-011 use scoped files when they need repo-wide context, STWD-015 undercounts frontmatter-matched families, and STWD-012 still emits a path-absent message.
- **Why it matters:** These are trust defects, not theoretical improvements. They produce false positives, false negatives, or diagnostics that are harder to act on than necessary.
- **Affected audience:** contributor-facing, maintainer-facing, agent-facing
- **Category:** bug fix, governance/rule improvement
- **Priority:** `P1`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.16.0`)
- **Suggested next action:** Land a small correctness batch for STWD-008, STWD-011, STWD-015, and STWD-012 before broader rule expansion; treat STWD-006 redesign as a bounded follow-on within the same trust track if capacity allows.
- **Rationale:** Rule correctness is a prerequisite for both scoped CI trust and adoption confidence.

### SYN-05

- **Title:** Reconcile the repo's runtime, changelog, and planning story
- **Source review artifact(s):** Fresh-Eyes Onboarding Audit
- **Problem summary:** Runtime/package metadata and public-facing docs still say `0.14.0`, while milestone/status artifacts describe `v0.15.0` as delivered. The repo currently tells two different stories about what version it is on.
- **Why it matters:** This is a visible credibility leak for anyone who leaves the README and checks current-state docs, release notes, or `steward version`.
- **Affected audience:** contributor-facing, maintainer-facing, general product usefulness
- **Category:** planning/doc consistency task
- **Priority:** `P1`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.16.0`), `docs/planning/pre-1-0-readiness-plan.md`
- **Suggested next action:** Decide whether the repo should be treated as "still on `0.14.0` with landed working-line changes" or "ready to bump to `0.15.0`," then update version metadata, README/current-state docs, and release-facing records together.
- **Rationale:** Version-story drift dilutes every other credibility improvement.

### SYN-06

- **Title:** Tighten help text, status language, and diagnostic coherence
- **Source review artifact(s):** Fresh-Eyes Onboarding Audit, Rule-System Completeness Audit
- **Problem summary:** Help still exposes `Steward.Cli` and missing placeholders; `check` and `orient --signals` still risk sounding cleaner than the repository state feels; several rule messages remain less self-contained than they should be.
- **Why it matters:** These are not blockers on their own, but they materially affect the feeling that the CLI is deliberate and trustworthy.
- **Affected audience:** contributor-facing, maintainer-facing, agent-facing
- **Category:** UX improvement, governance/rule improvement
- **Priority:** `P1`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.16.0`)
- **Suggested next action:** Finish the already-known help polish, reword warning-heavy `check` summaries, clarify `orient --signals` semantics, and clean up the highest-noise messages starting with STWD-012 and STWD-006.
- **Rationale:** These are small but compounding trust multipliers once the higher-priority defects are addressed.

### SYN-07

- **Title:** Finish the `config suggest` precision and transparency follow-on
- **Source review artifact(s):** Fresh-Eyes Onboarding Audit, RFC-007 backlog evidence
- **Problem summary:** `config suggest` is no longer shallow in the old sense, but it still lacks enough precision and confidence signaling on mature repos to be a dependable bootstrap surface.
- **Why it matters:** This is one of Steward's strongest maintainership promises. It should help maintainers author governance faster without seeding avoidable cleanup work.
- **Affected audience:** maintainer-facing, general product usefulness
- **Category:** config model improvement, UX improvement
- **Priority:** `P1`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.16.0`), `docs/planning/rfc-007-governance-enhancements-backlog.md`
- **Suggested next action:** Keep the existing precision work on the active milestone, add confidence/heuristic scoring, respect path-override/fixture-style exclusions, and label conservative suggestions as such.
- **Rationale:** This item is already correctly recognized as a baseline-depth gap; it needs sharper framing, not duplication.

### SYN-08

- **Title:** Prepare a scoped config-model follow-on for adoption-oriented governance
- **Source review artifact(s):** Config Expressiveness Stress Test
- **Problem summary:** The current policy model handles Markdown-centric governance well but still lacks clean support for non-Markdown transparency, intentionally ungoverned zones, and phase-in/grandfathering for mature repos.
- **Why it matters:** These are the next config-model gaps most likely to improve adoption credibility without trying to solve every advanced governance shape at once.
- **Affected audience:** maintainer-facing, general product usefulness
- **Category:** config model improvement, research/spike, RFC candidate
- **Priority:** `P2`
- **Recommended destination artifact or tracking mechanism:** `docs/planning/milestone-plan.md` (`v0.17.0`), new RFC candidate after current trust work lands
- **Suggested next action:** Scope one follow-on RFC around three adoption-oriented capabilities only: doctor/runtime transparency for unsupported content governance, intentionally ungoverned zones that remain discoverable, and a `new_files_only`/grandfathering mechanism.
- **Rationale:** This keeps the design effort aligned with real repo adoption pain instead of immediately expanding into the full advanced wish list.

## Priority Breakdown

| Priority | Items | Notes |
|----------|-------|-------|
| `P0` | `SYN-01`, `SYN-02`, `SYN-03` | These are the clearest adoption and trust blockers |
| `P1` | `SYN-04`, `SYN-05`, `SYN-06`, `SYN-07` | High-value near-term work that materially improves credibility and day-to-day usefulness |
| `P2` | `SYN-08` | Important follow-on design work, but not before the current trust floor is repaired |
| `P3` | None retained as committed backlog | Broad config-model expansion remains deferred, not scheduled |

## Dependency And Order Notes

1. `SYN-05` should be resolved before the next release-oriented doc refresh so README, version output, changelog, and planning all tell the same story.
2. `SYN-01` and `SYN-06` should be executed together where possible because the first-hour path depends on help/output language not feeling provisional.
3. `SYN-02` should precede or be bundled with the broader machine-contract work because it is a concrete safety defect, not an RFC-level design question.
4. `SYN-03` should establish the machine-contract floor before `SYN-08` expands the config model. Otherwise the repo risks adding model power on top of an unreliable automation contract.
5. `SYN-04` should land early in the trust-repair sequence because scoped CI and family-based governance are already part of the claimed baseline.

## Recommended Backlog Additions

| Destination artifact | Recommended additions |
|----------------------|-----------------------|
| `docs/planning/milestone-plan.md` | Refresh `v0.16.0` around onboarding trust, rule/runtime correctness, version-story cleanup, `config suggest` precision, and help/UX polish; refresh `v0.17.0` around JSON-contract completion and scoped config-model follow-ons |
| `docs/planning/pre-1-0-readiness-plan.md` | Add onboarding credibility and machine-contract reliability as active pre-stable readiness work |
| `docs/planning/rfc-007-governance-enhancements-backlog.md` | Downgrade G7-19 from "fully implemented" to "implemented baseline with JSON-mode safety follow-on"; keep G7-20 as baseline-depth work |
| `docs/planning-index.md` | Link this synthesis artifact as the canonical planning record for the April 18 review cycle |

## Recommended ADR / RFC Follow-Ups

- **RFC-010 follow-on:** Narrow follow-up to universal envelope coverage, structured failure envelopes, and unambiguous process-success semantics.
- **RFC-009 follow-on:** Keep the first implementation slice narrow and handoff-focused: `search`, `refs`, `check`, and `explain path` should share one reusable address/provenance story.
- **New RFC candidate after trust work lands:** Adoption-oriented config model extensions covering non-Markdown transparency, intentionally ungoverned zones, and grandfathering/new-files-only behavior.

No new ADR is recommended from this synthesis. The current gaps are execution, contract, and scoped config-model follow-on issues rather than architecture-decision reversals.

## Deferred Items With Rationale

| Item | Rationale for deferral |
|------|------------------------|
| Full RST or non-Markdown structural governance | Valuable, but broader than the current trust and adoption floor; transparency is the nearer-term need |
| CODEOWNERS coverage or source-code layer enforcement | Credible maintainer concerns in the stress test, but outside Steward's current core domain |
| Co-presence constraints, OR-logic, changelog-entry validation, lifecycle transitions, human-authored index integrity | Real future design space, but too broad for direct backlog insertion before contract and correctness work are stabilized |
| New standalone rules for duplicate artifact paths or unknown roles | Plausible gaps, but not raised by the validated review cycle as the highest-value immediate work |

## Final Recommendation

The next work should not be "add more surface area." It should be "make the existing surface trustworthy."

The strongest next slice is:

1. fix the repo story and first-hour onboarding path
2. fix the confirmed correctness and safety defects (`refactor move`, STWD-008/STWD-011/STWD-015/STWD-012)
3. complete the JSON contract floor

If Steward does those three things before expanding the config model, it will move materially closer to being both useful and believable for the maintainers and agents it is trying to serve.
