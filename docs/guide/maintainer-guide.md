---
type: guide
status: Active
last_updated: 2026-04-19
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

### Install from NuGet (when available)

```bash
dotnet tool install --global Steward
```

If this fails with "package not found," the latest version has not yet been published. Use the source build above or download from the [GitHub Releases page](https://github.com/jonv11/steward/releases).

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
| `software` | Code repositories with standard docs | README, LICENSE, CHANGELOG, CONTRIBUTING placeholders |
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
      allowed_values:
        status: [Draft, Proposed, Accepted, Superseded]
    required_sections: [Context, Decision, Consequences]
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
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

Generated artifacts (like `STRUCTURE.md` or indexes) should not be hand-edited — Steward regenerates them and `steward check` will flag stale content (STWD-007).

## Step 7: Add to CI

Add `steward check` to your CI pipeline. It returns exit code 0 on success and 1 on validation failures.

```yaml
# Example GitHub Actions step
- name: Validate repository governance
  run: steward check
```

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
- **3 of 18 rules support auto-fix.** Most violations require manual remediation. The fixable rules are STWD-003 (frontmatter), STWD-007 (stale artifacts), and STWD-012 (freshness dates).
