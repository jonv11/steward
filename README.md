# Steward

A configurable repository stewardship CLI for humans and AI agents. Steward helps maintain documentation structure, enforce governance policies, and keep repository artifacts in sync — all driven by declarative YAML configuration.

## Features

- **Repository orientation** — Auto-classify and outline repository structure
- **Policy-driven validation** — Enforce required artifacts, frontmatter fields, section sizes, and path policies
- **Markdown structural editing** — Query, edit, and manage Markdown documents with Section/frontmatter operations
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
git clone https://github.com/your-org/steward.git
cd steward
dotnet build
```

## Quick Start

1. **Initialize** a repository:

```bash
steward init
```

2. **Orient** yourself:

```bash
steward orient
steward outline README.md
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

## Commands

| Command | Description |
|---------|-------------|
| `steward version` | Show version information |
| `steward orient` | Show classified repository structure |
| `steward outline <file>` | Show structural outline of a Markdown file |
| `steward init` | Initialize .steward configuration |
| `steward config show` | Show current configuration |
| `steward check` | Validate repository against policy |
| `steward md query <file>` | Query Markdown structure with selectors |
| `steward md outline <file>` | Outline a Markdown document |
| `steward md edit <op> <file>` | Structural Markdown editing operations |
| `steward search <query>` | Search across repository content |
| `steward maintain` | Deterministic maintenance of governed artifacts |
| `steward status` | Show current repository state at a glance |
| `steward explain <rule-id>` | Explain a validation rule |

### Global Options

| Option | Description |
|--------|-------------|
| `--output text\|json` | Output format (default: text) |
| `--verbosity` | Verbosity level |
| `--no-color` | Disable colored output |
| `--config <path>` | Override config directory path |

## Validation Rules

| Rule | Category | Description |
|------|----------|-------------|
| STWD-001 | path-policy | Required artifacts must exist |
| STWD-002 | path-policy | Forbidden path patterns must not match |
| STWD-003 | frontmatter | Required frontmatter fields must be present |
| STWD-004 | section-size | Sections must not exceed size threshold |
| STWD-005 | managed-region | Managed region markers must be well-formed |
| STWD-006 | managed-region | Content in managed regions must not be manually edited |
| STWD-007 | stale-artifact | Maintained artifacts must match expected state |
| STWD-008 | broken-link | Internal Markdown links must resolve |

Use `steward explain <rule-id>` for detailed guidance on any rule.

## Configuration

Steward uses a `.steward/` directory with YAML configuration files:

- **config.yaml** — Profile selection and general settings
- **policy.yaml** — Repository policy: artifacts, governance rules, maintenance definitions
- **path-policy.yaml** — Path-level rules (forbidden patterns, required frontmatter)

### Example policy.yaml

```yaml
repository:
  name: my-project
  description: A sample project

artifacts:
  - path: README.md
    role: readme
    required: true
  - path: CHANGELOG.md
    role: changelog
    required: false

governance:
  section_size_warning_threshold: 500

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
