---
name: steward-cli
description: Install and orient with the Steward CLI (`steward`) in any repository — a configurable stewardship tool that validates documentation structure, governance policy, and generated artifacts against a `.steward/` contract. Use when a repo has (or should have) a `.steward/` directory, when asked to install/set up Steward, or to decide which Steward persona skill applies. Routes to steward-cli-maintainer (configure governance), steward-cli-contributor (validate changes), or steward-cli-agent (automate validation loops, CI, JSON/SARIF output). Not for the Steward source repo itself — that's steward-self-cli.
---

# Steward CLI

Steward is a stewardship CLI: it validates a repository's documentation structure, naming conventions, and artifact policy against a declarative `.steward/` contract, and can maintain generated artifacts (structure trees, indexes) deterministically. No network calls, no non-determinism — same input, same output.

This skill is the entry point. It covers installation and the commands that work with zero configuration. For task-specific work, it routes to one of three persona skills.

## Which skill do you actually need?

| You are... | Doing... | Use |
|---|---|---|
| A maintainer | Setting up `.steward/`, declaring required artifacts, defining rules, wiring CI | **steward-cli-maintainer** |
| A contributor | Validating your own changes before committing, fixing a failed `steward check` | **steward-cli-contributor** |
| An AI agent | Running an automated validate→diagnose→fix loop, parsing JSON/SARIF, gating a PR | **steward-cli-agent** |
| Anyone, in the Steward source repo | Working on Steward's own code/docs | **steward-self-cli** (repo-specific, not this skill) |

The three persona skills assume the CLI is already installed and cover their workflow end-to-end — read this skill first only for install and orientation, then load the persona skill.

## Install

Requires the **.NET 10 SDK** (10.0+). Verify with `dotnet --version`; earlier SDKs will not run Steward.

```bash
# From a published NuGet package (latest release)
dotnet tool install --global Steward

# From source, tool-path install (to test unreleased work, or pin a specific checkout)
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build steward.sln -c Release
dotnet pack src/Steward.Cli -c Release --no-build
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward
export PATH="$PWD/.tools/steward:$PATH"
```

Self-contained release bundles (`win-x64`, `linux-x64`, `osx-arm64`) and checksums are also on the [GitHub Releases page](https://github.com/jonv11/steward/releases).

**The `global.json` trap:** never invoke Steward via `dotnet run --project <path-to-steward-checkout>` from inside a *different* repository you're validating — that repository's own `global.json` can pin a different SDK and silently break the run. Always use a tool-path or global install (or the built executable directly) when operating on a repo other than Steward's own source checkout.

## Works with zero configuration

These commands run immediately on any repository, `.steward/` or not:

```bash
steward version
steward orient              # curated repo-start orientation
steward orient --full --tree
steward outline [path]      # directory tree, or heading outline for a .md file
steward status              # required/recommended artifact state at a glance
```

A `.steward/` directory unlocks policy-driven validation (`steward check`), frontmatter enforcement, artifact families, and maintenance — that's what the persona skills configure and use.

## The three config files

| File | Purpose | Required |
|---|---|---|
| `.steward/config.yaml` | Runtime: output format, discovery/coverage exclusions | No |
| `.steward/policy.yaml` | The contract: artifacts, artifact families, governance, validation, maintenance | No, but needed for meaningful validation |
| `.steward/path-policy.yaml` | Naming conventions, forbidden/required paths | No |

All three are optional and independently useful; `steward init --profile <software\|docs\|minimal>` scaffolds the first two as a starting point.

## Exit codes (every persona needs these)

| Code | Meaning |
|---|---|
| 0 | Clean — this includes runs with `warning`/`info` diagnostics, only `error` severity fails |
| 1 | At least one `error`-severity diagnostic |
| 2 | Usage error — bad arguments or config |
| 3 | Internal error |

Most validation rules default to `warning` severity, which reports but does not fail the exit code. Only STWD-001, STWD-002, STWD-003, and STWD-005 default to `error`. This is the single most common source of "why didn't CI catch that" surprises — see steward-cli-maintainer for how to raise a rule's severity.

## Discover the rest live, don't memorize it

Steward documents itself; treat these as the source of truth over any cached table, since exact field names, rule sets, and defaults shift between versions:

```bash
steward --help
steward <command> --help
steward explain                      # list all validation rules
steward explain <rule-id>            # one rule's meaning, severity, remediation
steward explain path <file>          # rules that apply to a specific file
steward config show --effective      # resolved runtime config + merged policy
```
