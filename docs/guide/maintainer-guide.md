---
type: guide
status: Active
last_updated: 2026-08-24
---

# Maintainer Guide

This guide is for maintainers who want to adopt Steward on their own repository. It covers the full path from installation to a working governance setup.

## What Steward does for maintainers

Steward lets you declare a repository contract in YAML and enforce it automatically:

- **Required artifacts** — certain files must exist (README, CHANGELOG, etc.)
- **Frontmatter standards** — Markdown files must include specific metadata fields
- **Naming conventions** — files in governed directories must match patterns
- **Document structure** — recurring document types (ADRs, RFCs, runbooks) must include required sections
- **Freshness tracking** — state documents must be updated within a time window
- **Broken link detection** — internal Markdown links must resolve
- **Deterministic maintenance** — structure documents and indexes are auto-generated

You configure what to enforce. Contributors run `steward check` to validate. The same command runs in CI.

## Prerequisites

- **.NET 10 SDK** (10.0 or later). Run `dotnet --version` to verify. Earlier versions (8, 9) will not work.
- **No other dependencies.** Steward has no runtime dependency on Node.js, Python, or any hosting platform.

## Installation

### Build from source (recommended during pre-release)

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build steward.sln -c Release
dotnet pack src/Steward.Cli -c Release --no-build
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward
```

Add the tool to your PATH:

```bash
# Unix / Git Bash
export PATH="$PWD/.tools/steward:$PATH"

# Windows PowerShell
$env:PATH = "$PWD\.tools\steward;$env:PATH"
```

### Install from NuGet

```bash
dotnet tool install --global Steward
```

This installs the latest published release. To test unreleased work, use the source build above. Published packages and self-contained bundles are also available from the [GitHub Releases page](https://github.com/jonv11/steward/releases).

### Important: the global.json trap

If you run Steward using `dotnet run --project` from inside another repository, that repository's `global.json` can select a different SDK and break Steward. For cross-repo use, always use a tool-path install or the built executable directly.

## Step 1: Initialize configuration

Navigate to the repository you want to govern:

```bash
cd /path/to/your-repo
steward init --profile software
```

This creates:

- `.steward/config.yaml` — runtime settings (output format, discovery exclusions)
- `.steward/policy.yaml` — repository contract (artifacts, governance rules)
- Placeholder files for required artifacts declared by the profile

Available profiles:

| Profile | Best for | Creates |
|---------|----------|---------|
| `software` | Code repositories with standard docs | Declares README, LICENSE, CHANGELOG, CONTRIBUTING; scaffolds placeholders for all but LICENSE |
| `docs` | Documentation-focused repositories | README placeholder + docs/ directory |
| `minimal` | Lightweight starting point | Minimal config, no required artifacts |

After init, add `.steward/` to version control. Add `path-policy.yaml` manually when you want naming or forbidden-path rules — init does not scaffold this file.

## Step 2: Discover existing artifacts

On a repository with existing content:

```bash
steward config suggest
```

This scans your repository and suggests artifact declarations you can add to `policy.yaml`. Treat the output as a starting point — it uses heuristics and may miss unusual paths or roles. Add missing artifacts manually.

## Step 3: Customize your policy

Edit `.steward/policy.yaml` to declare your repository's actual requirements. See the [Configuration Reference](configuration-reference.md) for all fields and valid values.

### Common customizations

**Declare required artifacts:**

```yaml
artifacts:
  - path: README.md
    role: authoritative
    required: true
    importance: required
  - path: docs/architecture.md
    role: documentation
    importance: recommended
```

**Add an artifact family for recurring document types:**

```yaml
artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/adr/ADR-*.md"
    frontmatter_schema:
      required: [type, status]
      allowed_fields: [type, status, description, last_updated]
      allowed_values:
        status: [Draft, Proposed, Accepted, Superseded]
      deprecated_fields:
        date: last_updated
    required_sections: [Context, Decision, Consequences]
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    title_pattern: "^ADR-[0-9]{3}: .+"
    section_pattern: "^[A-Z][A-Za-z ]+$"
    section_schema:
      heading_match: exact
      enforce_order: true
      allow_extra: true
      sections:
        - heading: Context
        - heading: Decision
        - heading: Consequences
        - heading: Alternatives
          required: false
    directory_expectations:
      min_count: 1
```

**Require frontmatter on all Markdown docs:**

```yaml
governance:
  frontmatter:
    required_fields: [status]
```

**Require different frontmatter per directory:**

```yaml
validation:
  frontmatter_requirements:
    - pattern: "docs/decisions/**/*.md"
      required_fields: [status, date, deciders]
      allowed_values:
        status: [proposed, accepted, deprecated, superseded]
```

**Override rule severity:**

```yaml
validation:
  severity_overrides:
    STWD-008: error           # Upgrade broken links from warning to error
  disabled_rules: [STWD-013]  # Suppress discoverability warnings globally
  path_overrides:
    - pattern: "drafts/**"
      disabled_rules: [STWD-003, STWD-012]  # No frontmatter or freshness for drafts
```

Suppression is global or glob-scoped. The one per-file exemption is `standalone: true` in a file's own frontmatter, which suppresses STWD-013 (discoverability) for that file. No other rule can be waived on a single file — scope it with a `path_overrides` glob instead.

**Set up automatic maintenance:**

```yaml
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
      options:
        depth: 3
        exclude:
          - ".git/**"
          - "node_modules/**"
```

**Add path-policy rules** (in `.steward/path-policy.yaml`):

```yaml
rulesets:
  - name: core-files
    rules:
      - pattern: ".env"
        category: forbidden
        description: Environment files must not be committed
      - pattern: "docs/adr/**/*.md"
        category: recommended
        must_match: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
```

## Step 4: Validate your configuration

```bash
steward config validate       # Check YAML syntax and semantic references
steward config doctor         # Detect dead declarations, unmatched patterns, unreachable families
```

Fix any issues before proceeding. `config validate` catches structural errors (invalid rule IDs, unknown types). `config doctor` catches valid-but-ineffective config (start_here pointing to missing files, family patterns matching nothing).

## Step 5: Run your first check

```bash
steward check
```

Review the diagnostics. Each violation includes:

- A severity marker (`[error]`, `[warning]`, `[info]`)
- A rule ID (e.g., `STWD-008`)
- The file path and optional line number
- A clear message
- A remediation line starting with `fix:`

Look up any rule:

```bash
steward explain STWD-003
```

See what rules apply to a specific file:

```bash
steward explain path docs/my-doc.md
```

## Step 6: Set up maintenance

If you configured maintenance artifacts, preview and apply:

```bash
steward maintain              # Preview what would be generated
steward maintain --apply      # Apply changes
```

Each artifact reports one of three statuses:

| Status | Meaning |
|--------|---------|
| `OK` | The artifact is up to date; nothing to do. |
| `MAINTAIN` | Content is stale and `--apply` will regenerate it. |
| `BLOCKED` | Steward cannot maintain the artifact at all — the target file is missing, its managed-region markers are absent, or its `source` does not exist. The line below the status says what to fix. |

`BLOCKED` is not a passing state: `--apply` writes nothing for that artifact until you resolve the cause.

Generated artifacts (like `STRUCTURE.md` or indexes) should not be hand-edited — Steward regenerates them and `steward check` will flag stale content (STWD-007).

## Step 7: Add to CI

Add `steward check` to your CI pipeline.

```yaml
# Example GitHub Actions step
- name: Validate repository governance
  run: steward check
```

### What actually fails the build

`steward check` exits 1 only when there is at least one **`error`**-severity diagnostic. `warning` and `info` diagnostics are printed but exit 0.

This matters when you configure CI: most rules default to `warning`, so by default they report without gating. To make a rule block a merge, raise its severity:

```yaml
validation:
  severity_overrides:
    STWD-008: error    # broken internal links now fail CI
    STWD-016: error    # family naming violations now fail CI
```

Rules that default to `error` (STWD-001, STWD-002, STWD-003, STWD-005) gate CI without any override. `governance.completion_policy` is reporting-only and never affects the exit code.

## Example configurations

### Minimal software project

```yaml
# .steward/config.yaml
profile: software
discovery:
  exclude:
    - "node_modules/"
    - "dist/"
```

```yaml
# .steward/policy.yaml
repository:
  name: my-app
  type: software

artifacts:
  - path: README.md
    role: authoritative
    required: true

governance:
  start_here: [README.md]
```

### Documentation repository

```yaml
# .steward/config.yaml
profile: docs
```

```yaml
# .steward/policy.yaml
repository:
  name: my-docs
  type: documentation

artifacts:
  - path: README.md
    role: authoritative
    required: true
  - path: docs/
    role: documentation
    required: true

governance:
  frontmatter:
    required_fields: [status]
  start_here:
    - README.md
```

### Project with ADRs and RFCs

```yaml
# .steward/policy.yaml
repository:
  name: my-platform
  type: software

artifacts:
  - path: README.md
    role: authoritative
    required: true
  - path: docs/decisions/
    role: governance
    index_of: docs/decisions/

artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/decisions/ADR-*.md"
    frontmatter_schema:
      required: [type, status, date]
      allowed_values:
        type: [adr]
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
    required_sections: [Context, Decision, Consequences]
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    directory_expectations:
      min_count: 1

governance:
  start_here: [README.md]
  frontmatter:
    required_fields: [status]
```

## Ongoing maintenance

| Task | Command | When |
|------|---------|------|
| Check everything | `steward check` | Before every commit/PR merge |
| Validate config changes | `steward config validate && steward config doctor` | After editing .steward/ |
| View resolved config | `steward config show --effective` | When debugging unexpected behavior |
| Refresh generated files | `steward maintain --apply` | After adding, moving, or renaming files |
| Check governance health | `steward status --coverage` | Periodically |
| Understand a file's rules | `steward explain path <file>` | When debugging unexpected violations |

## What Steward does not do

- **Not a code linter.** Steward validates documentation structure and repository governance, not source code quality.
- **Not a CI system.** Steward is a validation command. You run it in CI; it does not replace CI.
- **Not a content generator.** Steward generates structure indexes and managed sections. It does not write documentation content.
- **Not a hosting platform tool.** Steward operates on the local filesystem and git state. No GitHub/GitLab API integration.
- **Not a package manager.** Steward does not manage dependencies.
- **No IDE plugin.** Steward is CLI-only. No LSP, no editor extension.

## Current limitations

- **.NET 10 SDK required.** Not yet widely adopted. Contributors to your repo will need it if Steward is a local tool.
- **Three init profiles.** `software`, `docs`, and `minimal` are available. `mixed` and `knowledge` are defined internally but not yet scaffolded.
- **Search is basic.** `steward search` supports substring and regex matching. No fuzzy or semantic search.
- **`search --role` matches explicit artifact declarations only.** It does not find family-classified files. To find all files in a family, use glob patterns or `steward orient --full`.
- **4 of 21 rules support auto-fix.** Most violations require manual remediation. The fixable rules are STWD-003 (frontmatter), STWD-007 (stale artifacts), STWD-012 (freshness dates), and STWD-018 (unambiguous fragment links).
- **No baseline or phase-in mode.** Enabling a rule applies it to every existing file at once. On a large repository with existing content, turning on something like `governance.frontmatter.required_fields` produces a violation for every non-conforming file immediately. Adopt incrementally instead: scope new rules to a directory with `validation.frontmatter_requirements` or an artifact family, keep repository-wide rules at `warning` until the backlog is cleared, and use `steward check --since <ref>` so CI only judges what the branch touched.
- **Policy cannot be shared between repositories.** There is no `extends:` or import mechanism. Running the same governance across several repositories means copying `.steward/` and keeping the copies in sync yourself.
- **`explain path` lists which rules apply, not why.** It does not report which config file or stanza activated each rule, or each rule's effective severity. When a rule fires or fails to fire unexpectedly, compare against `steward config show --effective`.
