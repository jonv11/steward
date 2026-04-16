# CLI Full Assessment — 2026-04-16

- **Status:** Complete
- **Reviewer perspective:** Senior principal engineering, product review, CLI UX, repository-governance, and AI-agent workflow assessment
- **Scope:** Whether the current Steward CLI (v0.10.0), used on the Steward repository itself, fulfills the repository's documented promise, accepted requirements, and intended contributor/agent workflows
- **Evidence basis:** docs + code + tests + config + live CLI execution + observable dogfooding patterns + source-code inspection
- **Assessment date:** 2026-04-16
- **Method:** Full end-to-end product assessment: build, exercise, cross-reference, diagnose, score

---

## 1. Executive Assessment

### Verdict

**Expectations are partly met but need targeted correction.**

Steward v0.10.0 is a genuinely meaningful stewardship CLI — well beyond a generic validator. The full-scope loop (`orient → status → check → maintain`) is coherent, the governance model is rich, the Markdown subsystem is practical, and the dogfooding on this repository is non-trivial and real. The product does something that generic linters, tree commands, and markdown tools do not: it models a repository's contract and lets you inspect, validate, and maintain against it.

However, several concrete issues materially weaken trust, especially for the core "inner-loop" workflow that README, PRD, and RFC-003 all describe:

1. **Scoped validation is broken.** `check --scope changed` and `check --scope staged` on a clean tree produce massive false positives (6 errors, 21 warnings, Files checked: 0). This is the single most important product bug because it breaks the maintainer and agent inner loop.
2. **Preview/apply semantics are inconsistent across commands.** Three different patterns (`--fix/--dry-run`, `--apply` default-preview, `--preview+--apply` required) for the same conceptual workflow.
3. **`status --coverage --output json` omits coverage data** — a direct data-loss gap for agents.
4. **`md query --pattern` batch mode is effectively broken** due to argument parsing ambiguity.
5. **New-user init experience starts with immediate check failure** because scaffolded policy requires artifacts that don't exist yet.

### Where it clearly succeeds

- **Full-scope stewardship loop** is strong and coherent: `orient --signals`, `status --coverage`, `check`, `maintain --diff`, `maintain --apply`.
- **Governance model is materially richer** than generic linting: 13 rules covering naming, index completeness, freshness, discoverability, stale artifacts, broken references, managed regions, section size, frontmatter.
- **Config introspection** is excellent: `config show --effective`, `config validate`, `config doctor` form a trustworthy config debugging surface.
- **Markdown structural subsystem** is practical: `md query`, `md outline`, and `md edit` with preview-first workflow provide real structural document maintenance.
- **Deterministic maintenance** works well: `maintain --diff` shows exactly what changes, `maintain --apply` is idempotent.
- **Explainability** is present and useful: `explain STWD-XXX` and `explain path <file>` give meaningful guidance.
- **Self-dogfooding** is real: `.steward/policy.yaml` encodes a meaningful, non-trivial repository contract with 19 artifacts, completion policy, freshness rules, and naming conventions.
- **JSON output** is available on nearly all commands and is well-structured for agent consumption.
- **Search with heading context** is genuinely useful for agents and humans navigating large doc sets.
- **Refs command** accurately maps inbound/outbound markdown links and supports the refactor/move workflow.
- **Exit codes** are well-defined and used (though undertested).

### Where it clearly falls short

- **Scoped validation** produces catastrophic false positives on clean trees.
- **Three inconsistent preview/apply conventions** across `check`, `maintain`, and `refactor move`.
- **`status --coverage --output json`** silently drops coverage data.
- **`md query --pattern`** doesn't work in practice due to argument parsing issues.
- **`config suggest`** is too shallow for mature repos — only suggests 3 artifacts for a repo with 19.
- **`init` scaffolds policy that immediately fails `check`** on a new repo.
- **`explain path`** shows all 13 rules as applicable regardless of file type, reducing its utility.
- **`heading[Overview]` fails** when the heading is actually `1. Overview` — no substring/fuzzy matching.
- **`InternalError` exit code (3) is defined but never used** — dead code.
- **Governance coverage counts test fixtures** as ungoverned, adding noise.

### Product-promise trust level

| Scope | Trust |
|-------|-------|
| Full-repo stewardship loop | **High** |
| Governance model depth | **High** |
| Config introspection | **High** |
| Markdown structural subsystem | **Moderate-high** |
| Inner-loop scoped validation | **Low** (broken) |
| New-user onboarding | **Moderate-low** |
| Agent JSON surface | **Moderate-high** (one notable gap) |
| Overall promise trust | **Moderate** |

---

## 2. Expectation Model

Derived from repository artifacts. Sources: README.md, PRD.md, MRD-0001, accepted ADRs/RFCs, policy.yaml, implementation-status.md, planning docs, tests, CLI help text.

### A. Product Promise

| # | Expectation | Primary source |
|---|-------------|----------------|
| P1 | Steward is a repository stewardship CLI, not just a validator | README, PRD §1-2 |
| P2 | Humans and AI agents are both first-class users | PRD §3, README |
| P3 | Governance is policy-driven, explainable, and maintainable | PRD §8.2-8.4 |
| P4 | Maintenance/editing operations are deterministic and preview-first | PRD §8.12, RFC-006 |
| P5 | Pre-1.0 messaging is explicit and honest | ADR-013, implementation-status.md |
| P6 | Works across repository archetypes via profiles | PRD §6, README |
| P7 | Markdown is first-class governed, queryable, editable document type | PRD §8.8, RFC-004 |
| P8 | All discovery respects .gitignore | PRD §8.14 |
| P9 | Offline and portable, no host credentials required | PRD §4.8 |
| P10 | Conservative automation: preview before apply | PRD §9.3 |

### B. Primary Personas

| Persona | Key expectations | Source |
|---------|-----------------|--------|
| New contributor | Orient quickly, find authoritative docs, understand structure | PRD UC-01 |
| Maintainer | Check policy, understand governance state, fix drift safely | PRD UC-02/03/09 |
| AI agent | JSON output, inspect→change→validate→remediate loop | PRD §3, RFC-003 |
| CI/pre-commit | Trust scoped validation, stable exit codes | PRD §8.3, README |

### C. Workflow Expectations

| # | Workflow | Source |
|---|----------|--------|
| W1 | Session-start orientation | PRD UC-01, README "Quick Start" |
| W2 | Full repository validation | PRD UC-02/03 |
| W3 | Scoped pre-commit validation | PRD UC-02, RFC-003 |
| W4 | Deterministic maintenance | PRD UC-07, RFC-006 |
| W5 | Policy authoring and debugging | PRD UC-09, README |
| W6 | Rule explainability | PRD UC-10 |
| W7 | Structural markdown inspection and editing | PRD UC-05/06, RFC-004 |
| W8 | Repository search | PRD UC-04, RFC-005 |
| W9 | Cross-reference analysis and safe refactoring | README, RFC-006 |
| W10 | Ongoing maintenance for AI agents | PRD §3, README |

### D. Command Family Expectations

| Family | Commands | Expected coherence |
|--------|----------|--------------------|
| Orientation | `orient`, `outline`, `status` | Complementary views: classified structure, raw tree, health summary |
| Validation | `check`, `explain`, `explain path` | Validate, understand, remediate |
| Config | `config show`, `config validate`, `config doctor`, `config suggest` | Inspect, verify, diagnose, bootstrap |
| Maintenance | `maintain`, `check --fix` | Preview, apply, deterministic |
| Markdown | `md outline`, `md query`, `md edit *` | Inspect, extract, mutate with selectors |
| Refactoring | `refactor move`, `refs` | Analyze dependencies, safe rename/move |
| Bootstrap | `init` | Scaffold config, start clean |

### E. Repo Self-Usage Expectations

| # | Expectation | Source |
|---|-------------|--------|
| S1 | `steward check` passes clean on this repo | README, policy.yaml |
| S2 | `steward orient` provides useful session-start view | README §Using Steward |
| S3 | `steward status --coverage` shows governance health | README §Using Steward |
| S4 | `steward maintain --apply` keeps STRUCTURE.md in sync | policy.yaml maintenance section |
| S5 | Naming conventions enforced on decision docs | path-policy.yaml |
| S6 | 19 artifacts with roles, descriptions, importance | policy.yaml |
| S7 | Completion policy includes STWD-001, -007, -008, -009 | policy.yaml |
| S8 | Freshness enforcement on implementation-status.md (30d) | policy.yaml |

---

## 3. Expectation-to-Reality Matrix

### Product Promise

| # | Expectation | Status | Evidence |
|---|-------------|--------|----------|
| P1 | Stewardship, not just validation | **Mostly fulfilled** | orient, status, maintain, search, refs, and md all go beyond validation. The stewardship loop is genuine. |
| P2 | Dual-audience (human + AI) | **Mostly fulfilled** | JSON output on 18/19 commands. `init` lacks JSON. `status --coverage` JSON omits coverage data. |
| P3 | Policy-driven, explainable governance | **Fulfilled** | 13 rules, all explainable. Policy drives all validation. `explain path` lists applicable rules. |
| P4 | Deterministic preview-first operations | **Mostly fulfilled** | `maintain --diff`, `md edit` preview work well. Inconsistent flags across commands (`--fix/--dry-run` vs `--apply`). |
| P5 | Honest pre-1.0 messaging | **Fulfilled** | README, implementation-status.md, ADR-013 all correctly scope claims. Profile readiness table is honest. |
| P6 | Multi-archetype via profiles | **Partially fulfilled** | 5 profiles exist. Only `software` is exercised on this repo. Others are test-fixture-backed starting points. README is honest about this. |
| P7 | Markdown first-class | **Mostly fulfilled** | md query, md outline, md edit, managed regions, frontmatter operations all work. Heading selectors require exact text match (no fuzzy). Batch query via `--pattern` is broken. |
| P8 | .gitignore-aware discovery | **Fulfilled** | All commands respect .gitignore. Discovery excludes configured paths. |
| P9 | Offline, portable | **Fulfilled** | CLI-only, no network, no credentials. |
| P10 | Conservative automation | **Mostly fulfilled** | maintain defaults to preview, md edit defaults to preview. `refactor move` requires explicit flag. `check --fix` applies without preview unless `--dry-run` is added — slightly less conservative. |

### Workflow Expectations

| # | Workflow | Status | Evidence |
|---|----------|--------|----------|
| W1 | Session-start orientation | **Fulfilled** | `orient` and `orient --signals` provide excellent classified views. `status` complements well. |
| W2 | Full repository validation | **Fulfilled** | `check` with full scope works correctly, clean on this repo (1 stale warning). |
| W3 | Scoped pre-commit validation | **Not fulfilled** | `check --scope changed` and `--scope staged` produce catastrophic false positives on clean trees. **Critical bug.** |
| W4 | Deterministic maintenance | **Fulfilled** | `maintain --diff` shows diffs, `maintain --apply` applies idempotently. `check --fix` also works for fixable rules. |
| W5 | Policy authoring/debugging | **Mostly fulfilled** | `config validate`, `config show --effective`, `config doctor` form a strong trio. `config suggest` is too shallow for mature repos. |
| W6 | Rule explainability | **Mostly fulfilled** | `explain STWD-XXX` gives rule + remediation. `explain path` lists all 13 rules without filtering — less useful than it could be. |
| W7 | Markdown inspection/editing | **Mostly fulfilled** | `md query`, `md outline`, `md edit` work well for single files. Batch query is broken. Heading selector requires exact text match. |
| W8 | Repository search | **Fulfilled** | Content and heading search work. Role filtering works. JSON includes heading context. Regex available. |
| W9 | Cross-reference/refactoring | **Fulfilled** | `refs` correctly identifies inbound/outbound links. `refactor move --preview` correctly identifies files needing updates. |
| W10 | AI agent maintenance loop | **Partially fulfilled** | JSON output is good. Scoped validation (the core agent inner-loop) is broken. `status --coverage` JSON omits coverage. |

### Self-Usage Expectations

| # | Expectation | Status | Evidence |
|---|-------------|--------|----------|
| S1 | `check` passes clean | **Mostly fulfilled** | 0 errors, 1 warning (stale STRUCTURE.md). PASS result. The stale warning is legitimate. |
| S2 | `orient` provides useful session-start | **Fulfilled** | Classified view with start-here markers, role-based classification across 271 files. |
| S3 | `status --coverage` shows governance health | **Partially fulfilled** | Text mode shows 85% coverage and ungoverned files. JSON mode omits coverage data — gap. |
| S4 | `maintain` keeps STRUCTURE.md in sync | **Fulfilled** | `maintain --diff` shows exactly what changed. `maintain --apply` updates it. |
| S5 | Naming conventions enforced | **Fulfilled** | path-policy.yaml enforces ADR/RFC naming patterns. `check` validates. |
| S6 | 19 artifacts with rich metadata | **Fulfilled** | policy.yaml declares 19 artifacts with roles, descriptions, importance, freshness. |
| S7 | Completion policy works | **Fulfilled** | `check` output includes completion section with per-rule counts. |
| S8 | Freshness on implementation-status.md | **Fulfilled** | 30-day freshness declared. STWD-012 would fire if stale. |

---

## 4. Workflow Assessment

### W1: Session-Start Orientation

**Intended path:** New user clones repo → runs `steward orient` → understands structure, entry points, artifact roles.

**Observed behavior:** Excellent. `orient` displays a classified hierarchical view with `[start]` markers for entry points, role-based classification (`authoritative`, `current-state`, `guide`, `workflow`, `source`, etc.), and repository metadata. `orient --signals` adds lightweight missing/stale signals without full validation. `status` provides complementary artifact-presence and governance summary.

**Friction points:** None significant. The classification is informative and the start-here markers immediately guide attention. The text output is clean and readable.

**Severity:** N/A — this workflow works well.

**Recommended improvement:** Minor — `orient --compact` could explain the `~15` qualifier more precisely.

---

### W2: Full Repository Validation

**Intended path:** Maintainer runs `steward check` → sees clean pass or actionable diagnostics → understands what to fix.

**Observed behavior:** Strong. On this repo: `Files checked: 271, Errors: 0, Warnings: 1, Info: 0, Result: PASS`. The one warning (stale STRUCTURE.md) includes clear fix guidance. JSON output includes completion policy summary. `check --fix --dry-run` correctly shows what would be fixed.

**Friction points:** None for full-scope validation. The completion section is genuinely useful.

**Severity:** N/A — this workflow works well.

---

### W3: Scoped Pre-Commit Validation

**Intended path:** Developer changes a file → runs `steward check --scope changed` → sees only relevant violations → fixes before commit.

**Observed behavior:** **Catastrophically broken.** On a clean tree (no uncommitted changes), `check --scope changed` reports:
- 6 errors: all required artifacts "missing"
- 21 warnings: all recommended artifacts "missing", all policy references "broken"
- `Files checked: 0`

**Root cause:** `ValidationContext.TargetFiles` serves dual purpose — both "which files to scan" and "which files exist." When scope filters to empty set, existence-check rules (STWD-001, STWD-009) see no files and report everything as missing. The fix requires separating `AllFiles` (for existence lookups) from `TargetFiles` (for content scanning).

**Friction points:** Complete trust failure. An agent or CI system using scoped validation would get false failures on every clean-tree run.

**Severity:** **Critical.**

**Recommended improvement:** Add `AllFiles` property to `ValidationContext`. Repository-level existence rules check `AllFiles`; content-scanning rules use `TargetFiles`. Add regression tests for scoped validation with zero changed files.

---

### W4: Deterministic Maintenance

**Intended path:** Maintainer runs `steward maintain` → sees preview → runs `steward maintain --apply` → STRUCTURE.md updated.

**Observed behavior:** Clean and effective. `maintain` shows what would change, `maintain --diff` shows the specific diff, `maintain --apply` applies it idempotently. Re-running produces no further changes.

**Friction points:** None significant. Good workflow.

**Severity:** N/A.

---

### W5: Policy Authoring and Debugging

**Intended path:** Maintainer creates/edits policy → `config validate` checks syntax → `config show --effective` shows resolved state → `config doctor` finds silent issues → `config suggest` helps bootstrap.

**Observed behavior:** `config validate`, `config show --effective`, and `config doctor` form an excellent trio. `config suggest` on this mature repo only suggests 3 artifacts when 19 are declared — it's too shallow for iterative improvement.

**Friction points:**
- `config suggest` doesn't detect ADRs, RFCs, planning docs, or audit docs that are clearly present in the repo. Its heuristic-based detection is too narrow.
- No way to add a `path-policy.yaml` via `init`; the user must create it manually.

**Severity:** Moderate for `config suggest`. Low for `path-policy.yaml` scaffolding.

**Recommended improvement:** Improve `config suggest` to detect more artifact patterns (e.g., `docs/decisions/adrs/`, `docs/planning/`, dated review docs).

---

### W6: Rule Explainability

**Intended path:** Developer sees `STWD-008` in check output → runs `steward explain STWD-008` → understands the rule and how to fix.

**Observed behavior:** Works. `explain STWD-008` returns rule description, category, severity, and remediation text. `explain` (no args) lists all 13 rules with severity indicators.

**Friction points:**
- `explain path README.md` lists all 13 rules as "applicable" regardless of whether they can actually fire on that file. A text-only README has no managed regions (STWD-005/006), no maintenance declaration (STWD-007), no index_of (STWD-011), no freshness (STWD-012). Showing all rules reduces signal-to-noise.
- Remediation text is functional but brief — "Fix the broken link target or remove the link" is correct but doesn't help with complex cases.

**Severity:** Low-moderate. Functional but not maximally useful.

**Recommended improvement:** `explain path` should filter to rules that actually apply given the file's artifact declaration and governance context. Show only rules with active governance for that path.

---

### W7: Markdown Structural Operations

**Intended path:** Agent needs to extract a section → `md query <file> "heading[Section]"` → gets content → `md edit set-section <file> ...` → modifies it.

**Observed behavior:**
- `md query` works correctly with exact heading text: `heading[1. Overview]` returns the section content.
- `md query` fails on partial matches: `heading[Overview]` returns no matches when the heading is `1. Overview`. This is a meaningful UX problem for agents and humans who don't know the exact heading text.
- `md outline` works very well, showing heading hierarchy with line counts.
- `md edit` subcommands work with preview-first workflow.
- Batch query via `--pattern` is broken due to argument parsing ambiguity between positional `file` arg and `--pattern` option.

**Friction points:**
- No substring/fuzzy heading matching forces users to know exact heading text.
- `--pattern` batch query doesn't work in practice.
- `md query` help text doesn't document the selector syntax at all — no examples.
- MdPath selector syntax is undiscoverable from CLI alone.

**Severity:** Moderate. The single-file operations work well; the usability and batch gaps limit the subsystem's practical reach.

**Recommended improvement:**
1. Support substring matching in heading selectors (e.g., `heading[*Overview*]` or case-insensitive contains).
2. Fix `--pattern` argument parsing or make selector an option too.
3. Add selector examples to `md query --help`.

---

### W8: Repository Search

**Intended path:** Developer searches for a topic → `steward search "topic"` → sees results with file, line, snippet, heading context.

**Observed behavior:** Solid. Content search returns relevant results with heading context. Heading-only mode (`--mode headings`) is useful. Role filtering works. JSON output includes structured match data with headingContext field. Regex available.

**Friction points:**
- Search is substring-only by default. No word-boundary matching without `--regex`.
- Search across non-Markdown files works but heading context only applies to Markdown.
- `--max` limits displayed results but still scans everything.

**Severity:** Low. Search works well for its intended purpose.

---

### W9: Cross-Reference and Safe Refactoring

**Intended path:** Maintainer wants to rename a doc → `refs <file>` to understand dependencies → `refactor move --preview` → `refactor move --apply`.

**Observed behavior:** Good. `refs` correctly identifies inbound and outbound markdown links. `refactor move --preview` shows the file move and which files need reference updates.

**Friction points:**
- `refactor move` requires either `--preview` or `--apply` — no default. Different from `maintain` (which defaults to preview). Minor inconsistency but documented.
- `refs` output doesn't show the specific link text or line numbers — just file paths. Less useful for diagnosing specific broken links.

**Severity:** Low.

---

## 5. CLI UX Assessment

### Naming

| Aspect | Assessment |
|--------|-----------|
| Command names | **Good.** `orient`, `check`, `maintain`, `explain`, `refs` are clear verbs. `md` is a reasonable namespace for markdown ops. |
| `outline` vs `md outline` overlap | **Moderate friction.** Both produce heading outlines for .md files. The top-level `outline` auto-delegates for .md files, creating confusion about which to use. |
| `config suggest` vs `config doctor` | **Clear separation.** `suggest` bootstraps; `doctor` diagnoses existing config. Well-named. |
| `refactor move` | **Clear.** Verb + operation, naturally extensible to future refactoring commands. |

### Help Text

| Aspect | Assessment |
|--------|-----------|
| Root help | **Good.** Clean table of commands with short descriptions. Shows global options. |
| Command help | **Mostly good.** Most commands explain their purpose and options. |
| `md query` help | **Weak.** No selector syntax documentation or examples. A user running `md query --help` has no way to know what selectors are valid. |
| Verb consistency | **Minor inconsistency.** Mix of "Show" (`orient`, `outline`), "Print" (`version`, `config show`), and imperatives (`Validate`, `Search`). |

### Option Consistency

| Pattern | Commands | Assessment |
|---------|----------|------------|
| `--apply` default-preview | `maintain`, `md edit *` | **Good.** Conservative default. |
| `--fix` + `--dry-run` | `check` | **Different convention.** Same concept, different flags. |
| `--preview` + `--apply` required | `refactor move` | **Third convention.** Neither is default. |
| Global options | All commands | **Consistent.** `--output`, `--verbosity`, `--no-color`, `--config` everywhere. |
| Short aliases | Selective | **Inconsistent.** Some options have aliases (`-s`, `-a`, `-m`), others don't. |

**Verdict:** The three different preview/apply patterns are the most significant UX inconsistency. A user learning `maintain --apply` will try `check --apply` and get confused.

### Output Readability

| Format | Assessment |
|--------|-----------|
| Text | **Good.** Clean, well-formatted, colored output with severity indicators. `orient` classification tags are readable. `check` completion summary is useful. |
| JSON | **Good.** Well-structured for most commands. `check` JSON includes summary, diagnostics, and completion sections. `search` JSON includes headingContext. |
| JSON gaps | `init` has no JSON. `status --coverage` JSON omits coverage. These are notable gaps. |

### Default Behavior

| Command | Default | Assessment |
|---------|---------|------------|
| `check` | Full scope, text output | **Good.** Sensible default. |
| `maintain` | Preview mode | **Good.** Conservative. |
| `md edit` | Preview mode | **Good.** Conservative. |
| `refactor move` | Neither preview nor apply | **Friction.** Must specify one. Error message could explain better. |
| `orient` | Full depth | **Good.** Shows everything. |
| `outline` | Current directory | **Good.** Sensible. |

### Error and Remediation Clarity

| Aspect | Assessment |
|--------|-----------|
| Validation diagnostics | **Good.** Include severity, rule ID, file path, message, and fix suggestion. |
| Fix suggestions | **Mostly clear.** "Run 'steward maintain --apply'" is actionable. "Create the file" is less helpful. |
| Config errors | **Good.** `config validate` gives clear messages. |
| Unrecognized selector | **Adequate.** Returns error text but doesn't suggest valid formats. |
| Missing arguments | **Adequate.** System.CommandLine generates usage text automatically. |

### JSON Agent Usability

| Aspect | Assessment |
|--------|-----------|
| Schema stability | **Moderate.** No versioned schema. Anonymous types in some commands. |
| Envelope consistency | **Low.** `check` uses formal `CheckResponse`. Other commands use ad-hoc shapes. No common envelope. |
| Completeness | **Mostly good.** `status --coverage` JSON gap is the main omission. |
| Parse-friendliness | **Good.** Standard JSON, `JsonIgnoreCondition.WhenWritingNull` reduces noise. |

---

## 6. Dogfooding Assessment

### Does this repo prove the CLI's value?

**Yes, substantially.** The `.steward/` configuration is non-trivial:
- `policy.yaml`: 19 artifacts with roles, importance, descriptions, freshness, completion policy
- `config.yaml`: Custom excludes for .NET build artifacts
- `path-policy.yaml`: Naming conventions for ADRs, RFCs, planning docs, audit docs
- `STRUCTURE.md`: Auto-maintained by `steward maintain`
- `check` validates the full contract (271 files, 13 rules)
- `start_here` provides curated entry points

This is genuine dogfooding, not cosmetic.

### Does dogfooding expose rough edges?

**Yes, several:**

1. **`check --scope changed` is broken on this repo.** The primary inner-loop command produces false positives. This was already identified in the previous reassessment but remains unfixed.

2. **`status --coverage` counts test fixtures as ungoverned.** The 9 "ungoverned" files are all test fixture repos (`tests/Steward.TestFixtures/Repos/`). Discovery excludes should filter these, or coverage reporting should exclude non-repo files.

3. **`config suggest` only finds 3 of 19 declared artifacts.** On this mature repo, the suggestion engine's heuristics don't detect ADRs, RFCs, traceability docs, planning docs, or audit docs. The suggest surface undersells the tool's capability.

4. **`explain path README.md` shows all 13 rules.** For a specific file, this overpromises — many rules can't fire on a particular file.

5. **The repo relies heavily on planning-index.md as a manual navigation surface.** Steward's `orient` and `status` are useful but don't replace this hand-curated index. There's no way for Steward to auto-maintain the planning-index.md as a navigation surface — only `STRUCTURE.md` gets auto-maintenance.

### Are there manual conventions that Steward should handle?

1. **Planning index maintenance.** The planning-index.md is manually maintained and is a critical navigation surface. Steward could auto-maintain it using the `directory-index` maintainer type, but it's not configured to.

2. **Decision index maintenance.** `docs/decisions/decision-index.md` has `index_of: docs/decisions` in policy.yaml but no maintenance artifact configured for it. The STWD-011 rule checks index completeness, but there's no auto-maintenance to fix it.

3. **Dated audit naming.** Audit docs use a `YYYY-MM-DD` suffix convention enforced by path-policy.yaml, but there's no way for Steward to auto-generate audit file scaffolding or suggest the correct naming pattern when creating a new audit.

### Does the repo work around the tool?

Not significantly. The configuration is well-suited to the repo's needs. The main gap is that `config suggest` doesn't help iterate toward the current rich configuration — the maintainer had to manually craft all 19 artifact declarations.

---

## 7. Prioritized Findings

### Critical Credibility Gap

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F1 | **Scoped validation produces false positives on clean trees** | `check --scope changed` on clean repo: 6 errors, 21 warnings, 0 files checked. `RequiredArtifactRule` checks `TargetFiles` for existence, which is empty under scoped mode. | Breaks the core inner-loop workflow for maintainers and agents. Directly contradicts PRD UC-02 and RFC-003. | Add `AllFiles` to `ValidationContext`. Existence-check rules use `AllFiles`; content rules use `TargetFiles`. Add regression tests. |

### Important Product Gap

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F2 | **Three inconsistent preview/apply conventions** | `check`: `--fix/--dry-run`. `maintain`/`md edit`: `--apply`. `refactor move`: `--preview/--apply` required. | Users learn one pattern, expect it everywhere, and get confused. Erodes CLI coherence. | Standardize on `--apply` (default preview) across mutation commands. `check --fix` could remain as a semantic alias but should also respect `--apply`. |
| F3 | **`status --coverage --output json` omits coverage data** | JSON output has no `governanceCoverage`, `ungovernedFiles` fields even when `--coverage` flag is set. | Agents requesting coverage via JSON get incomplete data. Breaks UC-08 for agents. | Include coverage data in JSON when `--coverage` is requested. |
| F4 | **`md query --pattern` batch mode broken** | `steward md query --pattern "docs/**/*.md" "heading[Status]"` fails with "missing argument". Argument parsing assigns glob to file arg. | Multi-file structural queries don't work. | Make `--pattern` and `selector` both options (not positional) in batch mode, or require selector via option in batch mode. |
| F5 | **`init` scaffolds policy that immediately fails `check`** | Fresh `init --profile software` → `check` → 2 errors (README.md + LICENSE missing), 2 warnings (CHANGELOG.md + CONTRIBUTING.md references broken). | New user's first experience is a wall of failures. | Scaffold `required: false` for artifacts that don't exist yet, or scaffold with comments explaining which to enable. |

### Workflow Gap

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F6 | **`config suggest` too shallow for mature repos** | On this 271-file repo with 19 declared artifacts, suggest only finds 3. | Doesn't help with iterative improvement. Undersells tool capability. | Improve heuristics: detect `docs/decisions/`, `docs/planning/`, date-suffixed audits, traceability docs. |
| F7 | **`explain path` doesn't filter applicable rules** | `explain path README.md` shows all 13 rules. Many can't fire on README (no managed regions, no freshness, no index_of). | Reduces signal-to-noise of the explain surface. | Filter to rules that have active governance for the file's path and artifact declaration. |
| F8 | **MdPath heading selector requires exact text match** | `heading[Overview]` returns nothing when heading is `1. Overview`. | Agents and users must know exact heading text. Forces md outline before md query. | Support substring or contains matching (e.g., `heading[*Overview*]`). |

### UX Inconsistency

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F9 | **`outline` and `md outline` overlap for .md files** | Both produce heading outlines. `outline <file.md>` delegates to md outline internally. | User confusion about which to use. | Document clearly in help text that `outline <file.md>` is a shortcut for `md outline <file.md>`. |
| F10 | **Verb inconsistency in help text** | "Show" (`orient`), "Print" (`version`), "Detect" (`config doctor`), "Validate" (`check`). | Minor polish issue. | Standardize display verbs to "Show" for non-mutating commands. |
| F11 | **`--quiet` only on `check`** | No other command has `--quiet`. | Inconsistent CI ergonomics. | Consider `--quiet` globally or remove it from `check` if verbosity=quiet achieves the same. |

### Docs Mismatch

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F12 | **`md query` help has no selector syntax documentation** | `md query --help` shows argument names but no selector examples or syntax reference. | Users can't discover MdPath syntax from CLI. | Add 2-3 selector examples to help text or a `steward md selector-help` command. |
| F13 | **README command table lists `steward explain path <file>` but help shows `steward explain <rule-id>`** | README documents both as separate commands. CLI help for `explain` doesn't mention the `path` subcommand in its description. | New user might not discover `explain path`. | Improve explain help to mention both modes. |

### Implementation Gap

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F14 | **`InternalError` exit code (3) never used** | Defined in ExitCodes.cs but no command returns it. | Dead code. | Either wire it into try-catch at the top level or remove the constant. |
| F15 | **Exit code test coverage is thin** | Only 2 tests for UsageError. No tests for Success, ValidationFailure, or InternalError exit codes. | Low confidence in exit code contracts for CI users. | Add explicit exit code tests for check-pass (0), check-fail (1), and bad-input (2). |
| F16 | **Governance coverage counts test fixtures as ungoverned** | `status --coverage` reports 85% with 9 ungoverned test fixture files. | Noisy coverage metric. | Exclude test fixture repos from coverage calculation or make it configurable. |

### Nice-to-Have

| # | Finding | Evidence | Impact | Suggested direction |
|---|---------|----------|--------|---------------------|
| F17 | **`refs` doesn't show line numbers or link text** | Only shows file paths for inbound/outbound links. | Less useful for diagnosing specific link issues. | Include line number and link text in refs output. |
| F18 | **No JSON envelope consistency** | `check` uses formal types; other commands use anonymous types. | Harder for agents to write general parsers. | Consider a thin common envelope `{ "command": "...", "data": {...} }`. |
| F19 | **`init` has no JSON output** | Only text output, ignores `--output json`. | Breaks scripted workflows. | Add JSON output for init. |

---

## 8. Scores

| Dimension | Score (0–5) | Rationale |
|-----------|-------------|-----------|
| **Promise fidelity** | 3.5 | Product delivers on the "stewardship beyond validation" promise for full-scope use. Scoped validation bug and init experience weaken the promise for two key workflows. Pre-1.0 messaging is honest. |
| **Workflow usefulness** | 3.5 | Full-repo orientation, validation, maintenance, and search workflows are strong. Scoped validation (a core workflow) is broken. Config debugging is excellent. Markdown operations work well for single files. |
| **CLI ergonomics** | 3.0 | Naming is mostly good. Help text is mostly good. Three different preview/apply patterns hurt. `md query` syntax is undiscoverable. Global options are consistent. |
| **Stewardship value** | 4.0 | Genuinely goes beyond validation. Orient, maintain, explain, refs, search, and the governance model create real stewardship value. This is Steward's strongest dimension. |
| **Dogfooding quality** | 3.5 | The repo's self-use is real and non-trivial. 19 artifacts, naming conventions, completion policy, freshness — this proves the model works. Test fixture noise and scoped-validation bug weaken the picture. |
| **Configurability** | 3.5 | policy.yaml is expressive. Profiles work. Config introspection is strong. `config suggest` is too shallow. Path-policy model is useful. No silent footguns found. |
| **Markdown subsystem** | 3.5 | md query, md outline, md edit are practical and coherent. Heading selector requires exact match. Batch query broken. Selector syntax undiscoverable. But single-file ops work well. |
| **Governance/rule system** | 4.0 | 13 rules covering a wide governance surface (naming, index, freshness, discoverability, stale, broken, managed regions, section size, frontmatter). All explainable. Well-scoped. Rule IDs and categories are professional. |
| **AI-agent usefulness** | 3.0 | JSON output is well-structured on most commands. Scoped validation (core agent inner loop) is broken. Status coverage JSON is incomplete. Search heading context is genuinely useful. |
| **Release-line credibility** | 3.5 | Pre-1.0 claims are honest. Implementation-status.md is accurate. Remaining-work planning is explicit. The scoped validation bug is the main credibility risk — it's been identified in multiple audits but remains unfixed. |
| **Overall** | **3.4** | A meaningfully differentiated stewardship CLI with real value, honest positioning, and several concrete issues that need targeted correction before the product can claim strong coherence across all its stated workflows. |

---

## 9. Final Recommendation

**Expectations are partly met but need targeted correction.**

Steward v0.10.0 genuinely delivers on the "stewardship beyond validation" promise for full-scope usage. The governance model, orientation surfaces, deterministic maintenance, config introspection, and Markdown subsystem are real differentiators. The product is meaningfully distinct from generic linters and file-tree tools.

However, the scoped validation bug is a critical trust issue that directly undermines the most important inner-loop workflow documented in the README, PRD, and RFC-003. Combined with the preview/apply inconsistency, the `md query --pattern` breakage, and the init-scaffolding gap, the product's coherence is weakened enough that a serious new user exercising the documented workflows would encounter meaningful friction.

**The strongest parts today:**
- Governance model (13 rules, well-scoped, explainable)
- Full-scope stewardship loop (orient → status → check → maintain)
- Config introspection (show → validate → doctor)
- Markdown structural operations (query, outline, edit)
- Dogfooding authenticity (real, non-trivial self-use)

**What must change before stronger claims:**
1. Fix scoped validation (F1) — this is non-negotiable for inner-loop trust
2. Standardize preview/apply (F2) — this is about CLI coherence
3. Fix `status --coverage` JSON (F3) — this is about agent trust
4. Fix `md query --pattern` (F4) — this is about subsystem completeness

---

## 10. Actionable Remediation Plan

### Fix Immediately

| Item | Finding | Effort | Impact |
|------|---------|--------|--------|
| Split `TargetFiles`/`AllFiles` in ValidationContext | F1 | Small-medium | Fixes the single most critical product bug |
| Include coverage in status JSON output | F3 | Small | Fixes agent data completeness |
| Add selector examples to `md query --help` | F12 | Trivial | Improves discoverability |
| Add exit code tests for check-pass, check-fail, bad-input | F15 | Small | Improves CI contract confidence |

### Next Milestone

| Item | Finding | Effort | Impact |
|------|---------|--------|--------|
| Standardize preview/apply flag convention | F2 | Medium | Improves CLI coherence |
| Fix `md query --pattern` argument parsing | F4 | Small | Fixes batch markdown operations |
| Scaffold init with `required: false` for missing artifacts | F5 | Small | Improves new-user experience |
| Filter `explain path` to actually applicable rules | F7 | Medium | Improves explainability signal-to-noise |
| Exclude test fixtures from governance coverage | F16 | Small | Reduces noise |

### Later / Optional

| Item | Finding | Effort | Impact |
|------|---------|--------|--------|
| Improve `config suggest` heuristics | F6 | Medium | Better bootstrap experience |
| Support substring heading matching in MdPath | F8 | Medium | Better md query usability |
| Add line numbers to refs output | F17 | Small | Better reference diagnostics |
| Add JSON envelope consistency | F18 | Medium | Better agent ergonomics |
| Wire InternalError exit code or remove it | F14 | Trivial | Code hygiene |
| Standardize help text verb style | F10 | Trivial | Polish |
| Add JSON output to init | F19 | Small | Scripting completeness |
| Document outline/md outline overlap | F9 | Trivial | Reduces confusion |

---

## 11. Differentiation Assessment

### Does the CLI justify its existence?

**Yes.** Steward does something that no combination of generic tools provides:
1. **Repository contract modeling** — declaring what a repo should contain, with roles, importance, and governance rules
2. **Contract validation** — checking the repo against that contract with 13 different rule types
3. **Classified orientation** — showing repo structure by artifact role, not just file tree
4. **Deterministic maintenance** — auto-generating and refreshing governed documents
5. **Markdown structural operations** — querying and editing markdown with structural selectors
6. **Cross-reference analysis** — mapping inbound/outbound markdown links for safe refactoring
7. **Explainable governance** — every rule is explainable with remediation guidance

### What's strongest and most defensible?

The **governance model** (policy-driven, multi-rule, explainable, configurable) and the **stewardship loop** (orient → status → check → maintain) are the most defensible. No generic linter provides this level of repository-contract-aware stewardship.

### What still feels generic or not yet earned?

- **`outline`** feels like a dressed-up `tree` command for directory listing.
- **`search`** is useful but not dramatically better than `rg` for most use cases (heading context is the differentiator).
- **`config suggest`** promises bootstrap intelligence but delivers too little on mature repos.

---

## Appendix A: CLI Invocation Evidence

All findings in this assessment are backed by direct CLI invocation on the Steward repository itself (v0.10.0, .NET 10, Windows). Key invocations:

- `steward orient` / `orient --signals` / `orient --output json`
- `steward status` / `status --coverage` / `status --coverage --output json`
- `steward check` / `check --scope changed` / `check --scope staged` / `check --output json`
- `steward check --fix --dry-run`
- `steward maintain` / `maintain --diff`
- `steward explain` / `explain STWD-008` / `explain path README.md`
- `steward config show --effective` / `config validate` / `config doctor` / `config suggest`
- `steward search "validation"` / `search --mode headings` / `search --role authoritative` / `search --output json`
- `steward md outline docs/requirements/PRD.md`
- `steward md query docs/requirements/PRD.md "heading[1. Overview]"` / `"heading[Overview]"` / `"frontmatter"`
- `steward md query --pattern "docs/**/*.md" "heading[Status]"` (broken)
- `steward refs README.md` / `refs docs/planning-index.md`
- `steward refactor move docs/planning/curation-notes.md docs/planning/curation-notes-test.md --preview`
- `steward outline .`
- `steward version`
- `steward init --profile software` (in clean temp directory)

All 505 tests pass (367 core + 138 CLI) as of assessment date.
