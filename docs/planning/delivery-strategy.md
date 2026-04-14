# Delivery Strategy

- **Document ID:** PLAN-0001
- **Version:** 1.0.0
- **Status:** Accepted

---

## Approach

The project follows an **incremental milestone** delivery model. Each milestone delivers a coherent, testable slice of functionality that builds on previous milestones.

### Sequencing principles

1. **Foundation first.** Project scaffolding, CLI framework, config loading, and file discovery must exist before any feature commands.
2. **Read before write.** Discovery, orientation, outline, and search (read-only surfaces) are built before validation (advisory) and maintenance (mutation).
3. **Validate before maintain.** The validation engine and check command must exist before deterministic maintenance, because maintenance depends on staleness detection.
4. **Markdown query before edit.** The Markdown structural model and query operations must be stable before edit/mutation operations are added.
5. **Preview before apply.** All mutation commands are implemented with preview mode first; apply mode is added in the same or next milestone.
6. **Agent-friendly from the start.** JSON output and structured exit codes are available from v0.1.0, not bolted on later.

### Milestone boundaries

Each milestone:
- Has a clear, testable objective.
- Produces a working (if incomplete) CLI.
- Includes tests for all new functionality.
- Updates documentation where affected.
- Does not break prior milestone functionality.

### Versioning

Semantic versioning: `MAJOR.MINOR.PATCH`.
- `v0.x.0` milestones are feature additions.
- `v1.0.0` is the first complete release covering the planned v1.0 scope.
- Patch versions (`v0.x.1`) are reserved for bug fixes within a milestone.

### Milestone count

10 milestones from v0.1.0 through v1.0.0. This reflects the breadth of the requirements (~130 requirements across 20+ areas). Each milestone covers 1-3 requirement areas and delivers in a focused, reviewable increment.
