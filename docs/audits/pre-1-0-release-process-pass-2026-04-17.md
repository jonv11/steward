---
type: audit
status: Active
last_updated: 2026-04-17
---

# Pre-1.0 Release Process Pass — 2026-04-17

## Summary

This pass completed the repository's missing release-process spine for intentional public `0.x` releases without implying `1.0.0`.

## What Already Existed

- Centralized version source of truth in `Directory.Build.props`
- Cross-platform CI for build, test, and pack
- Accepted packaging and versioning ADRs
- A publication checklist
- Local `dotnet pack` viability

## Gaps Found

- No GitHub Release workflow that attached downloadable assets to a release page
- No changelog file serving as the canonical release-notes source
- No repo-managed release-intent label strategy
- Docs still blurred public pre-`1.0.0` release capability with separately gated stable-release authorization
- Release-process knowledge lived mostly in scattered docs, not one operator guide

## Changes Made

- Added a repo-managed release label manifest: `.github/release-labels.json`
- Added a PR workflow to require exactly one release-intent label on non-draft pull requests to the default branch
- Added a label-sync workflow plus a local sync script
- Added a tag-driven GitHub Release workflow that builds/tests, creates release assets, derives notes from `CHANGELOG.md`, and uploads assets to the GitHub Release page
- Added release helper scripts for asset creation and changelog note export
- Added `CHANGELOG.md`
- Added `docs/planning/release-process.md`
- Updated governance and planning docs to distinguish public `0.x` releases from the separately gated `1.0.0`

## Decisions Made

- Public `0.x` releases are allowed when readiness evidence is green and docs remain honest; this does not authorize `1.0.0`
- GitHub label intent is informative and enforced on PRs, but version bumps remain deliberate maintainer edits rather than fully automatic mutations
- GitHub Release notes come from curated changelog entries, not generated summaries
- Automated GitHub Release assets are the `.nupkg`, three curated self-contained bundles (`win-x64`, `linux-x64`, `osx-arm64`), and checksums
- NuGet publication remains manual and optional

## Deferred

- Hosted execution evidence for the new release workflow and the existing CI matrix
- Automatic bump calculation from merged PR labels
- Automatic changelog generation
- Automatic NuGet publication
- Stable-release (`1.0.0`) authorization and stable-release workflow logic

## How To Cut The Next 0.x Release

1. Review merged PR release-intent labels since the previous release.
2. Choose the smallest valid pre-1.0 bump per ADR-013.
3. Update `Directory.Build.props`, `CHANGELOG.md`, and active truth docs in one reviewed change.
4. Run the local release checklist.
5. Merge to the default branch.
6. Create and push `v<VERSION>`.
7. Let `.github/workflows/release.yml` publish the GitHub Release and assets.
8. Optionally push the attached `.nupkg` to NuGet afterward.
