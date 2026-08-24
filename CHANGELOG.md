# Changelog

All notable changes to Steward are documented in this file.

The format is based on Keep a Changelog. Steward remains on a pre-1.0 SemVer line until `1.0.0` is explicitly authorized per ADR-013, but intentional public `0.x` releases are allowed when the documented release process and readiness evidence are satisfied.

## [Unreleased]

### Fixed

- `steward status --coverage` now counts files matched by an `artifact_families[]` entry as governed. Family-matched files were previously listed as ungoverned even while `status` reported the family as matching them.
- `config doctor` no longer reports `forbidden` and `reserved` path-policy rules that match no files as dead config. For those categories, matching nothing is the success condition.

## [0.18.0] - 2026-06-06

### Added In 0.18.0

- SARIF 2.1.0 output for `steward check` and merge-base-aware `check --since <ref>` validation.
- Deterministic STWD-018 fragment-link auto-fix.
- Closed artifact-family schemas and H1 title-pattern validation (STWD-019).
- H2 section-heading pattern validation (STWD-020) and ordered document section schemas (STWD-021).

### Changed In 0.18.0

- Stabilized the CLI parser dependency on `System.CommandLine` 2.0.0 and aligned the root help snapshot with the stable renderer output.
- Reorganized repository documentation around a small active project spine, a selective docs landing page, and a lifecycle-governed historical archive.
- Moved the master requirements source under `docs/requirements/` and renamed the generated decision index to `docs/decisions/README.md`.

### Fixed In 0.18.0

- Hardened subdirectory execution, config validation, orphan detection, and changed-file resolution following the `v0.18.0` review pass.
- Restricted SARIF output to `steward check` and rejected SARIF as a repository-wide default output format instead of silently emitting text from unsupported commands.

## [0.17.0] - 2026-04-19

### Added In 0.17.0

- Dedicated end-user documentation for maintainers, contributors, AI agents, and configuration authors, plus a guide index under `docs/guide/`.
- Richer machine handoff data in `explain path`, `refs`, and `search`, including provenance, concrete link instances, and section-context selectors.
- A `skill` artifact family that governs `.agents/skills/**/SKILL.md` files and enforces the shared `name`/`description` frontmatter contract.

### Changed In 0.17.0

- `--output json` now always uses the standard envelope (`{ schemaVersion, command, toolVersion, success, exitCode, data }`); the legacy `--json-envelope` compatibility option is removed.
- Deprecated pre-`1.0.0` compatibility shims were narrowed: `check --dry-run` is removed and `validation.required_frontmatter_fields` compatibility is dropped in favor of `governance.frontmatter.required_fields`.
- The README and docs navigation now present Steward through clearer maintainer/contributor/agent paths instead of relying on one large contributor-oriented entry document.

### Fixed In 0.17.0

- Repository discovery now skips inaccessible directories and unreadable nested `.gitignore` files instead of crashing commands like `orient` when run from large or permission-constrained working trees.
- Unhandled CLI exceptions now terminate with stable exit codes and structured error output instead of dumping a raw stack trace to the terminal.
- Contract tests and structured JSON error handling now cover more command and handoff surfaces, while the remaining universal expected-failure-path cleanup is deferred to `v0.18.0`.

## [0.16.0] - 2026-04-18

### Added In 0.16.0

- A tested README "First 15 Minutes" path plus repo-independent source-build guidance, including the `global.json` cross-repo hazard.
- Standard JSON envelope mode (`--json-envelope standard`) and structured JSON error responses across the core command surface (CC-01 through CC-03).
- Richer machine-facing JSON details such as `explain path.exists`, diagnostic `details`, normalized `md query` result shape, and enriched `refactor move` preview/apply payloads (CC-05 through CC-10).
- Confidence and conservative-hint reporting in `config suggest`, plus path-override-aware suppression of low-trust suggestions.
- Clearer Markdown subsystem help and README examples for `md query`, `md edit`, and `fm-validate`.

### Changed In 0.16.0

- `refactor move --apply --output json` now executes the move before formatting output so JSON-mode apply behaves like text-mode apply (CC-04).
- `check` and `config doctor` now keep `success: true` for domain outcomes inside the standard envelope while still using exit codes to distinguish pass from fail.
- Runtime help now consistently uses the public command identity `steward`, and value placeholders are restored on `--config`, `--artifact`, `--role`, and `--max`.
- `check` now distinguishes clean passes from "pass with warnings", and `orient --signals` explicitly describes its cheap/non-exhaustive signal semantics.
- Unified maintenance-source matching across `check`, `status`, and `config doctor` so directory-style and glob-based `maintenance.artifacts[].source` values are interpreted consistently.
- `status` family summaries, `orient` classification, and family min-count enforcement now honor frontmatter-sensitive family criteria instead of path-only matching.

### Fixed In 0.16.0

- Explicit artifacts now inherit family frontmatter, section, naming, and min-count governance instead of silently opting out of family schema.
- Scoped frontmatter overlays now merge allowed-value exceptions with family schema instead of being overwritten, so explicit files like this repo's PRD can intentionally broaden a family baseline.
- Rule-system trust fixes from the completeness/review cycle landed on the release line, including self-contained freshness diagnostics and narrower managed-scope signaling.

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
