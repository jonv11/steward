# RFC-007 Governance Enhancements — Actionable Backlog

- **Source:** [RFC-007 Maintainer Governance and Stewardship Enhancements](../decisions/rfcs/RFC-007-maintainer-governance-and-stewardship-enhancements-draft.md) (Draft)
- **Supporting evidence:** [Maintainer Review — 2026-04-14](../audits/maintainer-review.md)
- **Status:** Accepted — RFC-007 accepted 2026-04-15, items scheduled in post-v1.0.0 milestones
- **Created:** 2026-04-15

---

## 1. Scope Statement

This backlog captures the actionable work derived from RFC-007 and the maintainer review. It covers enhancements to policy expressiveness, governance inspection, maintenance capabilities, discoverability analysis, workflow operations, and onboarding for mature repositories.

All items here are **proposed future work**. They are not part of the accepted v1.0.0 scope defined by PRD-0001, TRACE-0001, and the v0.1.0–v1.0.0 milestone plan. Implementation should not begin until RFC-007 is formally accepted and specific items are scheduled into a post-v1.0.0 milestone.

### Assumptions

- The current v1.0.0 product surface (14 commands, 9 rules, 5 maintainers) is the baseline.
- Enhancements must remain backward-compatible: existing repos without new config keys continue to work.
- New advisory surfaces default to low-noise behavior.
- All capabilities must remain deterministic, preview-first where relevant, and compatible with existing output contracts.
- Schema additions are non-breaking when omitted.

### Boundaries

- No external plugin/extensibility systems.
- No IDE or GUI integration.
- No LLM-driven content generation.
- No Git hosting platform API integration.
- Implementation-level architecture (ADRs) will be created as needed when items are scheduled; this backlog captures product-level decisions and scope only.

---

## 2. Phased Breakdown

### Phase 1 — Policy Expressiveness and Validation Feedback

Fills the most painful day-to-day maintainer gaps with targeted, lower-complexity enhancements to the existing policy and validation engine.

**Rationale:** These items address governance declarations that maintainers already want to express but currently cannot. They extend existing subsystems (validation rules, policy schema, explain output) rather than introducing new commands or engines.

### Phase 2 — Governance Inspection and Explainability

Adds the ability to inspect, explain, and diagnose governance configuration and coverage — making Steward useful for understanding governance, not only enforcing it.

**Rationale:** These capabilities answer "what applies here and why?" and "is my governance effective?" — questions that currently require manual file inspection. They build on Phase 1's policy expressiveness improvements.

### Phase 3 — Maintenance Evolution

Extends the maintenance engine with higher-value capabilities: directory-index generation, dependency modeling, richer artifact classification, and role-linked behavioral defaults.

**Rationale:** The directory-index generator is the single highest-value item from the maintainer review. Dependency modeling enables smarter refresh ordering. These items have higher implementation complexity and benefit from Phase 1–2 foundations.

### Phase 4 — Discoverability and Impact Analysis

Adds proactive detection of discoverability gaps, change-impact signaling, and staged-scope completeness analysis.

**Rationale:** These are genuinely new analytical capabilities. They require the governance model to be more fully expressed (Phases 1–3) before they can produce meaningful results.

### Phase 5 — Workflow Operations and Onboarding

Adds safe move/rename workflows, reference graph queries, and bootstrap-by-analysis for mature repositories.

**Rationale:** These are the highest-complexity items. Move/rename requires reference graph awareness (Phase 4). Bootstrap-by-analysis benefits from all prior governance model improvements.

---

## 3. Actionable Items

### Phase 1 — Policy Expressiveness and Validation Feedback

#### G7-01: Per-path rule suppression

- **Summary:** Allow disabling specific validation rules for files matching a path pattern, replacing the current global-only `disabled_rules` mechanism.
- **Rationale:** Maintainers need to suppress rules (e.g., STWD-004 section-size) for specific files like machine-navigable registries or long reference documents without disabling them globally.
- **User/maintainer value:** Eliminates "suppress globally because one file needs it" — preserves rule coverage where it matters.
- **Affected surfaces:** policy.yaml schema (`validation.path_overrides`), validation engine (scope resolution), config validate
- **Dependencies:** None — extends existing validation engine.
- **Phase:** 1
- **Requires follow-up ADR:** No
- **Acceptance criteria:** A path pattern in `validation.path_overrides` with `disabled_rules: [STWD-004]` causes STWD-004 to be skipped for matching files. `config validate` rejects invalid rule IDs in overrides.

#### G7-02: Scoped frontmatter requirements per path pattern

- **Summary:** Allow declaring required frontmatter fields (and optionally allowed values) scoped by file path pattern, extending STWD-003 from global to per-path.
- **Rationale:** Decision documents need `status` frontmatter; audit docs need `date`. These requirements are inappropriate for READMEs, structure docs, or planning artifacts.
- **User/maintainer value:** Enables lifecycle enforcement for decision documents without imposing frontmatter on every Markdown file.
- **Affected surfaces:** policy.yaml schema (`validation.frontmatter_requirements[]`), STWD-003 rule (path-aware evaluation), config validate
- **Dependencies:** None — extends existing STWD-003 rule.
- **Phase:** 1
- **Requires follow-up ADR:** No
- **Acceptance criteria:** Files matching a declared pattern are validated against scoped frontmatter requirements. Files outside all patterns fall back to the global `required_frontmatter_fields`. `config validate` rejects invalid patterns.

#### G7-03: Naming convention enforcement in path-policy

- **Summary:** Add a `naming` category to path-policy rulesets that can enforce filename patterns (regex) for files in specific directories.
- **Rationale:** Decision directories (`rfcs/`, `adrs/`) follow conventions like `RFC-NNN-title.md`. A file that breaks this convention passes `check` silently today.
- **User/maintainer value:** Catches naming drift before it causes navigational confusion or breaks indexing conventions.
- **Affected surfaces:** path-policy.yaml schema (new `naming` category with `must_match` regex), path-policy engine, new validation rule (STWD-010 or similar), explain
- **Dependencies:** None — extends existing path-policy engine.
- **Phase:** 1
- **Requires follow-up ADR:** Possibly — may need design decision on regex vs glob syntax for `must_match`.
- **Acceptance criteria:** A file under `docs/decisions/rfcs/` that does not match the declared naming pattern produces a Warning diagnostic. Pattern syntax is documented and validated by `config validate`.

#### G7-04: Post-fix and maintain diff output

- **Summary:** `steward check --fix` and `steward maintain --apply` should report what changed, not just "Changes applied."
- **Rationale:** The silent apply forces maintainers to run `git diff` after every fix/maintain cycle. This breaks the self-contained stewardship loop.
- **User/maintainer value:** Confirms changes were sensible before committing; eliminates the need for external `git diff` in the routine maintenance workflow.
- **Affected surfaces:** check command (fix output), maintain command (apply output), output formatters (diff rendering)
- **Dependencies:** None.
- **Phase:** 1
- **Requires follow-up ADR:** No
- **Acceptance criteria:** `check --fix` and `maintain --apply` print a summary of changed files with at minimum added/removed line counts. `--diff` flag (or `--verbosity verbose`) shows unified diff. JSON output includes a structured changes array.

#### G7-05: Rule scope transparency in explain

- **Summary:** `steward explain <rule-id>` in verbose mode should report how many files the rule actually evaluated — "files checked: N".
- **Rationale:** Maintainers cannot confirm whether a rule was applied to the expected file set without verbose debug output. This is a confidence gap.
- **User/maintainer value:** Confirms rule coverage without guessing; especially useful for STWD-008 (broken links) where resolution semantics matter.
- **Affected surfaces:** explain command (verbose output), validation engine (file-count metadata per rule)
- **Dependencies:** None.
- **Phase:** 1
- **Requires follow-up ADR:** No
- **Acceptance criteria:** `steward explain STWD-008 --verbosity verbose` includes a "files evaluated: N" count reflecting the last run (or current scope).

---

### Phase 2 — Governance Inspection and Explainability

#### G7-06: Effective policy explanation for a path

- **Summary:** New `steward explain path <path>` command (or subcommand) that shows the effective governance applying to a specific file or directory: matched artifacts, path-policy rules, precedence, effective role, frontmatter requirements, maintenance participation, suppressions, and source locations.
- **Rationale:** Maintainers currently must manually merge multiple config/policy files and resolve precedence to understand what applies to a path. This is error-prone and opaque.
- **User/maintainer value:** Core explainability — answers "what applies here, and why?" in one command.
- **Affected surfaces:** new subcommand under `explain` (or new `policy match` command), configuration/policy engine (effective-policy resolution), output formatters
- **Dependencies:** G7-01 (per-path suppressions must be reflected in effective-policy output), G7-02 (scoped frontmatter must appear)
- **Phase:** 2
- **Requires follow-up ADR:** Yes — command surface design (where this lives in the command hierarchy per RFC-001).
- **Acceptance criteria:** Running the command on a governed file shows all applicable rules, their sources, effective frontmatter requirements, role classification, and any overrides/suppressions — sufficient for a maintainer to understand governance without reading config files directly.

#### G7-07: Configuration doctor for ineffective governance

- **Summary:** New `steward config doctor` command (or extension of `config validate`) that detects valid but ineffective configuration: rulesets matching no paths, fully shadowed entries, redundant exclusions, dead `start_here` paths, artifact declarations that never participate in any surface, maintenance sources matching nothing.
- **Rationale:** Valid config can still be misleading. Accumulated YAML becomes a liability if stale entries are invisible.
- **User/maintainer value:** Builds trust in config as a living governance contract rather than a historical accumulation.
- **Affected surfaces:** new command or extension of `config validate`, configuration engine (dead-config detection), policy engine (shadowed-rule detection)
- **Dependencies:** G7-01 (path overrides are part of the config surface to doctor)
- **Phase:** 2
- **Requires follow-up ADR:** Yes — scope of "ineffective" detection, diagnostic severity model for config-doctor findings.
- **Acceptance criteria:** At least three classes of ineffective configuration are detected: rulesets matching no files, dead `start_here` entries, and artifact declarations matching no existing file. Output includes remediation guidance.

#### G7-08: Index-completeness validation rule

- **Summary:** New validation rule (STWD-010 or similar) that checks whether all `.md` files in a declared source directory are referenced (via Markdown link) in a declared index artifact.
- **Rationale:** The planning-index.md Decisions table is manually maintained and already out of date (ADR-010 missing). This class of drift is silent and recurring.
- **User/maintainer value:** Catches index drift before it becomes a discoverability gap. Critical for decision documentation governance.
- **Affected surfaces:** policy.yaml schema (new `index_of` property on artifact declarations), new validation rule, explain, check output
- **Dependencies:** None — but complements G7-14 (directory-index generator) by validating what the generator would maintain.
- **Phase:** 2
- **Requires follow-up ADR:** Possibly — schema design for `index_of` declaration.
- **Acceptance criteria:** A `.md` file under a declared `index_of` directory that is not linked from the index artifact produces a Warning diagnostic. The rule correctly resolves relative links.

#### G7-09: State-document freshness signaling

- **Summary:** Policy-level freshness declarations for state documents: `freshness.max_age_days` on artifact declarations. The corresponding rule produces Info or Warning when the file hasn't been modified within the window (via git mtime or declared frontmatter field).
- **Rationale:** `docs/implementation-status.md` is 9 months stale (last updated 2025-07-18). Steward has no signal for this. Stale state documents actively mislead contributors.
- **User/maintainer value:** Surfaces staleness of human-maintained state documents — not generated artifacts (already handled by STWD-007), but documents whose value depends on recency.
- **Affected surfaces:** policy.yaml schema (`freshness` property on artifact declarations), new validation rule or orient signal, status command (last-modified display)
- **Dependencies:** Git integration for file modification date.
- **Phase:** 2
- **Requires follow-up ADR:** Possibly — freshness source (git vs frontmatter vs filesystem) and severity model.
- **Acceptance criteria:** A state document declared with `freshness.max_age_days: 60` that hasn't been modified in >60 days produces an advisory diagnostic. `steward status` shows last-modified dates for state documents.

---

### Phase 3 — Maintenance Evolution

#### G7-10: Directory-index generator for maintained sections

- **Summary:** New `directory-index` generator type for the maintenance engine that scans a directory, extracts title and description from each file (via heading or frontmatter), and generates a Markdown table in a managed region.
- **Rationale:** This is the single highest-value item from the maintainer review. The planning-index.md Decisions table is manually maintained and already drifted (ADR-010 missing). This problem recurs every time a decision document is added.
- **User/maintainer value:** Converts a recurring manual maintenance chore into a deterministic `maintain --apply` operation. Eliminates the most common drift mode for this repo's planning documentation.
- **Affected surfaces:** maintenance engine (new generator type), policy.yaml schema (`maintenance.artifacts[].generator.type: directory-index`), maintain command, STWD-007 (stale detection for directory-index artifacts)
- **Dependencies:** G7-08 (validates what this generator produces); Markdown structural engine (heading/frontmatter extraction from source files).
- **Phase:** 3
- **Requires follow-up ADR:** Yes — generator configuration schema, content extraction strategy (heading[1] vs frontmatter fields), table format options.
- **Acceptance criteria:** `steward maintain --apply` generates a correct Markdown table from files in declared source directories. The table is idempotent on re-run. Adding a new file to a source directory and re-running maintain updates the table. `steward check` detects staleness when the table is out of date.

#### G7-11: Maintenance dependency modeling

- **Summary:** Support explicit dependency declarations between maintained artifacts and their source domains in policy, enabling ordered maintenance and incremental invalidation.
- **Rationale:** Planning-index depends on RFC/ADR directories; structure doc depends on file tree. Without declared dependencies, maintenance order is undefined and impact is opaque.
- **User/maintainer value:** Enables smarter refresh ordering, incremental invalidation, and clearer impact reporting during maintenance cycles.
- **Affected surfaces:** policy.yaml schema (`maintenance.artifacts[].depends_on`), maintenance engine (ordering, invalidation), maintain command (dependency-aware planning output)
- **Dependencies:** G7-10 (directory-index generator is a primary consumer of dependency modeling).
- **Phase:** 3
- **Requires follow-up ADR:** Yes — dependency resolution semantics, cycle detection, invalidation model.
- **Acceptance criteria:** Maintenance artifacts with declared dependencies are processed in dependency order. A change to a source directory correctly invalidates dependent maintained artifacts. Circular dependencies are detected and reported as config errors.

#### G7-12: Three-level artifact classification

- **Summary:** Extend artifact importance from binary (`required: true/false`) to three levels: `required`, `recommended`, `optional` — with corresponding diagnostic severities (Error, Warning, Info).
- **Rationale:** The current binary model forces maintainers to either make artifacts `required: true` (too strict, blocks CI) or `required: false` (no signal at all). A middle tier allows "this is important but not blocking."
- **User/maintainer value:** More granular governance expression. Recommended artifacts surface warnings without blocking commits.
- **Affected surfaces:** policy.yaml schema (new `importance` field or `required: required|recommended|optional`), STWD-001 (severity varies by classification), check output, status output
- **Dependencies:** None directly, but interacts with G7-13 (role-linked defaults may set default importance).
- **Phase:** 3
- **Requires follow-up ADR:** Yes — schema design, interaction with existing `required: bool`, backward compatibility.
- **Acceptance criteria:** A `recommended` artifact that is missing produces a Warning (not Error). An `optional` artifact that is missing produces Info (or nothing). Existing `required: true` continues to produce Error. `config validate` accepts the new classification values.

#### G7-13: Role-linked behavioral defaults

- **Summary:** Artifact roles (`generated`, `state-document`, `requirements`, `audit`, etc.) drive default validation and maintenance behavior, not just display classification.
- **Rationale:** Roles are currently taxonomy only. A `generated` artifact should trigger warnings if manually edited. A `state-document` should participate in freshness checks. A `requirements` artifact should get visual prominence.
- **User/maintainer value:** Makes roles meaningful beyond display — reduces per-artifact configuration by encoding sensible defaults into roles.
- **Affected surfaces:** policy model (role → behavior mapping), validation engine (role-aware rule application), orient (role-specific display), status (role-specific state display)
- **Dependencies:** G7-09 (freshness for state-document role), G7-12 (importance defaults per role).
- **Phase:** 3
- **Requires follow-up ADR:** Yes — which roles get which defaults, override mechanism, backward compatibility with free-form role strings.
- **Acceptance criteria:** At minimum: `generated` role triggers STWD-007 staleness detection without explicit maintenance declaration; `state-document` role participates in freshness checks; role-linked defaults are overridable per artifact.

---

### Phase 4 — Discoverability and Impact Analysis

#### G7-14: Orphaned-but-valid document detection

- **Summary:** Advisory detection of documents that are structurally valid but effectively undiscoverable: not linked from `start_here`, not referenced from declared indexes, not surfaced by orientation, and not intentionally marked as standalone.
- **Rationale:** Governance can be nominally correct while navigation and discovery remain weak. Valid documents can exist in governed directories without being reachable from any navigation surface.
- **User/maintainer value:** Surfaces discoverability gaps before they become long-lived maintenance problems.
- **Affected surfaces:** new diagnostic (advisory) or report command, policy.yaml schema (optional `discoverability.intentionally_unlinked` suppression), check or status output
- **Dependencies:** G7-08 (index completeness provides related but distinct coverage), STWD-008 (link resolution infrastructure).
- **Phase:** 4
- **Requires follow-up ADR:** Yes — definition of "discoverable", suppression model, advisory vs diagnostic.
- **Acceptance criteria:** A Markdown file under a governed directory that is not linked from any `start_here` entry, declared index, or orientation hub is surfaced as an advisory finding. Files with `discoverability.intentionally_unlinked: true` are suppressed.

#### G7-15: Change-impact output in check

- **Summary:** `steward check` reports likely downstream stewardship impact for changed files: which maintained artifacts, indexes, or state documents may need refresh as a consequence.
- **Rationale:** Maintainers currently discover impact obligations only after seeing stale-artifact warnings on a subsequent run. Proactive impact signaling closes the feedback loop within a single check.
- **User/maintainer value:** Actionable guidance on what else should be updated alongside the current change.
- **Affected surfaces:** check command (impact section in output), maintenance engine (dependency-based impact inference), output formatters
- **Dependencies:** G7-11 (maintenance dependency modeling provides the relationship graph for impact inference).
- **Phase:** 4
- **Requires follow-up ADR:** Possibly — impact granularity, severity model, interaction with staged-scope checks.
- **Acceptance criteria:** Modifying a file under a declared maintenance source directory causes `steward check` to surface an impact signal naming the affected maintained artifact(s). Impact signals are advisory (Info), not blocking.

#### G7-16: Governance coverage reporting

- **Summary:** Repository-level view of where governance is present, thin, inconsistent, or absent: unclassified directories, docs unreachable from orientation, Markdown-heavy areas with no frontmatter expectations, absent maintained artifact types.
- **Rationale:** Maintainers have no maturity view. They can see pass/fail per rule but cannot see which important repo areas remain outside the stewardship surface.
- **User/maintainer value:** Maturity and completeness surface for maintainers; helps prioritize governance expansion.
- **Affected surfaces:** new command (e.g., `steward status --coverage`) or report, policy/config analysis engine
- **Dependencies:** G7-02 (scoped frontmatter informs "areas with no frontmatter expectations"), G7-06 (effective policy is the foundation for coverage analysis).
- **Phase:** 4
- **Requires follow-up ADR:** Yes — coverage model, what "governed" means per area, report structure.
- **Acceptance criteria:** The report identifies at least: directories with no artifact declarations, Markdown directories with no frontmatter requirements, and important files unreachable from orientation entry points.

#### G7-17: Staged-scope completeness checks

- **Summary:** When validating with `--scope staged`, Steward reports whether the staged state is internally coherent: unstaged maintenance refreshes implied by staged source changes, staged references to absent files, unresolved governed references outside the staged set.
- **Rationale:** Current staged validation checks rules against staged files but doesn't assess commit completeness — whether the staged set is a coherent unit of stewardship work.
- **User/maintainer value:** Prevents "half-committed" stewardship changes where source files are staged but their dependent maintained artifacts are not.
- **Affected surfaces:** check command (staged-completeness diagnostics), scope resolver (cross-referencing staged vs unstaged), maintenance engine (staged-state invalidation detection)
- **Dependencies:** G7-11 (dependency modeling for staged-state cross-referencing), G7-15 (change-impact as the foundation for completeness inference).
- **Phase:** 4
- **Requires follow-up ADR:** Yes — staged completeness model, false-positive management, severity model.
- **Acceptance criteria:** Staging a source file without staging the dependent maintained artifact produces an advisory diagnostic about incomplete staging. Staging both produces no diagnostic.

---

### Phase 5 — Workflow Operations and Onboarding

#### G7-18: Reference graph queries

- **Summary:** `steward refs <path>` command that exposes inbound and outbound Markdown references for a governed file: what this file links to, and what links to this file.
- **Rationale:** Search answers "where does text match?" but not "what points to what?" Relational reference inspection is needed for safe refactoring, impact analysis, and orphan detection.
- **User/maintainer value:** Enables informed decision-making before moves/renames; supports impact analysis and orphan detection.
- **Affected surfaces:** new `refs` command, reference graph engine (Markdown link extraction and indexing), output formatters
- **Dependencies:** STWD-008 (link resolution infrastructure is reusable), G7-14 (orphan detection consumes reference graph).
- **Phase:** 5
- **Requires follow-up ADR:** Yes — command design (per RFC-001), reference scope (Markdown only vs broader), graph storage/caching model.
- **Acceptance criteria:** `steward refs docs/planning-index.md` shows all files linked from and all files linking to that document. `--output json` produces a stable schema. `--to` and `--from` filters work correctly.

#### G7-19: Safe move/rename for governed artifacts

- **Summary:** Preview-first `steward refactor move <old> <new>` command that proposes deterministic updates to relative Markdown links, governed indexes, policy references, and optionally frontmatter self-references.
- **Rationale:** Moving or renaming governed documents is currently manual and error-prone. It creates drift in links, indexes, and policy declarations.
- **User/maintainer value:** Shifts Steward from drift detection after refactors toward safe stewardship during refactors.
- **Affected surfaces:** new `refactor move` command, reference graph (G7-18), link-rewriting engine, policy reference updater, output formatters (preview diff)
- **Dependencies:** G7-18 (reference graph queries provide the relationship data needed for safe moves).
- **Phase:** 5
- **Requires follow-up ADR:** Yes — command design, scope of automatic updates (Markdown links only vs policy refs), preview/apply model, multi-file atomic previews.
- **Acceptance criteria:** `steward refactor move old.md new.md --preview` shows all files that would be updated and the proposed diffs. `--apply` executes the move and updates references. No governed references are silently broken.

#### G7-20: Bootstrap-by-analysis for mature repositories

- **Summary:** Enhanced `steward init --analyze` or `steward config suggest` that scans an existing repository and proposes initial governance configuration: likely `start_here` paths, artifact roles, exclusion patterns, maintained artifact opportunities.
- **Rationale:** Onboarding a mature repository into Steward currently requires high-effort manual authoring of `.steward/` files. Analysis-driven suggestions reduce setup friction.
- **User/maintainer value:** Lowers the barrier to adopting Steward on existing repositories.
- **Affected surfaces:** init command (extended `--analyze` mode), new `config suggest` command, heuristic analysis engine, output formatters (suggestion display)
- **Dependencies:** G7-06 (effective policy explanation surfaces what the suggestions would produce), G7-16 (governance coverage informs where suggestions focus).
- **Phase:** 5
- **Requires follow-up ADR:** Yes — heuristic model, suggestion quality threshold, noise management, interaction with existing config.
- **Acceptance criteria:** Running on a mature repository with no `.steward/` produces reviewable suggestions for `start_here`, artifact declarations, and exclusion patterns. Suggestions are preview-only (never auto-applied). Running on an already-configured repo suggests additions without overwriting existing config.

---

## 4. Cross-Cutting Concerns

### Command surface changes

| Item | Surface | Type |
|------|---------|------|
| G7-01 | policy.yaml | Schema addition |
| G7-02 | policy.yaml | Schema addition |
| G7-03 | path-policy.yaml | Schema addition + new rule |
| G7-04 | check, maintain | Output enhancement |
| G7-05 | explain | Output enhancement |
| G7-06 | explain path / policy match | New subcommand |
| G7-07 | config doctor | New subcommand |
| G7-08 | check | New rule (STWD-010+) |
| G7-09 | check, status, orient | Schema addition + new rule/signal |
| G7-10 | maintain | New generator type |
| G7-11 | maintain | Engine enhancement |
| G7-12 | policy.yaml, check, status | Schema change |
| G7-13 | policy model, check, orient, status | Behavior change |
| G7-14 | check or status | New diagnostic/report |
| G7-15 | check | Output enhancement |
| G7-16 | status | New report surface |
| G7-17 | check (staged) | New diagnostics |
| G7-18 | refs | New command |
| G7-19 | refactor move | New command |
| G7-20 | init, config suggest | Command extension + new subcommand |

### Items requiring follow-up ADRs before implementation

| Item | ADR topic |
|------|-----------|
| G7-06 | Command surface design for path explanation |
| G7-07 | Config doctor scope and diagnostic model |
| G7-10 | Directory-index generator schema and extraction strategy |
| G7-11 | Dependency resolution semantics |
| G7-12 | Three-level classification schema and backward compatibility |
| G7-13 | Role-to-behavior mapping design |
| G7-14 | Discoverability definition and suppression model |
| G7-16 | Coverage model and report structure |
| G7-17 | Staged completeness model |
| G7-18 | Reference graph command and storage design |
| G7-19 | Move/rename scope and atomic preview model |
| G7-20 | Heuristic model and suggestion quality |

### Test and documentation impact

Every item requires:
- Unit tests for new rules/engines/generators
- Integration tests for command-level behavior
- Explain text for new rules
- README updates for new commands/options
- Policy reference documentation for new schema fields

---

## 5. Priority Guidance

Priority within each phase is informed by the maintainer review's priority ranking and the RFC's phasing guidance.

**Highest-value items (from maintainer review):**

| Priority | Item | Effort | Maintainer pain without it |
|----------|------|--------|---------------------------|
| 1 | G7-10 Directory-index generator | High | High — index already drifted, will keep drifting |
| 2 | G7-08 Index-completeness rule | Medium | High — silent drift, no check fires |
| 3 | G7-02 Scoped frontmatter requirements | Medium | Medium — decision lifecycle invisible |
| 4 | G7-04 Post-fix/maintain diff output | Low | Medium — requires git diff after every maintain |
| 5 | G7-03 Naming convention enforcement | Medium | Medium — naming drift is silent |
| 6 | G7-09 State-document freshness | Medium | Medium — stale state docs invisible |
| 7 | G7-01 Per-path rule suppression | Low | Low — can suppress globally for now |
| 8 | G7-13 Role-linked behavioral defaults | High | Low — roles work as taxonomy today |
| 9 | G7-05 Rule scope transparency | Low | Low — confidence gap, not confirmed bug |

**Suggested implementation start order** (balancing value, effort, and dependency readiness):
1. G7-01 (low effort, unblocks G7-06)
2. G7-02 (medium effort, high standalone value)
3. G7-04 (low effort, immediate UX improvement)
4. G7-05 (low effort, quick win)
5. G7-03 (medium effort, immediate value)
6. G7-08 (medium effort, high value)
7. G7-06, G7-07 (medium effort, governance inspection)
8. G7-09 (medium effort, freshness signaling)
9. G7-10 (high effort, highest maintainer value)
10. Remaining items by phase

---

## 6. Open Questions — Resolved

All questions resolved at RFC-007 acceptance (2026-04-15):

1. **Command hierarchy for policy explanation:** `steward explain path <path>` — subcommand of `explain`, per RFC-001 conventions.
2. **Diagnostic vs report boundary:** Phase 1–2 findings are `check` diagnostics (rule ID, severity, remediation). Phase 4 coverage/discoverability are report-style under `steward status --coverage` / `--discoverability`.
3. **Bootstrap inference threshold:** Conservative. Only high-confidence suggestions, always preview-first.
4. **Move/rename scope:** Markdown-first for initial implementation.
5. **Dependency modeling explicitness:** Hybrid — explicit `depends_on` plus auto-derived from `index_of`. Cycle detection is mandatory.
6. **Schema additions vs derivable state:** New optional fields: `validation.path_overrides`, `validation.frontmatter_requirements`, `path_rule.must_match`, `artifact.index_of`, `artifact.freshness`, `artifact.importance`. All non-breaking when omitted.
7. **Naming pattern syntax:** .NET regex (`System.Text.RegularExpressions`). More expressive, native support, validated at config-load time.
8. **Freshness source:** Git commit timestamps primary, frontmatter `last_updated` field as override, filesystem mtime as fallback.

---

## 7. Relationship to Accepted Scope

This backlog does **not** modify or supersede:
- PRD-0001 (accepted product requirements for v1.0.0)
- TRACE-0001 (accepted requirements traceability)
- The v0.1.0–v1.0.0 milestone plan (PLAN-0002)
- Any accepted RFC (RFC-001 through RFC-006) or ADR (ADR-001 through ADR-010)

Items in this backlog represent **proposed post-v1.0.0 enhancements** pending RFC-007 acceptance. They may refine or extend accepted requirements but should not contradict them.

Some items partially overlap with existing accepted requirements that are already implemented but could be enhanced:
- G7-02 extends REQ-FM-003 (document-type-aware frontmatter expectations)
- G7-04 relates to REQ-VALIDATE-009 (dry-run support) and REQ-SAFE-002 (preview before changes)
- G7-06 extends REQ-EXPLAIN-001/004 (deeper explain surface)
- G7-10 extends REQ-MAINT-006 (indexes and registries maintenance)
- G7-17 relates to REQ-VALIDATE-002 (staged scope) and REQ-WORKFLOW-003 (completion)

These overlaps are intentional: RFC-007 proposes deepening capabilities that v1.0.0 established at a foundational level.
