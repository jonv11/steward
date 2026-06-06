---
type: audit
status: Historical
last_updated: 2026-04-19
standalone: true
---

# V1 Product Readiness Review — 2026-04-19

## Executive Summary

Steward is a real, functioning product with genuine value for its intended audience. The CLI works, the config model is expressive, the validation rules are meaningful, the Markdown engine is mature, and the test suite is strong. The core implementation is further along than many pre-1.0 tools that already call themselves stable.

**However, the repository does not yet feel like a strong v1 to an external maintainer or contributor evaluating it for adoption.** The reason is not implementation quality — it is product presentation.

The central problem: **99% of the `docs/` directory is internal development archaeology, and the sole user-facing surface is a single overloaded README.** An external evaluator arriving at this repo sees 80+ files under `docs/` and finds zero user documentation. Every doc is about building, reviewing, or governing the Steward project itself — not about using Steward in *your* repo. The implementation is strong; the user-facing story is underdeveloped.

**Verdict: Not ready for a "strong v1 feel."** The tool is likely ready for a credible `v0.x` public release aimed at early adopters. Reaching a v1 feel requires: (1) a user documentation surface that exists independently from the README, (2) a clear separation between project-internal docs and user docs, (3) a configuration reference, and (4) narrowing of several README claims that outpace implementation.

The product does not overpromise dangerously — the README is generally honest — but it implies broader maturity than the repo delivers when an evaluator looks beyond the README.

---

## 1. What The Product Actually Is Today

### Evidence-based product description

Steward is a .NET 10 CLI tool (`steward`) that helps enforce repository structure, documentation standards, and governance policies via declarative YAML configuration. It operates on the local filesystem and git state without requiring network access or hosting platform integration.

### Intended audience

- **Maintainers** of repositories who want to declare and enforce documentation structure, naming conventions, frontmatter requirements, and artifact lifecycle policies.
- **Contributors** working inside governed repositories who need to validate their changes against those policies.
- **AI coding agents** that operate in inspect → change → validate → remediate loops and benefit from machine-readable (JSON) output.

### Maintainer value proposition

A maintainer can define a `.steward/` config that declares required artifacts, naming rules, frontmatter expectations, artifact families, maintenance targets, and freshness windows. `steward check` validates the entire repository or scoped changes against this contract. `steward maintain` regenerates indexes and structure documents deterministically.

### Contributor value proposition

A contributor runs `steward check` (or `check --scope staged`) to validate work before committing. Violations include rule IDs that can be explained via `steward explain`. Some fixes are automatic. The contributor does not need to understand `.steward/` config — they just need to pass the check.

### Core supported workflows today

| Workflow | Commands | Maturity |
|----------|----------|----------|
| Repository orientation | `orient`, `outline`, `status` | Solid |
| Policy-driven validation | `check`, `check --scope changed/staged`, `check --fix --apply` | Solid |
| Rule explainability | `explain <rule>`, `explain path <file>` | Solid |
| Config scaffolding | `init --profile`, `config suggest`, `config validate`, `config doctor` | Solid |
| Deterministic maintenance | `maintain --apply` (structure docs, indexes, frontmatter dates) | Solid |
| Markdown structural query | `md query`, `md outline` | Solid |
| Markdown structural editing | `md edit` (9 operations) | Solid |
| Content search | `search` (substring/regex, headings/content) | Functional, basic |
| Reference tracking | `refs`, `refactor move` | Functional |
| Artifact family governance | `artifact_families` in policy, family-aware validation/status/orient | Solid |

### Notable limitations (practical non-goals inferred from evidence)

- No GUI, no IDE plugin, no LSP integration
- No hosting platform integration (no GitHub/GitLab API)
- No content generation or AI model invocation
- No source code linting (not a code linter)
- Profiles limited to `software`, `docs`, `minimal` (`mixed`/`knowledge` deferred)
- Search is substring/regex only — no fuzzy or semantic search
- `--role` search only matches explicit artifact declarations, not family-classified files
- No NuGet package published yet (workflow exists but no hosted run evidence)
- .NET 10 SDK required (not yet widely adopted; limits audience)
- `System.CommandLine` beta dependency (documented, intentional)
- No top-level exception handler — unhandled errors produce raw .NET stack traces

---

## 2. Promise vs Reality Matrix

| Area | What repo/docs imply | What implementation supports | Likely user interpretation | Risk to trust | Severity |
|------|---------------------|------------------------------|---------------------------|---------------|----------|
| **Installation via NuGet** | README: `dotnet tool install --global Steward` as first install option | Workflow exists but no hosted run. NuGet package may not exist on nuget.org. README includes caveat but leads with the command. | "I can install this right now." | High — first command fails | **P0** |
| **"No config needed"** for orient/check | README: "No `.steward/` config needed for `orient`, `outline`, or `check`" | True but misleading. Without config, `check` validates nothing and returns clean. `orient` shows a raw file list, not governance signals. | "Steward is useful without setup." | Medium — technically true but practically empty | **P1** |
| **User documentation** | 80+ files under `docs/` visible in repo tree | Zero user-facing docs. All 80 files are internal project governance (audits, RFCs, ADRs, planning). | "This project has thorough documentation." Opens docs, finds internal sprint artifacts. | High — credibility mismatch | **P0** |
| **Configuration reference** | README has inline YAML examples covering major features | No standalone schema reference exists. Users must piece together config from README examples, RFC-002, and C# source. | "The README covers enough." Then hits an unlisted field or edge case and has no reference. | Medium — limits adoption confidence | **P1** |
| **Auto-fix capabilities** | README: "Some rules have deterministic auto-fixes" | Only 3 of 18 rules support auto-fix (STWD-003, STWD-007, STWD-012). The rest require manual remediation. | "Most violations can be auto-fixed." | Low — the wording "some" is honest, but the ratio could be clearer | **P2** |
| **AI agent as first-class user** | README, PRD, and AGENTS.md strongly position AI agents as primary users | JSON output is solid (standard envelope, structured errors). But no agent-facing integration guide exists outside AGENTS.md (which is about *this* repo's agents, not *your* repo's agents). | "There's a guide for setting up Steward with my AI agent." | Medium — the AGENTS.md is for Steward contributors, not Steward users | **P1** |
| **"Tested first-hour path"** | README "First 15 Minutes" claims a tested path | The fresh-eyes audit (2026-04-18) found this path requires careful reading and .NET expertise. The path was tested *by the team* on their own repo. | "I can follow this in 15 minutes." | Low-medium — realistic for .NET developers | **P2** |
| **Three profiles** | `init --profile software\|docs\|minimal` | `software` is actively used and well-tested. `docs` and `minimal` have fixture-backed tests but limited real-world validation. | "All three profiles are production-ready." | Low — profiles work, just haven't been widely battle-tested | **P2** |
| **Cross-platform** | README mentions Windows, macOS, Linux CI matrix | CI workflow exists but has never had a hosted green run. Local testing only. | "This runs reliably on my platform." | Medium — probably works but unproven in CI | **P1** |
| **Broken link detection** | Listed as a feature; STWD-008 exists | Works well. Includes fragment-anchor validation (STWD-018). | Accurate | None | — |
| **Deterministic maintenance** | Feature list and maintainer docs | Works well for structure-documents, indexes, frontmatter-auto. 6 maintainer types implemented. | Accurate | None | — |
| **Rule explainability** | "Every validation rule is explainable with remediation guidance" | All 18 rules have explain text and remediation. Consistency test enforces non-generic remediation. | Accurate | None | — |
| **path-policy.yaml** | Documented in README, referenced by init output | `init` does not create path-policy.yaml. No template or example file is scaffolded. README has examples but a new user has to create the file manually with no starter content. | "Init sets this up for me." | Low-medium — the gap is noted in init output but still friction | **P2** |
| **Error handling maturity** | Product implies production-ready CLI | No top-level exception handler. `ExitCodes.InternalError = 3` is defined but never used. Unhandled exceptions produce raw .NET stack traces. | "Errors will be handled gracefully." | Medium — any unexpected error looks like a crash | **P1** |

---

## 3. Maintainer POV Review

### Maintainer strengths

1. **The config model is genuinely powerful.** `policy.yaml` with artifacts, artifact families, frontmatter requirements, maintenance declarations, severity overrides, and path-policy gives maintainers fine-grained control. The model is well-designed.

2. **`config validate` and `config doctor` are excellent.** These catch real problems: dead start_here entries, unreachable patterns, invalid rule IDs, semantic config errors. This is rare in CLI tools and builds real trust.

3. **`explain path` is a standout feature.** Being able to ask "what governance applies to this file?" and get a clear answer is exactly what maintainers need when debugging unexpected validation results.

4. **Artifact families solve a real problem.** Convention-based grouping with frontmatter schemas, naming patterns, required sections, and min-counts is the right abstraction for ADRs, RFCs, runbooks, and similar document types.

5. **Deterministic maintenance is well-executed.** Structure documents and indexes regenerate cleanly with minimal diff. Preview/apply safety is consistent.

6. **The README's maintainer path is well-structured.** Clear numbered steps from init through check, with a reference table of enforceable rules.

7. **18 validation rules with consistent rule IDs, severity levels, and remediation text.** The consistency test in the test suite ensures no rule has generic remediation. This is strong.

### Adoption clarity

A maintainer evaluating Steward can understand *what* it does from the README. The "Who Is Steward For?" section is clear. The maintainer getting-started path is logical.

**However:** The adoption story breaks down at "how do I configure this for my specific needs?" The README inline examples cover the basics but there is no configuration reference. A maintainer wanting to know all valid values for `importance`, all supported `role` strings, all maintenance `type` values, or the complete `frontmatter_schema` shape has to read C# source code.

### Config and governance clarity

The config model is expressive but **under-documented for external users**:

- No list of valid `role` values exists outside code
- No list of valid `importance` values exists outside code
- No explanation of profile merge semantics beyond a brief README note
- No explanation of `frontmatter_requirements` vs `frontmatter_schema` (on families) vs `governance.frontmatter.required_fields` and how they interact
- `coverage.exclude` vs `discovery.exclude` vs `validation.path_overrides[].disabled_rules` — three different exclusion mechanisms with no guide on when to use which
- `completion_policy` is mentioned in passing but not explained for user configuration

### Maintainer friction points

1. **No schema reference.** A maintainer trying to write a non-trivial policy.yaml is guessing at field names and valid values.
2. **NuGet install may fail.** The first thing a maintainer tries (`dotnet tool install --global Steward`) may not work if no package is published.
3. **.NET 10 SDK requirement.** .NET 10 is recent. Many potential users won't have it. The error when using an older SDK is unclear from Steward's side (it's a dotnet build error, not a Steward error).
4. **No example configurations.** No "here's what Steward looks like on a typical Node.js project" or "here's a Python monorepo config." The only example is Steward's own complex `.steward/` config.
5. **`config suggest` is helpful but limited.** It detected 11 suggestions on Steward's own 87-file repo, but several were marked `[conservative]`. On a fresh repo, it would find even fewer. The gap between `config suggest` output and a useful policy is large.
6. **No guided config editing.** After `init`, the maintainer must hand-edit YAML. No interactive mode, no validation-as-you-go.

### Trust gaps

- **The repo's `docs/` directory undermines product trust.** A maintainer evaluating adoption will look at the docs directory and find 29 audit files from a single development sprint, 13 RFCs, 14 ADRs, and 13 planning docs — all about Steward's own internal governance. This signals either that the project is still in heavy internal development or that the team has not yet prioritized external users.
- **No published release exists.** The release workflow is authored but never run. This means the maintainer cannot actually install Steward via the recommended method.
- **The `System.CommandLine` beta dependency is documented** but still raises questions about stability for a tool that governs other people's repos.

### Maintainer prioritized issues

| Priority | Issue |
|----------|-------|
| P0 | NuGet install path may fail — need published package or clearer fallback |
| P0 | No user documentation beyond README |
| P1 | No configuration schema reference |
| P1 | No example configs for common repo types |
| P1 | No error handling for unhandled exceptions |
| P1 | No agent integration guide for *users* of Steward (vs contributors to it) |
| P2 | `config suggest` gap between output and usable policy |
| P2 | No `path-policy.yaml` scaffolding from init |

---

## 4. Contributor POV Review

### Contributor strengths

1. **The contributor path is clean.** `steward check` → `steward explain <rule>` → fix → re-check is a well-designed loop.
2. **Scoped validation works.** `--scope changed` and `--scope staged` let contributors validate only their changes. The B6 false-positive bug is fixed.
3. **Exit codes are clear and stable.** 0/1/2/3 mapping is simple and well-documented.
4. **Rule explanations are actionable.** Each rule has a specific remediation, not generic advice.
5. **`explain path`** tells the contributor exactly what rules apply to the file they're editing.
6. **`check --fix --apply`** auto-fixes what it can (3 rules) with preview safety.
7. **`orient` works without config.** A contributor in an unconfigured repo still gets a classified file tree.

### Onboarding quality

The README "Getting Started — Contributor" section is clear and correct. A contributor who reads it can operate the tool in about 5 minutes.

**However:** The contributor section assumes Steward is already installed. The installation section is oriented toward maintainers building from source. A contributor in another repo needs the NuGet install path (which may not work) or needs the maintainer to provide the binary.

### First-success experience

On a repo with `.steward/` config already set up by a maintainer:
- `steward check` — works, gives clear pass/fail with rule IDs
- `steward explain STWD-003` — explains the rule clearly
- `steward check --scope staged` — validates only staged files

On a repo without config:
- `steward check` — passes with "no issues found" (technically correct, practically useless)
- `steward orient` — shows file tree but no governance signals
- A contributor may conclude "this tool doesn't do anything" if they try it on an unconfigured repo

### Clarity of commands / docs / examples

The command table in the README is clear. Help text (`--help`) is good — each command has a one-line description and example hints. The `md edit` subcommands have particularly good help text with inline examples.

**Gap:** The README has Markdown examples at the bottom but no "typical contributor workflow" example showing the full check → explain → fix → re-check cycle with actual output samples. A contributor doesn't know what `steward check` output looks like until they run it.

### Validation and remediation quality

Strong. Each diagnostic includes:
- Severity marker (`[error]`, `[warning]`, `[info]`)
- Rule ID (e.g., `STWD-008`)
- File path and optional line number
- Clear message
- Remediation line with `fix:` prefix

This is above average for CLI tools. The consistency test ensuring non-generic remediation is a notable quality signal.

### Contributor friction points

1. **No output examples in docs.** A contributor doesn't know what check/orient/status output looks like before running it.
2. **Text-mode errors lack suggested next steps.** JSON mode has `suggestedNextStep` fields; text mode often just prints the error.
3. **Unhandled exceptions produce stack traces.** A contributor hitting an edge case sees raw .NET output.
4. **`search --role` limitation is not documented for contributors.** Only matches explicit artifact entries, not family-matched files. A contributor searching for "all governance docs" via `--role governance` gets an incomplete list.
5. **No "what to do next" after a clean check.** Just "No issues found." A progressive tool might suggest `steward status --coverage` or `orient --signals` as next steps.

### Contributor prioritized issues

| Priority | Issue |
|----------|-------|
| P1 | No output examples showing what check/orient/status produce |
| P1 | Text-mode error messages lack next-step guidance |
| P1 | `search --role` limitation undocumented for users |
| P2 | No progressive guidance after clean check pass |
| P2 | Contributor install path depends on unreleased NuGet package |

---

## 5. Top Ambiguity / Over-Promise Hotspots

### Hotspot 1: "Install from NuGet" as the primary install option

**What the repo suggests:** README leads with `dotnet tool install --global Steward` and frames it as the recommended path.
**What actually exists:** The release workflow has never been run in CI. No package may exist on nuget.org.
**Why it matters:** The very first thing a potential user does is try to install. If this fails, trust is damaged immediately and may not recover.
**Severity:** P0 for v1 feel.

### Hotspot 2: docs/ directory signals mature user documentation

**What the repo suggests:** 80+ files under `docs/` in a well-organized tree with subdirectories.
**What actually exists:** 100% internal development artifacts. Zero user-facing docs.
**Why it matters:** An evaluator's second action (after install) is to look at documentation. Finding only internal audit records from a 5-day sprint creates a strong "this project isn't ready" impression.
**Severity:** P0 for v1 feel. The volume of internal docs actually hurts rather than helps external perception.

### Hotspot 3: AGENTS.md implies agent guidance for Steward users

**What the repo suggests:** Prominent AGENTS.md with detailed agent workflow guidance.
**What actually exists:** AGENTS.md is exclusively about contributing to the Steward repo itself. It describes how to work in *this* repo with *this* tool on itself.
**Why it matters:** An evaluator interested in using Steward with their AI agents opens AGENTS.md expecting integration guidance and finds contributor instructions for Steward development.
**Severity:** P1. Not a trust-breaker but a missed opportunity and a source of confusion.

### Hotspot 4: "Configurable repository stewardship CLI" without config reference

**What the repo suggests:** A highly configurable tool with three YAML files, profiles, families, path policies, severity overrides, and more.
**What actually exists:** README inline examples cover ~60% of the configuration surface. No standalone reference documents all fields, valid values, and interactions.
**Why it matters:** Configurability is the core value proposition. Without a reference, maintainers must reverse-engineer config from README examples and source code.
**Severity:** P1.

### Hotspot 5: "for humans and AI agents" dual-audience promise

**What the repo suggests:** AI agents are a first-class audience alongside humans.
**What actually exists:** JSON output is solid. Standard envelope with schema versioning, structured errors, machine-readable diagnostics. But no integration guide, no example agent workflow, no documentation of the JSON contract for agent consumers.
**Why it matters:** The promise sets expectations for agent-facing docs that don't exist for users.
**Severity:** P1.

### Hotspot 6: No error boundary for unexpected failures

**What the repo suggests:** A production-quality CLI with defined exit codes (0, 1, 2, 3).
**What actually exists:** Exit code 3 (`InternalError`) is defined but never emitted. No top-level exception handler exists. Unhandled exceptions produce raw .NET stack traces.
**Why it matters:** Any unexpected error path — a corrupt YAML file, a filesystem permission issue, a Markdown parsing edge case — crashes with no user-friendly message and no diagnostic guidance.
**Severity:** P1.

---

## 6. Recommended Product-Positioning Corrections

### README reframing

1. **Move NuGet install to "Install from NuGet (when available)"** and lead with the source-build path until a published package exists with evidence. Do not lead with a command that may fail.

2. **Add a "What Steward Does Not Do" section** near the top. The non-goals in the PRD are well-written but buried. External users need to see boundaries quickly: not a code linter, not a CI replacement, not a content generator, not a package manager.

3. **Narrow the "no config needed" claim.** Currently: "No `.steward/` config needed for `orient`, `outline`, or `check` — they work on any repo immediately." Recommend: "These commands work on any repo immediately, but Steward's validation power requires `.steward/` configuration. Without it, `check` has nothing to enforce."

4. **Be explicit about the 3-of-18 auto-fix ratio.** The "Some rules have deterministic auto-fixes" phrasing is technically honest but undersells the manual work. Consider listing which rules support auto-fix.

5. **Reframe the AI agent positioning.** The README and PRD make AI agents a co-primary audience. The actual product experience is CLI-with-JSON-output. Recommend: "Steward produces machine-readable JSON output suitable for AI agent integration" rather than "first-class AI agent companion."

### Maintainer vs contributor messaging

The README already separates these well. The main improvement needed is:
- Maintainer messaging should link to a config reference (once it exists)
- Contributor messaging should include output examples
- Both should acknowledge the .NET 10 SDK requirement more prominently

### Language changes

- "Repository stewardship CLI" is good but abstract. Consider adding a concrete subtitle: "Enforce documentation structure, naming conventions, and artifact policies in any repository."
- "Dual-audience" language in the PRD should not leak into user docs. Users don't care about product architecture decisions.

---

## 7. Recommended Repo/Doc Architecture Corrections

### Docs to retire or consolidate

The `docs/audits/` directory contains 29 files, of which 10+ are marked as "Historical Stub" or "Superseded." These are valuable as internal evidence but actively harm external perception.

**Recommendation:** Move internal-only docs to a less prominent location or clearly separate them:

| Action | Files | Rationale |
|--------|-------|-----------|
| Consider a `docs/internal/` or `docs/project/` directory | All of `docs/audits/`, `docs/reviews/`, `docs/planning/`, `docs/decisions/`, `docs/requirements/` | Separates internal project governance from (future) user docs |
| Alternatively, add a clear `docs/README.md` | — | Explain that `docs/` contains project governance artifacts, not user documentation, and point to README.md as the user entry point |
| Stub audits could be deleted | 8-10 "Historical Stub — superseded" files | They add navigational noise with minimal evidence value |

### Docs that should become authoritative

| Document | Recommendation |
|----------|---------------|
| README.md | Remains the primary user entry point but should be unburdened by extracting config reference and examples into separate files |
| A new `docs/guide/` or `docs/user/` directory | Should contain: configuration reference, example configs, output examples, troubleshooting, agent integration guide |

### Missing artifacts truly needed for clarity

| Artifact | Why needed | Priority |
|----------|-----------|----------|
| **Configuration reference** (all fields, valid values, defaults, interactions) | Core adoption blocker — maintainers cannot configure without guessing | P0 |
| **Example configs for common repo types** (Node.js project, Python project, monorepo, docs-only repo) | Reduces "how do I use this on MY repo?" friction | P1 |
| **Output examples** (what check/orient/status output looks like on success and failure) | Contributors don't know what to expect | P1 |
| **docs/ README or separator** (explaining that docs/ is internal) | Stops evaluators from concluding "no user docs" | P1 |

### Repo-level improvements for external understanding

1. **The `planning-index.md`** is the main navigation hub but it is purely internal. An external evaluator following links from README ends up in internal planning. Consider whether external users should ever reach this page, or whether it should be clearly marked as "Project Development Index."

2. **`STRUCTURE.md`** is auto-generated and shows the full repo tree. For an external evaluator, this reinforces the "lots of internal docs" impression. It's useful internally but not a good external orientation surface. The README already handles external orientation.

3. **The `repository-steward-master-requirements.md`** at repo root is a 2000+ line internal requirements document. Its prominence at the root level alongside README.md and CONTRIBUTING.md is confusing for external users.

---

## 8. Prioritized Action List

### P0 — Fix before calling this a strong v1 experience

| # | Title | Category | Rationale | Affected areas | Type |
|---|-------|----------|-----------|----------------|------|
| 1 | **Create user-facing documentation surface** | Docs | The product's sole user documentation is the README. A v1 product needs at minimum a configuration reference, output examples, and a guide for common adoption scenarios. | New `docs/guide/` or similar | Doc-only |
| 2 | **Create configuration schema reference** | Docs | Maintainers cannot configure Steward without reverse-engineering YAML from README examples and C# source. Document all fields, valid values, defaults, and interactions for config.yaml, policy.yaml, and path-policy.yaml. | New reference doc | Doc-only |
| 3 | **Ensure NuGet install works or reorder install guidance** | Docs + release | The README leads with a `dotnet tool install` command that may fail. Either publish the package or lead with the source-build path. | README.md, release workflow | Mixed |
| 4 | **Separate internal docs from user-facing content** | Repo structure | 80 internal files in `docs/` create a false impression of mature user documentation. Add at minimum a `docs/README.md` that explains the directory is for project governance and directs users to the product README. | docs/ structure | Repo-only |

### P1 — Should fix soon after or as part of hardening

| # | Title | Category | Rationale | Affected areas | Type |
|---|-------|----------|-----------|----------------|------|
| 5 | **Add top-level exception handler** | Code | Unhandled exceptions produce raw .NET stack traces. Exit code 3 is defined but never used. Wrap the command pipeline in a catch-all that emits a user-friendly message and returns exit 3. | Program.cs | Code-only |
| 6 | **Add output examples to docs** | Docs | Contributors don't know what steward check/orient/status output looks like. Show real output for pass, fail, and warning scenarios. | New doc or README section | Doc-only |
| 7 | **Create example configs for common repo types** | Docs | The only config example is Steward's own complex `.steward/`. Show what a minimal Node.js, Python, or docs-only repo config looks like. | New examples doc | Doc-only |
| 8 | **Document `search --role` limitation** | Docs | `--role` only matches explicit artifact entries, not family-classified files. Users expecting "find all governance docs" get incomplete results. | README.md search section or reference | Doc-only |
| 9 | **Add "What Steward Does Not Do" to README** | Docs | Non-goals exist in the PRD but not in user-facing docs. External users need to see boundaries early. | README.md | Doc-only |
| 10 | **Narrow "no config needed" claim in README** | Docs | Technically true but practically misleading. Without config, check validates nothing. Reframe to set correct expectations. | README.md | Doc-only |
| 11 | **Clarify AGENTS.md scope** | Docs | AGENTS.md is about contributing to Steward, not about using Steward with agents. Either rename/clarify or create separate agent integration guidance. | AGENTS.md | Doc-only |
| 12 | **Publish first hosted CI and release evidence** | Release | CI and release workflows exist but have never run hosted. Unverified CI is not credible evidence of cross-platform support. | .github/workflows/ | Workflow |
| 13 | **Add text-mode next-step hints on errors** | Code | JSON mode has `suggestedNextStep`; text mode often lacks guidance on errors. | Various command handlers | Code-only |

### P2 — Polish / quality-of-life

| # | Title | Category | Rationale | Affected areas | Type |
|---|-------|----------|-----------|----------------|------|
| 14 | **Add progressive guidance after clean check** | Code | "No issues found." is fine but a v1 tool could suggest `status --coverage` or `orient --signals` as next exploration steps. | CheckCommand | Code-only |
| 15 | **Scaffold path-policy.yaml from init** | Code | init mentions path-policy.yaml in next-steps but doesn't create even a commented template. | InitCommand | Code-only |
| 16 | **List auto-fixable rules explicitly** | Docs | "Some rules have auto-fixes" is vague. List the 3 fixable rules (STWD-003, STWD-007, STWD-012) in the README rules table or troubleshooting. | README.md | Doc-only |
| 17 | **Clean up historical stub audits** | Repo | 8-10 audit files are "Historical Stub — superseded" with minimal content. They add navigational noise. | docs/audits/ | Repo-only |
| 18 | **Move MRD to docs/requirements/** | Repo | `repository-steward-master-requirements.md` at repo root is a 2000+ line internal spec. Its root-level placement is confusing for external users. | Repo root | Repo-only |
| 19 | **Add `--dry-run` to init** | Code | Can't preview what init would create without writing files. | InitCommand | Code-only |
| 20 | **Improve `config suggest` gap-to-usable-policy** | Code | Suggestions are conservative and sparse. The gap between suggest output and a useful policy is large. | ConfigSuggestCommand | Code-only |

---

## 9. Deferred Items / Acceptable Gaps

The following items are acceptable to leave imperfect for a v1 if positioned honestly:

| Item | Why acceptable |
|------|---------------|
| **Only 3 profiles** (`software`, `docs`, `minimal`) | Covers the most common use cases. `mixed`/`knowledge` can come later. ADR-014 documents this clearly. |
| **`System.CommandLine` beta dependency** | Documented and intentional. The library is stable in practice despite the beta tag. No user-facing instability observed. |
| **Search is substring/regex only** | Adequate for the stated use cases. Fuzzy/semantic search is a nice-to-have, not a v1 requirement. |
| **No IDE integration** | Explicitly a non-goal. CLI-first is the right positioning for v1. |
| **No hosting platform integration** | Explicitly a non-goal. Offline-first local tool is the right scope. |
| **Only 3 auto-fixable rules** | Honest about this. Most governance violations require human judgment. Auto-fix for structural issues (stale artifacts, missing frontmatter, stale dates) is the right scope. |
| **.NET 10 SDK requirement** | Limits audience but is the right technical choice. Should be documented more prominently. |
| **No code coverage measurement in CI** | Tests exist and are strong (717 tests, 18 rule-specific test classes, contract tests, snapshot tests). Code coverage metrics would be nice but are not a trust blocker. |
| **`search --role` limitation** | Acceptable if documented. The feature works as designed; the limitation is that family classification and explicit artifact entries are separate namespaces. |
| **JSON envelope default is `legacy` in 0.15.x** | Standard envelope exists and works. Making it the default is a later migration. The `[Unreleased]` changelog already notes this is being addressed. |
| **No interactive config editing** | CLI tools don't need interactive modes. YAML editing is the right approach for the audience. |

---

## 10. Single Authoritative v1 Product-Truth Artifact

The repo currently has overlapping readiness/status narratives across:
- `docs/implementation-status.md` (detailed implementation history)
- `docs/planning/pre-1-0-readiness-plan.md` (remaining work)
- `docs/planning/milestone-plan.md` (milestone history)
- `README.md § Current Status` (user-facing summary)

**Recommendation:** For a v1, `README.md § Current Status` should be the sole user-facing truth, and `docs/implementation-status.md` should be the sole internal truth. The other artifacts are internal planning aids and should not be reachable from user-facing navigation without clear "internal development" framing.

---

## Methodology

This review was conducted by:

1. Reading all primary docs: README, CONTRIBUTING, CHANGELOG, AGENTS.md, PRD, implementation-status, planning-index, pre-1-0-readiness-plan, and the steward-cli SKILL.md
2. Running all major CLI commands against the live repo: `orient --signals`, `status --coverage`, `check`, `check --output json`, `explain`, `explain path`, `config doctor`, `config suggest`, `search`, `outline`, `md --help`, `md edit --help`, `version`, `init --help`, `refactor --help`
3. Reviewing the configuration model: `.steward/config.yaml`, `.steward/policy.yaml`, profile definitions in code
4. Surveying all test projects: 717 tests across 80+ test files, 5 fixture repos, contract tests, consistency tests, snapshot tests
5. Reviewing CI workflows: ci.yml, release.yml, pr-release-intent.yml, release-labels.yml
6. Assessing the docs architecture: 80+ files under docs/, 100% internal, zero user-facing
7. Checking error handling: Program.cs, CommandSetup.cs, ExitCodes.cs
8. Reviewing init scaffolding: ProfileDefaults.cs, InitCommand.cs
9. Cross-referencing claims in README against implementation, tests, and CLI output
10. Reading previous reviews (fresh-eyes-onboarding-audit, cli-expectation-fidelity-assessment) for context

All findings are grounded in evidence from the repository as of 2026-04-19.
