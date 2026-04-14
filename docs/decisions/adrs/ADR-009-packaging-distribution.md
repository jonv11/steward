# ADR-009: Packaging and Distribution

- **Status:** Accepted
- **Category:** Distribution

---

## Context

The CLI must be distributable as a multi-platform tool, usable without host-specific lock-in, and compatible with CI and local workflows.

## Decision

### Primary distribution: dotnet tool

The CLI is packaged as a **.NET global tool** and optionally as a **local tool**.

```bash
# Global install
dotnet tool install --global Steward.Cli

# Local install (per-repo)
dotnet tool install Steward.Cli

# Run after local install
dotnet steward check
```

### Secondary distribution: self-contained single-file

For environments without the .NET SDK/runtime, publish self-contained single-file executables:

```bash
dotnet publish src/Steward.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish src/Steward.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish src/Steward.Cli -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

### Target RIDs

| RID | Platform |
|-----|----------|
| `win-x64` | Windows x64 |
| `win-arm64` | Windows ARM64 |
| `linux-x64` | Linux x64 |
| `linux-arm64` | Linux ARM64 |
| `osx-x64` | macOS Intel |
| `osx-arm64` | macOS Apple Silicon |

### NuGet package

- Package ID: `Steward.Cli`
- Tool command name: `steward`
- Published to NuGet.org (when ready for public distribution).

### Project configuration

```xml
<!-- Steward.Cli.csproj -->
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>steward</ToolCommandName>
  <PackageId>Steward.Cli</PackageId>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>false</SelfContained>
</PropertyGroup>
```

### Versioning

Semantic versioning throughout. Assembly version, NuGet package version, and `steward version` output all match.

Version is set via `<Version>` property in the project file or CI-provided MSBuild property.

## Alternatives considered

1. **Container (Docker) distribution only:** Rejected—adds overhead and doesn't work well for local interactive use.
2. **Homebrew/Scoop/apt packages:** Deferred beyond v1.0.0. Can be added once the tool matures.
3. **Native AOT compilation:** Deferred—System.CommandLine and reflection-based YAML parsing may have AOT limitations in .NET 10. Revisit in a future version.

## Consequences

- Easy installation via `dotnet tool install`.
- Self-contained binaries for environments without .NET.
- Cross-platform support for all major OS/arch combinations.
- Standard .NET packaging and versioning model.
- No platform lock-in for installation.
