---
type: audit
status: Superseded
last_updated: 2026-04-18
---

# CLI Expectation Fidelity Review — 2026-04-16 [Historical Stub]

**Original scope:** Principal-level assessment of whether Steward-on-Steward (v0.10.0) fulfilled the repo's stated CLI promise, workflows, and trust expectations.

**Status:** This document has been reduced to a historical stub. Its full body is superseded by later fidelity assessments. Durable lessons from this review wave are captured in [historical-audit-synthesis.md](historical-audit-synthesis.md).

---

## Key Findings (for traceability)

Referenced by [pre-release-blockers.md](../planning/pre-release-blockers.md) as EF-001 and EF-002:

- **EF-001 / B6:** Scoped validation trust was broken — `check --scope changed|staged` reported repository-wide false failures on a clean tree. → **Resolved in v0.11.0.**
- **EF-002 / B7:** `status --coverage --output json` omitted coverage object. → **Resolved in v0.11.0.**

Overall verdict: "partly met but need targeted correction." The full-repo stewardship loop (orient, status, check, maintain) was assessed as strong. Inner-loop scoped-validation trust was assessed as low until the scoped rule semantics were corrected.

---

## Canonical Successors

- Resolved blockers: [implementation-status.md](../implementation-status.md) §v0.11.0
- Later fidelity view: [cli-expectation-fidelity-assessment-2026-04-17.md](cli-expectation-fidelity-assessment-2026-04-17.md)
- Durable synthesis: [historical-audit-synthesis.md](historical-audit-synthesis.md)
