---
type: planning
status: Active
last_updated: 2026-04-18
---

# Release Publication Checklist

- **Applies to:** Any intentional Steward release (pre-1.0 and stable)
- **Governed by:** [ADR-009 — Packaging and Distribution](../decisions/adrs/ADR-009-packaging-distribution.md), [ADR-013 — Pre-1.0 Versioning](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md)
- **Process guide:** [Release Process](release-process.md)

---

## Prerequisites

- [ ] All pre-release blockers in [pre-release-blockers.md](pre-release-blockers.md) are resolved or explicitly deferred with documented rationale.
- [ ] The chosen bump matches ADR-013 and the highest merged release-intent label since the previous release.
- [ ] Version string in `Directory.Build.props` (`Version`, `AssemblyVersion`, `FileVersion`) is updated to the target release version.
- [ ] `CHANGELOG.md` contains a dated section for the target release version and `Unreleased` is adjusted accordingly.
- [ ] `steward version` output matches the target release version.
- [ ] CI is green on all three platforms (Windows, Linux, macOS) from `.github/workflows/ci.yml`.
- [ ] If this is the first use of the tag-driven GitHub Release workflow, the workflow definition has been reviewed and the label set in `.github/release-labels.json` has been synchronized into GitHub.

## Local Verification

Run these commands from the repository root:

```bash
# 1. Clean build
dotnet build steward.sln -c Release

# 2. Full test suite
dotnet test steward.sln -c Release --no-build

# 3. Build release assets and checksums
pwsh ./scripts/release/Build-ReleaseAssets.ps1 -Version <VERSION>

# 4. Export release notes from the changelog
pwsh ./scripts/release/Export-ReleaseNotes.ps1 -Version <VERSION>

# 5. Install from the locally built package and smoke-test
dotnet tool install --tool-path ./.tools/steward --add-source ./artifacts/release/v<VERSION> Steward --version <VERSION>
./.tools/steward/steward version
./.tools/steward/steward orient
./.tools/steward/steward check
```

## Tagging

```bash
# Create annotated tag matching Directory.Build.props version
git tag -a v<VERSION> -m "Release v<VERSION>"
git push origin v<VERSION>
```

The push triggers `.github/workflows/release.yml`, which rebuilds/tests, validates the tag against `Directory.Build.props`, creates or updates the GitHub Release, and uploads the published assets.

## NuGet Publication

The tagged release workflow publishes to nuget.org automatically when `NUGET_ORG_API_KEY` is configured in GitHub.

```bash
# Optional local fallback if the workflow publish needs manual recovery
dotnet nuget push artifacts/release/v<VERSION>/Steward.<VERSION>.nupkg \
  --api-key <NUGET_ORG_API_KEY> \
  --source https://api.nuget.org/v3/index.json \
  --skip-duplicate
```

Verify on [nuget.org](https://www.nuget.org/packages/Steward) that the package appears, then install from the public feed:

```bash
dotnet tool install --global Steward --version <VERSION>
steward version
```

## Post-Release

- [ ] Confirm the GitHub Release page contains the `.nupkg`, all expected zipped bundles, and `SHA256SUMS.txt`.
- [ ] Confirm the GitHub Release notes match the target `CHANGELOG.md` entry.
- [ ] Download at least one asset and verify its checksum.
- [ ] Update `docs/implementation-status.md` to reflect the shipped version.
- [ ] Update `docs/planning/milestone-plan.md` to close the milestone.
- [ ] If this was a stable release (`1.0.0+`), update [pre-1-0-readiness-plan.md](pre-1-0-readiness-plan.md) status.
