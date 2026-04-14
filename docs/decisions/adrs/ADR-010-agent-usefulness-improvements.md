# ADR-010: Agent-Usefulness Improvements

- **Status:** Accepted
- **Category:** CLI Ergonomics

---

## Context

An evidence-based assessment of Steward's usefulness for coding agents (see `docs/audits/assessment-coding-agent-usefulness.md`) identified six improvements not covered by existing RFCs that would materially improve agent ergonomics. These are net-new capabilities that align with the product's dual-audience design (human + machine) but were not anticipated in the original requirement set.

## Decision

The following improvements are accepted for implementation:

### 1. `--compact` mode on `orient`

Add a `--compact` flag to the `orient` command that limits output to the 15 most important entries: start-here files, classified root directories, and required artifacts. Reduces context-window load for agents without losing the curated character of orient.

### 2. Regex mode on `search`

Add a `--regex` flag to the `search` command that treats the query as a .NET regular expression instead of a substring. Closes the gap with `rg` while preserving Steward's heading-context enrichment. Optional — not the default mode.

### 3. `--quiet` / exit-code-only mode on `check`

Add a `--quiet` flag to the `check` command that suppresses all stdout output and returns only the exit code (0 = pass, 1 = fail). Enables tighter agent automation loops.

### 4. Stdin support for `md edit` content

Accept `--content -` on `md edit` subcommands to read content from stdin instead of a command-line argument. Solves shell-quoting difficulties with multi-line content.

### 5. Diff output in `maintain` preview

Show unified diff for each artifact in `maintain` preview mode (default, non-`--apply`). Currently preview shows action descriptions but not the actual content differences.

### 6. Batch frontmatter query

Add `steward md query --pattern <glob>` to run the same MdPath selector against multiple files matching a glob pattern. Returns per-file results. Primary use case: `steward md query --pattern "docs/**/*.md" frontmatter.status`.

## Consequences

- Orient, check, and search gain new optional flags — existing behavior unchanged when flags are omitted.
- `md edit` gains stdin support — no breaking change to existing `--content` behavior.
- `maintain` preview output changes formatting — agents parsing preview text may need adaptation.
- `md query` gains batch mode — additive, no change to single-file behavior.
- All additions follow existing patterns: optional flags, dual text/JSON output, no breaking changes.
