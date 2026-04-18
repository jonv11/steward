# Agent Guidance — Steward Repository

This file is the primary entry point for coding agents working in this repository. Read it before making any changes.

## What This Repo Is

Steward is a configurable repository stewardship CLI for humans and AI agents. It validates repository structure against policy, orients contributors to repo content, maintains generated artifacts, and performs Markdown-aware structural editing. The binary is `steward`.

This repo is both the source implementation of the tool and its own self-dogfooded governed repository. That means:
- The source code is under `src/` and `tests/`.
- The repo uses its own `.steward/` config to enforce governance on its own docs, plans, decisions, and artifacts.
- Changes to the CLI must also pass the repo's own steward governance checks.

Current version: `v0.15.0` (pre-`1.0.0`).

## What Agents Do Here

There are two categories of work in this repo:

### Artifact work

Creating or updating repository artifacts: requirements, planning docs, PRDs, ADRs, RFCs, audits, reviews, release readiness assessments, status docs, implementation instructions, and decision indexes. This includes extracting backlog items from specs, writing up review findings, maintaining cross-references, and keeping planning artifacts current with code reality.

### Implementation work

Implementing features based on repo documents, fixing bugs in the CLI or core library, improving docs, keeping artifacts in sync with code, preparing commits with conventional commit messages, and handling version or release work when explicitly requested.

Both categories use the steward CLI as a navigation and validation surface. See [.agents/skills/steward-cli/SKILL.md](.agents/skills/steward-cli/SKILL.md) for how to use it effectively in this repo.

## What to Read First

Read these in order before starting any substantive work:

| Document | Why |
|----------|-----|
| [README.md](README.md) | Product overview, commands, config model, exit codes, contributor path |
| [docs/planning-index.md](docs/planning-index.md) | Central navigation for all planning, decisions, requirements, and audit docs |
| [docs/implementation-status.md](docs/implementation-status.md) | Current version baseline, delivered scope, remaining pre-1.0 gaps |
| [docs/planning/implementation-instructions.md](docs/planning/implementation-instructions.md) | Active contributor execution guide and next-step priorities |
| [docs/requirements/PRD.md](docs/requirements/PRD.md) | Canonical product requirements and design principles |

Open `steward.sln` when you are ready to enter the code. If you are changing repo governance or `.steward/` config, inspect `.steward/policy.yaml` next.

## Source-of-Truth Precedence

When documents disagree, trust in this order:

1. **`.steward/policy.yaml`** — the enforced repo contract for artifact governance
2. **`docs/implementation-status.md`** — current version truth and delivered scope
3. **`docs/planning/implementation-instructions.md`** — active contributor execution order
4. **`docs/requirements/PRD.md`** — product intent and design principles
5. **Audit docs** — evidence and historical analysis; not current truth unless a live document explicitly points to them
6. **`README.md`** — end-user-facing; kept consistent with the above but written for external consumers

Older audits describe past states. Verify claims against current code, tests, or the live documents listed above.

## Artifact Work Expectations

When creating or updating repository artifacts:

- **Naming and location**: Follow the conventions in `.steward/path-policy.yaml`. Planning docs use `lower-kebab-case.md`. ADRs use `ADR-NNN-lower-kebab.md`. RFCs use `RFC-NNN-lower-kebab.md`. Audit docs use `lower-kebab-case[-YYYY-MM-DD].md`.
- **Frontmatter**: Artifacts in `docs/planning/`, `docs/requirements/`, `docs/decisions/adrs/`, and `docs/decisions/rfcs/` require specific frontmatter fields. Run `steward explain path <file>` on any governed path before editing to see what is required.
- **Navigation**: New documents must be reachable from a governed navigation surface — typically `docs/planning-index.md` or `docs/decisions/decision-index.md`. Orphaned files violate STWD-013.
- **Freshness**: `docs/implementation-status.md` has a 30-day freshness window. `docs/planning/pre-1-0-readiness-plan.md` has a 45-day window. Update them when repo truth changes.
- **Regenerated artifacts**: `STRUCTURE.md` is generated. Never hand-edit it. Run `steward maintain --artifact structure --apply` after adding, moving, or removing files.
- **Decision indexes**: `docs/decisions/decision-index.md` is maintained by steward. Run `steward maintain --apply` after adding ADRs or RFCs.
- **Validation**: Run `steward check` before finishing any artifact work. Fix all errors and review all warnings.

## Implementation Work Expectations

When implementing features, fixing bugs, or improving docs:

- **Build**: `dotnet build steward.sln`
- **Test**: `dotnet test steward.sln` — all 644 tests must pass (450 core, 194 CLI)
- **Validate**: `steward check` — must exit 0
- **Scope**: One logical change per PR. Do not bundle unrelated fixes.
- **Changelog**: Add an entry to `CHANGELOG.md` under the appropriate version heading.
- **Versioning**: Do not bump the version casually. Follow [ADR-013](docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md). The current pre-1.0 line is `0.x.y` only. `v1.0.0` requires explicit authorization.
- **Artifact sync**: When implementation changes make a planning or status doc stale, update the doc in the same change. Documentation drift is a defect.

## Commit Expectations

Use [Conventional Commits](https://www.conventionalcommits.org/) for all commits:

```
feat: add heading fuzzy matching to MdPath
fix: correct STWD-009 false positive on scoped check
docs: update implementation-status to reflect v0.15.0 delivery
chore: refresh STRUCTURE.md after planning doc restructure
test: add coverage for RFC family frontmatter validation
```

Common types: `feat`, `fix`, `docs`, `test`, `chore`, `refactor`, `ci`.

Breaking changes use a `!` suffix or a `BREAKING CHANGE:` footer.

## Release and Version Work

Only perform release or version work when explicitly requested. The release process is documented in [docs/planning/release-process.md](docs/planning/release-process.md) and [docs/planning/release-publication-checklist.md](docs/planning/release-publication-checklist.md). Release operations are tag-driven via GitHub Actions.

## Using the Steward CLI in This Repo

When orientation, validation, artifact inspection, or Markdown structural work is relevant to your task, use the steward CLI.

See [.agents/skills/steward-cli/SKILL.md](.agents/skills/steward-cli/SKILL.md) for:
- when to use and when to skip the CLI
- the recommended workflow for this repo specifically
- high-value commands and their caveats
- guardrails against noisy or harmful edits
- verification expectations before finishing work

## Project Layout

```
src/Steward.Cli/       CLI entry point and commands
src/Steward.Core/      Core library: validation, Markdown, maintenance, orientation
tests/                 Core and CLI test suites, shared fixtures
docs/                  Planning, requirements, decisions, audits, reviews
.steward/              Repo-local steward governance config
.agents/               Agent guidance and skills
scripts/release/       Release asset scripts
```

## Conventions Not to Break

- Do not hand-edit `STRUCTURE.md` or `docs/decisions/decision-index.md` — they are managed by steward.
- Do not introduce new `1.x` version references in active docs or metadata.
- Do not skip `steward check` before finishing any change.
- Do not treat `search --role` as a complete family-aware search; in this repo it only finds explicit `artifacts[]` role entries, not family-matched docs.
- Do not commit to version bump, tag, or publish without following the release process.
