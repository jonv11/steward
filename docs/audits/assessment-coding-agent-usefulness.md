# Steward Assessment for Coding-Agent Usefulness

**Date:** 2026-04-14
**Reviewer:** Automated principal-level review
**Scope:** Assess Steward as a practical companion for coding agents following the standard terminal workflow

---

> Historical scope note (2026-04-16): This assessment is preserved as input evidence for later product and ADR work. It does not override the current state and readiness artifacts in [implementation-status.md](../implementation-status.md) and [docs/planning/](../planning-index.md).

## 1. Assessment Lens

This assessment evaluates Steward through the lens of a coding agent's most common terminal workflow:

**locate → understand → change → verify → review → commit**

The reference tool palette is:
- `git status`, `git diff`, `git log`, `git blame` — repo state and history
- `rg`, `find`, `ls`, `pwd`, `cat`, `sed -n`, `head`, `tail` — locate and inspect files
- `jq` — parse structured output
- test/build/lint commands — verify changes
- `git add`, `git commit` — package work

The central question: **Does Steward shorten or improve any stage of this loop compared to raw shell commands?**

---

## 2. Executive Summary

Steward is **useful in specific moments** of the agent workflow today, with clear potential to become a **strong companion surface** after targeted improvements.

**Where it already helps:**
- Session-start orientation (`orient`, `status`) provides structured context faster than `ls -la` + `cat README.md` + manual tree exploration.
- Repository-wide heading search (`search --mode headings`) provides Markdown-aware results that `rg` cannot match without manual context reconstruction.
- Markdown structural inspection (`md query`, `md outline`) gives agents deterministic section extraction without fragile `sed`/`awk` parsing.
- Policy compliance checking (`check --output json`) provides a single structured answer to "is this repo healthy?" that would otherwise require multiple ad-hoc checks.
- Structural Markdown editing (`md edit`) enables safe, preview-first section mutations that would otherwise require risky raw text operations.

**Where it falls short:**
- Orientation output is verbose (full file listing) rather than a concise "what matters" summary tuned for agents.
- Search is substring-only — no regex, no word boundaries — making it strictly less powerful than `rg` for precise queries.
- No command answers "what changed and what should I verify?" — the gap between `git diff` and `steward check` is bridged manually.
- JSON output is available but not always complete (completion policy data missing from check JSON).
- `outline` crashes on file paths, removing a potential time-saver.
- No `--quiet` or summary-only mode for quick yes/no checks in automation loops.

**Trajectory**: With ~5 targeted improvements, Steward could become the default first tool an agent reaches for when starting work in a governed repository.

---

## 3. Current Fit by Workflow Stage

### 3.1 Locate

**Raw shell commands agents use:** `find`, `ls`, `rg`, `fd`, `tree`
**Goal:** Find the right file or section to work on.

**What Steward offers:**
- `steward orient` — classified file tree with role annotations (`[source]`, `[documentation]`, `[configuration]`, `[testing]`)
- `steward search <query>` — repository-wide content and heading search with Markdown context
- `steward outline` — file tree with optional sizes and line counts
- `steward md outline <file>` — heading hierarchy with section line counts

**What works well:**
- `search --mode headings` is excellent for finding the right document section. Example: `steward search "Validation" --mode headings --max 10` returns 10 heading-level matches with document paths and heading text — faster than `rg "^#+.*Validation"` because results include the full heading context hierarchy.
- `md outline` with line counts lets an agent quickly assess document structure before reading. Example: `steward md outline docs/requirements/PRD.md` shows "8.3 Validation and Diagnostics (42 lines)" — the agent knows exactly where to look and how much content to expect.
- `orient` with `--output json` provides machine-parseable file classification that eliminates "scan and guess" behavior.

**What doesn't help enough:**
- `search` is substring-only. An agent needing `rg -w "ValidationEngine"` or `rg "IValidationRule|IFixableRule"` must fall back to `rg`. This is the most common locate operation for code-level work, and Steward doesn't compete.
- `orient` lists every file at the configured depth rather than focusing on the 10-15 most important files. An agent receives 100+ entries when it needs 10. There is no `--compact` or `--summary` mode.
- `outline README.md` crashes instead of showing the file's heading outline. This forces agents to use `steward md outline README.md` specifically — a discoverability gap.

**Verdict for locate stage:** Useful for document-level and heading-level search. Not useful for code-level search (regex, word-boundary, multi-pattern). Net positive for documentation-governed repos; neutral for code-heavy repos.

---

### 3.2 Understand

**Raw shell commands agents use:** `cat`, `head`, `tail`, `sed -n`, `git log`, `git blame`
**Goal:** Understand the context of the area being changed.

**What Steward offers:**
- `steward md query <file> <selector>` — extract specific Markdown sections by heading path
- `steward md outline <file>` — heading hierarchy for document structure
- `steward orient` — repo-level context (name, type, profile, start-here)
- `steward status` — current health/completeness state
- `steward explain <rule-id>` — understand what a validation rule checks

**What works well:**
- `md query docs/PRD.md "heading[Functional Requirements]"` extracts exactly the content under a heading. This is dramatically better than `sed -n '/^## Functional/,/^## /p' docs/PRD.md` because it handles nested headings correctly and doesn't require fragile regex construction.
- `status --output json` gives an agent a single structured answer to "what's the state of this repo?" Example output: `{"presentCount": 2, "requiredCount": 2, "staleCount": 0}` — one command replaces checking multiple files and running multiple queries.
- `explain STWD-007` tells an agent why a stale-artifact warning appeared and what to do about it. This is information an agent cannot get from any raw shell command.

**What doesn't help enough:**
- No command answers "what changed recently?" or "what is the intent of this file?" — agents still need `git log --oneline <file>` and `git blame`.
- `orient` does not surface policy intent or "what this repo is trying to achieve" — it's a classified file listing, not a semantic summary.
- `md query` with `frontmatter.status` is useful, but there's no batch frontmatter query across multiple files (e.g., "show me the status of all docs/*.md").

**Verdict for understand stage:** Strong for document-level structural inspection. Excellent for repo health queries. Weak for change history and code-level context. Net positive.

---

### 3.3 Change

**Raw shell commands agents use:** `sed`, `echo >>`, direct file writes
**Goal:** Make the intended change safely.

**What Steward offers:**
- `steward md edit ensure-section <file> --heading "Section"` — create section if missing
- `steward md edit set-section <file> --heading "Section" --content "..."` — replace section content
- `steward md edit insert-section <file> --heading "New" --under "Parent"` — add new section
- `steward md edit append-block <file> --under "Section" --content "..."` — append to section
- `steward md edit prepend-block <file> --under "Section" --content "..."` — prepend to section
- `steward md edit fm-set <file> --key status --value draft` — set frontmatter field
- `steward md edit fm-merge <file> --input <yaml>` — merge YAML into frontmatter
- `steward maintain --apply` — refresh governed artifacts

**What works well:**
- `md edit ensure-section` is exactly what agents need for "add this section if it doesn't exist" operations. This is substantially safer than scanning for a heading with `rg` and conditionally writing with `sed`, because Steward handles heading-level inference and doesn't corrupt other content.
- `fm-set` is better than hand-editing YAML frontmatter blocks with raw text tools, which frequently produce malformed YAML.
- `maintain --apply` is excellent for "refresh all governed artifacts after my changes" — a single command that would otherwise require multiple manual file updates.
- Preview-by-default on all edit operations is valuable for agent safety: the agent can verify the diff before committing to the change.

**What doesn't help enough:**
- No `--after "Existing Section"` or `--before "Existing Section"` flags on `insert-section`. Agents cannot specify sibling placement for new sections.
- All content must be passed as `--content` string arguments. For multi-line content, agents must use shell quoting or temporary files. No stdin piping support is evident.
- `set-section` replaces the entire section body. There is no "patch" or "append to existing content within a section at a specific location" operation beyond `append-block`/`prepend-block`.
- For code files (.cs, .py, .ts), Steward provides no change support — this is by design (PRD scope), but agents spend most of their change operations on code, not Markdown.

**Verdict for change stage:** Strong for Markdown structural changes. Excellent safety model with preview/apply. Not applicable to code changes (by design). Net positive for documentation-governed repos.

---

### 3.4 Verify

**Raw shell commands agents use:** `dotnet test`, `npm test`, `make check`, linters
**Goal:** Confirm the change is correct and doesn't break anything.

**What Steward offers:**
- `steward check` — full or scoped validation against policy
- `steward check --scope changed` — validate only changed files
- `steward check --output json` — machine-parseable results
- `steward check --fix --dry-run` — preview what auto-fixes would do
- `steward maintain` (preview mode) — check if maintenance artifacts are stale after changes

**What works well:**
- `check --output json | jq '.summary.pass'` gives a single boolean answer to "did my changes break any policy?" This is the ideal verify command for agent loops.
- `check --scope changed` focuses validation on what the agent actually modified, reducing noise and improving performance.
- `check --fix --dry-run` tells an agent exactly what auto-repairs are available without making any changes. This is excellent for the agent inner loop: change → check → fix (if safe) → re-check.
- Exit code 0/1 distinction means agents can use `steward check && echo OK || echo FAIL` without parsing output.

**What doesn't help enough:**
- Completion summary data is not in JSON output. An agent parsing `check --output json` sees `pass: true` but not the breakdown of "2/2 required artifacts present, 0 stale". This data is only in text output.
- No `--quiet` flag to suppress output and return exit code only. Agents piping output to `/dev/null` anyway would benefit from this.
- `check --scope changed` relies on git diff, which is correct, but there's no way to combine it with `--paths` (e.g., "check my explicit file list, but also include policy-level checks like required-artifacts").

**Verdict for verify stage:** Strong. This is Steward's best workflow stage. Machine-readable output, scoped validation, and deterministic exit codes all serve agents well. The missing JSON completion data is the main gap.

---

### 3.5 Review

**Raw shell commands agents use:** `git diff`, `git diff --staged`, `git log --oneline`
**Goal:** Review changes before committing.

**What Steward offers:**
- `steward md edit <op> <file>` (preview mode) — shows unified diff of what would change
- `steward maintain` (preview mode) — shows what maintenance actions would be taken
- `steward check --fix --dry-run` — shows what auto-fixes would change

**What works well:**
- Preview mode on all edit operations produces a diff that an agent can inspect before applying. This is a genuine safety improvement over making changes and then reviewing with `git diff`.
- `maintain` preview shows per-artifact action plans. Agents can confirm that maintenance actions are expected before applying.

**What doesn't help enough:**
- No command that shows "here's what changed in the repo from Steward's perspective" — a summary of files modified, new files, removed files, with Markdown-aware context.
- No integration with git diff to annotate changes with policy context (e.g., "this change modifies a managed section" or "this new file needs frontmatter per policy").
- Review is inherently a git-level operation and Steward correctly stays out of the git workflow. But it could add value by surfacing "you changed docs/PRD.md — here's what Steward thinks about it" as a post-change context command.

**Verdict for review stage:** Moderate. Preview mode is valuable when using Steward's own editing. No value for reviewing changes made by other tools.

---

### 3.6 Commit

**Raw shell commands agents use:** `git add`, `git commit`, `git push`
**Goal:** Package clean, reviewable work.

**What Steward offers:**
- `steward check --scope staged` — validate staged files before commit
- `steward status` — confirm repo is in a clean state

**What works well:**
- `check --scope staged` as a pre-commit check is the canonical use case. Combined with exit code checking, this can gate commits: `steward check --scope staged && git commit -m "..."`.

**What doesn't help enough:**
- No suggested commit message or change summary. Steward knows what was done (maintenance applied, sections edited, fixes applied) but doesn't surface this as commit guidance.
- No explicit pre-commit hook integration or guidance.

**Verdict for commit stage:** Minimal but correct. Pre-commit validation is the right surface.

---

## 4. Command-by-Command Usefulness Assessment

### 4.1 `steward orient`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Medium — good for session-start but verbose |
| **Trustworthiness** | High — output is deterministic and well-classified |
| **Discoverability** | High — natural first command |
| **Output ergonomics** | Mixed — text output lists every file; JSON is better for parsing but still verbose |
| **Place in agent workflow** | locate / understand |
| **Improvements** | Add `--compact` for top-15 entries only; surface start-here entries more prominently; add `--signals` data to JSON output distinctly |

### 4.2 `steward outline`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Medium — useful for directory structure, broken for file paths |
| **Trustworthiness** | Low — crashes on file input |
| **Discoverability** | Medium — `outline <file>` is intuitive but crashes |
| **Output ergonomics** | Good — sizes and lines flags are useful |
| **Place in agent workflow** | locate |
| **Improvements** | Fix file-path crash (delegate to md outline or error gracefully); add --headings; add --output json |

### 4.3 `steward search`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Medium-High — heading search is unique; content search is weaker than rg |
| **Trustworthiness** | High — results are well-structured |
| **Discoverability** | High — natural search command |
| **Output ergonomics** | Good — JSON output includes headingContext, path, line, column, snippet |
| **Place in agent workflow** | locate |
| **Improvements** | Add regex support; add `--type md` filter; consider returning heading path not just nearest heading |

### 4.4 `steward check`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | High — single command for policy verification |
| **Trustworthiness** | High — deterministic, well-tested, 9 rules |
| **Discoverability** | High — canonical verify command |
| **Output ergonomics** | Good text, Good JSON (missing completion summary in JSON) |
| **Place in agent workflow** | verify |
| **Improvements** | Add completion data to JSON; add --quiet for exit-code-only; add --strict for treating warnings as errors |

### 4.5 `steward md query`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | High — deterministic section extraction beats sed/awk |
| **Trustworthiness** | High — well-tested, handles ambiguity correctly |
| **Discoverability** | Medium — agents need to know MdPath syntax |
| **Output ergonomics** | Good — text shows content, JSON includes range and metadata |
| **Place in agent workflow** | understand |
| **Improvements** | Add `.lists`, `.tables`, `.codeblocks` selectors; add `managed[*]` wildcard; add batch query across files |

### 4.6 `steward md edit`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | High — 7 operations covering common Markdown mutations |
| **Trustworthiness** | High — preview-first, ownership enforcement |
| **Discoverability** | Medium — requires knowing subcommands |
| **Output ergonomics** | Good — preview shows diff, --apply makes changes |
| **Place in agent workflow** | change |
| **Improvements** | Add --after/--before/--level; add --stdin for multiline content; add --force for overriding ownership checks |

### 4.7 `steward maintain`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | High — single command to refresh all governed artifacts |
| **Trustworthiness** | High — idempotent, preview-first |
| **Discoverability** | High — natural maintenance command |
| **Output ergonomics** | Good — per-artifact status in both modes |
| **Place in agent workflow** | change (post-change refresh) |
| **Improvements** | Show diff in preview mode; add --scope multiple artifacts |

### 4.8 `steward status`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Medium-High — quick health snapshot |
| **Trustworthiness** | High — lightweight, deterministic |
| **Discoverability** | High — natural status command |
| **Output ergonomics** | Good text and JSON |
| **Place in agent workflow** | understand / verify |
| **Improvements** | Add maintenance freshness timestamps; add "next action" guidance |

### 4.9 `steward explain`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Medium — useful when a rule fires, rarely used proactively |
| **Trustworthiness** | High — deterministic |
| **Discoverability** | Medium — agents need to know rule IDs |
| **Output ergonomics** | Good — clear text and JSON |
| **Place in agent workflow** | understand (remediation) |
| **Improvements** | Inline remediation in check output reduces need to call explain separately |

### 4.10 `steward config validate / show`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Low — agents rarely validate config |
| **Trustworthiness** | High |
| **Discoverability** | Low |
| **Output ergonomics** | Adequate |
| **Place in agent workflow** | setup (rare) |
| **Improvements** | `config show --effective --output json` for machine-readable effective config |

### 4.11 `steward init`

| Dimension | Assessment |
|---|---|
| **Current value for agents** | Low — one-time setup |
| **Trustworthiness** | Adequate |
| **Discoverability** | High |
| **Output ergonomics** | N/A |
| **Place in agent workflow** | setup (one-time) |
| **Improvements** | None needed |

---

## 5. Where Steward Already Meaningfully Shortens the Path

### 5.1 Session-start orientation (locate + understand)

**Without Steward:**
```bash
ls -la
cat README.md | head -50
find . -name "*.md" -path "*/docs/*" | head -20
tree -L 2
```
~4 commands, manual synthesis of "what is this repo?"

**With Steward:**
```bash
steward orient --depth 2
steward status --output json
```
2 commands. Agent gets: repo name, type, profile, start-here files, classified file tree, required artifact status, maintenance status, completeness count.

**Shortening**: 2x fewer commands with 3x more structured information.

### 5.2 Finding the right Markdown section (locate)

**Without Steward:**
```bash
rg "^#+.*Validation" docs/ --no-heading
# agent must parse headings from raw grep output and guess hierarchy
cat docs/requirements/PRD.md | sed -n '/^### 8.3/,/^### 8.4/p'
```

**With Steward:**
```bash
steward search "Validation" --mode headings --max 10 --output json
steward md query docs/requirements/PRD.md "heading[Validation and Diagnostics]"
```
Agent gets: exact heading matches with file paths, then exact section content extraction by heading path.

**Shortening**: Eliminates fragile `sed` range expressions. Heading hierarchy context is automatic.

### 5.3 Structured Markdown editing (change)

**Without Steward:**
```bash
# Agent must: find insertion point, count line numbers, use sed or manual file rewrite
# Risk: corrupt unrelated content, break heading levels, produce malformed Markdown
```

**With Steward:**
```bash
steward md edit ensure-section README.md --heading "Contributing" --under "Development" --content "See CONTRIBUTING.md for details." --apply
```
Single command. Safe. Heading level inferred from parent. Existing content not touched.

**Shortening**: Eliminates fragile multi-step raw-text manipulation with a single atomic, preview-safe operation.

### 5.4 Policy compliance verification (verify)

**Without Steward:**
```bash
# Agent must check multiple things manually:
test -f README.md && echo OK  # required artifacts
rg -l "^---" docs/**/*.md     # frontmatter presence
# ...no automated equivalent for managed-region integrity, section size, stale artifacts
```

**With Steward:**
```bash
steward check --output json | jq '.summary.pass'
```
Single command. 9 rules. Machine-parseable. Exit code 0/1.

**Shortening**: One command replaces an unbounded set of ad-hoc checks.

### 5.5 Post-change maintenance refresh (change)

**Without Steward:**
```bash
# Agent must manually identify and update:
# - STRUCTURE.md
# - index files
# - managed sections
# - frontmatter timestamps
# Each requires custom logic
```

**With Steward:**
```bash
steward maintain --apply
```
Single command. Idempotent. All governed artifacts refreshed.

**Shortening**: Eliminates multiple error-prone manual update steps.

---

## 6. Where Steward Currently Duplicates Raw Shell Work Instead of Replacing It

### 6.1 Code-level search

`steward search "ValidationEngine"` is a substring search. An agent already has `rg ValidationEngine` which is faster, supports regex, word boundaries, and file-type filtering. Steward search adds heading context for Markdown files but provides no advantage for code search. An agent will always prefer `rg` for code-level work.

### 6.2 Full file tree listing

`steward outline` without flags produces output similar to `tree -I 'bin|obj|node_modules'`. The classified orient is better, but plain outline adds little over `tree` or `find`.

### 6.3 Simple file inspection

`steward md query README.md frontmatter` is useful, but for reading a whole file, `cat README.md` is simpler. Steward only adds value when the agent needs structural extraction, not whole-file reads.

### 6.4 Git integration

`steward check --scope changed` ultimately calls `git diff --name-only HEAD~1` internally. The scoping is useful, but agents that already use `git diff --name-only` don't get much beyond having validation applied to those files.

---

## 7. Highest-Value Improvements for Coding-Agent Usefulness

### Priority 1: Critical

**7.1 Fix `outline` crash on file-path input**
- **Problem**: `steward outline README.md` crashes with IOException instead of showing heading outline or error
- **Impact**: Breaks discoverable agent workflow; forces knowledge of `md outline` vs `outline` distinction
- **Fix**: Detect file paths in outline; delegate to `md outline` for `.md` files; friendly error for other file types

**7.2 Add completion/policy data to JSON check output**
- **Problem**: `check --output json` omits completion summary (required artifacts present/missing counts, stale artifact counts, guidance text)
- **Impact**: Agents parsing JSON miss critical "what to do next" information
- **Fix**: Add `completionPolicy` object to JSON check output alongside `summary` and `diagnostics`

### Priority 2: High

**7.3 Add `--compact` mode to orient**
- **Problem**: Orient lists all files at configured depth. Agents receive 100+ entries when they need 10-15 key files.
- **Impact**: Agents waste context window on low-value entries
- **Fix**: `--compact` or `--summary` flag that shows only: start-here files, classified directories (one level), required artifacts, and any signal flags

**7.4 Add regex mode to search**
- **Problem**: Search is substring-only, making it strictly weaker than `rg` for code-level queries
- **Impact**: Agents always fall back to `rg` for precise searches, reducing Steward's role
- **Fix**: `--regex` flag on search. Optional, not default. Adds value by combining regex power with heading-context enrichment.

**7.5 Add `--after`, `--before`, `--level` to md edit insert-section**
- **Problem**: Agents can only place new sections "under" a parent, not after/before a sibling
- **Impact**: Limits structural editing precision. Agents must fall back to raw text manipulation for sibling insertion.
- **Fix**: Per RFC-004, add `--after`, `--before`, `--level` options with heading-level inference

### Priority 3: Medium

**7.6 Add `--quiet` / `--exit-code-only` to check**
- **Problem**: Agents running check in tight loops must parse or discard output
- **Impact**: Minor overhead in automation
- **Fix**: `--quiet` flag that suppresses stdout and returns exit code only

**7.7 Add stdin support for md edit content**
- **Problem**: Multi-line content must be passed as `--content` string argument with shell quoting challenges
- **Impact**: Limits agent ability to pass complex content through md edit
- **Fix**: Accept `--content -` to read from stdin, or `--content-file <path>` to read from a file

**7.8 Show diff in maintain preview**
- **Problem**: `maintain` preview shows action descriptions but not the actual content diff
- **Impact**: Agents can't efficiently review what would change without applying and diffing
- **Fix**: Show unified diff for each artifact in preview mode

**7.9 Add batch frontmatter query**
- **Problem**: `md query <file> frontmatter.status` works for one file. No way to query frontmatter across multiple files.
- **Impact**: Agents checking "what's the status of every doc?" must loop manually
- **Fix**: `steward md query --pattern "docs/**/*.md" frontmatter.status` returning results per file

---

## 8. Proposed New Features or Command Improvements

### Requirement-backed

| Proposal | Requirement | Status |
|---|---|---|
| Profile default merging | REQ-CONFIG-004, REQ-CONFIG-007 | Accepted requirement, not yet implemented |
| `--headings` flag on outline | REQ-OUTLINE-005 | Accepted in RFC-001, not implemented |
| Content-type sub-selectors (`.lists`, `.tables`) | REQ-MD-002 | Accepted in RFC-004, not implemented |
| `managed[*]` wildcard | REQ-MD-002 | Accepted in RFC-004, not implemented |
| State document role handling | REQ-STATE-001–003 | Accepted requirements, not implemented |
| Configurable completion policy | REQ-WORKFLOW-007 | Accepted requirement, hardcoded currently |

### Net-new (not in current requirements, aligned with product direction)

| Proposal | Benefit | Who |
|---|---|---|
| `steward next` command | Answer "what should I do next?" by combining check results, status, and policy guidance into a single prioritized action list | Agent |
| `--compact` / `--summary` on orient | Reduce context-window load for agents; show only the 10-15 most important items | Agent |
| `--regex` on search | Close the gap with `rg` while adding Markdown heading context | Agent, human |
| `--stdin` for md edit content | Enable piping multi-line content without shell quoting | Agent |
| `check --changed-summary` | Show what changed + what failed, combining `git diff --stat` with check, in one output | Agent |
| `steward diff-check` | Run check only on changed files and annotate results with git diff context | Agent, CI |
| Exit-code-only / `--quiet` mode | Faster automation loops without parsing | Agent, CI |
| Pre-commit hook template in init | Lower barrier to integrating steward into commit workflow | Human, CI |
| `steward md batch-query` | Query the same selector across multiple files | Agent |
| `steward what-changed` | Repository-level summary of changes with Markdown-aware context | Agent |

---

## 9. Overall Verdict

Steward is currently **useful in specific moments** of the coding-agent terminal workflow.

It is strongest in:
- **Session-start orientation** (orient, status) — significantly better than raw shell exploration
- **Markdown structural inspection** (md query, md outline) — dramatically better than sed/awk for section extraction
- **Policy compliance verification** (check) — single structured command replacing ad-hoc checks
- **Structural Markdown editing** (md edit) — safe, preview-first operations unavailable from raw shell
- **Governed artifact maintenance** (maintain) — automating multi-file deterministic updates

It is weakest in:
- **Code-level search** — agents will always prefer `rg` for non-Markdown work
- **Output conciseness for agents** — orient is too verbose, check JSON is missing completion data
- **Workflow guidance** — no "what should I do next?" command
- **Change review context** — doesn't help review non-Steward changes

**What would move it to "strong companion surface":**
1. Fix the `outline` crash (trust issue)
2. Add completion data to JSON check output (completeness)
3. Add `--compact` to orient (agent ergonomics)
4. Add `steward next` or equivalent guidance command (workflow)
5. Add regex search (close the gap with `rg`)

**What would move it to "default first tool for agents":**
All of the above, plus:
6. Deeper integration between check results and change context (`what-changed`)
7. Batch Markdown queries
8. Richer status with "time since last maintain" and "next actions"
9. First-class support for agent automation loops (exit-code-only mode, stdin piping)

Steward's strongest architectural advantage is that it **knows about the repository's declared policy**. No raw shell command does. This makes it uniquely positioned to provide answers like "is this complete?", "what should I do?", and "is this safe?" — questions that agents otherwise answer by heuristic guessing. The implementation is solid, the safety model is correct, and the test discipline is strong. With targeted improvements to agent ergonomics and workflow guidance, it could become a high-trust default surface for agents working in governed repositories.
