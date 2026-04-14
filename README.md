# Steward

A configurable repository stewardship CLI for humans and AI agents. Steward helps maintain documentation structure, enforce governance policies, and keep repository artifacts in sync — all driven by declarative YAML configuration.

## Features

- **Repository orientation** — Auto-classify and outline repository structure
- **Policy-driven validation** — Enforce required artifacts, frontmatter fields, section sizes, and path policies
- **Markdown structural editing** — Query, edit, and manage Markdown documents with section and frontmatter operations
- **Deterministic maintenance** — Auto-generate structure documents, indexes, and managed sections
- **Broken link detection** — Find internal Markdown links that don't resolve
- **Full explainability** — Every rule is explainable with remediation guidance
- **Multi-format output** — Text and JSON output for human and agent consumption

## Installation

### As a .NET tool

```bash
dotnet tool install --global Steward.Cli
```

### From source

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build
```

## Quick Start

1. **Initialize** a repository:

```bash
steward init
```

1. **Orient** yourself:

```bash
steward orient           # classified structure with roles
steward orient --signals # cheap missing/stale signals
steward outline          # plain file tree
steward md outline README.md  # Markdown heading hierarchy
```

1. **Check** policy compliance:

```bash
steward check
```

1. **Maintain** generated artifacts:

```bash
steward maintain          # preview
steward maintain --apply  # apply changes
```

## Commands

| Command | Description |
| ------- | ----------- |
| `steward version` | Show version information |
| `steward orient` | Show classified repository structure with roles |
| `steward outline [path]` | Show directory file tree |
| `steward init` | Initialize .steward configuration |
| `steward config show [--effective]` | Show loaded configuration, raw files, and effective runtime defaults |
| `steward config validate` | Validate configuration files for errors |
| `steward check` | Validate repository against policy |
| `steward md outline <file>` | Show Markdown heading hierarchy |
| `steward md query <file> <selector>` | Query Markdown structure with selectors |
| `steward md edit <operation> <file>` | Structural Markdown editing operations |
| `steward search <query>` | Search across repository content |
| `steward maintain` | Deterministic maintenance of governed artifacts |
| `steward status` | Show current repository state at a glance |
| `steward explain [rule-id]` | Explain a validation rule, or list all rules |

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

Use `steward explain <rule-id>` for detailed guidance on any rule. Run `steward explain` (no argument) to list all rules.

## Configuration

Steward uses a `.steward/` directory with YAML configuration files. Run `steward init` to scaffold the initial files, then edit them to match your repository.

### config.yaml — Runtime settings

Controls output defaults and file discovery. These are defaults; CLI flags always override.

```yaml
profile: software       # Built-in profile label surfaced by steward commands

output:
  format: text          # Default output format: text or json
  no_color: false       # Disable colored output

discovery:
  exclude:              # Additional patterns to exclude beyond .gitignore
    - "node_modules/"
    - "dist/"
    - ".vs/"
```

### policy.yaml — Repository contract

Declares what the repository contains, what is required, and how governance rules apply.

```yaml
repository:
  name: my-project
  description: A sample project
  type: software        # software, docs, mixed, knowledge, minimal

artifacts:
  - path: README.md
    role: readme
    required: true
  - path: CHANGELOG.md
    role: changelog
    required: false

governance:
  section_size_warning_threshold: 500  # Lines per section before warning
  start_here:
    - README.md
    - docs/planning-index.md

validation:
  disabled_rules: []          # Rule IDs to disable, e.g. [STWD-004]
  required_frontmatter_fields: []   # Fields every Markdown file must declare

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

### path-policy.yaml — Path and naming rules

Defines per-path rules (required, forbidden, discouraged, ignored, etc.).

```yaml
rulesets:
  - name: core-files
    rules:
      - pattern: "README.md"
        category: required
        exact: true
  - name: forbidden
    rules:
      - pattern: ".env"
        category: forbidden
```

### Configuration precedence

Settings are resolved in this order (highest to lowest):

1. Explicit CLI flag (e.g. `--output json`)
2. `config.yaml` setting (e.g. `output.format: json`)
3. Built-in default (e.g. text output)

`steward config validate` rejects unknown fields and invalid profile names, and `steward config show --effective` prints the resolved runtime defaults that the CLI will use.

### Built-in profiles

`steward init --profile <name>` scaffolds reasonable defaults for common repository types:

| Profile | Description |
| ------- | ----------- |
| `software` | Software project with README, LICENSE, CHANGELOG |
| `docs` | Documentation repository |
| `mixed` | Mixed code and documentation |
| `knowledge` | Knowledge base or wiki |
| `minimal` | Minimal setup, only README suggested |

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
