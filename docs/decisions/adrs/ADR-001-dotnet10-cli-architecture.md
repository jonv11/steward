---
type: adr
status: Accepted
category: Architecture
---

# ADR-001: .NET 10 CLI Architecture

---

## Context

The project requires a multi-platform CLI tool. .NET 10 (LTS, GA November 2025) is the mandated runtime. We need to choose the CLI framework, establish the application architecture, and set platform expectations.

## Decision

### Runtime and framework

- **Target framework:** `net10.0`
- **CLI framework:** `System.CommandLine` (Microsoft's official CLI parsing library)
- **Language:** C# 13 (ships with .NET 10)

### Why System.CommandLine

- Official Microsoft library, designed for .NET CLI tools.
- Built-in help generation, tab completion, argument parsing, middleware pipeline.
- Clean separation between command definition and handler logic.
- Supports binding to strongly-typed handler parameters.
- Integrates with .NET's dependency injection.

### Application architecture

```
┌─────────────────────────────────────────────┐
│                  CLI Layer                    │
│  Commands, options, output formatting,       │
│  System.CommandLine middleware               │
├─────────────────────────────────────────────┤
│                 Core Layer                   │
│  Domain logic: discovery, policy eval,       │
│  validation, markdown engine, search,        │
│  orientation, maintenance                    │
├─────────────────────────────────────────────┤
│              Infrastructure                  │
│  File system, git integration, YAML parsing, │
│  Markdown parsing (Markdig), glob matching   │
└─────────────────────────────────────────────┘
```

The CLI layer depends on Core. Core defines interfaces for infrastructure concerns (file system, git) and uses dependency injection to receive implementations.

### Platform compatibility

| Platform | Support level |
|----------|--------------|
| Windows (x64, arm64) | Full |
| macOS (x64, arm64) | Full |
| Linux (x64, arm64) | Full |

Cross-platform path handling uses `Path.Combine`, `Path.DirectorySeparatorChar`, and normalized forward-slash paths in all output and policy.

### Dependency injection

Use `Microsoft.Extensions.DependencyInjection` for service registration. Keep it simple:
- Register services in `Program.cs` or a dedicated `ServiceRegistration` class.
- Inject via constructor injection in command handlers.
- No complex DI frameworks.

## Alternatives considered

1. **Spectre.Console.Cli:** Mature and feature-rich, but less aligned with Microsoft's direction. System.CommandLine provides better completion support and is the official solution.
2. **CliFx:** Lighter weight but smaller community. System.CommandLine has broader adoption and Microsoft backing.
3. **Raw `args` parsing:** Not viable for a tool with this many commands and options.
4. **.NET 9 (STS):** Rejected—.NET 10 is LTS and GA by project start. Using the LTS release ensures longer support.

## Consequences

- Modern C# 13 features available (primary constructors, collection expressions, etc.).
- System.CommandLine provides consistent CLI behavior (help, completions, parsing).
- Three-layer architecture keeps domain logic testable and independent of CLI concerns.
- Cross-platform by default with .NET 10.
