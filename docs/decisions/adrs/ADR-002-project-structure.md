---
type: adr
status: Accepted
category: Architecture
description: Defines the solution layout, project boundaries, and test-project structure for Steward
---

# ADR-002: Project Structure

---

## Context

The solution needs a clear project structure that separates CLI presentation from domain logic and supports comprehensive testing.

## Decision

### Solution layout

```
steward.sln
src/
  Steward.Cli/                    # Console application — entry point, commands, output
    Commands/                     # Command definitions and handlers
    Formatting/                   # Output formatters (text, JSON)
    Program.cs                    # Entry point and DI setup
    Steward.Cli.csproj
  Steward.Core/                   # Class library — all domain logic
    Configuration/                # Config and policy loading, profiles
    Discovery/                    # File discovery, .gitignore, tree walking
    Markdown/                     # Markdown parsing, structural model, selectors, editing
    Orientation/                  # Orient command logic
    Outline/                      # Outline command logic
    Search/                       # Search engine
    Validation/                   # Validation engine, rules, diagnostics
    Maintenance/                  # Maintenance engine, artifact updaters
    PathPolicy/                   # Path policy engine (ruleset evaluation)
    Models/                       # Shared domain models
    Steward.Core.csproj
tests/
  Steward.Core.Tests/             # Unit tests for core logic
    Configuration/
    Discovery/
    Markdown/
    Validation/
    PathPolicy/
    ...
    Steward.Core.Tests.csproj
  Steward.Cli.Tests/              # CLI integration tests (black-box)
    Steward.Cli.Tests.csproj
  Steward.TestFixtures/           # Shared test data and helpers
    Repos/                        # Sample repository fixtures
    Steward.TestFixtures.csproj
docs/                             # Planning, requirements, decisions
.steward/                         # Steward's own config (dog-fooding)
```

### Project responsibilities

| Project | Type | Responsibility |
|---------|------|---------------|
| `Steward.Cli` | Console app | CLI entry point, command definitions, DI setup, output formatting. No domain logic. |
| `Steward.Core` | Class library | All domain logic: config, discovery, validation, markdown, search, orientation, maintenance. Testable in isolation. |
| `Steward.Core.Tests` | Test project | Unit tests for core logic. Fast, no file system or process dependencies (uses abstractions). |
| `Steward.Cli.Tests` | Test project | Integration tests. Invokes the CLI as a process, checks stdout/stderr/exit codes. |
| `Steward.TestFixtures` | Class library | Shared test helpers, sample repo fixtures, builder utilities. |

### Assembly naming

- Root namespace matches project name: `Steward.Cli`, `Steward.Core`.
- No `Repository` prefix—"Steward" is already the product name.
- Sub-namespaces follow folder structure: `Steward.Core.Validation`, `Steward.Core.Markdown`.

### Key conventions

- **One class per file** as a default. Small related types (e.g., an enum used by one class) may share a file.
- **File names match type names.**
- **`internal` by default.** Only types needed across project boundaries are `public`.
- **Interfaces for infrastructure:** `IFileSystem`, `IGitProvider`, `IConsoleOutput` — enables testing.
- **No static singletons for services.** Use DI throughout.

## Alternatives considered

1. **Single project:** Rejected—mixing CLI and domain logic prevents unit testing core logic without invoking the CLI.
2. **Many fine-grained projects (Steward.Markdown, Steward.Validation, etc.):** Rejected for v1.0.0—premature splitting adds build complexity. One Core library is sufficient. Can split later if it grows significantly.
3. **Feature-folder structure (vertical slices):** Considered but rejected—the domain areas (markdown, validation, search) are coherent enough to be namespaces within one project. Vertical slices add complexity without clear benefit at this scale.

## Consequences

- Clean separation between CLI presentation and domain logic.
- Core logic is unit-testable without CLI overhead.
- Integration tests verify the full CLI contract.
- Two source projects keep the solution simple while maintaining proper boundaries.
- Sub-namespaces within Core provide logical organization.
