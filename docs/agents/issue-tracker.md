# Issue tracker: Local Markdown

> Part of the [Agent skills](../../AGENTS.md#agent-skills) configuration for this repository.

Issues and specs for this repo live as markdown files under `.scratch/`, one directory per effort.

`.scratch/` is **tracked repository content**, committed like any other source. Ticket state is repo state: the `Status:` line in a ticket file is how the project records what is done, claimed, or waiting, and it travels with the branch that changes it. Commit ticket updates alongside the work they describe.

Tickets, specs, maps and effort records are **governed artifacts**. They are declared as families in `.steward/policy.yaml` and are discovered, classified and validated like every other document in the repo — `steward status` reports them under Artifact Families with match counts.

| Family | Matches | Enforced |
| --- | --- | --- |
| `ticket` | `.scratch/*/issues/*.md` | Filename `NN-slug.md` (STWD-016) and an H1 of the form `NN — Title` (STWD-019) |
| `effort` | `.scratch/*/effort.md` | Classification only |
| `effort-spec` | `.scratch/*/spec.md` | Classification only |
| `effort-map` | `.scratch/*/map.md` | Classification only |

What steward does **not** validate is the metadata itself. `Status:`, `Type:`, `Priority:` and `Blocked by:` are body lines rather than YAML frontmatter, and `match.frontmatter` is steward's only content-based discriminator — so a family cannot condition on `Type:` and a section schema cannot require `## Acceptance criteria` of task tickets while exempting research tickets. Declaring the sections as all-optional would enforce nothing, so no section schema is declared. See the backlog entry on family matching for non-YAML metadata.

Keeping the metadata as body lines is deliberate, not an oversight: it is the format the skills write and the local-markdown ticket readers parse. Do not convert tickets to frontmatter to make them easier for steward to validate.

One rule is turned off for this tree:

| File | Entry | Why |
| --- | --- | --- |
| `.steward/policy.yaml` | `validation.path_overrides: ".scratch/**"` disables STWD-013 | Tickets are reached by effort slug, not by navigation from a `start_here` entry. Orphan detection would otherwise fire once per ticket — hundreds of warnings on a real effort, burying the genuine findings |

The structure maintainer excludes `.scratch/*/**` (the contents, not the directory). `STRUCTURE.md` shows that the tracker exists as one collapsed `.scratch` entry; listing every effort would churn constantly and orient no one.

`npm run lint:md` covers `.scratch/**/*.md`. The repo config already disables the rules that would be noisy on working notes (line length, duplicate headings, code-fence languages), leaving structural hygiene that keeps tickets readable for the next agent. If a ticket fails the gate, run `npm run lint:md:fix`.

## Conventions

One directory per effort: `.scratch/<effort-slug>/`, lower-kebab-case.

| Path | Role |
| --- | --- |
| `<effort>/spec.md` | The specification the effort implements |
| `<effort>/map.md` | Wayfinding state: Notes, Decisions-so-far, Fog |
| `<effort>/effort.md` | Effort-level `Status:` — `active`, `paused`, `done`, or `abandoned`. Absent means `active` |
| `<effort>/issues/NN-<slug>.md` | One file per ticket, numbered from `01`, never a single combined tickets file |

A ticket carries its metadata as plain `Key: value` lines directly under the `# NN — Title` heading, not as YAML frontmatter:

- `Status:` — triage state; see [triage-labels.md](triage-labels.md) for the vocabulary
- `Type:` — `task`, `bug`, `research`, `prototype`, or `grilling`
- `Priority:` — integer, lower first
- `Blocked by:` — ticket numbers, or an explicit `None — can start immediately`

The body states what to build, followed by `## Acceptance criteria` as a checklist and `## Out of scope` pointing at the tickets that cover the excluded parts. Comments and conversation history append at the bottom under `## Comments`.

## When a skill says "publish to the issue tracker"

Create a new file under `.scratch/<effort-slug>/` (creating the directory if needed).

## When a skill says "fetch the relevant ticket"

Read the file at the referenced path. The user will normally pass the path or the ticket number directly.

## Wayfinding operations

Used by `/wayfinder`. The **map** is a file with one **child** file per ticket.

- **Map**: `.scratch/<effort>/map.md` (the Notes / Decisions-so-far / Fog body).
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, with the question in the body.
- **Blocking**: the `Blocked by:` line. A ticket is unblocked when every ticket it lists is resolved.
- **Frontier**: scan `.scratch/<effort>/issues/` for tickets that are open, unblocked, and unclaimed; first by number wins.
- **Claim**: set `Status: claimed` and save before any work.
- **Resolve**: append the answer under an `## Answer` heading, set `Status: resolved`, then append a context pointer (gist + link) to the map's Decisions-so-far in `map.md`.

## Relationship to GitHub Issues

This repo has a GitHub remote, but agent workflows do not use GitHub Issues. Do not run `gh issue create` on behalf of these skills. External pull requests are not part of the triage queue.
