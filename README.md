# Steward

A configurable repository stewardship CLI for humans and AI agents. Steward helps maintain documentation structure, enforce governance policies, and keep repository artifacts in sync — all driven by declarative YAML configuration.

Current repository baseline: **`0.10.0`**. Steward is still on a pre-`1.0.0` line in this repository; public stable-release messaging and publication are intentionally not assumed yet.

## Features

- **Repository orientation** — Auto-classify and outline repository structure
- **Policy-driven validation** — Enforce required artifacts, frontmatter fields, section sizes, and path policies
- **Markdown structural editing** — Query, edit, and manage Markdown documents with section and frontmatter operations
- **Deterministic maintenance** — Auto-generate structure documents, indexes, and managed sections
- **Broken link detection** — Find internal Markdown links that don't resolve
- **Full explainability** — Every rule is explainable with remediation guidance
- **Multi-format output** — Text and JSON output for human and agent consumption

## Installation

### From source

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build
```

### Build and install the current local package

```bash
dotnet pack src/Steward.Cli -c Release
dotnet tool install --global --add-source ./src/Steward.Cli/bin/Release Steward.Cli --version 0.10.0
```

### Public feed install

Use a public-feed install only when the project intentionally publishes a release package. This repository does not treat public publication as an already-completed fact.

## Quick Start

1. **Initialize** a repository:

```bash
steward init
```

2. **Orient** yourself:

```bash
steward orient           # structure classified by artifact role
steward orient --signals # add quick missing/stale signals
steward outline          # plain file tree
steward outline README.md     # Markdown heading hierarchy (shortcut for md outline)
```

3. **Check** policy compliance:

```bash
steward check
```

4. **Maintain** generated artifacts:

```bash
steward maintain          # preview
steward maintain --apply  # apply changes
```

## Using Steward In This Repo

When you are contributing to the Steward repository itself, use Steward as the first navigation surface:

```bash
steward orient --signals
steward status --coverage
steward check
```

In this repo, the main session-start documents are `README.md`, `docs/implementation-status.md`, `docs/planning-index.md`, and `docs/requirements/PRD.md`. After structural moves or new documentation, refresh the generated map with `steward maintain --artifact structure --apply`.

## Commands

| Command | Description |
| ------- | ----------- |
| `steward version` | Show version information |
| `steward init [--profile]` | Initialize .steward configuration |
| `steward orient` | Show repository structure classified by artifact role |
| `steward outline [path]` | Show directory file tree (pass a `.md` file to see its heading hierarchy) |
| `steward status` | Show current repository state at a glance |
| `steward check` | Validate repository against policy (`--scope full\|changed\|staged`, `--fix`, `--dry-run`) |
| `steward maintain` | Preview or apply deterministic artifact maintenance (`--artifact <id>`, `--apply`, `--diff`) |
| `steward search <query>` | Search repository content and headings (`--role`, `--mode`, `--regex`) |
| `steward explain [rule-id]` | Explain a validation rule, or list all rules |
| `steward explain path <file>` | Show the effective governance rules that apply to a specific file |
| `steward refs <path>` | Show inbound and outbound Markdown references for a file |
| `steward refactor move <old> <new>` | Move/rename a file and update all Markdown references |
| `steward md outline <file>` | Show Markdown heading hierarchy with line counts |
| `steward md query <file> <selector>` | Extract content using an MdPath selector |
| `steward md edit <operation> <file>` | Structural Markdown editing (sections, frontmatter, blocks) |
| `steward config show [--effective]` | Print raw config files and (with `--effective`) the resolved runtime defaults |
| `steward config validate` | Check .steward/ YAML files for syntax and field errors |
| `steward config doctor` | Detect valid-but-ineffective config: dead `start_here` entries, unmatched patterns |
| `steward config suggest` | Analyze the repository and suggest artifact declarations for policy.yaml |

### Global Options

| Option | Description |
| ------ | ----------- |
| `--output text\|json` | Output format (default: text, overrides config.yaml) |
| `--verbosity` | Verbosity level: quiet, normal, verbose, debug |
| `--no-color` | Disable colored output (overrides config.yaml) |
| `--config <path>` | Override config directory path |

## Validation Rules

| Rule | Category | Description |
| ---- | -------- | ----------- |
| STWD-001 | path-policy | Required artifacts must exist |
| STWD-002 | path-policy | Forbidden path patterns must not match |
| STWD-003 | frontmatter | Required frontmatter fields must be present |
| STWD-004 | governance | Sections should not exceed the configured size threshold |
| STWD-005 | structure | Managed region markers must be well-formed |
| STWD-006 | ownership | Content in steward-managed regions should not be edited manually |
| STWD-007 | stale-artifact | Maintained artifacts must match expected state |
| STWD-008 | broken-link | Internal Markdown links should resolve |
| STWD-009 | broken-reference | Policy-declared artifact paths should resolve to existing files |
| STWD-010 | path-policy | Files in governed directories must match declared naming conventions |
| STWD-011 | index-completeness | All Markdown files in indexed directories should be linked from the index |
| STWD-012 | freshness | State documents with freshness declarations should be updated within window |
| STWD-013 | discoverability | Markdown files should be reachable from at least one navigation surface |

Use `steward explain <rule-id>` for detailed guidance on any rule. Run `steward explain` (no argument) to list all rules.

## Configuration

Steward uses a `.steward/` directory with three optional YAML configuration files. Run `steward init` to scaffold the initial files, then `steward config suggest` to get artifact suggestions for your specific repository.

### config.yaml — Runtime settings

Controls output defaults and file discovery. CLI flags always override these.

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
```

### policy.yaml — Repository contract

Declares what the repository contains, what governance rules apply, and what artifacts are maintained automatically.

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
  - path: CHANGELOG.md
    role: changelog
    required: false
  - path: docs/adr/
    role: decision
    description: Architecture Decision Records
    index_of: docs/adr/   # Signals that this artifact is a directory index

governance:
  section_size_warning_threshold: 500   # Lines per section before STWD-004 fires
  start_here:
    - README.md
    - docs/index.md

  frontmatter:
    required_fields: [status, owner]    # Fields all governed Markdown files must declare
    auto_fields:
      updated_at: true                  # Auto-populate updated_at on steward maintain --apply

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
      source: docs/
      sort: title
```

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

`steward config validate` checks YAML syntax and semantic references such as rule ids, maintainer types, glob patterns, and `depends_on` links. `steward config show --effective` prints the resolved runtime defaults. `steward config doctor` detects silent problems like `start_here` entries that point to files that do not exist.

### Built-in profiles

`steward init --profile <name>` scaffolds reasonable defaults for common repository types. Profile defaults are applied wherever your `policy.yaml` does not specify a value.

| Profile | Description |
| ------- | ----------- |
| `software` | Software project with README, LICENSE, CHANGELOG |
| `docs` | Documentation repository |
| `mixed` | Mixed code and documentation |
| `knowledge` | Knowledge base or wiki |
| `minimal` | Minimal setup, only README suggested |

### Adapting to your repository

A typical adoption workflow:

```bash
steward init --profile software      # scaffold .steward/
steward config suggest               # see what artifacts steward detects
# edit .steward/policy.yaml to match your structure
steward config doctor                # detect dead declarations
steward check                        # validate and see remaining gaps
steward maintain --apply             # generate any configured auto-artifacts
```

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

### Project Structure

- `src/Steward.Cli` — CLI entry point and commands
- `src/Steward.Core` — Core library (validation, Markdown, maintenance)
- `tests/Steward.Core.Tests` — Core library tests
- `tests/Steward.Cli.Tests` — CLI integration tests
- `tests/Steward.TestFixtures` — Shared test infrastructure

## License

MIT License.
