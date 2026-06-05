---
name: steward-cli
description: Orient, validate, and maintain artifacts in this repository using the Steward CLI. Use when navigating the repo, checking governance, inspecting a file's rules, refreshing generated artifacts, or extracting Markdown content.
---

# SKILL: Using the Steward CLI in This Repository

## Purpose

Use `steward` to orient, inspect governance, validate, and maintain artifacts in this repository using its own `.steward/` contract. This skill covers operating on the repo as a governed repository — not implementing Steward internals.

## When to Use This Skill

Use steward when you need to:

- Get a fast, semantically classified map of the repo before starting work
- Understand what governance rules apply to a file before editing it
- Validate doc, config, or structural changes before finishing
- Refresh maintained artifacts (`STRUCTURE.md`, decision indexes) after structural changes
- Extract Markdown sections or frontmatter from planning, requirements, or README content without hand-parsing
- Check which Markdown files reference a file you are about to move or rename

## When Not to Use This Skill

- Use `dotnet build steward.sln` and `dotnet test steward.sln` for build and test verification. Steward does not replace these.
- Use standard file search and code navigation for implementation work: finding C# symbols, reading test fixtures, investigating build failures.
- Do not run `config suggest` as the default workflow on this repo. The existing config is intentional and richer than the suggestion surface.
- Do not use `search --role` when you need every document in a family such as `docs/requirements/*.md`. In this repo, `search --role` only finds explicit `artifacts[]` role entries, not all family-matched docs. Use glob patterns or `orient --full` instead.

## Repo-Specific Prerequisites

- Run all commands from the repo root so `.steward/` is auto-discovered.
- This repo is the Steward source repo, so you can run the CLI from source:

```bash
dotnet run --project src/Steward.Cli -- <command>
```

- If steward is installed globally or at `.tools/steward/steward`, you can use the bare `steward` command instead.
- This repo uses all three config files: `.steward/config.yaml`, `.steward/policy.yaml`, and `.steward/path-policy.yaml`. Read `.steward/policy.yaml` before making assumptions about what is governed.
- Run `npm ci` when you need the repo-local Markdown lint commands (`npm run lint:md`, `npm run lint:md:fix`).

## How This Repo's Config Shapes Usage

| Config fact | Practical implication |
|-------------|----------------------|
| `profile: software` is active | Profile defaults apply, but `.steward/policy.yaml` is the real contract — read it, not the profile docs |
| `discovery.exclude` removes `node_modules/**` | Installing repo-local npm dependencies for Markdown linting does not pollute steward discovery or coverage |
| `governance.start_here` defines the session-start spine | `orient --signals` surfaces exactly these docs plus core decision roots |
| `coverage.exclude` removes `tests/Steward.TestFixtures/Repos/**` | `status --coverage` numbers reflect the main repo, not embedded fixture repos |
| The only configured maintenance artifact is `STRUCTURE.md` (plus decision indexes in `decision-index.md`) | `maintain` is primarily about keeping those generated files in sync |
| Freshness windows: `implementation-status.md` (30 days), `pre-1-0-readiness-plan.md` (45 days) | STWD-012 fires when these are stale — update them when repo truth changes |
| `docs/planning/**` and `docs/requirements/**` have scoped frontmatter rules | Planning docs require `type` and `status`; requirements docs require `type` and `status` with constrained allowed values |

## Recommended Workflow

### 1. Orient before acting

```bash
dotnet run --project src/Steward.Cli -- orient --signals
```

This surfaces the configured start-here docs, core decision roots, and state indicators. Use `--full --tree` when you need the full classified inventory.

### 2. Check repo state

```bash
dotnet run --project src/Steward.Cli -- status --coverage
```

Shows required and recommended artifacts, freshness state, artifact family counts, and governance coverage.

### 3. Inspect before editing a governed file

```bash
dotnet run --project src/Steward.Cli -- explain path docs/planning/implementation-instructions.md
```

Shows classification, matched path-policy pattern, required frontmatter, and applicable rules. Do this before editing any file under `docs/planning/`, `docs/requirements/`, `docs/decisions/`, or `docs/audits/`.

### 4. Make changes with normal tools

Edit files, write code, update docs — steward does not interfere with your editing workflow.

### 5. Refresh maintained artifacts after structural changes

If you added, moved, or renamed Markdown files, run:

```bash
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply
```

If you added or moved ADRs or RFCs, also run:

```bash
dotnet run --project src/Steward.Cli -- maintain --apply
```

`STRUCTURE.md` and `docs/decisions/decision-index.md` are generated — never hand-edit them.

### 6. Validate before finishing

```bash
dotnet run --project src/Steward.Cli -- check
```

Must exit 0. Fix all errors; review all warnings. If you changed Markdown, also run `npm run lint:md`. If you changed CLI code or tests, also run `dotnet test steward.sln`.

## High-Value Commands for This Repo

### `orient --signals`

Use first for session-start understanding. Surfaces configured start-here docs, decision roots, and state indicators without dumping the full inventory. Use `--full --tree` for the complete classified hierarchy.

```bash
dotnet run --project src/Steward.Cli -- orient --signals
dotnet run --project src/Steward.Cli -- orient --full --tree
```

### `status --coverage`

Use after orientation to see artifact health, freshness, family counts, and coverage.

```bash
dotnet run --project src/Steward.Cli -- status --coverage
dotnet run --project src/Steward.Cli -- status --coverage --output json
```

### `explain path <file>`

Use before editing any governed doc or config path. Shows classification, matched rules, required frontmatter, and any applicable family schema. Explicit artifact declarations take precedence over family classification — some files show artifact info rather than `family:<name>`.

```bash
dotnet run --project src/Steward.Cli -- explain path docs/planning/implementation-instructions.md
dotnet run --project src/Steward.Cli -- explain path docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md
```

### `check`, `check --scope changed`, `check --since <ref>`, and `check --output sarif`

Use `check` as the final governance verification after any change. Use `--scope changed` during iteration to narrow feedback, then run a full `check` before finishing.

`--since <ref>` validates only files changed since the merge-base of `<ref>` and HEAD (three-dot comparison). Use this in CI to enforce policy on exactly the PR diff:

```bash
dotnet run --project src/Steward.Cli -- check --since origin/main
```

`--output sarif` emits SARIF 2.1.0, the format consumed by GitHub Advanced Security for inline PR annotations:

```bash
dotnet run --project src/Steward.Cli -- check --output sarif > results.sarif
```

```bash
dotnet run --project src/Steward.Cli -- check
dotnet run --project src/Steward.Cli -- check --scope changed
dotnet run --project src/Steward.Cli -- check --output json
dotnet run --project src/Steward.Cli -- check --since origin/main
dotnet run --project src/Steward.Cli -- check --since origin/main --output sarif
```

### `config show --effective`, `config validate`, `config doctor`

Use when inspecting or changing `.steward/*`. `config show --effective` prints the merged runtime policy. `config validate` catches syntax and semantic errors. `config doctor` catches valid-but-ineffective declarations (dead start_here entries, unmatched patterns, unreachable families).

Do not treat `config suggest` as authoritative on this repo — the existing policy is richer and intentional.

```bash
dotnet run --project src/Steward.Cli -- config show --effective
dotnet run --project src/Steward.Cli -- config validate
dotnet run --project src/Steward.Cli -- config doctor
```

### `md query`

Use for structured extraction from Markdown when you need exact sections or frontmatter values without writing ad hoc parsers. Selectors are exact — use the visible heading text.

```bash
dotnet run --project src/Steward.Cli -- md query README.md "heading[Commands]"
dotnet run --project src/Steward.Cli -- md query --pattern "docs/planning/*.md" frontmatter.status
dotnet run --project src/Steward.Cli -- md query docs/implementation-status.md "#current-baseline"
```

### `refs <path>`

Use before moving or rewriting a navigation document to see Markdown link impact.

```bash
dotnet run --project src/Steward.Cli -- refs docs/planning-index.md --to
dotnet run --project src/Steward.Cli -- refs README.md --from
```

### `maintain --artifact structure --apply`

Use after adding, removing, or moving repo files so `STRUCTURE.md` stays correct.

```bash
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply
dotnet run --project src/Steward.Cli -- maintain --apply   # all maintained artifacts
```

## Guardrails

- Treat `README.md`, `docs/planning-index.md`, `docs/implementation-status.md`, `docs/planning/implementation-instructions.md`, `docs/requirements/PRD.md`, `docs/decisions/decision-index.md`, and `.steward/policy.yaml` as active repo truth. Treat audit docs as evidence unless a live document explicitly points to them.
- Do not assume older audits describe current behavior. Verify claims against current code, `--help` output, the live config, or passing tests.
- Do not hand-edit `STRUCTURE.md` or `docs/decisions/decision-index.md`.
- Preview before applying mutations. `maintain` previews by default; `check --fix` requires `--apply` to commit fixes.
- Do not force every task through steward. Normal file edits, builds, tests, and code search still belong to standard tools.
- `--json-envelope standard` is not yet applied consistently across all commands. If you get unexpected output on a mutation command, check stderr and exit code, not just the JSON body. This is a known contract gap being addressed in later pre-1.0 milestones.

## Verification Expectations

After `.steward/*` changes:

```bash
dotnet run --project src/Steward.Cli -- config show --effective
dotnet run --project src/Steward.Cli -- config validate
dotnet run --project src/Steward.Cli -- config doctor
dotnet run --project src/Steward.Cli -- check
```

After Markdown or structural changes:

1. Link new files from a governed navigation surface (`docs/planning-index.md` or `docs/decisions/decision-index.md`)
2. Run `maintain --artifact structure --apply` (and `maintain --apply` for decision indexes)
3. Run `npm run lint:md`
4. Run `check`

After C# source changes:

```bash
dotnet test steward.sln
dotnet run --project src/Steward.Cli -- check
```

## Example Flows

### Start a session in this repo

```bash
dotnet run --project src/Steward.Cli -- orient --signals
dotnet run --project src/Steward.Cli -- status --coverage
```

Then read the start-here docs that orient surfaced.

### Add a new planning document

```bash
# 1. Check naming expectations
dotnet run --project src/Steward.Cli -- explain path docs/planning/my-new-doc.md

# 2. Create the file with required frontmatter (type: planning, status: Draft)
# 3. Link it from docs/planning-index.md
# 4. Refresh structure
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply

# 5. Validate
dotnet run --project src/Steward.Cli -- check
```

### Add a new ADR or RFC

```bash
# 1. Check naming pattern (ADR-NNN-lower-kebab.md or RFC-NNN-lower-kebab.md)
dotnet run --project src/Steward.Cli -- explain path docs/decisions/adrs/ADR-015-my-decision.md

# 2. Create the file with required frontmatter (type, status, category for ADRs; type, status, resolves for RFCs)
# 3. Refresh decision index and structure
dotnet run --project src/Steward.Cli -- maintain --apply
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply

# 4. Validate
dotnet run --project src/Steward.Cli -- check
```

### Inspect governance before editing policy config

```bash
dotnet run --project src/Steward.Cli -- config show --effective
dotnet run --project src/Steward.Cli -- config validate
dotnet run --project src/Steward.Cli -- config doctor
```

### Move or rename a doc

```bash
# See what links to it before moving
dotnet run --project src/Steward.Cli -- refs docs/planning/old-name.md --to

# Preview the move
dotnet run --project src/Steward.Cli -- refactor move docs/planning/old-name.md docs/planning/new-name.md --preview

# Apply
dotnet run --project src/Steward.Cli -- refactor move docs/planning/old-name.md docs/planning/new-name.md --apply
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply
dotnet run --project src/Steward.Cli -- check
```

## References

- [AGENTS.md](../../../AGENTS.md) — repo-level agent guidance and source-of-truth precedence
- [README.md](../../../README.md) — product overview, full command reference, config model
- [CONTRIBUTING.md](../../../CONTRIBUTING.md) — contributor workflow and PR expectations
- [.steward/policy.yaml](../../../.steward/policy.yaml) — enforced repo contract
- [.steward/path-policy.yaml](../../../.steward/path-policy.yaml) — naming and path rules
- [docs/planning-index.md](../../../docs/planning-index.md) — navigation hub
- [docs/implementation-status.md](../../../docs/implementation-status.md) — current version truth
