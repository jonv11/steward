---
type: guide
status: Active
last_updated: 2026-04-19
---

# Contributor Guide

This guide is for contributors working in a repository that already uses Steward. You do not need to understand or modify the `.steward/` configuration — the maintainer has set that up.

## What Steward does for contributors

Steward validates your changes against the repository's declared governance rules. Before committing, you run `steward check` to verify your work complies with the repo's naming conventions, frontmatter requirements, link integrity, and structure rules. If something fails, Steward tells you exactly what the rule expects and how to fix it.

## Prerequisites

Steward must be installed and available on your PATH. If the maintainer has not documented how to install it for this repo, see the [Maintainer Guide](maintainer-guide.md#installation) for installation options.

Steward requires the **.NET 10 SDK** (10.0 or later). Run `dotnet --version` to verify.

## The basic workflow

```
1. Orient yourself
2. Make your changes
3. Validate
4. Fix any issues
5. Re-check
6. Commit
```

### 1. Orient yourself

See what the repo contains and where to start:

```bash
steward orient
```

For a more detailed view including missing/stale signals:

```bash
steward orient --signals
```

### 2. Make your changes

Edit files normally. Steward does not interfere with your editing workflow.

### 3. Validate your work

Before committing, check that your changes comply with the repository's rules:

```bash
steward check                    # Full repository validation
steward check --scope staged     # Only validate git-staged files
steward check --scope changed    # Only validate git-modified files (staged + unstaged)
```

Use `--scope staged` for the tightest pre-commit check — it validates only what you're about to commit.

### 4. Fix any issues

Each diagnostic includes a rule ID, file path, and remediation guidance:

```
[error] STWD-003 docs/my-doc.md: Required frontmatter field 'status' is missing.
         fix: Add 'status' to the YAML frontmatter block at the top of the file.
```

**Look up a rule:**

```bash
steward explain STWD-003
```

**See what rules apply to a specific file:**

```bash
steward explain path docs/my-doc.md
```

**Try auto-fix** for rules that support it:

```bash
steward check --fix              # Preview what Steward would fix
steward check --fix --apply      # Apply the fixes
```

Three rules support auto-fix:

| Rule | What it fixes |
|------|---------------|
| STWD-003 | Adds missing frontmatter fields with placeholder values |
| STWD-007 | Regenerates stale maintained artifacts |
| STWD-012 | Updates the `last_updated` frontmatter date |

All other rules require manual remediation. Run `steward explain <rule-id>` for guidance on any rule.

### 5. Refresh maintained artifacts

If you added, moved, or renamed files, generated artifacts (like `STRUCTURE.md` or indexes) may be stale:

```bash
steward maintain              # Preview changes
steward maintain --apply      # Apply changes
```

### 6. Re-check and commit

```bash
steward check
```

A clean check returns exit code `0`. You're ready to commit.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Clean — no validation failures |
| 1 | Validation failure — one or more rules violated |
| 2 | Usage error — invalid arguments or configuration |
| 3 | Internal error — unexpected runtime failure |

## Understanding output

### Successful check

```
No issues found.

Files checked: 87  Errors: 0  Warnings: 0  Info: 0
Result: PASS
```

### Check with violations

```
[error] STWD-003 docs/my-doc.md: Required frontmatter field 'status' is missing.
         fix: Add 'status' to the YAML frontmatter block at the top of the file.
[warn ] STWD-008 docs/guide.md:42: Broken internal link to 'docs/old-page.md'.
         fix: Fix or remove the broken internal link. Verify the referenced file exists.
[warn ] STWD-007 STRUCTURE.md: Maintained artifact 'structure' (structure-document) is stale.
         fix: Run 'steward maintain --apply' or 'steward check --fix' to update.

Files checked: 87  Errors: 1  Warnings: 2  Info: 0
Result: FAIL
```

Each line includes:

- **Severity:** `[error]`, `[warn ]`, or `[info ]`
- **Rule ID:** e.g., `STWD-003` — look it up with `steward explain STWD-003`
- **File and line:** where the issue is
- **Message:** what's wrong
- **fix:** how to resolve it

### Orient output

```
Repository: my-project
Context: type=software, profile=software

Start Here
  - README.md
  - docs/index.md

Orientation
  README.md [authoritative] [start]
  docs/index.md [guide] [start]
  CHANGELOG.md [changelog]
  CONTRIBUTING.md [governance]
  docs/architecture.md [documentation]
```

### Status output

```
Repository: my-project
Context: type=software, profile=software
Files: 42

Required Artifacts:
  [OK] README.md (authoritative)
  [MISSING] LICENSE (authoritative)

Recommended Artifacts:
  [OK] CHANGELOG.md (changelog)
  [OK] CONTRIBUTING.md (governance)
```

## Useful commands

| Command | Purpose |
|---------|---------|
| `steward orient` | See repo structure and key files |
| `steward orient --signals` | Same, plus missing/stale signals |
| `steward check` | Full validation |
| `steward check --scope staged` | Validate only staged files |
| `steward check --fix --apply` | Apply automatic fixes |
| `steward explain <rule-id>` | Understand a specific rule |
| `steward explain path <file>` | See all rules that apply to a file |
| `steward maintain --apply` | Refresh generated artifacts |
| `steward outline <path>` | Heading outline of a directory or file |
| `steward search <query>` | Search repo content and headings |
| `steward refs <path>` | Show inbound/outbound Markdown references |

## Common issues

### "Required frontmatter field is missing" (STWD-003)

Add the missing field to the YAML frontmatter block at the top of your file:

```markdown
---
status: Active
last_updated: 2026-04-19
---

# My Document
```

Or use auto-fix: `steward check --fix --apply`

### "Maintained artifact is stale" (STWD-007)

A generated file like `STRUCTURE.md` or an index is out of date. Run:

```bash
steward maintain --apply
```

Do not hand-edit files managed by Steward's maintenance engine.

### "Broken internal link" (STWD-008)

A Markdown link points to a file or heading that doesn't exist. Fix the link path or remove the link.

### "Heading text must be unique" (STWD-017)

Two headings in the same file produce the same anchor slug. Rename one of the headings to make it unique. For example, if you have two `### Strengths` headings, rename one to `### Maintainer strengths`.

### "Required frontmatter field 'status' has disallowed value" (STWD-003)

The frontmatter value doesn't match the allowed values declared by the maintainer. Run `steward explain path <file>` to see what values are allowed.

## JSON output

For scripting or automation, use JSON output:

```bash
steward check --output json
steward orient --output json
steward status --output json
```

JSON output uses a standard envelope with schema versioning, structured diagnostics, and machine-readable error information.
