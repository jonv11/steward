# Steward

A configurable repository stewardship CLI for humans and AI agents. Steward validates documentation structure, enforces governance policies, and keeps repository artifacts in sync — all driven by declarative YAML configuration.

Current version: **`v0.16.0`** (pre-`1.0.0`). See [Current Status](#current-status).

## Quick Start

```bash
# Build and install (requires .NET 10 SDK)
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build steward.sln -c Release
dotnet pack src/Steward.Cli -c Release --no-build
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward
export PATH="$PWD/.tools/steward:$PATH"    # or add to .bashrc / PowerShell profile

# Use on any repository
cd /path/to/your-repo
steward orient                       # see what the repo contains
steward init --profile software      # set up governance (maintainers)
steward check                        # validate against policy
```

`orient`, `outline`, and `check` work on any repository immediately — no `.steward/` config needed for basic structural analysis. A configured `.steward/` directory unlocks policy-driven validation, frontmatter enforcement, and artifact maintenance.

## Who Is Steward For?

**Maintainers** configure Steward for a repository: define required artifacts, naming conventions, frontmatter standards, and auto-generated indexes. See the [Maintainer Guide](docs/guide/maintainer-guide.md).

**Contributors** validate their changes against the configured rules before committing. See the [Contributor Guide](docs/guide/contributor-guide.md).

**AI agents** use Steward's structured diagnostics and JSON output for automated validation loops. See [Agent Integration](docs/guide/agent-integration.md).

## Features

- **Repository orientation** — auto-classify and outline repository structure
- **Policy-driven validation** — enforce required artifacts, frontmatter fields, section sizes, naming conventions, and path policies via 18 rules
- **Artifact families** — group recurring document types (ADRs, RFCs, etc.) with convention-based discovery and type-aware validation
- **Markdown structural editing** — query, edit, and manage Markdown documents with section and frontmatter operations
- **Deterministic maintenance** — auto-generate structure documents, indexes, and managed sections
- **Broken link detection** — find internal Markdown links that don't resolve
- **Rule explainability** — every validation rule is explainable with remediation guidance
- **Multi-format output** — text and JSON output for human and agent consumption
- **Auto-fix** — 3 rules (STWD-003, STWD-007, STWD-012) support deterministic auto-fix via `steward check --fix --apply`

## Installation

Steward requires the **[.NET 10 SDK](https://dotnet.microsoft.com/download)** (10.0 or later). Run `dotnet --version` to verify. Earlier SDK versions will not work.

### Build and install locally (recommended)

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build steward.sln -c Release
dotnet pack src/Steward.Cli -c Release --no-build
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward
```

Add to your PATH:

```bash
# Unix / Git Bash
export PATH="$PWD/.tools/steward:$PATH"

# Windows PowerShell
$env:PATH = "$PWD\.tools\steward;$env:PATH"
```

### Install from NuGet (when published)

```bash
dotnet tool install --global Steward
```

If this fails with "package not found," the latest version has not yet been published to NuGet. Use the source build above or download from [GitHub Releases](https://github.com/jonv11/steward/releases).

### GitHub Releases

Tagged releases attach `.nupkg`, self-contained bundles for `win-x64`/`linux-x64`/`osx-arm64`, and a `SHA256SUMS.txt` checksum file.

### The global.json trap

If you run Steward via `dotnet run --project` from inside another repository, that repository's `global.json` can select a different SDK and break Steward. Always use a tool-path install or the built executable for cross-repo use.

## Exit codes

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
| `steward md edit <operation> <file>` | Structural Markdown editing (sections, frontmatter, blocks) with preview/apply safety |
| `steward config show [--effective]` | Print raw config files and (with `--effective`) the resolved runtime defaults plus merged policy |
| `steward config validate` | Check .steward/ YAML files for syntax and field errors |
| `steward config doctor` | Detect valid-but-ineffective config: dead `start_here` entries, unmatched patterns, unreachable families |
| `steward config suggest` | Analyze the repository and suggest artifact declarations with confidence hints for `policy.yaml` |

### Global options

| Option | Description |
| ------ | ----------- |
| `--output text\|json` | Output format (default: text, overrides config.yaml) |
| `--verbosity quiet\|normal\|verbose\|debug` | Verbosity level (default: normal) |
| `--no-color` | Disable colored output (overrides config.yaml) |
| `--config <path>` | Override config directory path |

## Validation Rules

| Rule | Default Severity | Description |
| ---- | ---------------- | ----------- |
| STWD-001 | error | Required artifacts must exist |
| STWD-002 | error | Forbidden path patterns must not match |
| STWD-003 | error | Required frontmatter fields must be present |
| STWD-004 | info | Sections should not exceed size threshold |
| STWD-005 | error | Managed region markers must be well-formed |
| STWD-006 | warning | Managed regions should not be empty once declared |
| STWD-007 | warning | Maintained artifacts must match expected state |
| STWD-008 | warning | Internal Markdown links should resolve |
| STWD-009 | warning | Policy-declared artifact paths should exist |
| STWD-010 | warning | Files must match declared naming conventions |
| STWD-011 | warning | Indexed directories should have complete index coverage |
| STWD-012 | warning | State documents should be updated within freshness window |
| STWD-013 | info | Markdown files should be reachable from navigation |
| STWD-014 | warning | Family files must contain required section headings |
| STWD-015 | warning | Artifact families must meet declared minimum file count |
| STWD-016 | warning | Family files must satisfy the family's naming pattern |
| STWD-017 | warning | Heading text must be unique within a file |
| STWD-018 | warning | Fragment links should reference headings that exist |

Use `steward explain <rule-id>` for detailed guidance on any rule. Severities can be overridden via `validation.severity_overrides` in policy.yaml. Rules can be suppressed globally or per-path.

## Configuration

Steward uses a `.steward/` directory with three optional YAML configuration files:

| File | Purpose |
|------|---------|
| `config.yaml` | Runtime settings: output format, file discovery exclusions, coverage reporting |
| `policy.yaml` | Repository contract: required artifacts, frontmatter rules, artifact families, maintenance |
| `path-policy.yaml` | Naming conventions and forbidden/required path patterns |

Run `steward init --profile <name>` to scaffold initial files. Available profiles: `software`, `docs`, `minimal`.

For complete field documentation, valid values, defaults, and configuration examples, see the [Configuration Reference](docs/guide/configuration-reference.md).

## What Steward Does Not Do

- **Not a code linter.** Steward validates documentation structure and repository governance, not source code.
- **Not a CI system.** Steward is a validation command you run in CI — it does not replace CI.
- **Not a content generator.** Steward generates structure indexes and managed sections. It does not write documentation content.
- **Not a hosting platform tool.** No GitHub/GitLab API integration. Steward operates on the local filesystem and git state.
- **No IDE plugin.** CLI-only. No LSP, no editor extension.

## Current Status

Steward is at `v0.16.0` on a pre-`1.0.0` release line. `1.0.0` requires explicit authorization per [ADR-013](docs/decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

**What works today:** All 18 validation rules, all commands listed above, three built-in profiles (`software`, `docs`, `minimal`), artifact family classification, deterministic maintenance, Markdown structural editing, JSON output, and scoped validation.

**Known limitations:** .NET 10 SDK required (not yet widely adopted). `search --role` matches explicit artifact declarations only, not family-classified files. 3 of 18 rules support auto-fix. `mixed` and `knowledge` profiles are not yet scaffolded via `init`.

**Remaining before stable release:** Cross-platform CI evidence. See [implementation status](docs/implementation-status.md).

## Documentation

| Document | Audience |
|----------|----------|
| [Maintainer Guide](docs/guide/maintainer-guide.md) | Repo maintainers adopting Steward |
| [Contributor Guide](docs/guide/contributor-guide.md) | Contributors in Steward-governed repos |
| [Configuration Reference](docs/guide/configuration-reference.md) | All config fields, values, and defaults |
| [Agent Integration](docs/guide/agent-integration.md) | Using Steward with AI coding agents |

Internal project documents (planning, decisions, audits) are under [docs/](docs/README.md).

## Development

See [CONTRIBUTING.md](CONTRIBUTING.md) for contributor workflow and pull request guidelines.

### Prerequisites

- .NET 10 SDK
- Node.js (24.x) — only for Markdown linting, not required by the CLI itself

### Build and test

```bash
dotnet build steward.sln
dotnet test steward.sln
```

### Markdown lint

```bash
npm ci
npm run lint:md
```

### Project structure

- `src/Steward.Cli` — CLI entry point and commands
- `src/Steward.Core` — Core library (validation, Markdown, maintenance)
- `tests/Steward.Core.Tests` — Core library tests
- `tests/Steward.Cli.Tests` — CLI integration tests
- `tests/Steward.TestFixtures` — Shared test infrastructure

## License

MIT License.
