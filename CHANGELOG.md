# Changelog

All notable changes to Steward are documented in this file.

The format is based on Keep a Changelog. Steward remains on a pre-1.0 SemVer line until `1.0.0` is explicitly authorized per ADR-013, but intentional public `0.x` releases are allowed when the documented release process and readiness evidence are satisfied.

## [Unreleased]

### Added In Unreleased

- Standard JSON envelope mode (`--json-envelope standard`) wrapping all JSON output in `{ schemaVersion, command, toolVersion, success, exitCode, data }` (CC-02).
- Structured JSON error responses via `JsonEnvelopeWriter.WriteError()` with `{ kind, message, details, retryable, suggestedNextStep }` on all command error paths (CC-01).
- `exists` boolean field in `explain path` JSON output indicating whether the queried file is present on disk (CC-05).
- `details` field on `Diagnostic` record and JSON diagnostic output, populated by STWD-008 (`targetPath`), STWD-003 (`missingField`), STWD-010/STWD-016 (`expectedPattern`) (CC-06).
- Enriched `refactor move --preview` JSON with `sourceExists`, `destinationExists`, `collision`, `applied`, `affectedFileCount`, per-edit `linkCount` and `rewrites` array (CC-10).
- 16 new contract tests in `JsonContractTests.cs` covering envelope shape, error structure, CC-03 semantics, CC-05/CC-07/CC-08/CC-10 output.

### Changed In Unreleased

- `check` and `config doctor` JSON envelope now reports `success: true` for domain outcomes (violations/findings); exit code still differentiates pass from fail (CC-03).
- `md query` single-file JSON output normalized to match batch shape with `results[]` wrapper containing `matchCount` and `range` per match (CC-07).
- `config validate` JSON errors structured as `[{ file, message }]` objects instead of plain strings (CC-08).
- `refactor move --apply` now executes before output formatting so JSON mode apply actually moves files (CC-04).
- JSON error paths added to `md outline`, `md edit`, `search`, `config validate`, `orient`, `status`, and `maintain` commands (CC-01).

## [0.15.0] - 2026-04-18

### Added In 0.15.0

- Markdown anchor-style selectors for `md query`, including `#anchor-slug` and `README.md#anchor-slug`.
- STWD-017 to warn when a Markdown file repeats heading text after anchor-style normalization.
- Generated directory indexes now require a non-empty child-document `description` frontmatter field and surface a `Status` column when available.
- `governance.frontmatter.auto_fields` now drives `today-if-local-change` frontmatter maintenance for existing fields on locally modified Markdown files.
- Automated nuget.org publication in the tag-driven release workflow using the repository `NUGET_ORG_API_KEY` secret.

### Changed In 0.15.0

- Reconciled the repo version line to `0.15.0` across shared version metadata, changelog, and current-state docs.
- Aligned the packaged .NET tool identity to the product name with `PackageId=Steward` while keeping the command name `steward`.
- Converted the decision index to steward-managed directory-index sections backed by decision-record frontmatter descriptions.
- Expanded the v0.15.0 release notes to include the JSON envelope, Markdown split/extract, severity override, explainability, selector, maintenance, and release-flow work now present in the repo.

## [0.14.0] - 2026-04-17

### Added In 0.14.0

- Tag-driven GitHub Actions release workflow that builds the .NET tool package, curated self-contained bundles (`win-x64`, `linux-x64`, `osx-arm64`), and SHA256 checksums, then attaches them to a GitHub Release.
- Repo-managed release-intent label manifest and sync workflow for `release:none`, `release:patch`, and `release:minor`.
- Dedicated release-process documentation and operator checklist for pre-1.0 releases.

### Changed In 0.14.0

- Clarified pre-1.0 governance so intentional public `0.x` releases are distinct from the separately gated `1.0.0` stable-release authorization.
- Standardized GitHub Release notes to come from the matching `CHANGELOG.md` version entry.

## [0.13.0] - 2026-04-17

### Added In 0.13.0

- Artifact-family support in `policy.yaml`, including deterministic classification and family-aware status, orient, and explain surfaces.
- Type-aware governance validation for required sections, family minimum counts, and family naming patterns (`STWD-014` through `STWD-016`).
- Coverage exclusion config, deeper `config doctor` checks, richer `config suggest` heuristics, and `md edit fm-validate`.
- Cross-platform GitHub Actions CI for build, test, and pack on Windows, Linux, and macOS.
- Release-publication checklist, stronger contract tests, exit-code tests, and pre-1.0 governance hardening.

### Changed In 0.13.0

- Tightened README and planning artifacts to keep the repository explicitly on the `0.x` line until stable authorization exists.
- Standardized preview/apply behavior for mutation commands and improved text-mode orientation and reporting ergonomics.
