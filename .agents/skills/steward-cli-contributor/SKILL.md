---
name: steward-cli-contributor
description: Validate your changes against a repository's Steward governance rules before committing — run steward check, read and fix diagnostics, use auto-fix, refresh stale generated artifacts. Use when a repo has a .steward/ directory and you're making changes to it as a contributor, when `steward check` fails and needs fixing, or when asked what a specific STWD-* rule means. Requires steward installed and a repo already configured by a maintainer — see steward-cli and steward-cli-maintainer.
---

# Steward CLI — Contributor

Validate your own changes against a repository's already-configured `.steward/` contract. You don't need to understand or touch `.steward/*` — a maintainer set that up (see steward-cli-maintainer if you need to change the rules themselves, not just satisfy them).

## The workflow

```
orient → edit → validate → fix → re-validate → commit
```

### 1. Orient

```bash
steward orient              # what the repo contains, where to start
steward orient --signals    # same, plus missing/stale signals
```

### 2. Edit normally

Steward does not interfere with your editing workflow — no hooks, no wrapper commands. Just edit files.

### 3. Validate before committing

```bash
steward check                     # full repo
steward check --scope staged      # only git-staged files — tightest pre-commit check
steward check --scope changed     # staged + unstaged
steward check --since origin/main # merge-base-aware branch diff
```

Prefer `--scope staged` right before a commit: it validates exactly what you're about to commit, nothing more.

### 4. Read and fix diagnostics

```
[error] STWD-003 docs/my-doc.md: Required frontmatter field 'status' is missing.
         fix: Add 'status' to the YAML frontmatter block at the top of the file.
```

Each line carries severity, rule ID, file/line, message, and a `fix:` remediation hint. Look up more detail on any rule, or on what applies to a specific file:

```bash
steward explain STWD-003
steward explain path docs/my-doc.md
```

**Try auto-fix first** — four rules support it:

```bash
steward check --fix              # preview
steward check --fix --apply      # apply
```

| Rule | Auto-fixes |
|---|---|
| STWD-003 | Adds missing frontmatter fields with placeholder values |
| STWD-007 | Regenerates stale maintained artifacts |
| STWD-012 | Updates the `last_updated` frontmatter date |
| STWD-018 | Repairs a fragment link when exactly one unambiguous heading match exists |

Everything else needs a manual edit guided by `steward explain <rule-id>`.

### 5. Refresh generated artifacts if you moved or added files

```bash
steward maintain              # preview
steward maintain --apply      # write
```

Never hand-edit generated output (`STRUCTURE.md`, managed index sections) — Steward regenerates it and flags stale hand-edits as STWD-007.

### 6. Re-check, then commit

```bash
steward check
```

Exit code `0` means ready to commit.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Clean — includes runs with only `warning`/`info` diagnostics |
| 1 | At least one `error`-severity diagnostic |
| 2 | Usage error |
| 3 | Internal error |

## Frequently hit diagnostics

**STWD-003, missing/disallowed frontmatter field** — add the field to the YAML frontmatter block, or run `steward check --fix --apply`. If the value itself is disallowed, `steward explain path <file>` shows what the maintainer's schema permits.

**STWD-007, stale maintained artifact** — run `steward maintain --apply`. Don't hand-edit the file directly.

**STWD-008, broken internal link** — the link target doesn't resolve; fix the path or remove the link.

**STWD-017, duplicate heading anchor** — two headings in the same file normalize to the same slug (e.g. two `### Strengths`). Rename one to disambiguate.

## Useful commands beyond the core loop

```bash
steward outline <path>     # heading outline of a directory or file
steward search <query>     # search repo content and headings
steward refs <path>        # inbound/outbound Markdown references — check before moving a file
```

## JSON output, if you're scripting the loop

```bash
steward check --output json
steward orient --output json
steward status --output json
```

Structured envelope with a `diagnostics` array — useful for anything beyond eyeballing text output. (If you're building a full automated validate-fix-revalidate loop rather than a manual pre-commit check, steward-cli-agent covers that in depth, including SARIF and `--since` for PR gating.)
