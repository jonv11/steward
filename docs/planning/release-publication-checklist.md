# Release Publication Checklist

- **Applies to:** Any Steward release (pre-1.0 and stable)
- **Governed by:** [ADR-009 — Packaging and Distribution](../decisions/adrs/ADR-009-packaging-distribution.md), [ADR-013 — Pre-1.0 Versioning](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md)

---

## Prerequisites

- [ ] All pre-release blockers in [pre-release-blockers.md](pre-release-blockers.md) are resolved or explicitly deferred with documented rationale.
- [ ] Version string in `Directory.Build.props` (`Version`, `AssemblyVersion`, `FileVersion`) is updated to the target release version.
- [ ] `steward version` output matches the target release version.
- [ ] CI is green on all three platforms (Windows, Linux, macOS) from `.github/workflows/ci.yml`.

## Local Verification

Run these commands from the repository root:

```bash
# 1. Clean build
dotnet build steward.sln -c Release

# 2. Full test suite
dotnet test steward.sln -c Release --no-build

# 3. Pack the tool
dotnet pack src/Steward.Cli/Steward.Cli.csproj -c Release --no-build

# 4. Verify the .nupkg exists and has the correct version
ls src/Steward.Cli/bin/Release/*.nupkg

# 5. Install from local package and smoke-test
dotnet tool install --global --add-source ./src/Steward.Cli/bin/Release Steward.Cli --version <VERSION>
steward version
steward orient
steward check
dotnet tool uninstall --global Steward.Cli
```

## Tagging

```bash
# Create annotated tag matching Directory.Build.props version
git tag -a v<VERSION> -m "Release v<VERSION>"
git push origin v<VERSION>
```

## NuGet Publication (when applicable)

Publication to nuget.org is an explicit, manual action per ADR-009. It must not happen automatically.

```bash
# Push to NuGet (requires API key)
dotnet nuget push src/Steward.Cli/bin/Release/Steward.Cli.<VERSION>.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

Verify on [nuget.org](https://www.nuget.org/packages/Steward.Cli) that the package appears, then install from the public feed:

```bash
dotnet tool install --global Steward.Cli --version <VERSION>
steward version
```

## Self-Contained Binaries (optional)

Per ADR-009, self-contained single-file executables can be produced for environments without the .NET SDK:

```bash
dotnet publish src/Steward.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish src/Steward.Cli -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish src/Steward.Cli -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

Attach binaries to the GitHub Release if creating one.

## Post-Release

- [ ] Create a GitHub Release from the tag with a changelog summary.
- [ ] Update `docs/implementation-status.md` to reflect the shipped version.
- [ ] Update `docs/planning/milestone-plan.md` to close the milestone.
- [ ] If this was a stable release (`1.0.0+`), update [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md) status.
