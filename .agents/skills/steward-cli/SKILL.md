---
name: steward-cli
description: Use the Steward CLI (`steward`) in any repository — validate documentation structure and governance policy against a `.steward/` contract, fix diagnostics, keep generated artifacts current, edit Markdown structurally, and drive it from an agent loop or CI with JSON/SARIF output. Use when a repo has (or should have) a `.steward/` directory, when `steward check` fails, when asked what an STWD-* rule means, when changing `.steward/` rules, or when wiring Steward into CI or automation. For a guided first-time setup use setup-steward; for the Steward source repository itself use steward-self-cli.
---

# Steward CLI

Steward validates a repository's documentation structure, naming and artifact policy against a
declarative `.steward/` contract, and regenerates structure trees, indexes and managed sections
deterministically. It never makes network calls: same input, same output.

This skill tells you when to reach for which command and what not to do. The tool documents
itself; read its help and the guides below rather than trusting a memorised table.

## Install

Requires the **.NET 10 SDK** (`dotnet --version`).

```bash
dotnet tool install --global Steward        # latest release from NuGet
```

Self-contained bundles are on the [releases page](https://github.com/jonv11/steward/releases).
Never run Steward with `dotnet run --project <steward-checkout>` from inside another repository:
that repository's `global.json` can pin a different SDK and break the run. Use the installed tool.

## Read the contract live

```bash
steward --help                               # every command; `steward <command> --help` for its options
steward explain                              # every rule; `explain <rule-id>` for one
steward explain path <file>                  # the rules, frontmatter and naming that apply to one file
steward config show --effective              # the resolved config and merged policy
steward schema [check|common|...]            # JSON Schemas for --output json
```

## The loop: orient → inspect → change → check → fix → re-check

```bash
steward orient [--signals]                   # what the repo holds and where to start (no config needed)
steward explain path docs/x.md               # before editing a governed file
steward check --scope staged                 # before a commit: exactly what you are committing
steward check --since origin/main            # a branch, merge-base aware (PR gates)
steward check --fix --apply                  # auto-fix STWD-003, -007, -012, -018; the rest need an edit
steward maintain --apply                     # after adding, moving or renaming files
```

For automation add `--output json` and parse `data.diagnostics` (each has `ruleId`, `severity`
`error|warn|info`, `path`, `message`, `remediation`); validate against `steward schema check`.
`check --output sarif` feeds code-scanning annotations. Exit codes: `0` no error-severity
diagnostic (warnings may remain), `1` at least one error, `2` usage, configuration or I/O error,
`3` internal error — surface it, don't retry.

Edit Markdown with `steward md query|outline|edit|split` instead of regex: every `md edit`
operation previews until you pass `--apply`, and heading selectors match exactly, so run
`steward md outline <file>` first. Before moving a file, `steward refs <path>`; to move it with
its links, `steward refactor move <old> <new> --apply`.

## Guardrails

- **Exit `0` is not "no diagnostics".** Most rules default to `warning`. To make one fail CI, raise
  it under `validation.severity_overrides` in `policy.yaml`; branching on the exit code alone
  ignores warnings.
- **Never hand-edit generated output** (`STRUCTURE.md`, managed sections). It is flagged stale
  (STWD-007) and overwritten by the next `maintain --apply`.
- **Change rules, not files, only when you own the policy.** A contributor satisfies `.steward/`;
  a maintainer changes it. After any `.steward/` edit run `steward config validate` (what is
  wrong) and `steward config doctor` (what silently matches nothing).
- **`config suggest` is a heuristic** starting point, never a finished policy.
- **Profile list sections replace, not append**: `artifacts:` in `policy.yaml` replaces the profile's list.
- **`search --role` finds only explicit `artifacts[]` roles**, not family-matched files; use
  `orient --full --output json` or globs to enumerate a family.
- **Adopting a rule on an existing repo:** `steward baseline generate` grandfathers today's
  violations so `check` fails only on new ones; commit `.steward/baseline.json`.

## Go deeper

- First-time setup, guided by a repository investigation and a maintainer interview: the
  **setup-steward** skill (`/setup-steward`).
- Configuring governance (artifacts, families, severity, exclusions, maintenance, CI):
  [maintainer guide](https://github.com/jonv11/steward/blob/dev/docs/guide/maintainer-guide.md)
  and [configuration reference](https://github.com/jonv11/steward/blob/dev/docs/guide/configuration-reference.md).
- Validating your changes and the frequent diagnostics:
  [contributor guide](https://github.com/jonv11/steward/blob/dev/docs/guide/contributor-guide.md).
- Agent loops, JSON, SARIF and MdPath selectors:
  [agent integration guide](https://github.com/jonv11/steward/blob/dev/docs/guide/agent-integration.md).
