# Changelog

All notable changes to Steward are documented in this file.

The format is based on Keep a Changelog. Steward remains on a pre-1.0 SemVer line until `1.0.0` is explicitly authorized per ADR-013, but intentional public `0.x` releases are allowed when the documented release process and readiness evidence are satisfied.

## [Unreleased]

### Added

- No unreleased entries yet.

## [0.14.0] - 2026-04-17

### Added

- Tag-driven GitHub Actions release workflow that builds the .NET tool package, curated self-contained bundles (`win-x64`, `linux-x64`, `osx-arm64`), and SHA256 checksums, then attaches them to a GitHub Release.
- Repo-managed release-intent label manifest and sync workflow for `release:none`, `release:patch`, and `release:minor`.
- Dedicated release-process documentation and operator checklist for pre-1.0 releases.

### Changed

- Clarified pre-1.0 governance so intentional public `0.x` releases are distinct from the separately gated `1.0.0` stable-release authorization.
- Standardized GitHub Release notes to come from the matching `CHANGELOG.md` version entry.

## [0.13.0] - 2026-04-17

### Added

- Artifact-family support in `policy.yaml`, including deterministic classification and family-aware status, orient, and explain surfaces.
- Type-aware governance validation for required sections, family minimum counts, and family naming patterns (`STWD-014` through `STWD-016`).
- Coverage exclusion config, deeper `config doctor` checks, richer `config suggest` heuristics, and `md edit fm-validate`.
- Cross-platform GitHub Actions CI for build, test, and pack on Windows, Linux, and macOS.
- Release-publication checklist, stronger contract tests, exit-code tests, and pre-1.0 governance hardening.

### Changed

- Tightened README and planning artifacts to keep the repository explicitly on the `0.x` line until stable authorization exists.
- Standardized preview/apply behavior for mutation commands and improved text-mode orientation and reporting ergonomics.
