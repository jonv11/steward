---
type: project
status: Active
summary: Authoritative operator guide for intentional public releases
last_updated: 2026-06-06
---

# Release Process

---

## Purpose

This document is the authoritative operator guide for intentional public releases on the Steward pre-`1.0.0` line.

It defines how version bumps, labels, changelog entries, GitHub Actions, GitHub Releases, and NuGet publication fit together so the repo has one coherent release story.

## Governance Summary

- Steward remains on the `0.x.y` line until a separate accepted decision explicitly authorizes `1.0.0`.
- Public `0.x` releases are allowed when they are intentional, evidence-backed, and honestly described as pre-stable.
- `Directory.Build.props` is the source of truth for the current version.
- Do not bump versions casually. Every bump must follow ADR-013 and be reflected in the changelog and current-state docs.
- GitHub Releases and release assets do not imply stable-release authorization.

## Release Intent Labels

Steward uses a small release-intent label set on pull requests to keep pre-1.0 bump decisions reviewable:

- `release:none`
  Changes that do not justify a standalone release on their own.
- `release:patch`
  Bug fixes, packaging corrections, compatibility fixes, documentation fixes, and similar scoped corrections suitable for `0.x.(y+1)`.
- `release:minor`
  Intentional product-scope advancement suitable for `0.(x+1).0`.

### Label Policy

- Every non-draft pull request targeting the default branch must carry exactly one release-intent label.
- The workflow `.github/workflows/pr-release-intent.yml` enforces that rule.
- The repository-managed label definitions live in `.github/release-labels.json`.
- The workflow `.github/workflows/release-labels.yml` or the local `scripts/release/Sync-ReleaseLabels.ps1` script can sync the labels into GitHub.

### How Labels Drive The Bump

- The next release bump must be at least the highest release-intent label merged since the previous release.
- Any merged `release:minor` change means the next release must be a minor bump.
- If there are no merged `release:minor` changes but there are merged `release:patch` changes, the next release may be a patch bump.
- `release:none` does not force a release by itself, but its changes may still be included in the next planned release.

The labels inform the bump decision; they do not auto-edit `Directory.Build.props`.

## Changelog Policy

- `CHANGELOG.md` is the single release-notes source.
- Keep an `## [Unreleased]` section at the top for changes not yet shipped in a tag.
- When cutting a release, move the relevant `Unreleased` entries into a new version section using the format `## [x.y.z] - YYYY-MM-DD`.
- GitHub Release notes are generated from the matching version section in `CHANGELOG.md`.
- If there is no matching changelog section for the tag version, the release workflow fails rather than inventing notes.

## Published Asset Set

Each GitHub Release currently publishes:

- `Steward.<version>.nupkg`
- `steward-<version>-win-x64.zip`
- `steward-<version>-linux-x64.zip`
- `steward-<version>-osx-arm64.zip`
- `SHA256SUMS.txt`

### Why These Assets

- The `.nupkg` is the primary distribution artifact for `dotnet tool install`.
- The self-contained bundles provide a direct-download path from the GitHub Releases page without requiring a preinstalled .NET runtime.
- The automated binary set is intentionally a curated minimum: Windows x64, Linux x64, and macOS Apple Silicon.
- Additional RIDs allowed by ADR-009 remain possible as manual publishes when needed, but they are not yet part of the automated GitHub Release asset set.

## End-To-End Operator Flow

### 1. Decide the version bump

- Review merged pull requests since the previous release and inspect their release-intent labels.
- Choose the smallest SemVer bump allowed by ADR-013 and justified by those labels.
- Confirm the repo is still staying on the `0.x` line unless a separate stable-release ADR says otherwise.

### 2. Update the repository truth

- Update `Directory.Build.props` to the target version.
- Update `CHANGELOG.md` by moving the intended release notes out of `Unreleased` into a new dated version section.
- Update current-state and planning docs if the new version or release posture changes what they claim.

### 3. Verify locally

Run the checklist in [release-publication-checklist.md](release-publication-checklist.md). At minimum:

```bash
npm ci
npm run lint:md
dotnet build steward.sln -c Release
dotnet test steward.sln -c Release --no-build
dotnet run --project src/Steward.Cli -c Release --no-build -- check
pwsh ./scripts/release/Build-ReleaseAssets.ps1 -Version <VERSION>
pwsh ./scripts/release/Export-ReleaseNotes.ps1 -Version <VERSION>
```

### 4. Merge the release-prep change

- Merge the reviewed version/changelog/docs update to the default branch.
- Do not create the release tag from an unmerged branch or a dirty working tree.

### 5. Create and push the tag

Create an annotated tag whose name matches the version:

```bash
git tag -a v<VERSION> -m "Release v<VERSION>"
git push origin v<VERSION>
```

### 6. Let GitHub Actions publish the GitHub Release

- `.github/workflows/release.yml` runs on the tag.
- It validates that the tag matches `Directory.Build.props`.
- It installs the repo-local Markdown lint dependencies, runs `npm run lint:md`, restores, builds, tests, runs `steward check`, packages, exports release notes from `CHANGELOG.md`, creates or updates the GitHub Release, and uploads the release assets.

### 7. Let GitHub Actions publish to NuGet

- `.github/workflows/release.yml` publishes the packaged .NET tool to nuget.org using the repository secret `NUGET_ORG_API_KEY`.
- Publication uses `dotnet nuget push --skip-duplicate` so rerunning a tagged release is safe when the package already exists.
- The workflow fails if the secret is missing or if no package artifact was produced.

### 8. Verify post-release state

- Confirm the GitHub Release page has the expected notes and assets.
- Download at least one bundle and verify the checksum.
- Confirm the package appears on nuget.org.
- Install the `.nupkg` locally or from the release asset and smoke-test `steward version`, `steward orient`, and `steward check`.

## What Is Still Intentionally Manual

- Version bump selection and the actual edit to `Directory.Build.props`
- Changelog curation
- Expanding the automated binary matrix beyond the curated minimum
- Any future stable-release authorization decision for `1.0.0`

## Deferred

- Automatic version bumping directly from merged pull-request labels
- Automatic changelog generation
- Stable-release (`1.0.0`) workflow logic
