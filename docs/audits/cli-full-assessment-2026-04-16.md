---
type: audit
status: Superseded
last_updated: 2026-04-18
---

# CLI Full Assessment — 2026-04-16 [Historical Stub]

**Original scope:** Comprehensive end-to-end product assessment of Steward v0.10.0: build, exercise, cross-reference, diagnose, score.

**Status:** This document has been reduced to a historical stub. Its full body is superseded by later review artifacts. Durable lessons from this review wave are captured in [historical-audit-synthesis.md](historical-audit-synthesis.md).

---

## Key Findings (for traceability)

The original document identified five critical issues, referenced by [pre-release-blockers.md](../planning/pre-release-blockers.md) as F1–F5:

- **F1 / B6:** Scoped validation (`check --scope changed|staged`) produced false positives on clean trees (Files checked: 0 while reporting missing required artifacts). → **Resolved in v0.11.0.**
- **F2:** Preview/apply conventions inconsistent across check, maintain, and refactor move (three different patterns). → **Addressed in v0.12.0** (unified `--fix`/`--fix --apply` pattern).
- **F3 / B7:** `status --coverage --output json` silently dropped coverage data. → **Resolved in v0.11.0.**
- **F4:** `md query --pattern` batch mode broken due to argument parsing ambiguity. → **Resolved in v0.12.0.**
- **F5:** `init` scaffolded policy that immediately failed `check` on a new repo. → **Resolved in v0.12.0** (init scaffolding fix).

The review also affirmed strong areas: full-scope stewardship loop, governance model, config introspection, Markdown structural subsystem, and self-dogfooding quality.

---

## Canonical Successors

- Current state: [implementation-status.md](../implementation-status.md)
- Later fidelity view: [cli-expectation-fidelity-assessment-2026-04-17.md](cli-expectation-fidelity-assessment-2026-04-17.md)
- Current release/readiness reviews: [maintainer-remarks-implementation-summary-2026-04-18.md](maintainer-remarks-implementation-summary-2026-04-18.md)
- Durable synthesis: [historical-audit-synthesis.md](historical-audit-synthesis.md)
