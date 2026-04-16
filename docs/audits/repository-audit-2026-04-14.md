# Repository Audit — 2026-04-14

> Historical scope note (2026-04-16): This audit is preserved as evidence of the repository state on 2026-04-14. Current authoritative state now lives in [implementation-status.md](../implementation-status.md) and the active planning documents under [docs/planning/](../planning-index.md).

## 1. Repository Understanding

Steward is intended to be a repository stewardship CLI, not just a validator. The accepted PRD and RFCs position it as a dual-audience tool for repository maintainers, human contributors, and AI coding agents who need orientation, validation, structural Markdown operations, deterministic maintenance, and explainability from the same contract-driven surface.

The current repository is past initial scaffolding and already demonstrates real value in several areas:

- Versioning, JSON/text output, file discovery, Markdown parsing, search, stale-artifact detection, and maintenance all exist and are tested.
- The codebase has a clean split between CLI and core libraries, and the test suite is meaningful rather than placeholder-only.
- The repo is still pre-release in product terms. It behaves like a strong prototype spanning roughly the accepted v0.2.0 through v0.8.0 milestones, with selected v0.9.0 features, but it does not yet satisfy the full v1.0.0 contract.

Release-readiness maturity after this pass:

- Product maturity: late prototype / early release-candidate.
- Engineering maturity: solid core architecture and testability.
- Contract maturity: improved materially in this pass, but still partial in scoped validation, profile layering, workflow completeness, and Markdown structural breadth.

## 2. Requirement Coverage Assessment

| Area | State | Evidence | Assessment |
|------|-------|----------|------------|
| Core identity | Partial | PRD 8.1, RFC-001, `src/Steward.Cli`, `src/Steward.Core` | Steward now presents a coherent stewardship surface with orient, outline, search, check, maintain, status, and explain, but some commands still behave more like point features than a fully integrated stewardship workflow. |
| Configuration and policy | Partial | RFC-002, `ConfigLoader`, `ConfigCommand`, README | Separation of runtime config and repository policy exists and is now stricter, but full profile overlay/merge semantics from the accepted docs are not implemented. |
| Path policy | Partial | PRD 8.11, `PathPolicyDocument`, `PathPolicyEngine`, `ForbiddenPathRule` | Forbidden-path evaluation works, but the accepted ruleset model is only partially implemented and required-path obligations from `path-policy.yaml` are still not enforced by `check`. |
| Validation and diagnostics | Partial | RFC-003, `ValidationEngine`, `CheckCommand`, tests | Deterministic validation, disabled-rule filtering, text/JSON output, and stale/broken-link checks work. Scoped validation, `--fix`, `--dry-run`, and completion-policy checks remain missing. |
| Orientation / outline / search | Partial | RFC-005, `OrientCommand`, `OutlineCommand`, `SearchCommand` | `outline` is restored as the top-level command, `orient` now surfaces policy roles, `start_here`, and cheap signals, and search is useful. Orientation is still less curated than the accepted RFC describes. |
| Markdown structural operations | Partial | RFC-004, `MdCommand`, `MdEditCommand`, `StructuralEditor`, tests | Query, outline, section edits, and frontmatter set/merge exist. Wildcard selectors, content-type selectors, `fm-validate`, richer placement options, and the accepted `md edit <file> <operation>` UX remain incomplete. |
| Maintenance and workflow | Partial | RFC-006, `MaintenanceEngine`, `StatusCommand`, `StaleArtifactRule` | Deterministic maintenance, preview-before-apply, stale detection, and status are in place. Completion-policy rules, richer state/memory roles, and full maintainer/agent workflow guidance are not yet complete. |
| Output contracts | Partial | ADR-006, `JsonOutputFormatter`, `CheckCommand`, CLI tests | Text and JSON outputs are present. This pass fixed the `check` JSON schema to use explicit DTOs and string severities, but other outputs are still mostly implicit object shapes rather than fully documented contracts. |
| Docs and onboarding | Partial | README, planning docs, new audit | README is materially closer to the implementation after this pass. The planning/decision docs remain strong. A maintainer migration guide and a concise command reference are still missing. |
| Test coverage | Partial | ADR-007, `tests/Steward.Core.Tests`, `tests/Steward.Cli.Tests` | Coverage is strong for core logic and improved for CLI contract alignment in this pass. Snapshot coverage is now present for help and validation JSON, but output stability is not yet covered for every major command. |

### What Is Implemented Well

- `version`, `search`, `maintain`, and stale-artifact detection are real, usable features rather than placeholders.
- The core architecture is cohesive: configuration, discovery, markdown, search, maintenance, and validation are separated cleanly.
- Tests are behavior-first and fast. They exercise real command invocation and core logic instead of only inspecting DTOs.
- This pass repaired several contract-critical mismatches:
  - `outline` is again the canonical top-level command.
  - `config show --effective` now exists.
  - `config validate` now rejects unknown fields and invalid profile names.
  - `check` now honors `validation.disabled_rules`.
  - `check --output json` now emits string-valued severities from explicit DTOs.
  - `orient` and `status` now surface policy context more usefully.

### Partial or Incorrect Areas

- Profile support is still mostly labeling and init scaffolding. Accepted repo-local profile layering is not implemented.
- Path-policy coverage is narrower than the accepted RFC and PRD describe.
- Validation is still repository-wide only. Accepted `changed`, `staged`, `paths`, `--fix`, and `--dry-run` behaviors are absent.
- Orientation is improved but still flatter and less curated than the accepted product description.
- Markdown structural support is useful but narrower than the accepted mdpath and edit contract.
- Completion policy and richer workflow guidance remain largely planned, not implemented.

### Documented but Not Implemented

- RFC-003 scoped validation and fix flow: `--scope changed|staged`, `--paths`, `--fix`, `--dry-run`.
- RFC-004 wildcard/content-type mdpath selectors, `fm-validate`, sibling-placement options, and richer ownership enforcement.
- RFC-006 completion-policy and broader memory/state artifact support.
- ADR-009 package-readme expectation is not yet satisfied; `dotnet pack` still warns about a missing NuGet package README.

### Implemented but Previously Underdocumented

- The repo already had meaningful stale-artifact checking, internal-link validation, and frontmatter editing.
- The repo already had real maintenance artifact types and a usable status surface.
- This pass made the effective runtime config and policy-driven orientation/status behavior discoverable in the CLI and README.

### Stale or Contradictory Material

- The accepted planning docs correctly described `outline`, while the working tree had drifted toward `tree`. This pass reconciled the CLI, README, and tests back to the accepted command structure.
- The traceability document still uses milestone status values like `Planned` instead of implementation status. It remains useful for mapping, but not as a live progress source.

## 3. Maintainer Usability Assessment

Maintainer usability is materially better after this pass, but still not fully easy.

What now works well:

- A maintainer can discover `.steward/config.yaml` and `.steward/policy.yaml`, validate them, and see the raw files plus effective runtime defaults with `steward config show --effective`.
- Configuration mistakes are caught earlier and more honestly. Unknown fields and invalid profiles now fail fast instead of being silently ignored.
- `orient` and `status` now surface configured `start_here` entries and policy roles, which makes repo-specific onboarding more visible.

What still degrades the experience:

- Profile semantics are not yet what the accepted RFC promises. A maintainer can select a profile name, but full layering and overlay behavior are still not there.
- Path-policy authoring is only partially aligned with the documented model, which makes advanced policy authoring riskier than the docs imply.
- There is still no maintainer-focused migration guide for onboarding an unconfigured repository or evolving policy safely over time.

Overall assessment:

- Maintainers can configure and validate the tool for straightforward scenarios.
- Maintainers cannot yet rely on every documented policy mechanism behaving as described in the accepted design docs.

## 4. Human and AI Companion Assessment

The CLI is closer to the intended companion role after this pass, especially for inspect-first loops:

- `orient` is now more trustworthy for session-start context because it can surface repo-defined roles, `start_here`, and cheap missing/stale signals.
- `check` now produces a cleaner machine-readable contract for agents.
- `config show --effective` removes one of the biggest hidden-state problems for both humans and agents.

Current limitations for companion quality:

- The accepted AI inner loop is still incomplete because scoped validation and deterministic `check --fix` do not exist.
- Markdown structural operations are usable but not broad enough yet to support the full accepted mdpath contract.
- Some output schemas are still implicit and not yet documented as stable public contracts.

Overall assessment:

- Stronger companion than before this pass.
- Still not fully the “repository stewardship companion” promised by the PRD; it remains partly a stewardship tool and partly a collection of solid feature surfaces.

## 5. Documentation Audit

Strong areas:

- The accepted PRD, RFCs, ADRs, and milestone/implementation docs are unusually strong. They are concrete, structured, and useful as real engineering source material.
- The README is now materially closer to the implemented command surface after this pass.
- The new audit artifact provides an explicit release-readiness record inside the repo.

Weak or missing areas:

- README still has to balance accepted intent against partial implementation. It is improved, but it cannot yet present every accepted feature as implemented.
- There is still no concise end-user command reference beyond help output and README summary tables.
- There is still no maintainer migration/onboarding guide for unconfigured repositories.
- The traceability document is not a live implementation dashboard and should not be read that way.

Documentation verdict:

- High-quality governing docs.
- Improved user-facing docs.
- Still missing the maintainer-operational layer that would make adoption truly low-friction.

## 6. Test Audit

What is strong:

- Core coverage is substantial across config loading, discovery, path policy, markdown parsing/editing, search, maintenance, and validation rules.
- CLI tests now cover restored command registration, strict config validation behavior, `config show --effective`, `orient --signals`, disabled-rule filtering, and `check` JSON severity strings.
- Snapshot coverage now exists for root help and `check --output json`, with invariant-culture normalization in the test helper so snapshots are stable for agent-oriented review.

What remains weaker than the accepted strategy:

- Snapshot coverage does not yet cover every major text/JSON command surface.
- Cross-platform output verification is still represented by design intent and local testability, not by checked-in multi-platform execution evidence.
- Search and maintenance still have fewer end-to-end contract tests than the accepted v1.0.0 test strategy envisions.

Test verdict:

- The test suite meaningfully supports the current implementation.
- The repo is still short of the fully documented release-readiness test bar.

## 7. Implemented Improvements

This audit pass changed the repository in the following concrete ways:

- Restored `steward outline` as the canonical top-level tree/outline command and updated CLI tests accordingly.
- Added `Program.CreateRootCommand()` and aligned the test helper with the real root-command surface and parse-error exit-code mapping.
- Removed `Environment.Exit` from config-loading flow and replaced it with reusable error handling in `CommandSetup.TryBuild(...)`.
- Added effective runtime resolution to `CommandContext` so commands can surface what the CLI is actually using.
- Made config parsing strict for unknown YAML fields and invalid profile names.
- Added `steward config show --effective` with raw-file output plus resolved runtime defaults.
- Changed `config validate` to report configuration errors as usage/config failures instead of repo-validation failures.
- Made `ValidationEngine` honor `validation.disabled_rules`.
- Reworked `check --output json` to use explicit DTOs with string-valued severities and deterministic diagnostic ordering.
- Made `orient` and `status` surface policy roles, `start_here`, repo type/profile context, and cheap missing/stale signals.
- Updated the repo’s own `.steward/config.yaml` to a valid profile and added `start_here` entries to `.steward/policy.yaml`.
- Added CLI contract tests and Verify snapshot tests for the corrected public surfaces.
- Added this audit document and linked it from `docs/planning-index.md`.

## 8. Remaining High-Value Follow-Up Items

1. Implement the accepted scoped-validation and fix contract.
   This is the biggest remaining gap for the human/agent workflow loop: `changed`, `staged`, `paths`, `--fix`, and `--dry-run`.

2. Complete profile layering and effective policy/runtime merging.
   The accepted configuration model promises more than the current implementation delivers.

3. Finish the accepted path-policy contract.
   Required-path obligations from `path-policy.yaml`, richer rule shapes, and documented precedence behavior still need to be aligned fully.

4. Expand Markdown structural parity with RFC-004.
   Wildcard selectors, content-type selectors, `fm-validate`, richer placement options, and tighter ownership behavior are still needed.

5. Implement completion-policy and richer workflow guidance.
   This is necessary before Steward fully matches the PRD’s “what is pending / what should be done next / is work complete?” promise.

6. Broaden stable output-contract coverage.
   More JSON outputs should use explicit response DTOs and have snapshot-backed text/JSON tests.

7. Add maintainer onboarding and migration docs.
   A practical guide for adopting Steward in a new repository would materially improve product usability.

8. Add package README support for NuGet distribution.
   `dotnet pack` currently succeeds but warns about the missing package README, which should be addressed before release.
