# CLI Expectation Fidelity Reassessment — 2026-04-16

- Status: Complete
- Reviewer perspective: Senior principal engineering, CLI UX, repository governance, and AI-agent workflow assessment
- Scope: Whether the current Steward CLI, used on the Steward repository itself, fulfills the repository's documented promise, accepted requirements, and intended contributor/agent workflows
- Evidence basis: docs + code + tests + config + live CLI execution + observable dogfooding patterns

---

## 1. Executive Assessment

### Verdict

Expectations are partly met but need targeted correction.

Steward is already a meaningful stewardship CLI on this repository for full-repo orientation, governance visibility, explainability basics, deterministic maintenance, and Markdown structure operations. This is no longer "just another checker."

However, one critical workflow still breaks product trust: scoped validation (`check --scope changed|staged`) reports repository-wide false failures on a clean tree (`Files checked: 0` while claiming required artifacts are missing). That directly undermines a central maintainer/agent loop described by README, PRD, and RFC-003.

### Where it clearly succeeds

- Full-scope stewardship loop is strong: `orient --signals`, `status --coverage`, `check`, `maintain`.
- Governance model is materially richer than generic linting (naming, index completeness, freshness, discoverability, stale artifacts, broken references).
- Config introspection is strong (`config show --effective`) and supports trust-building.
- Markdown structural subsystem is practical (`md query`, `md outline`, preview-first edits).
- Self-dogfooding is real and non-trivial: `.steward/policy.yaml` encodes meaningful repository contract and completion policy.

### Where it clearly falls short

- Scoped validation trust is broken in current runtime behavior.
- `status --coverage --output json` omits coverage object even when `--coverage` is requested.
- `config suggest` is too shallow for this mature repo and under-delivers on bootstrap-by-analysis expectations.
- `explain path` is useful but still thin for deep policy provenance/precedence debugging.

### Product-promise trust level

- Full-repo trust: moderate-high.
- Inner-loop scoped-validation trust: low until scoped rule semantics are corrected.
- Overall promise trust: moderate.

---

## 2. Expectation Model

This model is derived from repository artifacts, not external assumptions.

### A. Product promise expectations

1. Steward is a repository stewardship CLI, not only a validator.
2. Humans and AI agents are both first-class users.
3. Governance is policy-driven, explainable, and maintainable.
4. Maintenance/editing operations are deterministic and preview-first.
5. Pre-1.0 messaging should be explicit and honest.

Primary sources:

- `README.md`
- `docs/requirements/PRD.md`
- `docs/requirements/assumptions-constraints.md`
- `docs/implementation-status.md`
- `docs/decisions/rfcs/RFC-001-cli-command-structure.md`
- `docs/decisions/rfcs/RFC-003-validation-and-diagnostics.md`
- `docs/decisions/rfcs/RFC-004-markdown-structural-model.md`
- `docs/decisions/rfcs/RFC-005-orientation-search-outline.md`
- `docs/decisions/rfcs/RFC-006-maintenance-and-memory.md`
- `docs/decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md`

### B. Primary persona expectations

1. New serious contributor: orient quickly and identify authoritative surfaces.
2. Maintainer: check policy contract, understand current governance state, fix drift safely.
3. AI agent: consume JSON output for inspect -> change -> validate -> remediate loops.
4. CI/pre-commit actor: trust scoped validation semantics.

### C. Workflow expectations

1. Session-start orientation.
2. Repo-structure understanding.
3. Governance understanding.
4. Full and scoped validation.
5. Failure explanation and remediation.
6. Configuration authoring/debugging.
7. Structural markdown work.
8. Deterministic artifact maintenance.
9. Safe rename/move with reduced drift.

### D. Command-family expectations

1. `orient` is curated session-start map.
2. `outline` and `md outline` form a coherent structure-inspection pair.
3. `check` is canonical validation/completion gate.
4. `status` is lightweight governance surface.
5. `config` supports validate/show/doctor/suggest with actionable clarity.
6. `maintain` is preview-first, deterministic, diffable.
7. `search`, `refs`, and `refactor move` create discover -> inspect -> safe-change loop.

### E. Repo-self-usage expectations

1. Steward repo should prove Steward value on itself.
2. `.steward/policy.yaml` should be reflected coherently by CLI behavior.
3. Recommended repo entry loop in README should remain trustworthy.

### F. Quality/release expectations

1. Pre-1.0 roughness is acceptable, but core documented workflows must still be trustworthy.
2. Messaging should not imply stronger reliability than current behavior supports.

---

## 3. Expectation-to-Reality Matrix

Rating legend: fulfilled | mostly fulfilled | partially fulfilled | weakly fulfilled | not fulfilled | unclear

| Expectation | Status | Rationale and evidence |
| --- | --- | --- |
| Steward is more than a checker | mostly fulfilled | Runtime shows meaningful stewardship surfaces (`orient`, `status`, `maintain`, `refs`, `explain path`, `md query`), and code organizes these as first-class commands (`src/Steward.Cli/Program.cs`). |
| Full-repo validation is trustworthy | fulfilled | `check` passes on this repo; diagnostics/remediation model is coherent in code and runtime (`src/Steward.Cli/Commands/CheckCommand.cs`). |
| Changed/staged scope is trustworthy | not fulfilled | `check --scope changed` and `check --scope staged` fail with false missing artifact/reference diagnostics while `Files checked: 0`. Rules currently evaluate existence using `context.TargetFiles` (`RequiredArtifactRule`, `BrokenArtifactReferenceRule`, `StaleArtifactRule`). |
| Orientation is curated and operational | mostly fulfilled | `orient --signals` and `orient --compact` are useful and role-aware (`OrientationEngine`); README start-here loop is workable. |
| Status gives meaningful governance surface | mostly fulfilled | Text mode includes required/recommended artifacts, state docs, stale signal, and coverage. JSON mode is solid for baseline state, but `--coverage` parity is missing. |
| Agent-facing JSON parity | partially fulfilled | Strong JSON for `check`, `status`, `refs`, `search`, `maintain`; missing coverage payload in `status --coverage --output json`. |
| Config explainability is strong | mostly fulfilled | `config show --effective` clearly surfaces merged effective policy/runtime defaults; this is one of the strongest trust surfaces. |
| Config doctor catches ineffective governance broadly | partially fulfilled | Doctor covers dead start-here, missing artifacts, unmatched rules/sources, overlapping frontmatter globals; does not catch many deeper "valid but ineffective" cases from RFC-007 intent. |
| Bootstrap-by-analysis helps mature repos | weakly fulfilled | `config suggest --output json` returns minimal suggestions for this repo (README, STRUCTURE, PRD), not close to repo's actual governance richness. |
| Markdown subsystem is practical and coherent | mostly fulfilled | `md query`, `md outline`, and `md edit` preview/apply flows are useful and test-backed; command family is coherent. |
| Rule system is meaningful for governance | mostly fulfilled | 13 rules include naming/index/freshness/discoverability in addition to core checks (`RuleRegistry`, rule implementations). |
| Explainability is actionable | mostly fulfilled | Rule-level explain and path-level explain are useful; path explain still lacks richer provenance/precedence details. |
| Safe refactor loop exists | mostly fulfilled | `refactor move --preview` identifies impacted markdown links deterministically; explicit safety boundary (preview vs apply). |
| Dogfooding proves usefulness | mostly fulfilled | Repo policy is substantive and exercised; trust gap exposed by scoped check semantics. |
| Pre-1.0 messaging alignment | mostly fulfilled | README and implementation-status are conservative; strongest misalignment is practical workflow trust on scoped validation, not version posture messaging. |

---

## 4. Workflow Assessment (End-to-End)

| Workflow | Intended path | Observed behavior | Friction / dead-end | Severity | Recommended improvement |
| --- | --- | --- | --- | --- | --- |
| New-user orientation | `orient --signals` -> `status --coverage` -> `check` | Works and is informative | Coverage includes fixture repositories by default, which muddies repo-governance signal | medium | Add coverage scoping/ignore options for fixture ecosystems; optionally refine default repo boundary conventions |
| Repo structure understanding | `orient`, `outline`, `md outline` | Strong; markdown shortcut is coherent | None material | low | Keep stable |
| Governance understanding | `status --coverage`, `explain path` | Useful for humans | JSON coverage gap; explain path lacks deeper provenance reasoning | medium | Add JSON coverage object; enrich explain path output |
| Full validation | `check` | Trustworthy on full scope | None major | low | Keep stable, expand regression tests |
| Scoped validation | `check --scope changed\|staged` | Incorrect false failures | Breaks inner-loop trust and maintainers' expected flow | critical | Split repo-wide obligation rules from target-scoped rules; use full repository discovery for existence checks |
| Maintenance loop | `maintain --artifact structure --diff` | Deterministic and safe | No major friction | low | Keep stable |
| Config authoring/debugging | `config validate`, `show --effective`, `doctor` | Good clarity and actionable output | Doctor depth limited beyond basic anti-footgun checks | medium | Expand ineffective-config diagnostics incrementally |
| Bootstrap onboarding | `init`, `config suggest` | Works technically | Suggestion depth too low for mature repositories | high | Improve repository analysis heuristics and role/start-here inference |
| Markdown structural maintenance | `md query`, `md edit`, `outline <md>` | Strong practical value | Selector/operation learning curve remains moderately high | medium | Add more operation examples in docs and help snippets |
| Safe file move | `refactor move --preview\|--apply` | Useful and explicit | Scope limited to markdown link rewriting | medium | Keep current scope explicit; optionally add policy reference updates as opt-in |

---

## 5. CLI UX Assessment

### Naming and information architecture

- Command names are generally clear and deliberate.
- Family boundaries are coherent (`md`, `config`, `refactor`).
- Root help presents a professional stewardship surface.

### Help text quality

- Most help text is accurate and operational.
- `md edit --help` is concise and clear for implemented operations.

### Option consistency and defaults

- Global options are consistent across commands (`--output`, `--verbosity`, `--no-color`, `--config`).
- Safe defaults exist for risky operations (preview-first maintenance/edit flows).

### Output readability and remediation

- Text output is readable and structured.
- `check` diagnostics include remediation guidance and completion summary.
- Remediation quality is undermined when diagnostics themselves are false in scoped mode.

### JSON utility for agents

- Strong schemas in `check`, `status`, `search`, `refs`, and `maintain`.
- Notable parity hole: no coverage payload for `status --coverage --output json`.

### Professionalism signal

Overall UX feels professional in broad strokes, with one major reliability defect concentrated in scoped validation semantics.

---

## 6. Dogfooding Assessment

### Confidence-building evidence

- Repo uses real `.steward` policy with start-here, completion policy, path policy, and maintained structure artifact.
- README explicitly promotes Steward-first contributor loop and that loop mostly works.
- Status/orient surfaces make this repository easier to navigate and reason about.

### Dogfooding-exposed rough edges

- Scoped check false positives are reproduced on this repo and materially damage trust.
- Coverage currently includes fixture-repo markdown files by default, diluting governance signal precision.
- Suggestion engine does not infer the governance complexity Steward itself already uses.

Dogfooding verdict: convincing for full-repo stewardship, not yet convincing for changed/staged trust and mature-repo bootstrap strength.

---

## 7. Prioritized Findings

### F-01 Scoped validation false positives on clean tree

- Category: critical credibility gap
- Severity: critical
- Evidence:
  - Runtime: `check --scope changed|staged` fails with required artifact missing and broken reference diagnostics while checking zero files.
  - Code: existence checks use `context.TargetFiles` in `src/Steward.Core/Validation/Rules/RequiredArtifactRule.cs`, `src/Steward.Core/Validation/Rules/BrokenArtifactReferenceRule.cs`, and maintenance stale check uses target-scoped files in `src/Steward.Core/Validation/Rules/StaleArtifactRule.cs`.
- Why this matters: It breaks a core intended workflow and introduces false confidence/failure.
- Suggested direction: Add full-repo file view to validation context and classify rules as repository-wide vs target-scoped.

### F-02 JSON parity gap for governance coverage

- Category: important product gap
- Severity: high
- Evidence:
  - Runtime: `status --coverage` shows coverage in text mode only; `status --coverage --output json` returns baseline status object with no coverage object.
  - Tests: coverage logic is tested (`GovernanceCoverageTests`) but no JSON coverage contract test exists.
- Why this matters: Weakens AI-agent contract for a recommended stewardship workflow.
- Suggested direction: include `coverage` object when `--coverage` is set, with governed/total/percentage/ungoverned fields.

### F-03 Bootstrap suggestions underfit mature repos

- Category: workflow gap
- Severity: high
- Evidence:
  - Runtime: `config suggest --output json` yields only three artifact suggestions on this repo.
  - Code: `BootstrapAnalyzer` heuristics are intentionally narrow and largely filename-based.
- Why this matters: Reduces practical adoption value in repositories that most need governance help.
- Suggested direction: add richer heuristics (decision/planning/status indexes, solution/workflow anchors, role inference from links/frontmatter/policy-like docs).

### F-04 Explain-path provenance remains shallow

- Category: UX inconsistency
- Severity: medium
- Evidence:
  - Runtime JSON shows classification, artifact, rules, and frontmatter requirements, but not precedence/source traces.
  - RFC-007 expectation implies stronger "what applies and why" depth.
- Why this matters: Harder for maintainers/agents to debug policy interactions confidently.
- Suggested direction: add matched-source fields, precedence notes, and maintenance participation details.

### F-05 Coverage signal includes fixture repository markdown by default

- Category: important product gap
- Severity: medium
- Evidence:
  - Runtime coverage output includes test-fixture repositories under `tests/Steward.TestFixtures/Repos` as ungoverned markdown.
- Why this matters: Makes governance maturity signal noisier for this repo's real product surface.
- Suggested direction: introduce coverage scoping or repo-zone ignore controls.

### F-06 Doctor breadth lags RFC-007 ambition

- Category: implementation gap
- Severity: medium
- Evidence:
  - Code and runtime show useful but narrow diagnostics in `config doctor`.
- Why this matters: "Valid but ineffective governance" remains partially hidden.
- Suggested direction: incremental checks for shadowed rules, no-effect suppressions, and dead declarations.

### F-07 Scoped-validation gap is not explicitly regression-tested

- Category: technical debt affecting trust
- Severity: medium
- Evidence:
  - Existing tests cover completion/impact/staged completeness units but do not assert that scoped check avoids false repo-wide missing-artifact failures.
- Why this matters: Critical behavior can regress silently.
- Suggested direction: add CLI integration tests for changed/staged semantics on clean repos.

### F-08 Markdown subsystem still has moderate discoverability curve

- Category: nice-to-have improvement
- Severity: low
- Evidence:
  - Command surface is coherent but requires prior familiarity with selectors/operations.
- Why this matters: New contributors and agents need examples to exploit subsystem fully.
- Suggested direction: add concise real-repo examples in README/docs.

---

## 8. Final Recommendation

Recommendation: expectations are partly met but need targeted correction.

Rationale:

- The product has clear, differentiable stewardship value and substantial implementation maturity.
- The remaining gap is not broad product incoherence; it is concentrated around high-impact trust defects and a few thin surfaces.
- Fixing scoped-validation semantics plus JSON coverage parity and bootstrap depth would significantly improve trust without requiring a large repositioning.

No recommendation to reposition the product narrative broadly at this time. Recommendation is to tighten reliability and parity before stronger adoption claims.

---

## 9. Actionable Remediation Plan

### Fix immediately

1. Correct scoped-validation semantics for repository-wide obligation rules.
2. Add regression tests reproducing current changed/staged false-positive behavior and asserting corrected behavior.
3. Add coverage payload to `status --coverage --output json` and a contract test for it.

### Next milestone

1. Expand `config suggest` analysis depth for mature repositories.
2. Expand `config doctor` ineffective-governance checks (shadowed/no-effect cases).
3. Enrich `explain path` with provenance and precedence details.
4. Add optional coverage scoping/exclusion controls for fixture-heavy repositories.

### Later / optional

1. Add policy-aware update suggestions to `refactor move` (opt-in beyond markdown link rewriting).
2. Add richer guided examples for markdown selectors/edit operations.
3. Add machine-readable impact metadata in `check` output (optional enhancement).

---

## 10. Scoring (0 to 5)

| Dimension | Score | Rationale |
| --- | ---: | --- |
| Promise fidelity | 3.6 | Broadly aligned, but critical scoped-validation mismatch lowers trust. |
| Workflow usefulness | 3.4 | Strong full-repo workflows; scoped flow break is severe. |
| CLI ergonomics | 4.1 | Naming/help/defaults are strong; targeted parity gaps remain. |
| Stewardship value | 4.2 | Clear repository-level value beyond linting/checking. |
| Dogfooding quality | 3.8 | Real and useful dogfooding, with one major exposed defect. |
| Configurability | 4.0 | Expressive policy model and effective config introspection; doctor/suggest depth can improve. |
| Markdown subsystem quality | 4.2 | Practical, coherent, deterministic preview/apply flow. |
| Governance/rule-system quality | 4.1 | Meaningful rule set with useful governance dimensions; scoped behavior bug affects perceived reliability. |
| AI-agent usefulness | 3.5 | Strong JSON surfaces overall, but important coverage parity + suggestion depth gaps. |
| Release-line credibility | 3.7 | Honest pre-1.0 posture, but high-impact workflow defect remains for trust-sensitive use. |

Overall score: 3.9 / 5.0

Narrative score verdict: strong pre-1.0 stewardship foundation with a concentrated trust defect and a few parity/depth gaps that should be corrected before stronger confidence claims.

---

## 11. Evidence Appendix

### Docs reviewed

- `README.md`
- `docs/implementation-status.md`
- `docs/planning-index.md`
- `docs/planning/pre-1-0-readiness-plan.md`
- `docs/planning/rfc-007-governance-enhancements-backlog.md`
- `docs/requirements/PRD.md`
- `docs/requirements/requirements-traceability.md`
- `docs/requirements/assumptions-constraints.md`
- `docs/decisions/decision-index.md`
- `docs/decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements.md`

### Code inspected

- `src/Steward.Cli/Program.cs`
- `src/Steward.Cli/CommandSetup.cs`
- `src/Steward.Cli/Commands/CheckCommand.cs`
- `src/Steward.Cli/Commands/StatusCommand.cs`
- `src/Steward.Cli/Commands/OrientCommand.cs`
- `src/Steward.Cli/Commands/ConfigCommand.cs`
- `src/Steward.Cli/Commands/MdCommand.cs`
- `src/Steward.Cli/Commands/MdEditCommand.cs`
- `src/Steward.Cli/Commands/MaintainCommand.cs`
- `src/Steward.Cli/Commands/RefsCommand.cs`
- `src/Steward.Cli/Commands/RefactorCommand.cs`
- `src/Steward.Cli/Commands/ExplainCommand.cs`
- `src/Steward.Core/Validation/Rules/RequiredArtifactRule.cs`
- `src/Steward.Core/Validation/Rules/BrokenArtifactReferenceRule.cs`
- `src/Steward.Core/Validation/Rules/StaleArtifactRule.cs`
- `src/Steward.Core/Configuration/RepositoryPolicy.cs`
- `src/Steward.Core/Configuration/ConfigLoader.cs`
- `src/Steward.Core/Configuration/BootstrapAnalyzer.cs`
- `src/Steward.Core/Orientation/OrientationEngine.cs`
- `src/Steward.Core/Search/SearchEngine.cs`
- `src/Steward.Core/Maintenance/MaintenanceEngine.cs`

### Tests inspected

- `tests/Steward.Cli.Tests/CheckCommandTests.cs`
- `tests/Steward.Cli.Tests/StagedCompletenessTests.cs`
- `tests/Steward.Cli.Tests/ChangeImpactTests.cs`
- `tests/Steward.Cli.Tests/StatusCommandTests.cs`
- `tests/Steward.Cli.Tests/GovernanceCoverageTests.cs`
- `tests/Steward.Cli.Tests/MdEditCommandTests.cs`
- `tests/Steward.Cli.Tests/ProfileReadinessTests.cs`

### Live CLI evidence (executed on this repo)

- `dotnet build steward.sln` -> success
- `dotnet test steward.sln --no-build -m:1` -> 505 passing, 0 failing
- `dotnet run --project src/Steward.Cli -- --help`
- `dotnet run --project src/Steward.Cli -- orient --signals`
- `dotnet run --project src/Steward.Cli -- status --coverage`
- `dotnet run --project src/Steward.Cli -- status --output json`
- `dotnet run --project src/Steward.Cli -- status --coverage --output json`
- `dotnet run --project src/Steward.Cli -- check`
- `dotnet run --project src/Steward.Cli -- check --scope changed`
- `dotnet run --project src/Steward.Cli -- check --scope staged`
- `dotnet run --project src/Steward.Cli -- config validate`
- `dotnet run --project src/Steward.Cli -- config show --effective`
- `dotnet run --project src/Steward.Cli -- config doctor`
- `dotnet run --project src/Steward.Cli -- config suggest --output json`
- `dotnet run --project src/Steward.Cli -- explain STWD-013`
- `dotnet run --project src/Steward.Cli -- explain path docs/planning-index.md`
- `dotnet run --project src/Steward.Cli -- explain path docs/planning-index.md --output json`
- `dotnet run --project src/Steward.Cli -- maintain --artifact structure --diff`
- `dotnet run --project src/Steward.Cli -- refs docs/planning-index.md --output json`
- `dotnet run --project src/Steward.Cli -- md --help`
- `dotnet run --project src/Steward.Cli -- md edit --help`
- `dotnet run --project src/Steward.Cli -- md query README.md "heading[Using Steward In This Repo]"`
- `dotnet run --project src/Steward.Cli -- outline README.md --headings`
- `dotnet run --project src/Steward.Cli -- search governance --mode headings`
- `dotnet run --project src/Steward.Cli -- refactor move docs/planning-index.md docs/planning-index-temp.md --preview`
