---
type: planning
status: Active
last_updated: 2026-04-18
---

# Repo Quality Hardening Pass Plan

## Purpose

Drive a disciplined implementation-quality pass across the current Steward repository without changing intended product behavior or introducing speculative architecture.

## Scope And Guardrails

- Preserve externally intended behavior unless a change is clearly a bug fix or a correction of an obviously bad implementation.
- Prefer small, reviewable fixes that improve maintainability, correctness, or testability.
- Keep docs, changelog entries, and governed artifacts aligned with the code changes made in this pass.
- Treat active repo documents as source of truth and record any deferred work explicitly.

## Must-Fix Now

### 1. Maintenance-source matching inconsistency

- Problem: multiple surfaces interpret `maintenance.artifacts[].source` differently. Some handle directory-style sources, some assume glob-only matching, and some only do simple prefix checks.
- Risk: false negatives in `check` impact signals and staged completeness; false positives in `config doctor`; future contributors have to reason about multiple inconsistent matching rules.
- Remediation: introduce one shared maintenance-source matching path and reuse it in the affected command/reporting surfaces.
- Behavior-preservation care: preserve current behavior for directory-prefix sources while adding correct glob support.

### 2. Family classification gaps on reporting surfaces

- Problem: some reporting/classification surfaces classify families by path only and ignore frontmatter-based family criteria.
- Risk: `status` and `orient` can drift from the repository contract when families depend on frontmatter, making the tool less trustworthy on richer policies.
- Remediation: make family-aware reporting surfaces load frontmatter only when needed and classify consistently with the core family engine.
- Behavior-preservation care: keep explicit `artifacts[]` precedence unchanged.

### 3. `config doctor` false-positive conflict detection

- Problem: the conflicting-allowed-values check computes family and requirement globs but never actually verifies that they overlap on real files.
- Risk: misleading findings, dead code, and avoidable operator churn when policies use different non-overlapping document groups.
- Remediation: evaluate conflicts only when a discovered path matches both scopes.
- Behavior-preservation care: keep the warning when real overlap exists; only remove spurious findings.

## Acceptable To Defer

### 1. Broad command-file decomposition

- Rationale: several command files are large, but most of the current size comes from output-shape assembly and command wiring rather than clearly broken logic. Splitting them now would create more churn than value.

### 2. Cross-cutting internal-error contract hardening

- Rationale: the repo documents an internal-error exit path, but fully standardizing unexpected-exception handling across text and JSON surfaces is broader than this pass. It should be tackled deliberately as a focused contract-hardening change if prioritized.

## Validation Plan

- Run targeted tests after each change cluster.
- Re-run `dotnet build steward.sln -c Release`, `dotnet test steward.sln -c Release --no-build`, `npm run lint:md`, and `dotnet run --project src/Steward.Cli -c Release --no-build -- check` before closing the pass.
- Refresh maintained artifacts if new docs alter generated surfaces.
