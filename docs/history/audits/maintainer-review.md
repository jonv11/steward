---
type: audit
status: Historical
standalone: true
---
# Maintainer Review — Steward as Its Own Governance Tool

**Date:** 2026-04-14
**Role:** Repo maintainer and power-user of Steward
**Scope:** Honest assessment of what Steward can and cannot enforce in this specific repository, with concrete improvement requests grounded in real maintenance needs

---

> Historical scope note (2026-04-16): This review is preserved as maintainer-perspective evidence that informed later governance work. Current authoritative state now lives in [implementation-status.md](../plans/implementation-status-2026-06-06.md) and the active planning docs under [docs/planning/](../plans/planning-index-2026-06-06.md).

## 1. Context and Purpose

This review is written from the perspective of a maintainer who uses Steward as the primary tool for keeping this repository consistent, discoverable, and correctly governed. It is distinct from the existing audit documents:

- The [repository audit](../stubs/repository-audit-2026-04-14.md) assesses contract-alignment between implementation and accepted design.
- The [coding-agent assessment](assessment-coding-agent-usefulness.md) evaluates Steward's usefulness in an agent's terminal workflow.

This review asks a different question: **as the person responsible for this repository's health day to day, what does Steward fail to let me express or enforce, and what is the maintenance cost of those gaps?**

The goal is to be direct and honest. Where Steward works well, I will say so briefly. Where it does not, I will state exactly what I need, why it matters here, and what the smallest useful improvement would look like. This is not a wish list — every item below reflects a real maintenance situation I have encountered or will encounter in this repo.

---

## 2. What Already Works Well

I'll keep this section short, not because the positives don't exist but because the audit and assessment docs cover them thoroughly.

**`steward check` as a pre-commit gate is solid.** Exit code semantics are correct, JSON output is stable, and the nine rules cover the most critical categories. Running `steward check` before committing is a genuine habit I've built.

**`steward maintain --apply` eliminates a manual chore.** Keeping `STRUCTURE.md` in sync with the actual repo layout used to require a manual step. With the maintain contract, it's one command and idempotent. This is the right model.

**`steward orient` now surfaces meaningful roles.** After the policy improvements made in this session, `docs/requirements/PRD.md` shows as `[requirements]`, `docs/implementation-status.md` shows as `[state-document]` with a `[start]` marker, and `docs/decisions/decision-index.md` shows as `[reference]`. This is a real improvement over a flat `[documentation]` listing for everything.

**Policy-declared artifact validation (STWD-001, STWD-009) prevents silent rot.** Knowing that `check` will error if `docs/requirements/PRD.md` disappears gives me confidence that required artifacts can't be quietly deleted without breaking CI.

---

## 3. Gaps and Improvement Requests

### 3.1 Index completeness is not enforceable

**The problem.**
`docs/planning-index.md` is the navigation hub for all planning, decision, and requirements artifacts. It contains a manually maintained table listing every RFC and ADR. As of this writing, the table lists ADR-001 through ADR-009 — but `docs/decisions/adrs/ADR-010-agent-usefulness-improvements.md` exists on disk and is not referenced anywhere in the index. I only discovered this by running `find docs/decisions/adrs -name "*.md"` manually.

There is no Steward rule that detects this class of divergence: a file exists in a policy-relevant directory, but the index document that should reference it does not.

**Why it matters here.**
Every time a new RFC or ADR is added to `docs/decisions/rfcs/` or `docs/decisions/adrs/`, the planning index must be updated. This is a purely mechanical obligation, and it is easy to forget. The planning index is a declared `planning` artifact, and the decisions directory contains files with a declared `reference` role. The relationship between them is real and maintainable — but not currently declared in any way that Steward can enforce.

**What I want.**
A policy declaration that ties an index artifact to a source directory, such that `steward check` warns when files in the source directory are not referenced in the index. Concretely:

```yaml
artifacts:
  - path: docs/planning-index.md
    role: planning
    required: false
    index_of:
      - docs/decisions/rfcs/
      - docs/decisions/adrs/
      - docs/audits/
```

The corresponding check rule (call it STWD-010 or similar) would verify that every `.md` file under the `index_of` directories is referenced (via a Markdown link) in the index document. Missing entries would produce a `Warning` — enough to catch drift without blocking commits.

**The alternative today.** There is no alternative short of a custom script. `steward check` passes cleanly even though the index is incomplete.

---

### 3.2 Naming conventions in structured directories cannot be declared or enforced

**The problem.**
`docs/decisions/rfcs/` contains files named `RFC-NNN-kebab-case-title.md`. `docs/decisions/adrs/` contains files named `ADR-NNN-kebab-case-title.md`. These are deliberate conventions. A contributor adding `docs/decisions/rfcs/my-proposal.md` (missing the RFC-NNN prefix) would produce a file that breaks the convention, is invisible to the naming pattern expected by the index, and silently passes `steward check`.

**Why it matters here.**
Decision documents are long-lived. A badly named RFC or ADR causes navigational confusion and breaks the automatic ordering the naming scheme provides. For a tool that is itself governed by these conventions, enforcement is especially important.

**What I want.**
Path-policy.yaml rulesets that can be applied to directories with naming pattern requirements. The current path-policy model defines rules at the top level of the repo but does not have a clean way to say "all `.md` files directly under this directory must match this pattern". Concretely:

```yaml
rulesets:
  - name: decision-naming
    rules:
      - pattern: "docs/decisions/rfcs/*.md"
        category: naming
        must_match: "RFC-[0-9]{3}-.+"
        message: "RFC files must follow the RFC-NNN-title.md naming convention"

      - pattern: "docs/decisions/adrs/*.md"
        category: naming
        must_match: "ADR-[0-9]{3}-.+"
        message: "ADR files must follow the ADR-NNN-title.md naming convention"
```

This would require a new `naming` category in the path-policy engine and a corresponding validation rule (STWD-011 or similar) — but the policy-expression side is straightforward to model and the ergonomics would be immediately useful.

---

### 3.3 Per-directory frontmatter requirements are not expressible

**The problem.**
Every ADR and RFC in this repo includes a status declaration (`- **Status:** Accepted`) — but as plain body text, not YAML frontmatter. STWD-003 requires frontmatter fields, but it applies globally across all Markdown files. Even if I converted the status to real frontmatter, I cannot tell Steward "require `status` only for files under `docs/decisions/`" without also requiring it for every other Markdown file in the repo — including README.md, STRUCTURE.md, planning docs, and audit notes, none of which need it.

**Why it matters here.**
Decision documents have a lifecycle (Draft → Accepted → Superseded). Enforcing that lifecycle requires a machine-readable status field. The current situation is that status is in body text, which makes it:
- Invisible to STWD-003 (no frontmatter, so no check fires)
- Invisible to `steward search --mode headings` filtering by status
- Invisible to any future automation that might flag superseded decisions for attention

**What I want.**
Scoped frontmatter requirements: the ability to declare that files matching a path pattern must have specific frontmatter fields. Concretely:

```yaml
validation:
  frontmatter_requirements:
    - pattern: "docs/decisions/**/*.md"
      required_fields:
        - status
      allowed_values:
        status: [Draft, Accepted, Superseded, Withdrawn]
    - pattern: "docs/audits/*.md"
      required_fields:
        - date
```

This is a natural extension of STWD-003 from "global" to "scoped by path pattern". The improvement is significant: it would catch missing or invalid status values in decision documents without imposing frontmatter requirements on every other Markdown file.

**Secondary need: `steward md query` aggregation across pattern.**
Even if I adopt frontmatter for status today, I have no way to ask Steward "show me the status of all ADRs". A `steward md query --pattern "docs/decisions/adrs/*.md" frontmatter.status` command would answer that question in a single call. Without it, I must loop manually.

---

### 3.4 Rule disablement is only global, not per-file or per-directory

**The problem.**
STWD-004 warns when a Markdown section exceeds 500 lines. This is a useful rule for documentation that contributors write and read. However, `repository-steward-master-requirements.md` is a machine-navigable requirements registry with 100+ short sections — none of which individually exceeds 500 lines, but the file as a whole is a reference artifact, not a narrative document. The rule is not harmful here today, but if I ever add a long rationale section to a requirement, STWD-004 will fire on a file that is explicitly structured to be long.

More practically: `docs/requirements/PRD.md` section `8. Functional Requirements` has many subsections. A large functional area could legitimately exceed 500 lines without being badly structured. I want to suppress STWD-004 for that file specifically, without disabling it globally.

**What I want.**
Per-path rule suppression in policy.yaml:

```yaml
validation:
  disabled_rules: []
  path_overrides:
    - pattern: "repository-steward-master-requirements.md"
      disabled_rules: [STWD-004]
    - pattern: "docs/requirements/PRD.md"
      disabled_rules: [STWD-004]
```

This is a small addition to the validation model but removes a class of "suppress globally because one file needs it" decisions that currently make the global `disabled_rules` list a blunt instrument.

---

### 3.5 State-document freshness is not detectable

**The problem.**
`docs/implementation-status.md` declares `Last updated: 2025-07-18` in its content — nearly nine months ago as of this writing. It is now a `state-document` in policy. But Steward has no concept of "this state document should have been updated within the last N days", and no signal fires to indicate that the declared current-state view is potentially stale.

The STWD-007 stale-artifact rule correctly detects when a *generated* artifact diverges from what `maintain` would produce. But `docs/implementation-status.md` is *human-maintained* — its freshness depends on whether its content reflects reality, not whether it matches a deterministic generator output. There is no equivalent rule for manually maintained state documents.

**Why it matters here.**
A stale implementation-status document actively misleads contributors and agents. Someone reading it today would believe the repo is at "100% v1.0.0 complete" without context for subsequent work. The state-document role I declared in policy should mean something beyond just display classification.

**What I want.**
A policy-level freshness declaration for state documents:

```yaml
artifacts:
  - path: docs/implementation-status.md
    role: state-document
    required: false
    freshness:
      max_age_days: 60
      frontmatter_field: updated   # or detect from file modification date
```

The corresponding check rule would produce an `Info` or `Warning` diagnostic when the declared state document hasn't been modified (by git mtime or by a declared frontmatter field) within the specified window. This is a signal, not a hard error — but it surfaces the kind of "this looks stale" information that today requires manual inspection.

**A lighter alternative, acceptable as a first step:**
`steward orient --signals` could include a "state documents not modified in >60 days" signal without requiring a new rule, using git log to check file modification recency. This would be a configuration-free improvement over the current silence.

---

### 3.6 The planning-index.md Decisions table should be a maintained artifact, not a manual one

**The problem.**
The Decisions section of `docs/planning-index.md` is a Markdown table listing every RFC and ADR with a one-line description. This table is entirely mechanical: every row corresponds to a file in `docs/decisions/rfcs/` or `docs/decisions/adrs/` with a title and purpose. Yet it is maintained manually.

As noted in §3.1, ADR-010 already exists on disk but is missing from this table. This will happen again with every future decision document. Manual maintenance of mechanical content is exactly the class of problem Steward is designed to solve.

**What I want.**
An `index` maintainer type that can scan a directory, extract a heading and a description from each file (using a declared field or the first heading + first paragraph), and generate a Markdown table in a managed region. Concretely:

```yaml
maintenance:
  artifacts:
    - id: decisions-index-section
      path: docs/planning-index.md
      type: managed-section
      target: "heading[Decisions]"
      generator:
        type: directory-index
        sources:
          - pattern: "docs/decisions/rfcs/*.md"
            title_from: heading[1]
            description_from: frontmatter.resolves   # or first paragraph
          - pattern: "docs/decisions/adrs/*.md"
            title_from: heading[1]
            description_from: frontmatter.category
        format: table
        columns: [Document, Purpose]
```

This would allow `steward maintain --apply` to refresh the Decisions table automatically whenever a new RFC or ADR is added, eliminating a manual step that is currently error-prone and invisible to `check`.

The `index` maintainer type already exists in the codebase (per `docs/implementation-status.md`). What's missing is the `directory-index` generator that knows how to read and aggregate content from multiple source files into a table. This is the highest-value maintenance gap for this specific repo.

---

### 3.7 The `check` feedback loop after `maintain --apply` is silent

**The problem.**
When I run `steward check --fix` to resolve a STWD-007 stale-artifact warning, the fix is applied but the output is:

```
Changes applied.
```

There is no indication of what specifically changed. I then need to run `git diff` to understand what was updated. This is a friction point in the maintenance loop that appears repeatedly: check → see stale warning → fix → verify. The verify step requires leaving Steward and using a different tool.

**Why it matters here.**
In this repo, the maintained artifact is `STRUCTURE.md`. When I add a new source file or doc, `check --fix` updates STRUCTURE.md silently. I want to confirm the update was sensible before committing. Currently I cannot do that without `git diff`.

**What I want.**
`steward check --fix` (and `steward maintain --apply`) should print a unified diff of each change it made, the same way `md edit` preview mode does. This is consistent with the "show before commit" safety model already present in the tool:

```
MAINTAIN  structure  STRUCTURE.md
  Added:   └── docs/audits/assessment-coding-agent-usefulness.md
  Added:   └── docs/audits/review-requirements.md
  Added:   └── docs/decisions/adrs/ADR-010-agent-usefulness-improvements.md
```

Even a line-count summary ("STRUCTURE.md: +3 lines") would be a significant improvement over the current silence. A full unified diff (opt-in via `--diff`) would be ideal.

---

### 3.8 Artifact roles carry no semantic weight beyond display

**The problem.**
I declared `docs/audits/repository-audit-2026-04-14.md` with `role: audit`. In the orient output, it displays as `[audit]`. But the role has no behavioral consequence: no rule checks for it, no maintain behavior is triggered by it, and no policy-level expectations are attached.

The same is true of `role: state-document`, `role: build`, `role: generated`, and `role: reference`. These roles classify files for display, but they don't unlock any rule behavior.

**Why it matters here.**
If I declare something as a `state-document`, I expect that Steward can act on that declaration — for example, by surfacing freshness signals (§3.5 above) or by enforcing that state documents have a declared update frequency. Similarly, if I declare something as `generated`, I expect that manually editing it would trigger STWD-006 (or similar), because generated files should not be edited by hand.

**What I want.**
Role-linked behavioral defaults. Roles should be more than display labels. The minimum useful expansion:

- `generated`: files with this role should trigger a `Warning` if they appear in `git diff --staged` without a preceding `steward maintain --apply` in the same session (or alternatively, STWD-007 should apply to any `generated` artifact, not just maintained ones).
- `state-document`: files with this role should participate in freshness checks (§3.5) and should be surfaced in `steward status` with their last-modified date alongside their OK/STALE classification.
- `requirements`: files with this role should be treated as authoritative sources and get a distinct visual prominence in orient beyond just the role label.

I recognize that implementing all of this is substantial work. But the point is that roles should be a foundation for behavior, not just a taxonomy. Today they are only a taxonomy.

---

### 3.9 STWD-008 (broken internal links) coverage in planning-index.md

**The observation.**
`docs/planning-index.md` links to RFC and ADR files using relative paths like `decisions/rfcs/RFC-001-cli-command-structure.md`. STWD-008 checks that internal Markdown links resolve. I trust this is working, but I want to confirm: does STWD-008 resolve these relative to the file's location (giving `docs/decisions/rfcs/RFC-001-...`), or relative to the repo root? The distinction matters because the paths in planning-index.md omit the `docs/` prefix.

During `check`, no STWD-008 warnings fire. Either the links resolve correctly, or the rule is not traversing this file. I cannot easily tell which. This is a confidence gap, not a confirmed bug — but it reflects a broader issue: there is no easy way to ask "which files did STWD-008 actually scan?" without verbose debug output.

**What I want.**
`steward explain STWD-008` (and all rules) should include a "files checked: N" summary when run in verbose mode, so a maintainer can confirm the rule was actually applied to the expected set of files. Currently, the explain output describes the rule but not its actual scope.

---

## 4. Observations on the Policy Model

Having now authored and iterated on `.steward/policy.yaml` for this repo, I have two structural observations about the policy model itself.

**The artifacts list is the right primitive, but its vocabulary is too flat.** Everything is either `required: true` or `required: false`, and roles are display-only. A three-level classification — `required`, `recommended`, and `optional` — with different diagnostic severities (Error vs Warning vs Info) would let me express "this is important but not blocking" without either promoting a file to `required: true` (too strict) or leaving it with no enforcement signal (current behavior).

**The gap between policy.yaml and path-policy.yaml is confusing.** I declare required artifacts in `policy.yaml` and path patterns in `path-policy.yaml`, but the two systems are not clearly integrated. When I add `docs/decisions/rfcs/RFC-001.md` to `policy.yaml` as a required artifact, it validates existence (STWD-001). When I want to enforce a naming pattern across all files in that directory, I need `path-policy.yaml` — but the `check` command behavior for path-policy.yaml rules is only partial. A unified "what is this path supposed to be, and is it?" model would be more maintainable than two parallel systems.

---

## 5. Priority Order

| Priority | Gap | Effort to implement | Maintenance pain without it |
|---|---|---|---|
| 1 | **§3.6** Directory-index maintained section for planning-index.md Decisions table | High | High — ADR-010 already missing; will keep happening |
| 2 | **§3.1** Index-completeness check (STWD-010 or equivalent) | Medium | High — silent drift, no check fires |
| 3 | **§3.3** Scoped frontmatter requirements per path pattern | Medium | Medium — decision lifecycle is invisible to Steward |
| 4 | **§3.7** Post-fix/maintain diff output | Low | Medium — requires `git diff` after every maintain |
| 5 | **§3.2** Naming convention enforcement in path-policy | Medium | Medium — naming drift is silent today |
| 6 | **§3.5** State-document freshness signals | Medium | Medium — stale state docs are invisible to Steward |
| 7 | **§3.4** Per-path rule disablement | Low | Low — can suppress globally for now |
| 8 | **§3.8** Role-linked behavioral defaults | High | Low — roles work as taxonomy today, behavioral upgrade is future value |
| 9 | **§3.9** STWD-008 scope transparency | Low | Low — confidence gap, not confirmed problem |

---

## 6. What I Am Not Requesting

To be explicit about scope: I am not requesting improvements to the agent-facing workflow, search ergonomics, output verbosity, or Markdown editing operations. Those are covered in `assessment-coding-agent-usefulness.md`. This review focuses solely on the maintainer perspective: what I need to express and enforce the governance contract for this specific repository.

I am also not requesting governance rules that would be unrealistic for this repo to maintain. The requests above are scoped to obligations I would actually uphold: a naming convention I already follow, an index I already maintain manually, frontmatter I am ready to add. The goal is to make Steward enforce what the repo already intends, not to impose aspirational discipline that wouldn't survive contact with real development velocity.

---

## 7. Summary

Steward is genuinely useful as the entry point and consistency tool for this repository. The `check + maintain + orient` loop is a real workflow that saves time and prevents drift. The gaps described here are not theoretical — they reflect maintenance situations that arose or will arise in this specific repo.

The single most important improvement is the **directory-index maintained section** (§3.6), because it would convert the planning-index.md Decisions table from a manually maintained artifact (already out of date) into a deterministically generated one. Every other gap on this list is real but manageable without automation. This one is not: ADR-010 is already missing, and the index will continue to drift every time a new decision document is added.

The second most important improvement is the **index-completeness check** (§3.1), because it closes the gap where files can exist in a policy-relevant directory without being referenced in the declared index — and `check` passes silently. The two improvements together would eliminate the most common maintenance failure mode for this repo's decision and planning documentation.
