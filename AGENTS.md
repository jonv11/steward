# Agent Guidance — Steward Repository

> **Scope note:** This file is for AI agents contributing to the Steward source code repository. If you are looking for guidance on using Steward as a tool in your own repository's agent workflows, see the [Agent Integration Guide](docs/guide/agent-integration.md).

This file is the primary entry point for coding agents working in this repository. Read it before making any changes.

## What This Repo Is

Steward is a configurable repository stewardship CLI for humans and AI agents. It validates repository structure against policy, orients contributors to repo content, maintains generated artifacts, and performs Markdown-aware structural editing. The binary is `steward`.

This repo is both the source implementation of the tool and its own self-dogfooded governed repository. That means:

- The source code is under `src/` and `tests/`.
- The repo uses its own `.steward/` config to enforce governance on its own docs, plans, decisions, and artifacts.
- Changes to the CLI must also pass the repo's own steward governance checks.

Latest published release: `v0.18.1` (pre-`1.0.0`).

## What Agents Do Here

There are two categories of work in this repo:

### Artifact work

Creating or updating repository artifacts: requirements, planning docs, PRDs, ADRs, RFCs, audits, reviews, release readiness assessments, status docs, implementation instructions, and decision indexes. This includes extracting backlog items from specs, writing up review findings, maintaining cross-references, and keeping planning artifacts current with code reality.

### Implementation work

Implementing features based on repo documents, fixing bugs in the CLI or core library, improving docs, keeping artifacts in sync with code, preparing commits with conventional commit messages, and handling version or release work when explicitly requested.

Both categories use the steward CLI as a navigation and validation surface. See [.agents/skills/steward-self-cli/SKILL.md](.agents/skills/steward-self-cli/SKILL.md) for how to use it effectively in this repo.

## What to Read First

Read these in order before starting any substantive work:

| Document | Why |
|----------|-----|
| [README.md](README.md) | Product overview, commands, config model, exit codes, contributor path |
| [docs/README.md](docs/README.md) | Selective landing page for user docs, project docs, decisions, requirements, and history |
| [docs/project/status.md](docs/project/status.md) | Current implemented capabilities, repository health, and known gaps |
| [docs/project/roadmap.md](docs/project/roadmap.md) | Current milestone and next milestone only |
| [docs/project/workflow-guide.md](docs/project/workflow-guide.md) | Canonical contributor and agent execution workflow |
| [docs/requirements/PRD.md](docs/requirements/PRD.md) | Canonical product requirements and design principles |

Open `steward.sln` when you are ready to enter the code. If you are changing repo governance or `.steward/` config, inspect `.steward/policy.yaml` next.

## Source-of-Truth Precedence

When documents disagree, trust in this order:

1. **`.steward/policy.yaml`** — the enforced repo contract for artifact governance
2. **`docs/project/status.md`** — current implemented capability and repository-health truth
3. **`docs/project/roadmap.md`** — current and next milestone scope
4. **`.scratch/`** — every work item as efforts and tickets; see [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md)
5. **`docs/requirements/PRD.md`** — product intent and design principles
6. **`docs/decisions/README.md` and decision records** — accepted design authority
7. **`docs/history/**`** — point-in-time evidence, never current truth
8. **`README.md`** — end-user-facing summary kept consistent with the above

Older audits describe past states. Verify claims against current code, tests, or the live documents listed above.

## How to Work in This Repo

The canonical workflow reference is [docs/project/workflow-guide.md](docs/project/workflow-guide.md). It defines the expected process for every category of work — features, bug fixes, documentation, reviews, releases, governance changes, and more.

Key points for agents:

- **Identify the right workflow** for your change using the [workflow selection guide](docs/project/workflow-guide.md#workflow-selection-guide).
- **Follow the steps** in order, including the [shared finalization checklist](docs/project/workflow-guide.md#shared-finalization-checklist).
- **Use Conventional Commits** with specific, descriptive messages. See [commit conventions](docs/project/workflow-guide.md#commit-conventions).
- **One logical change per commit.** Do not bundle unrelated work.
- **Update affected docs** in the same change. Documentation drift is a defect.
- **Do not bump versions casually.** Follow [ADR-013](docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).
- **Only perform release work when explicitly requested.** See [release execution workflow](docs/project/workflow-guide.md#6-release-execution).

## Using the Steward CLI in This Repo

When orientation, validation, artifact inspection, or Markdown structural work is relevant to your task, use the steward CLI.

See [.agents/skills/steward-self-cli/SKILL.md](.agents/skills/steward-self-cli/SKILL.md) for:

- when to use and when to skip the CLI
- the recommended workflow for this repo specifically
- high-value commands and their caveats
- guardrails against noisy or harmful edits
- verification expectations before finishing work

### Portable Steward CLI skills

This repo also authors the general-purpose skills that teach *any* repository's agents how to use `steward` as a product — not specific to this repo, and usable once copied into another repo's `.agents/skills/`:

- [.agents/skills/steward-cli/SKILL.md](.agents/skills/steward-cli/SKILL.md) — install and orientation entry point
- [.agents/skills/setup-steward/SKILL.md](.agents/skills/setup-steward/SKILL.md) — guided first-time setup: investigate, interview, dry-run, then write `.steward/` (user-invoked)
- [.agents/skills/steward-cli-maintainer/SKILL.md](.agents/skills/steward-cli-maintainer/SKILL.md) — configuring `.steward/` governance
- [.agents/skills/steward-cli-contributor/SKILL.md](.agents/skills/steward-cli-contributor/SKILL.md) — validating changes against an existing policy
- [.agents/skills/steward-cli-agent/SKILL.md](.agents/skills/steward-cli-agent/SKILL.md) — automated validation loops, JSON/SARIF, structural Markdown editing

Do not confuse these with `steward-self-cli` above, which is specific to working in this source repo.

## Project Layout

```
src/Steward.Cli/       CLI entry point and commands
src/Steward.Core/      Core library: validation, Markdown, maintenance, orientation
tests/                 Core and CLI test suites, shared fixtures
docs/                  User guides, active project truth, decisions, requirements, and history
.steward/              Repo-local steward governance config
.agents/               Agent guidance and skills
scripts/release/       Release asset scripts
```

## Conventions Not to Break

- Do not hand-edit `STRUCTURE.md` or the managed sections in `docs/decisions/README.md` — they are managed by steward.
- Do not introduce new `1.x` version references in active docs or metadata.
- Do not skip `npm run lint:md` when you modify Markdown or workflow docs.
- Do not skip `steward check` before finishing any change.
- Do not treat `search --role` as a complete family-aware search; in this repo it only finds explicit `artifacts[]` role entries, not family-matched docs.
- Do not leave `.scratch/` ticket state uncommitted. Tickets are tracked repo content; commit `Status:` changes alongside the work they describe. See [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md).
- Do not add a `Priority:` field or an `effort.md` file to `.scratch/`. Priority is derived from status and blocking, and an effort's state lives in its `spec.md` or `map.md`.
- Do not commit to version bump, tag, or publish without following the release process.

## Agent skills

Configuration consumed by the installed engineering skills (`to-tickets`, `triage`, `to-spec`, `wayfinder`, `domain-modeling`, and friends).

### Issue tracker

Issues and specs live as local markdown under `.scratch/<effort-slug>/`, the same convention as the sibling `graphs-everywhere`, `jvcode` and `tracker-cli` repositories: each directory is driven by exactly one `spec.md` or `map.md`, with one `issues/NN-<slug>.md` file per ticket whose header is `Status:`, `Type:`, `Category:`, `Blocked by:` and a `Spec:` or `Map:` pointer. Commit each cleanly sliced change. The `ticket`, `effort-spec` and `effort-map` families in `.steward/policy.yaml` validate what steward can see; this repo does not use GitHub Issues for agent workflows. See [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md).

### Triage labels

The five canonical roles, unchanged (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`), written on a `Status:` line, plus `claimed` and `done`, which a markdown tracker has to spell for itself. `Category:` carries `bug` or `enhancement`. See [docs/agents/triage-labels.md](docs/agents/triage-labels.md).

### Domain docs

Single-context: one `GLOSSARY.md` at the repo root, with decisions in `docs/decisions/`. See [docs/agents/domain.md](docs/agents/domain.md).
