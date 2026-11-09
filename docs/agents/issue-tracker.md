# Issue tracker: Local Markdown

> Part of the [Agent skills](../../AGENTS.md#agent-skills) configuration for this repository.

Issues and specs for this repo live as markdown files in `.scratch/`. There is no
external tracker; `.scratch/` is committed with the repo. Ticket state is repo state:
the `Status:` line in a ticket file is how the project records what is done, claimed or
waiting, and it travels with the branch that changes it.

This is the same convention the sibling repositories (`graphs-everywhere`, `jvcode`) use,
so the engineering skills (`to-tickets`, `triage`, `to-spec`, `wayfinder`) behave the
same way in all of them.

## Conventions

- One feature or effort per directory: `.scratch/<slug>/`, lower-kebab-case
- Implementation issues are one file per ticket at `.scratch/<slug>/issues/<NN>-<slug>.md`, numbered from `01` — never a single combined tickets file
- Comments and conversation history append to the bottom of the file under a `## Comments` heading
- Use the vocabulary defined in `GLOSSARY.md` for domain terms in titles and bodies (see [domain.md](domain.md))

### Two kinds of directory

A directory is driven by **one** of these, never both:

- **Spec-driven** — a feature that was specified up front. `.scratch/<slug>/spec.md` holds the spec, opening with `# Spec — Title` and carrying its own `Status:` line, and each ticket points back at it.
- **Map-driven** — a multi-session effort whose shape is still being found. `.scratch/<slug>/map.md` opens with `# Map — Title` and holds the Notes / Decisions so far / Fog body, and each ticket points back at it. See [Wayfinding operations](#wayfinding-operations).

Nothing else lives at the root of an effort directory as Markdown. Supporting material that
is standing context rather than frontier belongs in `docs/`, not next to the map.

### Ticket header

Every ticket opens with a `# NN — Title` heading, then these fields, one per line, in this order:

```text
Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: None — can start immediately
Spec: `.scratch/<slug>/spec.md`
```

- **`Status:`** — one of the five triage roles in [triage-labels.md](triage-labels.md), or `claimed` or `done`. One field, one vocabulary; see [Claiming and resolving](#claiming-and-resolving).
- **`Type:`** — `task`, `research`, `prototype` or `grilling`. It says what *kind of work* closes the ticket, and it determines the body shape below. Required on every ticket, not only wayfinder ones.
- **`Category:`** — `bug` or `enhancement`. Every triaged ticket carries exactly one.
- **`Blocked by:`** — the two-digit ticket numbers in the same directory that gate this one (`01, 03`), or exactly `None — can start immediately`.
- **`Spec:`** or **`Map:`** — a backticked path to whichever of the two drives this directory.

The header is plain `Key: value` lines, not YAML frontmatter. That is deliberate: it is the
format the skills write and the sibling repositories' ticket readers parse. Do not convert
tickets to frontmatter to make them easier for steward to validate.

There is no `Priority:` field. Priority is derived, not recorded: what can start now comes
first — unblocked before blocked, then by `Status:` (`ready-for-agent`, `ready-for-human`,
`claimed`, `needs-triage`, `needs-info`), with `done` and `wontfix` last — and ties break by
how many tickets this one unblocks, then by number. Recording a priority would put a
judgment call in every ticket and go stale the moment a blocker closes.

### Two ticket shapes

The `Type:` field picks the shape. Do not mix them.

| `Type:` | Body | Closes with |
| --- | --- | --- |
| `task`, `research`, `prototype` | `## What to build` (or `## The question` for research), then `## Acceptance criteria` as a checklist | The work landing, criteria checked |
| `grilling` | `## The question`, `## Why a human decides this`, `## What is needed to close this` | A decision, recorded as described below |

A `grilling` ticket has no acceptance criteria on purpose: nothing is built, so there is nothing to check off. If you find yourself writing criteria for one, it was the wrong type.

## Where planning lives

`.scratch/` is the only work-item tracker. There is no separate backlog file: a validated but
unscheduled item is a ticket whose effort the [roadmap](../project/roadmap.md) does not name.
The roadmap stays thin — it names the current and next milestone, why they were chosen, and
which effort carries each — and never restates ticket content.

When work is committed to a milestone, update the roadmap to name its effort. When a ticket
lands, close it as described below; `docs/project/status.md` records the resulting
capability, and the CHANGELOG records the release.

## When a skill says "publish to the issue tracker"

Create a new file under `.scratch/<feature-slug>/` (creating the directory if needed).

## When a skill says "fetch the relevant ticket"

Read the file at the referenced path. The user will normally pass the path or the issue number directly.

## Committing tracker files

Commit whenever the work slices cleanly — a spec, a ticket set, or a resolved ticket plus
the change it drove, each as its own self-contained commit. Do not batch unrelated slices,
and do not leave a slice uncommitted waiting for a later one. Commit messages follow the
Conventional Commits rules in the [workflow guide](../project/workflow-guide.md#commit-conventions)
(`docs(...)` or `chore(...)` for tracker-only changes); no WIP commits.

## Claiming and resolving

`Status:` is a single field with a single vocabulary — the five triage roles in
[triage-labels.md](triage-labels.md), plus two more this tracker needs and a label-based
tracker gets for free:

- **`claimed`** — transient; a session is working this ticket right now. Set it and save
  *before* any work, so a second session picks something else.
- **`done`** — terminal; the ticket's work landed. This is the word `/wayfinder` calls
  `resolved`. One terminal success word, spelled the same way everywhere.

`wontfix` is the other terminal value. A ticket file is never deleted on closure: it holds
the reasoning that produced the change, and `.scratch/` is committed precisely so that
reasoning survives.

Closing a ticket:

1. Append the outcome under an `## Answer` heading — for a `grilling` ticket, the decision
   and its reasoning; for the others, what landed.
2. Set `Status: done`.
3. If the directory is map-driven, append a one-line context pointer to the map's
   **Decisions so far**, and remove the matching line from its **Fog**.
4. If the decision was hard to reverse, it belongs in `docs/decisions/` too — the ticket
   records that it was decided, the ADR records why it stays decided.

An effort has no status file of its own. A spec-driven effort's state is its spec's
`Status:` line; a map-driven effort is finished when its Fog is empty and every ticket is
`done` or `wontfix`.

## Wayfinding operations

Used by `/wayfinder` on a map-driven directory.

- **Map**: `.scratch/<effort>/map.md` — the Notes / Decisions so far / Fog body. The map is
  the frontier only; standing context lives in `docs/`. Do not copy content between them.
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, numbered from `01`, with the
  question in the body and the header described above.
- **Blocking**: a ticket is unblocked when every ticket its `Blocked by:` line names is
  `done`.
- **Frontier**: scan `.scratch/<effort>/issues/` for tickets that are unblocked, not
  `claimed`, and in a state an agent may take — `ready-for-agent` for an agent,
  `ready-for-human` for a person. First by number wins.

## How steward governs the tracker

Specs, maps and tickets are **governed artifacts**. They are declared as families in
`.steward/policy.yaml` and are discovered, classified and validated like every other
document in the repo — `steward status` reports them under Artifact Families with match
counts, and `steward check` enforces what the engine can see:

| Family | Matches | Enforced |
| --- | --- | --- |
| `ticket` | `.scratch/*/issues/*.md` | Filename `NN-slug.md` (STWD-016); H1 `NN — Title` with a two-digit number (STWD-019) |
| `effort-spec` | `.scratch/*/spec.md` | H1 `Spec — Title` (STWD-019); `## Problem statement` then `## Solution` (STWD-021) |
| `effort-map` | `.scratch/*/map.md` | H1 `Map — Title` (STWD-019); `## Notes`, `## Decisions so far`, `## Fog`, in that order (STWD-021) |

`.steward/path-policy.yaml` adds the `scratch-tracker-layout` ruleset (STWD-010): the only
Markdown at the root of an effort directory is `spec.md` or `map.md`, and every file under
`issues/` is an `NN-slug.md` ticket. Path rules match filenames, not directory names, so the
lower-kebab-case effort slug is a convention steward does not check.

What steward does **not** validate is the ticket header itself. `Status:`, `Type:`,
`Category:`, `Blocked by:` and `Spec:`/`Map:` are body lines rather than YAML frontmatter,
and `match.frontmatter` is steward's only content-based discriminator. So a family cannot
condition on `Type:`, which means no section schema can require `## Acceptance criteria` of
a `task` ticket while forbidding it on a `grilling` one, and neither the field vocabulary nor
the field order is checked. Nor can steward tell that a directory has both a `spec.md` and a
`map.md`. Those rules are held by review and by the skills that write the files. The gap is
tracked as `.scratch/policy-expressiveness/issues/02-family-matching-on-body-metadata.md`;
it is a product gap to close in steward, not a reason to change the ticket format.

One rule is turned off for this tree:

| File | Entry | Why |
| --- | --- | --- |
| `.steward/policy.yaml` | `validation.path_overrides: ".scratch/**"` disables STWD-013 | Tickets are reached by effort slug, not by navigation from a `start_here` entry. Orphan detection would otherwise fire once per ticket — hundreds of warnings on a real effort, burying the genuine findings |

The structure maintainer excludes `.scratch/*/**` (the contents, not the directory).
`STRUCTURE.md` shows that the tracker exists as one collapsed `.scratch` entry; listing
every effort would churn constantly and orient no one.

`npm run lint:md` covers `.scratch/**/*.md`. The repo config already disables the rules
that would be noisy on working notes (line length, duplicate headings, code-fence
languages), leaving structural hygiene that keeps tickets readable for the next agent. If a
ticket fails the gate, run `npm run lint:md:fix`.

## Relationship to GitHub Issues

This repo has a GitHub remote, but agent workflows do not use GitHub Issues. Do not run
`gh issue create` on behalf of these skills. External pull requests are not part of the
triage queue.
