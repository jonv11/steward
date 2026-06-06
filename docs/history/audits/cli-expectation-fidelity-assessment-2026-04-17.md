---
type: audit
status: Historical
last_updated: 2026-04-17
standalone: true
---

# CLI Expectation Fidelity Assessment - 2026-04-17

## Executive Assessment

### Verdict

**Final recommendation:** expectations are partly met but need targeted correction.

Steward is already a real repository stewardship CLI on this repository. A serious contributor or power user would get clear value from `orient`, `status`, `check`, `maintain`, `config show --effective`, `refs`, `refactor move --preview`, and the stronger parts of the Markdown subsystem. The product is meaningfully more useful than a generic validator or Markdown helper.

However, the experience is not yet fully coherent enough to call the repo promise fully met. The biggest trust problems are not missing command nouns; they are mismatches between documented/configured behavior and real behavior:

- `validation.severity_overrides` is documented, validated, and exampled, but not applied at runtime.
- `explain path` still overstates rule applicability and under-reports family membership on explicit artifacts.
- `config suggest` is still too noisy on this repo to feel trustworthy as a serious maintainer bootstrap surface.
- The repo's own active planning/state artifacts disagree with each other about what has shipped.

### Where Steward clearly succeeds today

- Session-start orientation on this repo is strong and genuinely helpful.
- Status/reporting surfaces are coherent and useful for humans and agents.
- The stewardship loop is more than validation: orientation, governance inspection, maintenance, reference discovery, and safe move preview all work.
- Deterministic maintenance and repo structure management are credible.
- The rule set is substantial and differentiated from generic linting.
- Dogfooding is real: this repo uses start-here, completion policy, artifact families, maintenance, and coverage exclusions in meaningful ways.

### Where it clearly falls short

- Maintainer bootstrap and config-authoring trust are weaker than the README suggests.
- Some config surface area is still dead or misleading.
- Explainability is good at the rule level but still uneven at the file-governance level.
- Repo-self-truth is not fully coherent; some active docs still lag shipped behavior.

### Product-promise trust level

- Full-repo stewardship trust: **moderate-high**
- Maintainer bootstrap/config trust: **moderate**
- Fine-grained explainability trust: **moderate**
- AI-agent contract trust: **moderate**
- Overall current promise trust: **moderate**

## Method And Evidence

### Review basis

This assessment is grounded in:

- Docs: `README.md`, `docs/requirements/PRD.md`, `docs/implementation-status.md`, `docs/planning-index.md`, `docs/planning/milestone-plan.md`, `docs/planning/pre-1-0-readiness-plan.md`, `docs/planning/implementation-instructions.md`, `docs/decisions/decision-index.md`, and accepted RFCs.
- Repo contract: `.steward/config.yaml`, `.steward/policy.yaml`, `.steward/path-policy.yaml`, `STRUCTURE.md`.
- Code: `src/Steward.Cli` command implementations and `src/Steward.Core` config, validation, maintenance, orientation, search, and Markdown subsystems.
- Tests: CLI and core tests, especially config/bootstrap/explainability/family-rule suites.
- Runtime behavior: local build/test plus live CLI execution on this repository and on throwaway repos for onboarding/config repros.

### Commands exercised

- `dotnet build steward.sln`
- `dotnet test steward.sln --no-build`
- `steward orient`, `orient --signals`, `orient --output json`
- `steward status`, `status --coverage --output json`
- `steward check`, `check --output json`, `check --scope changed`
- `steward explain STWD-003`, `explain path ...`, `explain path ... --output json`
- `steward config validate`, `config doctor`, `config show --effective`, `config suggest`
- `steward maintain`, `maintain --diff`, `maintain --output json`
- `steward outline`, `steward search`, `steward refs`, `steward refactor move --preview`
- `steward md outline`, `md query`, `md edit fm-validate`
- `steward init --profile software` in a throwaway repo

### Runtime evidence highlights

- Build/test baseline is healthy: `627` tests passing (`436` core, `191` CLI), matching `docs/implementation-status.md`.
- `status --coverage --output json` on this repo reports `59/59` governed Markdown files and `100%` coverage, which is a strong dogfooding signal.
- `check` on this repo passes but still reports one informational discoverability issue (`STWD-013`) for `docs/audits/code-quality-review-2025-07-23.md`.
- `config suggest` on this repo proposes test-fixture files under `tests/Steward.TestFixtures/Repos/**` as real artifact candidates.
- Fresh `init --profile software` does not fail hard on first `check`, but first `check` still warns on policy-declared `CHANGELOG.md` and `CONTRIBUTING.md`.
- In a throwaway repo with `validation.severity_overrides: { STWD-008: error }`, a broken link still reports as `warn`, not `error`.

## Expectation Model

### Product promise expectations

1. Steward is a stewardship CLI, not just a validator.
2. Steward is a primary surface for both humans and AI agents.
3. Policy/config surfaces should be trustworthy, explicit, and explainable.
4. Markdown should be a first-class structural document type.
5. Maintenance should be deterministic, preview-first, and low-noise.
6. Pre-1.0 messaging should be honest about what is shipped, deferred, and rough.

Primary sources:

- `README.md:3-27`
- `docs/requirements/PRD.md:16-59`
- `docs/implementation-status.md:7-18`

### Primary persona expectations

1. Maintainer: bootstrap repo governance, inspect config, validate semantics, understand drift, and maintain generated artifacts.
2. Contributor: orient, validate work, understand failures, refresh maintained artifacts, and re-check.
3. AI agent: consume stable-enough JSON surfaces for inspect -> change -> validate -> remediate loops.
4. Repo maintainer using Steward-on-Steward: trust the repo's own docs, config, and maintained artifacts as proof that the product works.

Primary sources:

- `README.md:69-201`
- `docs/requirements/PRD.md:34-49`
- `.steward/policy.yaml:176-241`

### Workflow expectations

1. Session-start orientation.
2. Governance understanding for a specific path.
3. Full and scoped validation.
4. Maintainer bootstrap by analysis.
5. Deterministic maintenance after structural changes.
6. Structural Markdown inspection/editing for humans and agents.
7. Safe move/rename flows with reference updates.

Primary sources:

- `README.md:430-521`
- `docs/requirements/PRD.md:80-110`
- RFC-004, RFC-005, RFC-006, RFC-007, RFC-008

### Command-family expectations

1. `orient`, `outline`, `search`, and `status` should feel like a coherent discovery/reporting family.
2. `check` and `explain` should form a coherent diagnose/remediate pair.
3. `config show`, `validate`, `doctor`, and `suggest` should form a coherent maintainer authoring loop.
4. `md query`, `md outline`, and `md edit` should feel like one deliberate Markdown subsystem.
5. `refs` and `refactor move` should create a credible safe-structure workflow.

### Repo-self-usage expectations

1. The repo should demonstrate Steward's strongest use case, not workaround it.
2. Active repo truth should be synchronized across README, planning docs, implementation status, config, and runtime behavior.
3. The repo should show that Steward helps maintain itself, not just that commands can be run inside the repo.

Primary sources:

- `README.md:509-521`
- `.steward/policy.yaml`
- `docs/planning-index.md:39-74`

### Quality and release expectations

1. Pre-1.0 roughness is acceptable, but false confidence is not.
2. Contract/config surfaces should not accept knobs they do not honor.
3. Active planning docs should not contradict shipped code on core capability.
4. The strongest claims should be backed by runtime behavior on this repo.

## Expectation-To-Reality Matrix

| Expectation | Source | Status | Rationale and evidence |
| --- | --- | --- | --- |
| Steward is more than a validator | README, PRD | mostly fulfilled | `orient`, `status`, `maintain`, `refs`, `refactor move`, and `md` surfaces provide real stewardship value beyond `check`. |
| Session-start orientation should be strong on this repo | README, PRD, RFC-005 | fulfilled | `orient` and `status` are genuinely useful on this repo; start-here entries, artifact roles, family counts, and coverage create a credible repo-start surface. |
| `check` is the canonical workflow entry point | README, PRD | mostly fulfilled | Full check is useful, deterministic, and informative. `check --scope changed` behaves correctly on a clean tree. The weaker part is config trust around severity overrides and pass-with-info nuance. |
| Maintainer bootstrap should be coherent: init -> suggest -> validate -> doctor -> check | README, RFC-007 | partially fulfilled | `validate`, `doctor`, and `show --effective` are good. `init` does not create `path-policy.yaml` despite README claim, first `check` still warns, and `suggest` is too noisy on this repo. |
| File-level governance explanation should reflect actual applicability | README, RFC-007 | partially fulfilled | `explain path` gives useful artifact/path-policy/frontmatter data, but it lists STWD-014/015/016 on files with no family match and hides family membership for explicit artifacts under family patterns. |
| Configuration should be expressive and debuggable | PRD, README | mostly fulfilled | Strong on `config show --effective`, `validate`, and `doctor`. Weaker because `severity_overrides` is validated but not applied, and explicit-artifact precedence still forces duplicate frontmatter config in this repo. |
| Artifact families should reduce config duplication and scale governance | README, RFC-008 | mostly fulfilled | Family classification and STWD-003/014/015/016 are implemented and tested, but explicit artifacts still take precedence in a way that weakens family inheritance and complicates dogfooding. |
| Markdown is a first-class structural subsystem | PRD, RFC-004 | mostly fulfilled | `md outline`, `md query --pattern`, `md edit`, and `fm-validate` are real and useful. Selector ergonomics and help quality are still thinner than the subsystem ambition. |
| Safe structural change workflows should exist | RFC-007 | mostly fulfilled | `refs` and `refactor move --preview` are useful and differentiated. Preview currently lists touched files rather than showing per-link diffs, so confidence is good but not maximal. |
| AI-agent JSON surfaces should be useful and stable | README, PRD, ADR-010 | mostly fulfilled | JSON exists on core commands and is useful. Shapes are inconsistent across commands and there is no common envelope, which weakens agent integration polish. |
| The repo should convincingly dogfood Steward | README, `.steward/policy.yaml` | mostly fulfilled | The repo uses real policy, completion rules, start-here, coverage exclusions, structure maintenance, and artifact families. Confidence is reduced by contradictory active docs and config workarounds. |
| Repo truth should stay coherent across docs, config, code, and runtime | README, planning docs, implementation status | partially fulfilled | `implementation-status.md` says STWD-014/015/016 are delivered, while active planning docs and RFC-008 scope notes still defer them. This is a direct repo-self-stewardship miss. |
| Pre-1.0 messaging should be honest and scoped to current reality | README, implementation status, ADR-013 | mostly fulfilled | The repo is explicit about being pre-1.0 and not publicly shipped. The remaining problem is local honesty drift in active planning docs and dead config/documentation surfaces. |

## Workflow Assessment

| Workflow | Intended path | Observed behavior | Friction points | Severity | Recommended improvement |
| --- | --- | --- | --- | --- | --- |
| New contributor orientation on this repo | `orient --signals` -> `status --coverage` -> `planning-index.md` | Strong. The default compact orient view is useful, and status coverage is a high-trust summary. | Minor text-mode polish only. | low | Keep current shape; preserve readability and semantic classification. |
| Contributor validation and remediation | `check` -> `explain` -> `maintain` -> re-check | Mostly strong. Diagnostics are readable and remediation exists. | `PASS` can coexist with visible info diagnostics; severity override dead surface weakens trust when repos want stricter gating. | medium | Implement severity overrides and make "pass with infos" more visually explicit. |
| Maintainer bootstrap for a new repo | `init` -> `config suggest` -> `config validate` -> `config doctor` -> `check` | Partial. Better than earlier versions, but still not a convincingly clean start. | README says `path-policy.yaml` is created; it is not. Fresh check still warns. `suggest` quality is too noisy on mature repos. | high | Make init/create/check path genuinely clean and improve suggestion precision plus confidence/exclusion logic. |
| Governance debugging for a specific file | `config show --effective` + `explain path <file>` | Mixed. `config show --effective` is excellent. `explain path` is useful but not fully trustworthy. | Family membership on explicit artifacts is hidden; family-only rules appear on unrelated files. | high | Fix applicability filtering and show both explicit artifact match and family match when both matter. |
| Structural Markdown inspection/editing | `md outline`, `md query`, `md edit`, `fm-validate` | Mostly strong, especially for deterministic/agent use. | Selector syntax is exact and unforgiving; help assumes MdPath familiarity. | medium | Improve help/examples and add more user-facing selector guidance before adding broader selector power. |
| Safe move/rename flow | `refs <path>` -> `refactor move --preview` | Good and differentiated. Preview found 8 impacted files for a planning-index rename. | Preview is file-list level, not link-diff level. | low-medium | Add optional per-file or per-link diff details in preview/JSON output. |
| Maintained artifact refresh | `maintain`, `maintain --diff`, `maintain --apply` | Strong. Preview/apply semantics are coherent and deterministic. | No major issue observed on this repo because artifact was already up to date. | low | Keep behavior stable; add more diff evidence when changes exist. |

## CLI UX Assessment

### Naming and command-family coherence

Overall naming is good. `orient`, `outline`, `status`, `search`, `check`, `maintain`, `refs`, and `refactor` feel deliberate rather than random. The command families are more coherent than most pre-1.0 CLIs.

The weaker area is the Markdown family. `md query` and `md edit` are conceptually strong, but the subsystem still depends on users already understanding selector syntax and operation semantics.

### Help text and discoverability

Strengths:

- Root help is organized and the command surface is easy to scan.
- Descriptions are generally accurate and concise.
- High-value options like `--signals`, `--scope`, `--fix`, `--diff`, and `--regex` are present.

Weaknesses:

- Source-build help uses `Steward.Cli` in usage text instead of `steward`, which conflicts with the README's recommended binary name.
- Several options lose their value placeholders in generated help: `--config`, `--artifact`, `--role`, `--max`.
- `md query` help gives examples like `heading[Status]`, but not enough help for users who do not already know the selector grammar.
- Subcommand help is descriptive, but still light on operational examples.

### Defaults and output behavior

Strengths:

- Text-mode `orient` defaulting to compact output is a good default for session-start use.
- `status` is a strong default summary command.
- `check --fix` preview-first semantics are coherent.
- JSON output exists on all major surfaces exercised in this review.

Weaknesses:

- `check` returns `PASS` even when it emits informational governance findings; that is technically defensible but can read as cleaner than it is.
- JSON shapes are useful but inconsistent. `check`, `status`, `search`, `orient`, `maintain`, and `explain path` all use different top-level envelopes and naming conventions.

### Error and remediation experience

Strengths:

- Rule-level remediation text is present and generally useful.
- `config validate` and `TryBuild` error paths point users toward `steward config validate`.
- `refs` and `refactor move --preview` feel safely bounded.

Weaknesses:

- `config suggest` produces output that looks authoritative even when suggestion quality is low.
- `md query` fails cleanly, but the recovery path is mostly "already know MdPath syntax."

### Consistency between docs and actual UX

The biggest consistency issues are:

- README says `init` creates `path-policy.yaml`; runtime does not.
- README documents `validation.severity_overrides`; runtime ignores it.
- README and command descriptions imply `explain path` shows rules that apply to the specific file; actual output still includes family-only rules too broadly.
- Active planning docs still defer features that the code and tests already implement.

## Repository Stewardship Value

This is where Steward is strongest.

On this repository, Steward clearly does more than "check files":

- It creates a high-value navigation surface (`orient`, `status`, `planning-index.md`, `STRUCTURE.md`).
- It reduces ambiguity around what is important (`start_here`, required/recommended artifacts, state docs, family counts).
- It makes repo governance visible rather than implicit (`config show --effective`, `explain path`, `check` completion summaries).
- It keeps artifacts in sync in a deterministic, reviewable way (`maintain` + stale-artifact rule).
- It supports repo structure operations that generic linters do not (`refs`, `refactor move --preview`, structural Markdown commands).

The product's distinct value is strongest in the combined loop:

1. `orient` / `status` tell you what matters.
2. `check` tells you what is wrong.
3. `explain` tells you why.
4. `maintain` / `refactor` / `md` help you fix it safely.

That loop is real today. The main reason the overall verdict is not stronger is that some supporting surfaces still inject avoidable doubt into that loop.

## Dogfooding Assessment

### Does this repo use Steward in a confidence-inspiring way?

Mostly yes.

Strong evidence:

- `.steward/policy.yaml` is non-trivial and repo-specific.
- The repo uses real `start_here`, `completion_policy`, artifact families, maintenance, and coverage exclusion.
- `status --coverage --output json` reports full governed coverage on this repo.
- `STRUCTURE.md` is maintained and the repo explicitly tells contributors to refresh it after structural changes.
- `planning-index.md` acts as a meaningful navigation hub, not an empty showcase artifact.

### Where dogfooding exposes rough edges

- The repo carries a workaround comment acknowledging that explicit artifacts do not inherit family schemas, so path-scoped frontmatter rules still need duplication.
- The repo's own active docs disagree about whether family rules STWD-014/015/016 are delivered or deferred.
- One historical audit still triggers STWD-013, which is minor but symbolically relevant in a repo claiming stewardship coherence.

### Does the repo seem to work around the tool?

In a few places, yes:

- `validation.frontmatter_requirements` is still used to compensate for explicit-artifact/family interaction.
- `coverage.exclude` is necessary to keep test fixtures from diluting governance coverage.
- Planning docs are not fully kept in sync with code/test truth, even though keeping state artifacts coherent is one of Steward's stated values.

## Prioritized Findings

1. **Critical credibility gap:** `validation.severity_overrides` is a dead config surface.
   Evidence: README config example includes it (`README.md:353-359`), `RepositoryPolicy` models it, `ConfigLoader` validates it (`src/Steward.Core/Configuration/ConfigLoader.cs:165-176`), but `ValidationEngine` never applies it (`src/Steward.Core/Validation/ValidationEngine.cs:15-58`). Runtime repro: a repo overriding `STWD-008` to `error` still reports `[warn] STWD-008`.
   Suggested direction: either implement severity rewriting before output/summary/exit-code computation, or remove/undocument the surface until it is real.

2. **Important product gap:** `explain path` is still not trustworthy enough as an "effective governance" surface.
   Evidence: runtime `explain path steward.sln` lists STWD-014/015/016 even though no family applies; `explain path docs/planning/milestone-plan.md --output json` reports `matchedFamily: null` while still listing family-only rules. Code cause: fallback `_ => true` in applicability filtering plus family resolution gated on `artifactSummary == null` (`src/Steward.Cli/Commands/ExplainCommand.cs:372-452`).
   Suggested direction: separate "explicit artifact match" from "family match", and apply family-rule filtering only when a family actually matches.

3. **Important product gap:** `config suggest` is still too noisy for serious maintainer bootstrap on mature repos.
   Evidence: live output on this repo suggests fixture repo files under `tests/Steward.TestFixtures/Repos/**` as artifact candidates. `BootstrapAnalyzer` scans discovered files generically and has no confidence model or fixture/test exclusion logic (`src/Steward.Core/Configuration/BootstrapAnalyzer.cs:48-142`).
   Suggested direction: honor repo-specific excludes or add mature-repo heuristics/confidence scoring so suggestion output feels reviewable instead of accidental.

4. **Technical debt affecting trust:** active repo truth documents contradict shipped implementation.
   Evidence: `docs/implementation-status.md:94-96` says STWD-014/015/016 are implemented; `docs/planning/milestone-plan.md:39-40,76` and `docs/planning/implementation-instructions.md:33-40` still frame the same features as upcoming `v0.14.0` work; RFC-008 still defers them in the accepted-scope section (`docs/decisions/rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md:245-251`).
   Suggested direction: align active planning/state docs with current code/test truth and clearly mark RFC-008 scope notes as historical if later work exceeded them.

5. **Workflow gap:** the fresh `init` experience is improved but still not convincingly clean.
   Evidence: README says init creates `config.yaml`, `policy.yaml`, and `path-policy.yaml` (`README.md:79`), but runtime creates only the first two. Fresh `check` on a new software-profile repo still warns on missing `CHANGELOG.md` and `CONTRIBUTING.md`.
   Suggested direction: either scaffold a clean first-check policy or message optional missing artifacts more deliberately. Also correct the README claim about `path-policy.yaml`.

6. **UX inconsistency:** help/output polish still leaks implementation details.
   Evidence: runtime help uses `Steward.Cli` instead of `steward`; several options lose value placeholders (`--config`, `--artifact`, `--role`, `--max`). This is minor, but it makes the CLI feel less finished than the docs imply.
   Suggested direction: pin command name explicitly and review option help metadata for placeholder clarity.

7. **Nice-to-have improvement:** JSON surfaces are useful but not yet uniform enough for a single strong agent contract.
   Evidence: `check`, `status`, `search`, `orient`, `maintain`, and `explain path` all have different top-level shapes. This does not break utility, but it does increase adapter friction.
   Suggested direction: define a light common JSON envelope or response-contract guidance for the major command families.

## Scoring

| Dimension | Score (0-5) | Rationale |
| --- | --- | --- |
| Promise fidelity | 3.0 | Core promise is materially delivered, but dead config and doc/runtime mismatches still matter. |
| Workflow usefulness | 3.5 | Core contributor and maintainer loops are useful; bootstrap and explain-path depth are weaker. |
| CLI ergonomics | 3.5 | Command family design is good; help/value placeholder polish and subsystem discoverability lag. |
| Stewardship value | 4.0 | Steward clearly justifies itself as more than a validator on this repo. |
| Dogfooding quality | 3.0 | Real and substantial, but repo-self-truth drift weakens confidence. |
| Configurability | 2.5 | Powerful in shape, but some surfaces are partial or misleading in practice. |
| Markdown subsystem quality | 3.5 | Useful and real, but still sharp-edged in selector UX and guidance. |
| Governance / rule-system quality | 4.0 | Rule set is meaningful, differentiated, and well tested. |
| AI-agent usefulness | 3.5 | Good JSON coverage and deterministic behavior, but contract consistency and explainability fidelity still need work. |
| Release-line credibility | 3.0 | Pre-1.0 framing is mostly honest, but active-doc drift and dead config reduce trust. |

**Overall score:** **3.4 / 5**

### Narrative score verdict

Steward on Steward is already beyond "promising prototype" and into "real, differentiated tool with specific trust gaps." The right interpretation is not "the product needs repositioning," but also not "the current promise is already fully earned." The repo is close enough that targeted correction will materially improve trust quickly.

## Final Recommendation

**Expectations are partly met but need targeted correction.**

Rationale:

- The core stewardship surface is already useful and differentiated.
- The repo successfully demonstrates real value in orientation, governance visibility, maintenance, and structural operations.
- The remaining issues are mostly trust-and-coherence problems rather than total capability gaps.
- Those problems matter because they sit directly in maintainer bootstrap, file-level explainability, config trust, and repo-self-honesty.

## Actionable Remediation Plan

### Fix immediately

1. Implement `validation.severity_overrides` end-to-end or remove/undocument it until it is real.
2. Fix `explain path` to report actual family membership and to stop listing family-only rules on unrelated files.
3. Align active planning/state docs with shipped STWD-014/015/016 behavior and mark outdated deferred wording as superseded.
4. Correct the README `init` claim about `path-policy.yaml` and tighten the first-check bootstrap story.

### Next milestone

1. Improve `config suggest` precision for mature repos, with confidence signaling and better test-fixture/sample exclusion behavior.
2. Make init scaffolding land in a genuinely clean starter state, not merely a non-failing one.
3. Improve help text polish: command name, value placeholders, and more operational examples for Markdown selectors and config commands.
4. Revisit explicit-artifact precedence so family schemas can be leveraged more consistently without duplicated path-scoped rules.

### Later / optional

1. Standardize JSON response envelopes across the major command families.
2. Add richer preview detail for `refactor move` and multi-file maintenance changes.
3. Improve Markdown selector ergonomics, including better discoverability and later fuzzy/contains matching if still justified.

## Closing Judgment

If a serious contributor cloned this repo today and used Steward as the primary repository stewardship surface, the experience would feel **genuinely promising and often strong**, not hollow. But it would still feel **partially uneven** in exactly the places where maintainers need trust most: bootstrap quality, config truth, and deep explainability.

That is a good pre-1.0 position. It is not yet a fully earned "the repo promise is broadly met" position.
