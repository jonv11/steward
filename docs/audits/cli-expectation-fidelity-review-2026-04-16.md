# CLI Expectation Fidelity Review — 2026-04-16

- **Status:** Complete
- **Reviewer role:** Senior principal engineering, CLI UX, and repository-governance assessment
- **Scope:** Whether the current Steward CLI, used on the Steward repository itself, lives up to the repo's stated promise, accepted product expectations, and intended contributor/agent workflows
- **Source-of-truth order used:** `README.md`, accepted requirements and decision artifacts, active status/planning docs, self-dogfooded `.steward/` config, code/tests, then live CLI execution on this repo

---

## 1. Executive Assessment

The current Steward CLI is **no longer just a checker**. On this repository, it already provides a materially useful stewardship surface through `orient`, `status`, `check` (full scope), `maintain`, `refs`, `explain`, and Markdown structure inspection. The repo's pre-`1.0` messaging is also much more honest than earlier historical snapshots: the README, implementation status, and blocker docs now mostly describe a real pre-stable product rather than an imagined finished one.

However, the CLI **does not fully live up to the stronger expectations the repo now sets for it as the primary stewardship surface**. The most serious problem is that the documented changed/staged workflow is currently untrustworthy on the dogfooded repo itself: `steward check --scope changed` and `--scope staged` return large numbers of false missing-artifact and broken-reference failures on a clean working tree. That is a direct break in one of the core workflows the PRD, README, and RFC-001 present as central.

The product is strongest today in:

- full-repository inspection and maintenance
- configuration transparency via `config show --effective`
- Markdown query/outline operations
- governance/status surfacing in human-readable text mode
- deterministic artifact maintenance and rule explainability basics

It is weakest today in:

- changed/staged validation trust
- governance coverage for JSON/agent consumption
- bootstrap-by-analysis depth (`config suggest` is much too shallow for this repo)
- the gap between the RFC-007 status ledger's confident "implemented" language and the actual depth of several governance-assistance surfaces

**High-level verdict:** the CLI's current expectations are **partly met but need targeted correction**.

**Trustworthiness of the current product promise:** moderate for full-repo stewardship; low for changed/staged inner-loop use until scoped validation is fixed.

---

## 2. Method And Evidence

### Reviewed expectation sources

- `README.md`
- `docs/requirements/PRD.md`
- `docs/requirements/assumptions-constraints.md`
- `docs/implementation-status.md`
- `docs/planning/pre-release-blockers.md`
- `docs/planning/rfc-007-governance-enhancements-backlog.md`
- `docs/decisions/rfcs/RFC-001-cli-command-structure.md`
- `docs/decisions/rfcs/RFC-002-configuration-model.md`
- `docs/decisions/rfcs/RFC-004-markdown-structural-model.md`
- `docs/decisions/rfcs/RFC-005-orientation-search-outline.md`
- `docs/decisions/rfcs/RFC-006-maintenance-and-memory.md`
- `docs/decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md`
- `.steward/config.yaml`
- `.steward/policy.yaml`
- `.steward/path-policy.yaml`

### Inspected implementation surfaces

- CLI command registration and global option handling
- `check`, `status`, `orient`, `config`, `md`, `search`, `maintain`, `refs`, `refactor`, and `explain` command implementations
- rule registry and the rule implementations most relevant to repo-wide vs scoped validation
- profile defaults, profile merge behavior, bootstrap analyzer, and move engine
- representative tests, especially snapshot/help, profile readiness, governance coverage, markdown editing, refs, search, staged completeness, and impact tests

### Executable evidence gathered

- `dotnet build steward.sln` => success
- `dotnet test steward.sln --no-build -m:1` => `505` passing, `0` failing
- `steward orient --signals`
- `steward orient --compact`
- `steward status --coverage`
- `steward status --output json`
- `steward status --coverage --output json`
- `steward check`
- `steward check --scope changed`
- `steward check --scope staged`
- `steward config show --effective`
- `steward config doctor`
- `steward config suggest --output json`
- `steward explain path docs/planning-index.md`
- `steward explain STWD-013`
- `steward maintain --artifact structure --diff`
- `steward search governance --mode headings`
- `steward refs docs/planning-index.md --output json`
- `steward md outline README.md`
- `steward md query README.md "heading[Using Steward In This Repo]"`

### Key runtime observations

- `steward check` passes cleanly on the repo at full scope.
- `steward check --scope changed` and `--scope staged` fail on the clean repo with false `STWD-001`, `STWD-007`, and `STWD-009` diagnostics while `Files checked: 0`.
- `steward status --coverage` is useful in text mode, but `steward status --coverage --output json` omits coverage data entirely.
- `steward config suggest --output json` returns only three artifact suggestions on this repo: `README.md`, `STRUCTURE.md`, and `docs/requirements/PRD.md`.
- `steward md edit --help` exposes `fm-set` and `fm-merge`, but not `fm-validate`.

---

## 3. Expectation Model

### Product promise expectations

- Steward promises a **repository stewardship CLI**, not merely a validator.
  - Sources: `README.md`, PRD section 4, RFC-001, RFC-006, RFC-007.
- Steward promises to work for **humans and AI agents** with coherent text and JSON surfaces.
  - Sources: `README.md`, PRD sections 1, 3, and 4, RFC-001, RFC-003, RFC-007.
- Steward promises **deterministic, preview-first, policy-driven** maintenance and structural editing.
  - Sources: PRD goals, RFC-004, RFC-006.
- Steward promises **pre-1.0 honesty** rather than stable-release theater.
  - Sources: `README.md`, `docs/implementation-status.md`, `docs/planning/pre-release-blockers.md`, ADR-013.

### Primary persona expectations

- A new serious contributor should be able to orient quickly and identify authoritative repo documents.
- A maintainer should be able to validate governance, inspect effective policy, detect drift, and refresh maintained artifacts.
- An AI agent should be able to consume stable JSON for state, diagnostics, references, and suggestions.
- A pre-commit or staged-scope user should be able to trust changed/staged validation results.

### Workflow expectations

- Session-start orientation should be curated and operationally useful.
- Full-repo validation should be trustworthy and explainable.
- Changed/staged validation should support real inner-loop use.
- Governance coverage should expose where the repo is and is not meaningfully governed.
- Config authoring/debugging should be inspectable, validatable, and diagnosable.
- Bootstrap/suggestion flows should reduce setup effort on mature repositories.
- Markdown structural work should be strong enough for real documentation maintenance.
- Safe move flows should reduce drift during file moves.

### Command-family expectations

- `orient` answers "what is this repo and where do I start?"
- `outline` answers "what is inside this directory or file?"
- `search` answers "where can I find X?"
- `status` gives a lightweight state surface without full validation.
- `check` is the canonical workflow command, including scoped validation.
- `config` surfaces actual effective behavior, not just raw YAML.
- `md` is a coherent structural subsystem, not a bag of unrelated commands.
- `maintain` is explicit, preview-first, and deterministic.
- `refs` and `refactor` should close the loop between analysis and safe change.

### Repo-self-usage expectations

- The README explicitly tells contributors to use Steward as the **first navigation surface** on this repo: `orient --signals`, `status --coverage`, `check`.
- The repo's own `.steward/policy.yaml` declares start-here docs, maintained artifacts, naming policy, and completion policy, so Steward should be able to interpret and police those choices convincingly.
- The repo should demonstrate Steward's value rather than work around it.

### Quality and release expectations

- Pre-`1.0` roughness is acceptable, but core documented workflows must still be trustworthy.
- Stronger public claims should wait until the remaining blockers and credibility gaps are closed.
- Accepted RFC/Roadmap artifacts that claim an enhancement is "implemented" create an internal expectation that the shipped depth is operationally meaningful, not merely present as a command stub.

---

## 4. Expectation-To-Reality Matrix

| Expectation | Source | Status | Rationale and evidence |
|---|---|---|---|
| Steward should feel like a stewardship tool, not just a validator | `README.md`, PRD, RFC-001, RFC-006 | Mostly fulfilled | The repo benefits from `orient`, `status`, `maintain`, `refs`, `md query`, and `explain path`, not just `check`. This is materially stronger than a linter-only surface. |
| Full-repo validation should be trustworthy on the Steward repo | PRD UC-03, README quick start | Fulfilled | `steward check` passes cleanly on the repo and rule explanations/remediation are coherent. |
| Changed/staged validation should support pre-commit and agent inner loops | PRD UC-02, REQ-VALIDATE-002/003, RFC-001 | Not fulfilled | On the clean repo, `steward check --scope changed` and `--scope staged` report false missing artifacts and broken policy references with `Files checked: 0`. `RequiredArtifactRule`, `BrokenArtifactReferenceRule`, and `StaleArtifactRule` all evaluate repo-wide obligations against `context.TargetFiles` rather than repository existence. |
| `orient` should be a curated session-start map | PRD REQ-ORIENT-001..013, RFC-005 | Partially fulfilled | `orient --compact` is close to the promised surface. Default `orient --signals` on this repo is much longer and more exhaustive than the RFC's "curated, high-level" framing. |
| `status --coverage` should provide meaningful governance coverage reporting | RFC-007 G7-16, README repo-use guidance | Partially fulfilled | Text-mode coverage is useful and honest. It reports `50/59 Markdown files (85%)` and calls out unguided fixture-repo docs. But JSON mode drops coverage entirely, which weakens the dual-audience promise. |
| The repo should expose effective policy transparently | RFC-002, RFC-007 G7-06 | Mostly fulfilled | `config show --effective` is strong and one of the best operator surfaces in the product. It exposes raw files, effective runtime defaults, and merged policy. |
| `config doctor` should detect ineffective governance | RFC-007 section 5.2, backlog G7-07 | Weakly fulfilled | It catches some useful cases, but only a narrow subset: dead `start_here`, overlapping global frontmatter declarations, missing artifacts, unmatched path rules, unmatched maintenance sources. It does not detect several RFC-listed trust failures such as shadowing, dead suppressions, or artifact declarations with no real stewardship effect. |
| `config suggest` should meaningfully help bootstrap mature repos | RFC-007 section 5.7, backlog G7-20 | Weakly fulfilled | On the Steward repo itself it suggests only `README.md`, `STRUCTURE.md`, and `docs/requirements/PRD.md`. The code in `BootstrapAnalyzer` is intentionally heuristic and narrow, so the output falls far short of the repo's actual governance model. |
| `explain path` should answer what applies here, and why | RFC-007 section 5.1, backlog G7-06 | Partially fulfilled | It shows classification, artifact match, path-policy category, frontmatter requirements, suppressions, and applicable rules. It does not show source locations, maintenance participation, managed-region expectations, or precedence reasoning that explains *why* those rules won. |
| Markdown structural inspection should be strong and reliable | PRD AREA-MARKDOWN, RFC-004 | Mostly fulfilled | `md outline`, `md query`, and the `outline README.md` shortcut are strong and coherent. The edit subsystem is preview/apply and well-tested. |
| Markdown frontmatter validation should exist within the structural editing family | RFC-004 edit operations | Not fulfilled | Accepted RFC-004 includes `fm-validate`, but `MdEditCommand` only registers `ensure-section`, `set-section`, `insert-section`, `append-block`, `prepend-block`, `fm-set`, and `fm-merge`. `md edit --help` confirms the gap. |
| Safe move/rename should reduce structural drift | RFC-007 section 5.5, backlog G7-19, README command table | Mostly fulfilled | `refactor move` is preview-first, discoverable, and accurately described in the README: it updates Markdown references. The move engine is narrower than the broader RFC aspiration because it rewrites Markdown links only, not policy declarations or non-Markdown governed metadata. |
| Governance/discoverability rules should add real stewardship value | README validation list, RuleRegistry, RFC-007 | Mostly fulfilled | `STWD-010` through `STWD-013` are real and useful. `IndexCompletenessRule`, `FreshnessRule`, and `OrphanedDocumentRule` give Steward a more distinct governance character than generic doc linting. |
| AI-agent surfaces should be consistently machine-readable where claimed | README, PRD dual-audience goal, RFC-001 | Partially fulfilled | `check`, `status`, `refs`, `search`, `maintain`, and `config suggest` all have usable JSON. But `status --coverage --output json` silently omits coverage, and the thin `config suggest`/`explain path` outputs reduce agent usefulness in the harder stewardship workflows. |
| This repo should convincingly dogfood Steward | README "Using Steward In This Repo", `.steward/policy.yaml` | Mostly fulfilled | The repo demonstrably uses real policy, maintenance, start-here, completion, and naming constructs. But the repo also exposes two important rough edges: broken changed/staged validation and noisy governance coverage caused by unexcluded fixture repos. |
| Pre-1.0 messaging should be honest and not overclaim | `README.md`, `docs/implementation-status.md`, `docs/planning/pre-release-blockers.md` | Mostly fulfilled | The pre-stable posture is clear and conservative. The largest remaining overstatement is not in the README; it is in the RFC-007 status ledger, which uses "implemented" language for surfaces that are present but still thinner than the ledger implies. |

---

## 5. Workflow Assessment

| Workflow | Intended path | Observed behavior | Friction points | Severity | Recommended improvement |
|---|---|---|---|---|---|
| New-user orientation on this repo | `steward orient --signals` -> `steward status --coverage` -> `steward check` | Works end-to-end, but default `orient` is verbose; `status --coverage` is the clearer maintainer surface | Default orientation feels closer to a filtered tree than a concise start surface; README recommends the verbose path rather than the stronger compact variant | Medium | Make `orient` more curated by default or update repo guidance to recommend `orient --compact --signals` |
| Full repository validation | `steward check` | Strong on the clean repo; clear PASS result; coherent remediation text when failures occur | Completion summary is still fairly narrow, but usable | Low | Keep this surface stable; add more contract tests, not redesign |
| Pre-commit / changed-file validation | `steward check --scope changed` | Fails catastrophically on the clean repo with false missing-artifact and broken-reference diagnostics | Core workflow is untrustworthy; directly contradicts PRD and README expectations | Critical | Fix scope semantics so repo-wide obligations consult repository existence while content-scoped rules keep respecting the changed set |
| Staged completeness | `steward check --scope staged` | Staged completeness signal exists, but the same false-positive scope bug makes the overall workflow untrustworthy | Useful informational signal is buried under broken baseline diagnostics | High | Fix scope correctness first, then keep staged completeness as a secondary signal |
| Understanding governance/current state | `steward status --coverage`, `steward explain path <file>` | Good human workflow; `status` and `explain path` complement each other well | `explain path` is too thin for deep maintainer reasoning; JSON coverage missing hurts agents | Medium | Enrich `explain path` and include coverage in JSON |
| Config authoring and debugging | `steward config validate`, `show --effective`, `doctor` | `show --effective` is excellent; `doctor` is helpful for a few cases; current repo validates cleanly | Doctor breadth is still narrow; suggestion depth is weak | Medium | Deepen `doctor` and `suggest`, not `show` |
| Bootstrap / mature repo onboarding | `steward init`, `steward config suggest` | Functional, but the suggestion engine is far less expressive than the product narrative around mature-repo adoption | On the Steward repo itself, suggestion output is nowhere near the eventual self-dogfooded policy richness | Important | Either deepen `BootstrapAnalyzer` materially or narrow the wording around bootstrap strength |
| Markdown structural inspection | `steward md outline`, `steward md query`, `steward outline README.md` | Strong and coherent | None serious observed | Low | Preserve surface; add more examples in docs |
| Markdown structural editing | `steward md edit ...` | Preview/apply model and command set are coherent | Missing `fm-validate` leaves the subsystem slightly incomplete relative to accepted RFC-004 | Medium | Add `fm-validate` or explicitly narrow the accepted contract |
| Safe move / rename | `steward refactor move --preview|--apply` | Discoverable and accurately scoped for Markdown link rewriting | Broader governed metadata and policy references remain manual | Medium | Keep current scope explicit and add policy-aware follow-on only when ready |
| Ongoing maintenance after structural change | `steward maintain --artifact structure --diff` | Good; on current repo it correctly reports up-to-date status | None serious observed | Low | Keep as-is; it is one of the more trustworthy surfaces |

---

## 6. CLI UX Assessment

### Naming and command boundaries

- Command-family naming is mostly professional and deliberate.
- `orient`, `outline`, and `search` have distinct identities that match RFC-005.
- `config`, `refs`, and `refactor` are discoverable and sensibly placed.
- `outline README.md` delegating to Markdown outline is a strong coherence choice.

### Help text quality

- Root help is clear and broadly accurate.
- `config --help`, `search --help`, `check --help`, and `refactor move --help` are operationally useful.
- `md edit --help` is clear for the implemented subcommands, but its surface exposes the missing `fm-validate` gap immediately.

### Default behavior quality

- `maintain` is preview-first and behaves safely.
- `config show --effective` is a strong default inspection surface.
- Default `orient` is too expansive on this repo for the session-start promise. `--compact` produces a much better answer, which makes the default feel under-shaped rather than absent.

### Output readability

- Text output for `status`, `check`, `maintain`, `search`, `md outline`, and `explain` is readable and consistent.
- `refs --output json` and `status --output json` are easy for agents to consume.
- The weakest output design issue found is feature asymmetry: some options exist only in text mode even when the command otherwise claims dual-audience support.

### JSON usefulness for agents

- Strong: `check`, `status`, `refs`, `search`, `config suggest`, `maintain`.
- Weak spots:
  - `status --coverage --output json` omits coverage entirely.
  - `config suggest` JSON is structurally fine but too shallow to be highly useful for this repo.
  - `explain path` lacks richer provenance/explanation details that would make automated reasoning much easier.

### Errors and remediation

- Rule explanations and diagnostics generally include good remediation text.
- The most damaging remediation issue is not wording but false confidence: when scoped validation is wrong, the remediation guidance is attached to diagnostics that should not exist.

### Docs vs actual UX

- The README and help text are broadly aligned for the shipped public surface.
- The accepted RFC/RFC-007 backlog wording is more confident than the actual product depth in several governance-assistance areas.

---

## 7. Repository Stewardship Value

Steward does provide meaningful repository stewardship value on this repo today.

### Where it clearly adds value

- It gives the repo a coherent start-here model.
- It makes state documents, generated structure, required artifacts, and governance coverage visible.
- It adds real repo-specific rules beyond generic linting: naming conventions, index completeness, freshness, orphaned discoverability, broken policy references, managed regions, stale artifacts.
- It creates a meaningful explanation/navigation loop via `status`, `explain path`, `refs`, `search`, and Markdown query/outline.

### Where the stewardship loop is still incomplete

- Changed/staged validation is currently not safe to trust.
- Bootstrap analysis is not yet strong enough to infer anything close to the repo's own governance model.
- Governance coverage is visible to humans but not fully exposed to agents.
- Safe rename remains link-focused rather than fully governance-aware.

### Distinctiveness vs generic tools

The strongest defensible differentiator today is the combination of:

- repository-aware orientation/status
- explicit policy and role modeling
- deterministic maintained artifacts
- governance rules around freshness, discoverability, indexing, and path policy
- Markdown structure operations that are coupled to repo governance rather than isolated text editing

That is enough to justify Steward's existence. The product does not read like "just another validator" anymore. The gap is that some of the most credibility-sensitive stewardship loops are still weaker than the repository now implies.

---

## 8. Dogfooding Assessment

### What the repo proves well

- The repo demonstrates real self-dogfooding rather than toy examples.
- The self-policy is meaningful: start-here docs, state documents, completion policy, maintenance, and path naming rules are all in active use.
- The repo is readable through Steward. `status`, `orient`, and `explain path` do help a new contributor understand the documentation/governance shape.

### What dogfooding exposes as rough edges

- The repo's own clean working tree fails under `check --scope changed` and `--scope staged`. That is the most important dogfooding failure because it directly attacks stewardship trust.
- Governance coverage includes test-fixture mini-repositories under `tests/Steward.TestFixtures/Repos/`, dropping the repo to `85%` governed Markdown coverage. That is honest data, but it is also a signal that the repo has not fully tuned Steward to its own intended repository boundary.
- The repo's real policy is much richer than what `config suggest` can infer. Steward does not yet bootstrap the kind of governance that Steward itself actually relies on.

### Dogfooding verdict

This repo **does demonstrate Steward's value**, but it also demonstrates exactly where trust is still lost. The self-use story is convincing for full-repo navigation and maintenance, not yet convincing for changed/staged stewardship or mature-repo bootstrap.

---

## 9. Prioritized Findings

### EF-001 — Scoped validation is broken on the dogfooded repo

- **Category:** Critical credibility gap
- **Severity:** Critical
- **Evidence:** On the clean Steward repo, `steward check --scope changed` and `steward check --scope staged` both fail with false `STWD-001`, `STWD-007`, and `STWD-009` diagnostics while `Files checked: 0`.
- **Implementation path:** `src/Steward.Core/Validation/Rules/RequiredArtifactRule.cs`, `src/Steward.Core/Validation/Rules/BrokenArtifactReferenceRule.cs`, and `src/Steward.Core/Validation/Rules/StaleArtifactRule.cs` all evaluate repo-wide obligations against `context.TargetFiles` instead of repository existence/full discovery.
- **Why it matters:** This breaks a core documented workflow for maintainers and agents. It is not a cosmetic issue; it makes a headline capability untrustworthy.
- **Suggested direction:** Split rules into repo-wide obligations vs target-scoped checks, or give validation rules access to both the scoped set and the full discovered repository so scope-sensitive and scope-insensitive rules can behave correctly.

### EF-002 — Governance coverage is not available in JSON despite the `--coverage` flag

- **Category:** Important product gap
- **Severity:** High
- **Evidence:** `steward status --coverage` prints coverage in text mode, but `steward status --coverage --output json` returns the same JSON object as plain `status` with no coverage fields.
- **Implementation path:** `src/Steward.Cli/Commands/StatusCommand.cs` computes coverage only in the text-output branch.
- **Why it matters:** This weakens the dual-audience promise precisely on a governance-reporting surface the repo encourages users to run.
- **Suggested direction:** Add a `coverage` object to JSON output when `--coverage` is requested, including governed count, total Markdown files, percentage, and unguided paths (possibly truncated with an explicit flag).

### EF-003 — `config suggest` is too shallow to support the repo's own mature-repo expectations

- **Category:** Important product gap
- **Severity:** High
- **Evidence:** On this repo, `steward config suggest --output json` suggests only `README.md`, `STRUCTURE.md`, and `docs/requirements/PRD.md` despite the live dogfooded policy covering start-here documents, decision index, planning index, implementation status, the solution file, audits, and maintenance/completion structures.
- **Implementation path:** `src/Steward.Core/Configuration/BootstrapAnalyzer.cs` is intentionally narrow: a short well-known-file table, a docs index heuristic, simple PRD detection, and common exclude patterns.
- **Why it matters:** The repo frames mature-repo adoption and bootstrap-by-analysis as part of Steward's value. The current depth does not yet earn that framing on Steward's own repository.
- **Suggested direction:** Either materially deepen the analyzer around start-here/state/index/workflow inference, or narrow the product/status wording so `config suggest` is described as a conservative starter rather than a substantial bootstrap assistant.

### EF-004 — Default orientation is too verbose for the promised session-start experience

- **Category:** Workflow gap
- **Severity:** Medium
- **Evidence:** `steward orient --signals` on this repo emits a long list including many audit files and test files. `steward orient --compact` is much closer to the RFC-005 promise of a curated, session-start map.
- **Implementation path:** `src/Steward.Cli/Commands/OrientCommand.cs`, `src/Steward.Core/Orientation/OrientationEngine.cs`.
- **Why it matters:** The repo tells new contributors to use `orient --signals` first. The better experience currently exists behind a flag.
- **Suggested direction:** Either make the default more curated, or explicitly update repo guidance to point new users to `orient --compact --signals`.

### EF-005 — The markdown subsystem is coherent, but still not fully complete relative to accepted design

- **Category:** Implementation gap
- **Severity:** Medium
- **Evidence:** RFC-004 includes `fm-validate`, but `md edit --help` and `MdEditCommand` expose no such subcommand.
- **Implementation path:** `src/Steward.Cli/Commands/MdEditCommand.cs`.
- **Why it matters:** The missing operation is not catastrophic, but it creates a small contract gap inside one of Steward's most differentiated subsystems.
- **Suggested direction:** Add `fm-validate` or explicitly narrow the accepted Markdown-edit contract and corresponding docs.

### EF-006 — `explain path` is useful, but thinner than the repo's explainability standard

- **Category:** Important product gap
- **Severity:** Medium
- **Evidence:** `steward explain path docs/planning-index.md` shows classification, artifact match, path-policy category, and applicable rules, but not source locations, maintenance participation, matched override sources, or managed-region ownership expectations. RFC-007 section 5.1 asks for those deeper reasons.
- **Implementation path:** `src/Steward.Cli/Commands/ExplainCommand.cs`.
- **Why it matters:** Maintainers and agents still need external reasoning to answer "why does this apply?" in more complex cases.
- **Suggested direction:** Add provenance/source-location details and explicit maintenance/frontmatter/ownership participation to the explained result.

### EF-007 — The repo's own governance coverage signal is muddied by fixture mini-repositories

- **Category:** Technical debt affecting trust
- **Severity:** Medium
- **Evidence:** `steward status --coverage` reports `50/59 Markdown files (85%)` and lists fixture repo Markdown files under `tests/Steward.TestFixtures/Repos/` as unguided.
- **Why it matters:** The signal is honest, but it makes the repo appear less governed than it functionally is for actual contributor-facing content. That weakens the self-dogfooding demonstration.
- **Suggested direction:** Decide whether fixture repos should be excluded from governance coverage or explicitly classified as intentionally out-of-scope test assets.

### EF-008 — RFC-007 status language overstates the delivered depth of several governance-assistance surfaces

- **Category:** Docs mismatch
- **Severity:** Low-Medium
- **Evidence:** `docs/planning/rfc-007-governance-enhancements-backlog.md` marks `config doctor`, `explain path`, `bootstrap-by-analysis`, and safe move/rename as implemented. They do exist, but current depth is materially thinner than the ledger language implies.
- **Why it matters:** Internal artifact confidence matters in this repo. Overstated "implemented" language encourages false confidence in the current line.
- **Suggested direction:** Keep the commands marked delivered, but qualify depth where appropriate: "implemented baseline" or note current limitations directly in the ledger.

---

## 10. Scores

| Dimension | Score (0-5) | Rationale |
|---|---:|---|
| Promise fidelity | 3.0 | The repo's pre-1.0 messaging is fairly honest, and many major surfaces are real. The scoped-validation failure materially lowers fidelity. |
| Workflow usefulness | 2.5 | Full-repo workflows are good; changed/staged and mature-repo bootstrap are not yet strong enough. |
| CLI ergonomics | 3.0 | Naming/help are mostly good, but default orientation, coverage JSON asymmetry, and missing Markdown subcommand completeness hurt polish. |
| Stewardship value | 3.5 | Orientation, status, rules, maintenance, refs, and Markdown structure together create real stewardship value beyond generic linting. |
| Dogfooding quality | 3.0 | The repo demonstrates real use and value, but also exposes serious scope bugs and noisy coverage boundaries. |
| Configurability | 3.5 | Policy expressiveness and `config show --effective` are strong. Doctoring and suggestion flows are still thinner than they should be. |
| Markdown subsystem quality | 3.5 | Query/outline/edit model is one of the product's strongest areas; the missing `fm-validate` keeps it from feeling fully complete. |
| Governance / rule-system quality | 3.0 | The rule set is meaningful and distinctive, but scoped validation semantics currently undermine trust in key modes. |
| AI-agent usefulness | 3.0 | JSON support is broadly real, but a few important surfaces are incomplete or too thin for strong agent stewardship loops. |
| Release-line credibility | 3.0 | Honest pre-1.0 posture, clean build, and passing tests help. Core workflow trust and existing blocker docs keep the line from feeling stronger. |

**Overall score:** **3.1 / 5.0**

**Narrative verdict:** The product is now meaningfully useful and differentiated, but it is not yet consistently trustworthy enough to fully justify the repo's stronger "primary stewardship surface" expectation without qualification.

---

## 11. Final Recommendation

**Recommendation:** **expectations are partly met but need targeted correction**

### Rationale

- The product is already useful enough that repositioning the entire CLI downward would be too harsh and would understate real progress.
- The repo's strongest human-facing stewardship surfaces are genuinely good.
- The largest remaining issues are specific and correctable rather than signs that the whole concept is unsound.
- But the changed/staged validation bug is severe enough that the repo cannot honestly claim the full intended stewardship loop is already convincing.

---

## 12. Actionable Remediation Plan

### Fix immediately

- Correct scoped validation semantics so repo-wide obligations (`STWD-001`, `STWD-007`, `STWD-009`, and any similar rules) do not evaluate against an empty changed/staged target set.
- Add regression tests covering `check --scope changed` and `--scope staged` on a clean governed repo and on a repo with a single changed file.
- Add coverage data to `status --coverage --output json` so the governance coverage surface is actually dual-audience.
- Decide whether the recommended session-start path in `README.md` should be `orient --compact --signals` until default orientation is tightened.

### Next milestone

- Deepen `config suggest` so it can infer more of the repo's real governance shape, or explicitly narrow its positioning to "starter hints only".
- Enrich `explain path` with provenance/source locations, maintenance participation, and effective override reasoning.
- Add `fm-validate` or formally remove it from the accepted Markdown editing contract.
- Tune governance coverage for fixture/test repositories so repo-level maturity reporting is cleaner and more intentional.

### Later / optional

- Expand `refactor move` beyond Markdown link rewrites to policy and governed-metadata participation where that can be done deterministically.
- Broaden `config doctor` toward the fuller RFC-007 list: dead suppressions, shadowed rules, redundant excludes, and artifacts that never meaningfully participate.
- Add explicit JSON schema/versioning markers for the most agent-consumed outputs if Steward wants to position itself more strongly as automation infrastructure.

---

## 13. Bottom Line

If a serious contributor cloned this repo today and used Steward as the primary repository stewardship surface, the experience would feel **promising and increasingly coherent**, not fake or empty. But it would **not yet feel fully strong or sufficient**, because one of the most important trust-bearing workflows, scoped validation, is currently broken on the repo itself, and several governance-assistance features are present at a thinner depth than the repo's own internal status language suggests.

The repo has earned the right to say Steward is useful. It has **not yet fully earned** the stronger claim that the current CLI is already a fully trustworthy primary stewardship loop for day-to-day changed/staged work.
