---
name: steward-repo-usage
description: Use when working inside the Steward repository and you need to use the repo's configured steward CLI as the primary surface for orientation, governance inspection, markdown-aware repo analysis, validation, or structure maintenance. Covers repo-local invocation, repo-specific start-here docs, policy-driven expectations, high-value commands, and guardrails such as refreshing STRUCTURE.md after structural changes and not relying on search --role for family-matched docs.
---

# SKILL: Using Steward for This Repository

## Purpose

Use `steward` to understand and validate this repository through its own `.steward/` contract. This skill is for operating on the repo as a governed repository; it is not a guide to implementing Steward internals.

## When to use this skill

- Start a session in this repo and need the fastest repo-specific map.
- Inspect why a document or path is governed a certain way before editing it.
- Validate repo-oriented changes to docs, links, structure, or `.steward/` config.
- Refresh maintained artifacts after structural changes.
- Extract Markdown sections or frontmatter from planning, requirements, or README content without hand parsing.

## When not to use this skill

- Use shell tools and source inspection for low-level code debugging, build failures, or broad code search.
- Use `dotnet build` and `dotnet test steward.sln` for implementation verification; Steward does not replace build and test.
- Do not use `config suggest` as the default workflow on this repo; the existing config is intentional and richer than the suggestion surface.
- Do not use `search --role` when you need every document in a family such as `docs/requirements/*.md`; in this repo it only searches explicit `artifacts[]` role entries, not all family-matched docs.

## Repo-specific assumptions and prerequisites

- Run commands from the repo root so `.steward/` is auto-discovered.
- Prefer repo-local invocation so the command surface matches the current source tree:

```bash
dotnet run --project src/Steward.Cli -- <command>
```

- If `steward` is installed globally, you can substitute `steward` for the prefix.
- This repo uses all three Steward config files: `.steward/config.yaml`, `.steward/policy.yaml`, and `.steward/path-policy.yaml`.

## How this repo's Steward config shapes usage

- `profile: software` is active, but `.steward/policy.yaml` is the real repository contract. Read it before assuming profile defaults.
- `governance.start_here` defines the session-start spine: `README.md`, `docs/planning-index.md`, `docs/implementation-status.md`, `docs/planning/implementation-instructions.md`, `docs/requirements/PRD.md`, and `steward.sln`.
- Required artifacts are the repo's onboarding and truth spine. Recommended artifacts include `.steward/policy.yaml`, requirements traceability, milestone and readiness plans, and `STRUCTURE.md`.
- State freshness matters. `docs/implementation-status.md` has a 30-day freshness window and `docs/planning/pre-1-0-readiness-plan.md` has a 45-day window.
- Artifact families classify recurring docs: ADRs, RFCs, planning docs, requirements support docs, and audits. `orient`, `status`, and `explain path` surface this classification.
- Path-scoped frontmatter rules still matter for `docs/planning/**` and `docs/requirements/**`. ADR and RFC families add their own frontmatter expectations.
- Path-policy rules enforce ADR and RFC filenames and lower-kebab naming for planning and audit docs.
- `coverage.exclude` removes `tests/Steward.TestFixtures/Repos/**` from governance coverage, so `status --coverage` reflects the main repo rather than embedded fixture repos.
- The only configured maintenance artifact is `STRUCTURE.md`. In this repo, `maintain` is primarily about keeping that generated structure document in sync.

## Recommended workflow

1. Start with a curated map:

```bash
dotnet run --project src/Steward.Cli -- orient --signals
```

2. Get the repo contract summary:

```bash
dotnet run --project src/Steward.Cli -- status --coverage
```

3. Before editing a governed doc or config path, inspect its effective governance:

```bash
dotnet run --project src/Steward.Cli -- explain path docs/planning/implementation-instructions.md
```

4. Make the actual code, doc, or config changes with normal tools.

5. If you changed repo structure or added or moved Markdown files, refresh the generated map:

```bash
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply
```

6. Validate before finishing:

```bash
dotnet run --project src/Steward.Cli -- check
```

7. If you changed CLI code or tests, also run:

```bash
dotnet test steward.sln
```

## High-value Steward commands for this repo

### `orient --signals`

Use this first for session-start understanding. In text mode, `orient` is now already compact by default, so this surfaces the configured start-here docs, core decision roots, state docs, and solution entry point without dumping the whole inventory.

Caveat: use `--full` when you need the full classified inventory, and `--tree` when you want actual hierarchy instead of a flat path list.

```bash
dotnet run --project src/Steward.Cli -- orient --signals
dotnet run --project src/Steward.Cli -- orient --full --tree
```

### `status --coverage`

Use this after orientation to see required and recommended artifacts, state-document freshness, artifact-family counts, and governance coverage.

Caveat: this repo already excludes fixture repos from coverage, so the numbers are about the main repo, not test fixtures.

```bash
dotnet run --project src/Steward.Cli -- status --coverage --output json
```

### `explain path <file>`

Use this before editing a doc or config file to see classification, matched path-policy pattern, required frontmatter, and applicable rules.

Caveat: explicit artifact declarations take precedence over family classification, so some files show artifact info rather than `family:<name>`.

```bash
dotnet run --project src/Steward.Cli -- explain path docs/planning/implementation-instructions.md
```

### `config show --effective`, `config validate`, `config doctor`

Use these when changing `.steward/*` so you inspect the merged profile plus repo policy, verify syntax and semantics, and catch declarations that are valid but ineffective.

Caveat: `config suggest` is useful only as secondary input here; do not treat it as authoritative over the existing policy.

```bash
dotnet run --project src/Steward.Cli -- config show --effective
dotnet run --project src/Steward.Cli -- config validate
dotnet run --project src/Steward.Cli -- config doctor
```

### `md query`

Use this for structured extraction from Markdown when you need exact sections or frontmatter values without writing ad hoc parsers.

Caveat: selectors are exact. Use the visible heading text, not a fuzzy match.

```bash
dotnet run --project src/Steward.Cli -- md query README.md "heading[Using Steward In This Repo]"
dotnet run --project src/Steward.Cli -- md query --pattern "docs/planning/*.md" frontmatter.status
```

### `refs <path>`

Use this before moving or rewriting a navigation document to inspect Markdown link impact.

Caveat: `refs` reports Markdown references; it does not explain governance or artifact-family rules.

```bash
dotnet run --project src/Steward.Cli -- refs docs/planning-index.md --to
```

### `maintain --artifact structure --apply`

Use this after adding, removing, or moving repo files so `STRUCTURE.md` stays correct.

Caveat: `STRUCTURE.md` is generated. Do not hand-edit it.

```bash
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply
```

### `check`

Use this as the final repo-governance verification after doc, config, or structural changes.

Caveat: prefer full `check` before you stop. `--scope changed` is useful during iteration, but it is intentionally narrower than a full repo pass.

```bash
dotnet run --project src/Steward.Cli -- check
```

## Guardrails

- Treat `README.md`, `docs/planning-index.md`, `docs/implementation-status.md`, `docs/planning/implementation-instructions.md`, `docs/requirements/PRD.md`, `docs/decisions/decision-index.md`, and `.steward/policy.yaml` as active repo truth. Treat audit docs as evidence and historical analysis unless an active doc points you there.
- Do not assume roadmap items or older audits describe current behavior. Verify against `--help`, the current config, or tests.
- Do not hand-edit `STRUCTURE.md`.
- Do not treat `search --role` as a complete family-aware search surface in this repo.
- Do not force every repo task through Steward. Normal file edits, builds, tests, and raw code search still belong to standard tools.
- Review preview or diff output before applying mutations. `maintain` previews by default, and `check --fix` requires `--apply` to commit fixes.

## Verification expectations

- After `.steward/*` changes, run `config show --effective`, `config validate`, `config doctor`, and then `check`.
- After Markdown or structure changes, make sure the new file is linked from a governed navigation surface, run `maintain --artifact structure --apply`, then run `check`.
- After link-heavy doc edits, use `refs` on affected navigation docs if you need impact detail, then rely on `check` for broken-link validation.
- After C# changes, run `dotnet test steward.sln`.
- Review diffs for `.steward/*`, `README.md`, `STRUCTURE.md`, and any planning or requirements docs you touched.

## Examples

### Understand the repo before editing policy

```bash
dotnet run --project src/Steward.Cli -- orient --signals
dotnet run --project src/Steward.Cli -- status --coverage
dotnet run --project src/Steward.Cli -- config show --effective
```

### Check how a planning doc is governed before editing it

```bash
dotnet run --project src/Steward.Cli -- explain path docs/planning/implementation-instructions.md
dotnet run --project src/Steward.Cli -- md query --pattern "docs/planning/*.md" frontmatter.status
```

### Add a new repo-facing Markdown file such as `SKILL.md`

```bash
# add the file
# link it from README.md or another governed navigation surface so it is not orphaned
dotnet run --project src/Steward.Cli -- maintain --artifact structure --apply
dotnet run --project src/Steward.Cli -- check
```

## References

- `README.md`
- `.steward/config.yaml`
- `.steward/policy.yaml`
- `.steward/path-policy.yaml`
- `docs/planning-index.md`
- `docs/implementation-status.md`
- `docs/planning/implementation-instructions.md`
- `docs/requirements/PRD.md`
- `docs/decisions/decision-index.md`
- `dotnet run --project src/Steward.Cli -- --help`
