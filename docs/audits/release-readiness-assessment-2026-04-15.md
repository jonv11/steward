# Release-Readiness Assessment — End-User Product Review

- **Date:** 2026-04-15
- **Reviewer stance:** External end-user and product reviewer — skeptical, unbiased, demand-driven
- **Scope:** Whether the current CLI (v0.10.0) is ready for a first meaningful public release (0.x or otherwise)
- **Baseline tested:** `steward 0.10.0` on .NET 10.0.6, Windows

---

> Historical scope note (2026-04-16): This assessment remains the durable end-user review that fed the active blocker list, but current blocker status now lives in [pre-release-blockers.md](../planning/pre-release-blockers.md) and the latest release-gate verdict lives in [release-governance-conformance-review-2026-04-16.md](release-governance-conformance-review-2026-04-16.md).

## 1. Executive Verdict

**Conditionally ready for a first meaningful 0.x public release — but only if a short list of critical items is addressed first.**

Steward has a real core loop, genuine differentiators, and a coherent command model. It is not vaporware and it is not just a linter with extra commands bolted on. The Markdown structural operations, policy-driven validation, and deterministic maintenance are concrete, working capabilities that no single existing tool combines.

However, the product currently over-promises in several visible places relative to what it actually delivers, and a handful of commands are too thin to justify their presence at launch. A first-impression user who follows the README would encounter promises the tool cannot yet fulfill conveniently, which would erode trust and make the tool feel unfinished rather than early-stage.

The gap between "technically implemented" and "product-ready" is small enough to close before a first release — but it must be closed, not shipped around.

---

## 2. Product Reality Check

### What it actually is today

Steward is a CLI that validates repository structure and Markdown documents against declarative YAML policy, provides Markdown-aware structural queries and edits, generates and maintains a structure document, and offers basic orientation, search, and reference-tracking surfaces.

It is a **documentation-governance tool with Markdown superpowers**, packaged as a broader "repository stewardship" product. The strongest value is concentrated in:

- `check` — policy-driven validation with 13 rules, scoped evaluation, and deterministic exit codes
- `md edit` / `md query` / `md outline` — structural Markdown operations that are genuinely better than raw text tools
- `maintain` — idempotent structure-document generation
- `orient` / `status` — repository-at-a-glance with artifact role classification

### What it genuinely does well

1. **Policy-driven validation is real and useful.** `steward check` runs 13 rules against declared policy, produces clean diagnostics, supports JSON output, and uses correct exit codes. A user can add this to CI today and get genuine value.
2. **Markdown structural editing is a genuine differentiator.** `md edit ensure-section`, `fm-set`, `md query` — these solve real problems that `sed`/`awk`/manual editing solve poorly. The preview-before-apply model is well-implemented.
3. **Deterministic maintenance works.** `maintain --apply` keeps STRUCTURE.md in sync. The preview/diff workflow is correct.
4. **Configuration model is well-designed.** The config/policy/path-policy separation is clean. Profiles provide sensible defaults. The `init` → `config suggest` → `config doctor` → `check` adoption flow is coherent.
5. **Dual-format output is real.** JSON output exists on the important commands and is parseable. Not all commands are complete, but the main surfaces work.
6. **The self-dogfooding is credible.** This repo uses Steward to govern itself, and `steward check` passes clean. That is a meaningful signal.

### What it claims but does not yet fully deliver

1. **"Stewardship, not just validation"** — The PRD explicitly distinguishes stewardship from linting. In practice, the "stewardship" experience is mostly check + maintain-one-artifact. The orient/status surfaces are informational but passive; they do not guide action or provide workflow continuity.
2. **"Dual-audience: humans and AI agents as first-class users"** — JSON output exists but is incomplete (`config suggest` ignores `--output json`). Agent-oriented features like `--compact` orient are new and useful but thin. The agent assessment in the repo itself identifies multiple gaps.
3. **"Works across repository archetypes"** — Five profiles exist, but in practice the only exercised archetype is `software`/`tool`. There is no evidence that `docs`, `knowledge`, or `mixed` profiles produce meaningfully different or useful experiences.
4. **"Full explainability"** — `steward explain STWD-008` produces three lines: rule name, description, and a single-sentence remediation. This is technically present but hardly "full". A user who reads "full explainability" in the README and then sees the actual output would feel oversold.
5. **13 validation rules, broad coverage** — The rules exist and work, but several (STWD-010 naming, STWD-011 index-completeness, STWD-012 freshness, STWD-013 discoverability) are recent additions. Product maturity varies across rules.

### Does it feel like a product or a foundation?

It feels like a **strong foundation with product-quality surfaces in specific areas** (check, md edit, maintain) and **prototype-quality surfaces in others** (explain, refs, config suggest, orient without compact). The command breadth is impressive for 0.10.0, but depth is uneven. A user working within the strong surfaces would be satisfied; a user exploring the full command set would encounter thin spots.

---

## 3. Promise Audit

### P1: "Repository stewardship — not just validation"

- **Source:** README headline, PRD §1, PRD §4 Goal 1
- **User expectation:** A tool that actively helps maintain a repository over time, not just flags violations. Guidance, maintenance, workflow support.
- **Fulfillment:** Partially fulfilled
- **Convenient enough?** For check + maintain, yes. For the broader stewardship promise, no — orient/status are passive readouts, not actionable guidance.
- **Optimal for first release?** Acceptable if the promise language is tightened. Currently over-claims.
- **Rationale:** The maintain command genuinely crosses the line from "checker" to "maintainer" for structure documents. But the broader stewardship promise — workflow guidance, completeness tracking, "what should I do next?" — is aspirational. The `status` command shows artifact presence but does not surface actionable next steps.

### P2: "For humans and AI agents"

- **Source:** README subtitle, PRD §3, PRD §4 Goal 2
- **User expectation:** Every command works well in both human-readable and machine-readable modes. Agent workflows are directly supported.
- **Fulfillment:** Partially fulfilled
- **Convenient enough?** Core commands (check, orient, status) support both modes. Peripheral commands (config suggest, explain) do not.
- **Optimal for first release?** Close. The JSON gaps on secondary commands are small but visible.
- **Rationale:** The dual-audience promise is genuine — JSON output, exit codes, and `--compact` orient show real investment. But `config suggest` without JSON support is a direct agent-usefulness gap, and `explain` output is too thin for either audience.

### P3: "Configurable across repository archetypes"

- **Source:** README configuration section, PRD §6, init profiles
- **User expectation:** I can use this on a docs-only repo, a knowledge base, or a software project and it adapts meaningfully.
- **Fulfillment:** Weakly fulfilled
- **Convenient enough?** Only `software` is exercised. Other profiles likely produce reasonable defaults but have no demonstrated or documented value.
- **Optimal for first release?** Acceptable for 0.x if profiles are presented as starting points rather than curated experiences. Currently presented as if all five are equally ready.
- **Rationale:** The configuration model itself is flexible, but the profiles are untested outside `software`/`tool`. A user running `steward init --profile knowledge` and then finding nothing tuned for knowledge repos would feel let down.

### P4: "Markdown-native"

- **Source:** README features, PRD §4 Goal 5, PRD §8.8
- **User expectation:** Markdown files are first-class citizens: queryable, editable, validatable at the structural level.
- **Fulfillment:** Fulfilled
- **Convenient enough?** Yes. `md query`, `md edit`, `md outline` work as advertised.
- **Optimal for first release?** Yes. This is the strongest delivered promise.
- **Rationale:** The Markdown structural engine is real, useful, and differentiated. MdPath selectors, section-level editing, frontmatter operations, and preview-before-apply all work correctly.

### P5: "Deterministic maintenance"

- **Source:** README features, PRD §8.12, maintain command
- **User expectation:** The tool auto-generates and keeps governed artifacts in sync — indexes, structure docs, registries, etc.
- **Fulfillment:** Partially fulfilled
- **Convenient enough?** For structure-document, yes. For the broader promise (indexes, registries, catalogs, glossaries), no.
- **Optimal for first release?** Acceptable if scoped honestly. The current README lists `structure-document` and `directory-index` types but the self-dogfooding only uses `structure-document`.
- **Rationale:** Maintain is the right model and the implementation is solid. But the promise of "indexes, registries, catalogs, glossaries" from the PRD is far ahead of what is exercised or documented for end users.

### P6: "Full explainability"

- **Source:** README features bullet
- **User expectation:** I can understand any rule, why it fired, what it means, and how to fix it — in meaningful detail.
- **Fulfillment:** Weakly fulfilled
- **Convenient enough?** No. `steward explain STWD-008` produces a rule name, category, severity, one-line description, and one-line remediation. That is a tooltip, not "full explainability".
- **Optimal for first release?** No. Either the explain output needs to be richer (examples, affected config, policy context) or the "full" claim needs to be dropped.
- **Rationale:** This is the clearest overpromise. The explain command exists and is correct, but "full explainability" sets an expectation of depth that the current three-line output does not meet.

### P7: "Broken link detection"

- **Source:** README features bullet
- **User expectation:** The tool finds broken internal Markdown links.
- **Fulfillment:** Fulfilled
- **Convenient enough?** Yes.
- **Optimal for first release?** Yes.
- **Rationale:** STWD-008 works, produces clear diagnostics, and is genuinely useful.

### P8: "Cross-reference analysis" (refs command)

- **Source:** README command table (`steward refs <path>`)
- **User expectation:** I can see what links to what — useful for understanding document relationships, finding orphans, planning moves.
- **Fulfillment:** Partially fulfilled
- **Convenient enough?** Marginally. The output is correct but minimal. `refs README.md` returned "(none)" for both directions on a README that is a declared start-here artifact, because it has no Markdown links in or to it. This is technically correct but suggests the command is narrower than the promise implies.
- **Optimal for first release?** Acceptable as-is for 0.x. The feature is simple, correct, and occasionally useful.
- **Rationale:** Refs is a thin feature. It shows link relationships but provides no analysis, no graph view, no orphan detection (that is STWD-013). It exists but does not yet feel like a "must-have."

### P9: "Config doctor detects valid-but-ineffective config"

- **Source:** README command table, config doctor help text
- **User expectation:** Doctor catches things that are syntactically valid but practically useless — dead entries, unmatched patterns, etc.
- **Fulfillment:** Fulfilled
- **Convenient enough?** Yes, for what it covers.
- **Optimal for first release?** Yes.
- **Rationale:** A small but valuable command. It ran cleanly on this repo and provides a real safety net for configuration mistakes that `config validate` would miss.

### P10: "Refactoring operations — move with link rewriting"

- **Source:** README command table (`steward refactor move`)
- **User expectation:** Move a file and have all Markdown links updated automatically.
- **Fulfillment:** Present (not tested in this review due to destructive nature)
- **Convenient enough?** Likely yes for the single operation it supports.
- **Optimal for first release?** Acceptable for 0.x. A single refactoring operation under a `refactor` namespace is fine if other operations are planned.
- **Rationale:** The command exists and has tests. The `refactor` namespace with only `move` inside it is slightly premature but not harmful.

---

## 4. End-User Value Assessment

### Practical usefulness

Steward delivers **real, measurable value in three specific scenarios today:**

1. **Pre-commit / CI validation gate:** `steward check` as a pipeline step catches missing artifacts, broken links, stale structure docs, naming violations, and frontmatter gaps. This is the strongest value proposition and it works well.
2. **Markdown structural operations for automation:** `md edit`, `md query`, and `md outline` are genuinely useful for scripts and AI agents that need to manipulate Markdown files safely. No mainstream competitor offers this.
3. **Structure document maintenance:** `steward maintain --apply` automates a real chore.

### Coherence of workflows

The **init → orient → check → maintain → fix** loop is coherent and feels intentional. A user can follow this path and get real results.

The **search → refs → md query** discovery path exists but is less integrated. These commands work independently but do not chain into a workflow the way check + maintain do.

### Clarity of purpose

**Moderate.** The product knows what it wants to be (a stewardship companion), but the current delivery is strongest as a **docs-governance validator with Markdown editing superpowers**. The broader stewardship framing is aspirational. A first-time user would understand what `check` does immediately; they would be less clear on why `orient` and `status` are separate commands, or what "stewardship" means concretely beyond validation.

### Distinctiveness

**The Markdown structural engine is the strongest differentiator.** No general-purpose repo tool offers `md edit ensure-section`, `md query`, or `fm-set` with preview-before-apply semantics. If the product were positioned primarily around this, the distinctiveness would be sharper.

Policy-driven validation is also distinctive — not because linting is new, but because the *declarative artifact-policy model* is unusually well-designed for repository-level governance.

### Adoption-worthiness

**Conditionally yes.** A user with a documentation-heavy repository, especially one with governance needs (required artifacts, managed structure, Markdown conventions), would find genuine value today. A user with a code-heavy repo and minimal Markdown would find less to justify adoption.

### Promise-experience alignment

**Mixed.** The strongest promises (validation, Markdown operations, maintenance) are well-aligned. The broader promises (stewardship, dual-audience, cross-archetype, full explainability) are ahead of the actual experience. This gap is the primary release-readiness concern.

---

## 5. Imperative Pre-Release Gaps

These must be addressed before a first meaningful public release.

### G1: Tighten the "full explainability" claim or deliver on it

- **Promise affected:** P6 — "Full explainability"
- **Why imperative:** The README says "Full explainability". The actual explain output is three lines per rule. A first-impression user who reads the promise and then tests it will feel actively misled.
- **User impact:** Erodes trust in the product's self-description. Makes the tool seem oversold.
- **Suggested priority:** Critical
- **Category:** Docs + product
- **Resolution:** Either (a) expand explain output to include examples, configuration context, severity rationale, and affected file patterns — making it actually "full", or (b) change the README to "Rule explainability" and drop the "full" qualifier. Option (b) is fast; option (a) is better.

### G2: `config suggest` must respect `--output json`

- **Promise affected:** P2 — dual-audience, agent-friendliness
- **Why imperative:** `config suggest` is the bootstrapping command agents and humans use to set up policy.yaml. Its output is plain text with no JSON mode. For a tool that promises dual-audience output, a bootstrapping command that ignores the agent audience is a visible gap in the first-run experience.
- **User impact:** Agents cannot consume suggestions programmatically. The init → suggest → edit flow is broken for automation.
- **Suggested priority:** Critical
- **Category:** Implementation
- **Resolution:** Honour `--output json` in `config suggest`. The analyzer already returns a typed result; it needs serialization.

### G3: Dependency on System.CommandLine beta

- **Promise affected:** Product reliability for a public release
- **Why imperative:** `System.CommandLine` is still in beta. Shipping a public release (even 0.x) with a beta dependency is a known risk for API stability and user trust. Users inspecting the dependency chain will notice.
- **User impact:** Potential breaking changes in a transitive dependency that could force API changes in Steward.
- **Suggested priority:** High
- **Category:** Dependency / release
- **Resolution:** Either pin the specific beta version explicitly and accept the risk with a documented note, or evaluate whether the beta is close enough to GA. This was already identified in the pre-1.0 readiness plan — it must not be deferred past first public release.

### G4: Cross-platform validation

- **Promise affected:** "Offline and portable" (PRD §4 Goal 8), trustworthiness for CI
- **Why imperative:** The CLI has only been tested on Windows. The stated target includes macOS and Linux. Shipping a public release without cross-platform CI is a trust gap for the primary use case (CI validation gate).
- **User impact:** Users on macOS/Linux may encounter path-handling, line-ending, or git-integration issues.
- **Suggested priority:** High
- **Category:** Workflow / release
- **Resolution:** Minimum: run `dotnet test` on Linux and macOS in CI before the first public release.

### G5: Profile quality for non-software archetypes

- **Promise affected:** P3 — "Works across repository archetypes"
- **Why imperative:** Five profiles are offered (`software`, `docs`, `mixed`, `knowledge`, `minimal`). Only `software`/`tool` is exercised. Users selecting `docs` or `knowledge` have no evidence these profiles produce useful defaults. If the profiles are empty or meaningless, offering them creates a false promise.
- **User impact:** Users outside the software archetype get a bad first impression when their profile produces generic defaults that don't reflect their repo type.
- **Suggested priority:** High
- **Category:** Product / config
- **Resolution:** Either (a) verify and document what each profile actually provides, or (b) reduce the profile set to `software` and `minimal` for the first release and re-add others when they are tested. Presenting untested profiles as ready options is worse than offering fewer, quality options.

---

## 6. Non-Blocking Improvements

These are worthwhile but can wait for subsequent 0.x releases.

| Item | Category | Rationale |
|------|----------|-----------|
| Richer `explain` output (examples, config context, affected patterns) | Product | Would strengthen the explainability story but not a release blocker if the README claim is softened |
| `config show --effective` with merged policy | Implementation | Useful for operators but not required for first release |
| Search directory-scope option (`--path`) | UX | Quality-of-life for large repos |
| `refs` graph/orphan summary | Product | Would make refs more compelling but current basic output is acceptable |
| Batch `md query` across files | Implementation | Useful for agents but not core workflow |
| `status` actionable next-steps | Product | Would strengthen the stewardship story |
| `orient` smarter compact mode (true top-N curation, not just fewer entries) | UX | Current `--compact` is better than nothing but still list-heavy |
| `check --quiet` mode for scripting | UX | Already identified in agent assessment; simple addition |
| `maintain` with more exercised maintainer types beyond `structure-document` | Product | The model is correct; usage needs to catch up |
| `GitDiffHelper` stdin protection | Implementation | Edge-case hang prevention for changed/staged scope |

---

## 7. Feature Bloat / Weak-Value Items

### 7.1 `orient` vs `status` — overlapping concern, unclear boundary

Both commands show repository context (name, type, profile, start-here entries). `orient` adds a classified file listing; `status` adds artifact presence/freshness/completeness counts. The boundary is documented in the PRD but invisible to end users. A new user will run one, get partial information, then run the other, get overlapping information with different extras. Neither feels complete on its own.

**Risk:** Feels like two half-finished commands instead of one good one.

### 7.2 `outline .` vs `orient` — overlapping file-listing surfaces

`outline .` produces a file tree. `orient` produces a classified file tree. They cover substantially similar ground. The distinction (raw tree vs role-classified tree) is meaningful in theory but creates "which command do I use?" friction for new users.

### 7.3 `outline <dir>` vs `outline <file.md>` — overloaded semantics

`outline .` shows a directory tree. `outline README.md` shows heading hierarchy. These are two fundamentally different operations sharing one command name. This is documented but still a UX surprise. `md outline <file>` exists for the same heading use case, making `outline <file.md>` a shortcut alias that adds confusion rather than clarity.

### 7.4 `refs` — correct but thin

`refs` shows inbound/outbound Markdown links for a file. It does what it says. But the output is minimal (flat lists), there's no analysis dimension, and the value over `grep -r "filename" *.md` is marginal. This command exists because it could be powerful — but it is not powerful yet.

### 7.5 `refactor` namespace with only `move`

`refactor` is a namespace containing exactly one subcommand. This is premature generalization. `steward move` would be simpler until there are multiple refactoring operations.

### 7.6 Wide command surface for the depth delivered

16 commands (counting subcommands) is a large surface for a 0.x product. Several (refs, refactor, config suggest, config doctor, outline file shortcut) are individually thin. The breadth creates an impression of comprehensiveness but the depth does not always justify the command's existence.

**Risk:** Feels like a showcase of capabilities rather than a focused tool.

---

## 8. Core Differentiators

### Already strong today

1. **Markdown structural editing with preview/apply.** `md edit ensure-section`, `set-section`, `fm-set`, `fm-merge` — no other general-purpose repo CLI offers this. This is a real, unique capability.
2. **Declarative artifact-policy model.** The config/policy/path-policy YAML model is well-designed and genuinely enables repository-specific governance without hardcoded conventions.
3. **Idempotent structure-document maintenance.** `maintain --apply` eliminates a real manual chore. Preview + diff + apply is the right workflow.
4. **13 cohesive validation rules with correct exit codes and JSON output.** The check command is solid and CI-ready.

### Still too weak to carry a release

1. **"Stewardship" as a product identity.** The word "stewardship" implies active guidance, workflow continuity, and lifecycle management. The current reality is closer to "governance validation + Markdown tools + structure generation". The identity is aspirational but the product needs to earn it through richer status/orient actionability before it can carry the brand.
2. **"Cross-archetype" support.** Only one archetype is tested. This cannot be a headline claim until at least 2-3 profiles are demonstrated.
3. **"Full explainability."** Three-line explain output cannot carry this label.

---

## 9. Minimum Meaningful Release Bar

For a first public 0.x release, the minimum bar is:

| Criterion | Required state |
|-----------|---------------|
| Core loop works end-to-end | init → check → maintain → fix delivers real value ✅ |
| Promises match reality | README claims are honest about current state ❌ (three key overpromises) |
| Primary value prop is clear | A user knows why to use this tool within 2 minutes ✅ (check + md edit) |
| Commands that exist actually work | All shipped commands are functional and non-trivial ⚠️ (refs and explain are thin) |
| Agent audience is served | JSON output on key commands, stable exit codes ⚠️ (config suggest gap) |
| Cross-platform confidence | At least basic CI on macOS/Linux ❌ |
| Dependency posture is defensible | No known-unstable transitive deps ❌ (System.CommandLine beta) |
| Untested features are not presented as ready | Profiles not overpromised ❌ |

**Current state vs bar:** 4 of 8 criteria are met. 2 are close. 2 are clearly unmet.

The gaps are all closable in a short cycle. This is not a "go back to the drawing board" situation — it is a "tighten claims, fix the JSON gap, validate cross-platform, and acknowledge dependency risk" situation.

---

## 10. Final Release Recommendation

**Release only after the critical items are completed.**

Steward v0.10.0 is close to a meaningful first public release. It has genuine, differentiated value in its core loop (policy validation, Markdown structural editing, structure-document maintenance) and a well-designed configuration model. The implementation quality is solid, the test coverage is strong, and the self-dogfooding is credible.

However, the product currently overpromises in its public-facing docs relative to what it actually delivers, and a few visible functional gaps would undermine first impressions. Specifically:

1. **The README must be honest.** "Full explainability" must become "Rule explainability" (or the explain output must substantively improve). Profile descriptions should note which are battle-tested. The features list should accurately describe current scope rather than aspirational scope.

2. **`config suggest --output json` must work.** This is a trivial implementation gap that visibly violates the dual-audience promise exactly at the point where agents would first interact with the tool.

3. **Cross-platform CI must exist before public release.** Shipping a CLI tool that claims to work in CI without ever running on Linux is an unforced trust error.

4. **The dependency risk must be acknowledged.** Either pin System.CommandLine explicitly and document the risk, or wait for GA. A 0.x release can tolerate a beta dependency if the risk is openly stated.

These four items are achievable in a single focused sprint. The payoff is high: with them addressed, Steward has a credible, differentiated, and honest first-release story that is stronger for not overpromising.

**Do not delay indefinitely.** The core product is good enough to ship once these items are addressed. Perfectionism is a greater risk than early release at this point. But sloppy self-description and visible functional gaps are an even greater risk than shipping a week later with them fixed.

---

## 11. Artifact Follow-Through

| Artifact | Path | Rationale |
|----------|------|-----------|
| Primary assessment | `docs/audits/release-readiness-assessment-2026-04-15.md` (this file) | Follows existing audit naming convention and placement in `docs/audits/`. Serves as the durable record of the release-readiness decision for maintainer review. |
| Pre-release blocking items | `docs/planning/pre-release-blockers.md` | Captures the specific critical items as a concrete, trackable checklist. Placed in `docs/planning/` alongside the existing pre-1.0 readiness plan and milestone plan. Cross-referenced from this assessment. |

**How to use these artifacts:**

1. Maintainers should review this assessment and either accept or dispute the verdict.
2. If accepted, the pre-release blockers list becomes the working checklist for the release-gating sprint.
3. Once all blockers are resolved, re-run `steward check`, re-test the first-run experience, and update this assessment with a "Re-assessed" status.
4. This assessment should be referenced in any future release-authorization ADR or decision.

---

## Appendix

### Top 3 strongest reasons to release now

1. **The core loop works and delivers real value.** `check` + `maintain` + `md edit` is a coherent, useful workflow that no single competitor offers.
2. **The Markdown structural engine is a genuine differentiator** with no close equivalent in the ecosystem.
3. **The configuration model is well-designed and extensible** — early adopters can meaningfully configure governance for their repos today.

### Top 3 strongest reasons not to release now

1. **The README overpromises** relative to what the product actually delivers ("full explainability", five equally-ready profiles, broad stewardship beyond check+maintain). A first impression built on overpromise is worse than a delayed release.
2. **No cross-platform CI exists**, making the CI-gate use case — the primary value proposition — unvalidated on the two most common CI platforms (Linux, macOS).
3. **`config suggest` ignores `--output json`**, which breaks the dual-audience promise at the bootstrapping step where agents first interact with the tool.

### Single most important missing capability

**Cross-platform CI.** Without it, the primary use case (CI validation gate) is a promise, not a demonstrated capability.

### Single most overpromised / under-fulfilled promise

**"Full explainability."** The explain command produces a rule name, category, severity, one-line description, and one-line remediation. That is a tooltip, not full explainability. The word "full" sets an expectation the product does not meet.
