---
type: adr
status: Accepted
category: Distribution
---

# ADR-009: Packaging and Distribution

---

## Context

The CLI must be distributable as a multi-platform tool, usable without host-specific lock-in, and compatible with CI and local workflows.

## Decision

### Primary distribution: dotnet tool

The CLI is packaged as a **.NET tool** and may be installed either from a locally built package or, later, from an intentional public feed publication.

```bash
# Build the current pre-1.0 package locally
dotnet pack src/Steward.Cli -c Release

# Install from the local package source
dotnet tool install --global --add-source ./src/Steward.Cli/bin/Release Steward.Cli --version <VERSION>

# When a public release is intentionally published, the same package id is used.
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
- Public publication is optional and must be an explicit release action; active repo docs must not imply that publication already happened.
- When GitHub Releases are used, the release page should attach the `.nupkg`, published binary bundles, and checksums so the page is directly useful as a download surface.

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

Versioning is governed by [ADR-013](ADR-013-pre-1-0-versioning-and-release-authorization.md).

The source of truth is `Directory.Build.props`. Assembly version, NuGet package version, and `steward version` output must derive from that shared property set.

## Alternatives considered

1. **Container (Docker) distribution only:** Rejected—adds overhead and doesn't work well for local interactive use.
2. **Homebrew/Scoop/apt packages:** Deferred to a later pre-1.0 or stable milestone once the tool matures and release operations are in place.
3. **Native AOT compilation:** Deferred—System.CommandLine and reflection-based YAML parsing may have AOT limitations in .NET 10. Revisit in a future version.

## Consequences

- Easy installation via `dotnet tool install`.
- Self-contained binaries for environments without .NET.
- Cross-platform support for all major OS/arch combinations.
- Standard .NET packaging and versioning model.
- No platform lock-in for installation.
