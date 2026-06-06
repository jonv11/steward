---
type: audit
status: Historical
standalone: true
---
# Repo Quality Hardening Pass — 2026-04-18

## Scope

This pass targeted implementation-quality issues that were safe to correct without redesigning Steward or changing intended product behavior. The goal was to reduce inconsistency, eliminate real false positives/false negatives, and leave the codebase easier to extend in later pre-1.0 work.

## Baseline

Before changes, the repository passed:

- `dotnet build steward.sln -c Release`
- `dotnet test steward.sln -c Release --no-build`
- `npm run lint:md`
- `dotnet run --project src/Steward.Cli -c Release --no-build -- check`

## What Was Fixed

### 1. Maintenance source matching is now consistent across repo surfaces

The pass introduced a shared maintenance-source matcher so commands no longer interpret `maintenance.artifacts[].source` differently. This corrected a real inconsistency where:

- `check` impact signals handled only simple prefix matching
- staged completeness handled only simple prefix matching
- `config doctor` treated sources as glob-only matches
- coverage logic had separate ad hoc matching logic

The result is one coherent rule for exact-path, directory-prefix, and glob-based maintenance sources.

### 2. Frontmatter-based artifact families now classify correctly on reporting surfaces

`status` family summaries and `orient` classification previously treated family matching as path-only on those surfaces, even though the core family model supports frontmatter criteria. This pass made those surfaces load frontmatter only when needed so reporting stays aligned with the actual family contract.

### 3. `config doctor` no longer reports conflicting allowed-values when scopes do not overlap

The conflicting-allowed-values check computed family and requirement globs but never verified real overlap on discovered files. That created false positives and left dead logic behind. The check now warns only when a discovered path actually matches both scopes.

### 4. Test coverage now protects the risky paths that were corrected

New tests cover:

- directory, exact-file, and glob maintenance-source matching
- glob-based impact signal generation
- glob-based staged completeness reporting
- directory-style maintenance sources in `config doctor`
- overlapping vs non-overlapping allowed-values conflict detection
- frontmatter-based family classification in `status` and `orient`

## Intentionally Deferred

### 1. Broad command-file decomposition

Large command files remain, but this pass avoided churn-heavy extraction work where the code was large yet still coherent enough to leave alone for now.

### 2. Cross-cutting internal-error contract hardening

The repo still deserves a focused pass on unexpected-exception handling and internal-error output consistency. That work touches top-level process behavior and JSON/text error contracts and was left for a dedicated change rather than mixed into this pass.

## Risks And Follow-Up

- The new maintenance-source matcher uses a practical directory-vs-file heuristic for non-glob sources. It is correct for the current repository patterns and new tests, but future work should keep source-shape expectations explicit in docs and validation.
- Frontmatter-aware family reporting now parses files when families depend on frontmatter. This is intentionally lazy and bounded, but future performance-sensitive work should keep an eye on repeated parse patterns across CLI surfaces.

## Validation After Changes

The pass closed with:

- targeted core and CLI test-project runs green after each change cluster
- full repo build, test, Markdown lint, and `steward check` rerun
- governed navigation updated for the new plan and audit artifacts
