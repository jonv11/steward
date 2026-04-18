# Steward

A configurable repository stewardship CLI for humans and AI agents. Steward helps maintain documentation structure, enforce governance policies, and keep repository artifacts in sync — all driven by declarative YAML configuration.

Current repository baseline: **`0.15.0`**. Steward is pre-`1.0.0`: intentional public `0.x` releases are allowed when the documented release process is satisfied, but `1.0.0` remains separately gated by explicit stable-release authorization. See [Current Status](#current-status) for what works today and what is still planned.

## Who Is Steward For?

Steward serves two distinct user roles. The same person may fill both roles, but the tasks and workflows differ.

**Maintainer** — You configure Steward for a repository. You define what artifacts must exist, what naming conventions apply, what frontmatter is required, and what gets auto-generated. You author the `.steward/` configuration files and evolve them as the repository grows.

**Contributor** — You add or modify content in a repository that uses Steward. You run validation to check your work against the configured rules, interpret any failures, and fix issues before committing.

Both roles use the same CLI binary. This README covers both paths and marks sections by role where the distinction matters.

## Features

- **Repository orientation** — Auto-classify and outline repository structure
- **Policy-driven validation** — Enforce required artifacts, frontmatter fields, section sizes, naming conventions, and path policies
- **Artifact families** — Group recurring document types (ADRs, RFCs, etc.) with convention-based discovery and type-aware validation
- **Markdown structural editing** — Query, edit, and manage Markdown documents with section and frontmatter operations
- **Deterministic maintenance** — Auto-generate structure documents, indexes, and managed sections
- **Broken link detection** — Find internal Markdown links that don't resolve
- **Rule explainability** — Every validation rule is explainable with remediation guidance
- **Multi-format output** — Text and JSON output for human and agent consumption

## Installation

### Development Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### From source

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build
```

After building from source, run commands using:

```bash
dotnet run --project src/Steward.Cli -- <command>
```

### Build and install as a global tool

```bash
dotnet pack src/Steward.Cli -c Release
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward --version 0.15.0
```

After installing locally with `--tool-path`, run commands using:

```bash
./.tools/steward/steward <command>
```

### Public feed install

NuGet publication is intentional and tag-driven. Tagged releases publish the `Steward` .NET tool package to nuget.org from GitHub Actions, but docs in the repo should still avoid implying a package exists before a given release has actually published successfully.

### GitHub Releases

When Steward cuts an intentional public `0.x` release, the GitHub Releases page is the primary download surface. Each tagged release attaches:

- the `.nupkg` for `dotnet tool install --add-source`
- self-contained bundles for `win-x64`, `linux-x64`, and `osx-arm64`
- a `SHA256SUMS.txt` checksum file

The release operator path is documented in [docs/planning/release-process.md](docs/planning/release-process.md).

### Dependency posture

Steward currently pins exact prerelease CLI-stack versions in [Directory.Packages.props](Directory.Packages.props), including `System.CommandLine` beta5. All other runtime dependencies are at stable GA versions. That is an intentional pre-`1.0.0` tradeoff for the current repo line, not a claim that stable-release dependency hardening is already complete.

## Getting Started — Maintainer

If you are setting up Steward for a repository, follow this path.

### 1. Initialize configuration

```bash
steward init --profile software    # or: docs, minimal
```

This creates a `.steward/` directory with starter `config.yaml` and `policy.yaml` files, plus placeholder files for required artifacts declared by the chosen profile. Add `path-policy.yaml` separately when you want explicit naming or forbidden-path rules.

### 2. Discover your repository

```bash
steward config suggest             # detect artifacts steward can see
```

Review the suggestions and edit `.steward/policy.yaml` to declare the artifacts, roles, governance rules, and maintenance targets that match your repository.

### 3. Validate your configuration

```bash
steward config validate            # check YAML syntax and semantic references
steward config doctor              # detect dead declarations, unmatched patterns, unreachable families
```

### 4. Run a full check

```bash
steward check                      # validate the entire repository against your policy
```

Review the diagnostics. Each violation includes a rule ID you can look up:

```bash
steward explain STWD-003           # understand what the rule checks and how to fix violations
```

### 5. Set up maintenance

If you configured maintenance artifacts (structure documents, indexes, managed sections):

```bash
steward maintain                   # preview what would be generated
steward maintain --apply           # apply changes
```

### 6. Iterate

After editing policy, re-run `config validate`, `config doctor`, and `check` to verify correctness. Use `steward config show --effective` to inspect the fully resolved runtime configuration including profile defaults.

### Maintainer reference: what can be enforced today

| Enforcement area | How to configure | Rule(s) |
| ---------------- | ---------------- | ------- |
| Required artifacts | `artifacts[].required: true` in policy.yaml | STWD-001 |
| Forbidden paths | `category: forbidden` in path-policy.yaml | STWD-002 |
| Required frontmatter fields | `governance.frontmatter.required_fields` or `frontmatter_requirements` in policy.yaml, or `frontmatter_schema` on artifact families | STWD-003 |
| Section size limits | `governance.section_size_warning_threshold` in policy.yaml | STWD-004 |
| Managed region integrity | Automatically enforced when maintenance artifacts use managed sections | STWD-005, STWD-006 |
| Stale maintained artifacts | Automatically enforced for configured maintenance artifacts | STWD-007 |
| Broken internal links | Automatically enforced on all Markdown files | STWD-008 |
| Broken policy references | Automatically enforced when declared artifact paths don't exist | STWD-009 |
| Naming conventions | `must_match` in path-policy.yaml | STWD-010 |
| Index completeness | `index_of` on artifacts in policy.yaml | STWD-011 |
| Document freshness | `freshness.max_age_days` on artifacts in policy.yaml | STWD-012 |
| Document discoverability | Automatically enforced — Markdown files should be reachable from navigation | STWD-013 |
| Required sections per family | `required_sections` on artifact families in policy.yaml | STWD-014 |
| Minimum file count per family | `directory_expectations.min_count` on artifact families in policy.yaml | STWD-015 |
| Naming pattern per family | `naming_pattern` on artifact families in policy.yaml | STWD-016 |
| Unique Markdown heading text | Automatically enforced on Markdown files after anchor-style normalization | STWD-017 |

## Getting Started — Contributor

If you are contributing to a repository that already uses Steward, follow this path. You do not need to understand or modify the `.steward/` configuration — the maintainer has already set that up.

### 1. Orient yourself

```bash
steward orient                     # see what the repo contains and where to start
steward orient --signals           # also show quick missing/stale signals
```

### 2. Make your changes

Add or edit files normally. Steward does not interfere with your editing workflow.

### 3. Validate your work

Before committing, check that your changes comply with the repository's rules:

```bash
steward check                      # full repository validation
steward check --scope changed      # only validate git-modified files
steward check --scope staged       # only validate git-staged files
```

### 4. Understand failures

If `steward check` reports violations, each diagnostic includes a rule ID. Look up what the rule enforces and how to fix it:

```bash
steward explain STWD-008           # explain the broken-link rule
steward explain path docs/my-doc.md  # show all rules that apply to a specific file
```

### 5. Fix issues

Some rules have deterministic auto-fixes. Preview and apply them:

```bash
steward check --fix                # preview what steward would fix
steward check --fix --apply        # apply the fixes
```

For rules without auto-fix, `steward explain <rule-id>` provides remediation guidance.

### 6. Refresh maintained artifacts

If you added, moved, or renamed files, generated artifacts like `STRUCTURE.md` or indexes may be stale:

```bash
steward maintain                   # preview changes
steward maintain --apply           # apply changes
```

### 7. Re-check

```bash
steward check                      # confirm everything passes
```

A clean check returns exit code `0` and reports no errors. In CI, the same `steward check` command can run as a gate.

### Exit codes

| Code | Meaning |
| ---- | ------- |
| 0 | Clean — no validation failures |
| 1 | Validation failure — one or more rules violated |
| 2 | Usage error — invalid arguments or configuration |
| 3 | Internal error — unexpected runtime failure |

## Commands

| Command | Description |
| ------- | ----------- |
| `steward version` | Show version information |
| `steward init [--profile]` | Initialize .steward configuration (`software`, `docs`, `minimal`) |
| `steward orient` | Show a curated repository-start orientation (`--signals`, `--full`, `--compact`, `--tree`, `--depth`) |
| `steward outline [path]` | Show a tree view of a directory or, for `.md`, a heading outline (`--counts`, `--sizes`, `--lines`) |
| `steward status [--coverage]` | Show current repository state at a glance |
| `steward check` | Validate repository against policy (`--scope full\|changed\|staged`, `--paths`, `--fix`, `--apply`, `--quiet`) |
| `steward maintain` | Preview or apply deterministic artifact maintenance (`--artifact <id>`, `--apply`, `--diff`) |
| `steward search <query>` | Search repository content and headings (`--role`, `--mode all\|content\|headings`, `--regex`, `--max`) |
| `steward explain [rule-id]` | Explain a validation rule, or list all rules |
| `steward explain path <file>` | Show the effective governance rules that apply to a specific file |
| `steward refs <path>` | Show inbound and outbound Markdown references for a file (`--to`, `--from`) |
| `steward refactor move <old> <new>` | Move/rename a file and update all Markdown references (`--preview`, `--apply`) |
| `steward md outline <file>` | Show Markdown heading hierarchy with line counts |
| `steward md query <file> <selector>` | Extract content using an MdPath selector or Markdown anchor slug such as `#who-is-steward-for` (`--pattern` for batch) |
| `steward md edit <operation> <file>` | Structural Markdown editing (sections, frontmatter, blocks) |
| `steward config show [--effective]` | Print raw config files and (with `--effective`) the resolved runtime defaults plus merged policy |
| `steward config validate` | Check .steward/ YAML files for syntax and field errors |
| `steward config doctor` | Detect valid-but-ineffective config: dead `start_here` entries, unmatched patterns, unreachable families |
| `steward config suggest` | Analyze the repository and suggest artifact declarations for policy.yaml |

### Global Options

| Option | Description |
| ------ | ----------- |
| `--output text\|json` | Output format (default: text, overrides config.yaml) |
| `--verbosity quiet\|normal\|verbose\|debug` | Verbosity level (default: normal) |
| `--no-color` | Disable colored output (overrides config.yaml) |
| `--config <path>` | Override config directory path |

## Validation Rules

| Rule | Default Severity | Category | Description |
| ---- | ---------------- | -------- | ----------- |
| STWD-001 | error | path-policy | Required artifacts must exist |
| STWD-002 | error | path-policy | Forbidden path patterns must not match |
| STWD-003 | error | frontmatter | Required frontmatter fields must be present (global, scoped, or family-level) |
| STWD-004 | info | governance | Sections should not exceed the configured size threshold |
| STWD-005 | error | structure | Managed region markers must be well-formed |
| STWD-006 | warning | ownership | Content in steward-managed regions should not be edited manually |
| STWD-007 | warning | stale-artifact | Maintained artifacts must match expected state |
| STWD-008 | warning | broken-link | Internal Markdown links should resolve |
| STWD-009 | warning | broken-reference | Policy-declared artifact paths should resolve to existing files |
| STWD-010 | warning | path-policy | Files in governed directories must match declared naming conventions |
| STWD-011 | warning | index-completeness | All Markdown files in indexed directories should be linked from the index |
| STWD-012 | warning | freshness | State documents with freshness declarations should be updated within window |
| STWD-013 | info | discoverability | Markdown files should be reachable from at least one navigation surface |
| STWD-014 | warning | structure | Files in an artifact family must contain all required section headings |
| STWD-015 | warning | family-completeness | Artifact families with `min_count` must meet the declared minimum |
| STWD-016 | warning | naming | Files matched by an artifact family must satisfy the family's `naming_pattern` |
| STWD-017 | warning | structure | Heading text must be unique within a Markdown file after anchor-style normalization |

Use `steward explain <rule-id>` for detailed guidance on any rule. Run `steward explain` (no argument) to list all rules with their current severity and description.

Severities can be overridden per repository via `validation.severity_overrides` in policy.yaml. Rules can be suppressed globally via `validation.disabled_rules` or per-path via `validation.path_overrides`.

## Configuration

Steward uses a `.steward/` directory with three optional YAML configuration files. Run `steward init` to scaffold the initial files, then `steward config suggest` to get artifact suggestions for your specific repository.

### config.yaml — Runtime settings

Controls output defaults, file discovery, and coverage reporting. CLI flags always override these settings.

```yaml
profile: software       # Built-in profile that supplies default artifact declarations

output:
  format: text          # Default output format: text or json
  no_color: false       # Disable colored output
  verbosity: normal     # quiet, normal, verbose, or debug

discovery:
  exclude:              # Glob patterns to exclude beyond .gitignore
    - "node_modules/"
    - "dist/"
    - ".vs/"

coverage:
  exclude:              # Glob patterns to exclude from governance-coverage calculations
    - "tests/fixtures/**"
```

### policy.yaml — Repository contract

Declares what the repository contains, what governance rules apply, what artifact families exist, and what artifacts are maintained automatically. This is the core of Steward's configuration.

```yaml
repository:
  name: my-project
  description: A sample project
  type: software        # Informational: software, docs, mixed, knowledge, minimal

artifacts:
  - path: README.md
    role: readme          # Role used by orient, search --role, and discoverability rules
    description: Project overview
    required: true
    importance: required  # required, recommended, or optional
  - path: CHANGELOG.md
    role: changelog
    importance: recommended
  - path: docs/adr/
    role: decision
    description: Architecture Decision Records
    index_of: docs/adr/   # Signals that this artifact is a directory index
  - path: docs/status.md
    role: current-state
    freshness:
      max_age_days: 30    # STWD-012 fires if not updated within this window

artifact_families:        # Convention-based document type grouping (v0.13.0+)
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/adr/ADR-*.md"
    role: governance
    importance: recommended
    frontmatter_schema:
      required: [type, status]
      allowed_values:
        type: [adr]
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
    required_sections: [Context, Decision, Consequences]
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    directory_expectations:
      min_count: 1

governance:
  section_size_warning_threshold: 500   # Lines per section before STWD-004 fires
  start_here:
    - README.md
    - docs/index.md

  frontmatter:
    required_fields: [status, owner]    # Fields all governed Markdown files must declare
    auto_fields:
      updated_at: true                  # Update existing updated_at fields to today's date on locally changed Markdown files

validation:
  disabled_rules: [STWD-004]           # Suppress rules globally
  severity_overrides:
    STWD-008: error                     # Upgrade broken-link from warning to error
  path_overrides:
    - pattern: "src/**/*.md"
      disabled_rules: [STWD-003]       # No frontmatter required in source-adjacent docs
  frontmatter_requirements:
    - pattern: "docs/decisions/**/*.md"
      required_fields: [status, date, deciders]
      allowed_values:
        status: [proposed, accepted, deprecated, superseded]

maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document          # Auto-generates a directory tree document
      options:
        depth: 3
        exclude:
          - ".git/**"
          - "node_modules/**"
    - id: docs-index
      path: docs/index.md
      type: directory-index             # Auto-generates an index of a directory
      source: "docs/*.md"
      sort: filename
```

Supported maintenance types: `structure-document`, `index`, `directory-index`, `managed-section`, `frontmatter-auto`, `manifest`.

`directory-index` maintenance requires each indexed Markdown file to declare a non-empty `description` field in frontmatter so generated tables stay reviewable without hand-authored summaries.

`governance.frontmatter.auto_fields.<field>: true` is shorthand for maintaining that existing frontmatter field with today's date when the file is reported as locally changed by `git diff --name-only HEAD`. Steward updates existing fields only; it does not create new date fields implicitly.

### path-policy.yaml — Path and naming rules

Enforces naming conventions and file presence/absence patterns. This file is optional.

```yaml
rulesets:
  - name: core-files
    rules:
      - pattern: "README.md"
        category: required
        exact: true
      - pattern: ".env"
        category: forbidden             # forbidden files must never exist

  - name: adr-naming
    rules:
      - pattern: "docs/adr/**/*.md"
        category: required
        must_match: "^[0-9]{4}-[a-z0-9-]+\\.md$"   # Enforce e.g. 0001-use-postgres.md
```

### Configuration precedence

Settings are resolved in this order (highest to lowest):

1. Explicit CLI flag (e.g. `--output json`)
2. `config.yaml` setting (e.g. `output.format: json`)
3. Built-in default (e.g. text output)

Use `steward config validate` to check YAML syntax and semantic references (rule IDs, maintainer types, glob patterns, `depends_on` links). Use `steward config show --effective` to print the resolved runtime defaults plus the merged effective policy. Use `steward config doctor` to detect silent problems like `start_here` entries that point to missing files, dead suppressions, unreachable patterns, or families whose path patterns match nothing.

For global Markdown frontmatter requirements, `governance.frontmatter.required_fields` is the canonical location. Steward still accepts the legacy `validation.required_frontmatter_fields`; if both are present, they are treated additively and `steward config doctor` warns so the policy can be simplified.

### Built-in profiles

`steward init --profile <name>` scaffolds starting-point defaults for common repository types, including placeholder files for required artifacts so that an immediate `steward check` does not fail on missing files. At runtime, profile defaults merge in shallowly: repository-local scalar/object values override profile values, while repository-local list sections such as `artifacts:` replace the corresponding profile list as a whole.

| Profile | Description | Status |
| ------- | ----------- | ------ |
| `software` | Software project with README, LICENSE, CHANGELOG | Actively used on this repository |
| `docs` | Documentation repository | Tested via fixtures; usable starting point |
| `minimal` | README-first baseline with minimal additional defaults | Tested via fixtures; intentionally sparse |

> **Note:** `mixed` and `knowledge` profiles are not yet offered via `init`. They remain in code for backward compatibility and will be enabled when their governance contracts are enriched. See [ADR-014](docs/decisions/adrs/ADR-014-non-software-profile-scope.md).

## Common Workflows

### Maintainer: adding a new artifact family

To enforce conventions on a group of recurring documents (e.g., ADRs, RFCs, runbooks):

1. Add an `artifact_families` entry in `.steward/policy.yaml` with a `match` section (path glob and/or frontmatter criteria)
2. Optionally declare `frontmatter_schema`, `required_sections`, `naming_pattern`, and `directory_expectations`
3. Run `steward config validate` and `steward config doctor` to verify the family matches the expected files
4. Run `steward check` to see any new violations the family introduces

### Maintainer: tuning severity and suppression

```yaml
# In policy.yaml → validation:
severity_overrides:
  STWD-008: error         # Promote broken links to errors
disabled_rules: [STWD-013] # Suppress discoverability warnings globally
path_overrides:
  - pattern: "drafts/**"
    disabled_rules: [STWD-003, STWD-012]  # No frontmatter or freshness for drafts
```

### Contributor: pre-commit check on staged files

```bash
steward check --scope staged
```

This validates only files in the git staging area. Use `--scope changed` for all modified files (staged and unstaged).

### Contributor: understanding a specific file's governance

```bash
steward explain path docs/planning/my-doc.md
```

This shows which rules apply to that file, including any artifact family membership, required frontmatter, and naming expectations.

### Contributor: searching for content

```bash
steward search "deployment"                    # search content and headings
steward search "TODO" --mode content --regex   # regex search in content only
steward search "architecture" --mode headings  # search only headings
```

## Troubleshooting

### `steward check` fails immediately after `steward init`

This usually means the profile declared required artifacts that don't have placeholder files. As of v0.12.0, `steward init --profile software` scaffolds placeholders for required artifacts. If you initialized with an older version, create the missing files manually or re-run `steward init`.

### STWD-007 keeps firing even after edits

STWD-007 means a maintained artifact is stale — its content doesn't match what `steward maintain` would generate. Run `steward maintain --apply` to regenerate it. Do not hand-edit files that are managed by Steward's maintenance engine.

### STWD-003 fires for files that shouldn't need frontmatter

The maintainer can suppress frontmatter requirements per-path using `validation.path_overrides` in policy.yaml. If you are a contributor and this seems wrong, raise the issue with the repository maintainer.

### Scoped check (`--scope changed`) reports false positives

This was a known defect resolved in v0.11.0. If you see false positives for STWD-001, STWD-007, or STWD-009 on a clean tree with `--scope changed` or `--scope staged`, update to the latest version.

### `steward config suggest` doesn't detect all my artifacts

`config suggest` uses heuristics to detect common patterns. It may miss artifacts with unusual paths or roles. Treat its output as a starting point and add missing artifacts manually.

## Current Status

Steward is at `v0.15.0` on a pre-`1.0.0` release line. Intentional public `0.x` releases are allowed when the repo is ready and the release process is followed. The version `1.0.0` still requires explicit authorization per [ADR-013](docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md) and has not been scheduled.

**What works today:** All 17 validation rules, all commands listed above, three built-in profiles, artifact family classification, deterministic maintenance, Markdown structural editing, and JSON output for automation.

**Release operations today:** The repo has a changelog-backed, tag-driven GitHub Release workflow and repo-managed release-intent labels for pre-`1.0.0` releases. See [docs/planning/release-process.md](docs/planning/release-process.md).

**Remaining before first stable release:** Cross-platform CI and release-workflow green evidence from GitHub-hosted runs. See [implementation status](docs/implementation-status.md) for the full picture.

**Planned for later pre-1.0 milestones:** Heading selector fuzzy matching in MdPath, workflow/session modeling, and broader machine-contract hardening. See [pre-1.0 readiness plan](docs/planning/pre-1-0-readiness-plan.md) for the categorized list.

## Using Steward In This Repo

When contributing to the Steward repository itself, use Steward as the primary navigation and validation surface:

```bash
steward orient --signals
steward status --coverage
steward check
```

Agent-specific operational guidance for using Steward on this repo lives in [SKILL.md](SKILL.md).

For the strongest repo-specific orientation flow, start with `README.md`, then [docs/planning-index.md](docs/planning-index.md), [docs/implementation-status.md](docs/implementation-status.md), [docs/planning/implementation-instructions.md](docs/planning/implementation-instructions.md), and [docs/requirements/PRD.md](docs/requirements/PRD.md). Open `steward.sln` when you are ready to enter the code. If you are changing repo guidance or stewardship behavior, inspect `.steward/policy.yaml` next. After structural moves or new documentation, refresh the generated map with `steward maintain --artifact structure --apply`.

## Development

### Prerequisites

- .NET 10 SDK

### Build

```bash
dotnet build steward.sln
```

### Test

```bash
dotnet test steward.sln
```

### CI

The repository includes a GitHub Actions matrix in `.github/workflows/ci.yml` that runs `dotnet build`, `dotnet test`, and `dotnet pack src/Steward.Cli/Steward.Cli.csproj -c Release` on Windows, macOS, and Linux.

### Release Process

Pre-`1.0.0` release operations are documented in [docs/planning/release-process.md](docs/planning/release-process.md) and summarized in [CHANGELOG.md](CHANGELOG.md). GitHub Release notes are sourced from changelog entries, and pull requests use repo-managed release-intent labels to make bump decisions reviewable.

### Project Structure

- `src/Steward.Cli` — CLI entry point and commands
- `src/Steward.Core` — Core library (validation, Markdown, maintenance)
- `tests/Steward.Core.Tests` — Core library tests
- `tests/Steward.Cli.Tests` — CLI integration tests
- `tests/Steward.TestFixtures` — Shared test infrastructure

## License

MIT License.
