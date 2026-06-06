---
type: stub
status: Superseded
last_updated: 2026-04-18
summary: Traceability stub for the early repository audit and its resolved findings
superseded_by: ../audits/historical-audit-synthesis.md
standalone: true
---

# Repository Audit — 2026-04-14 [Historical Stub]

**Original scope:** Full requirement-driven audit and contract-alignment review against the repo state on 2026-04-14. Reviewed PRD, RFC-001–006, ADR-001–009, source code, tests, and runtime behavior.

**Status:** This document has been reduced to a historical stub. It reflects an early repo snapshot. This file is preserved for historical traceability only. Archived implementation-status and planning documents are evidence, not current authority. Current project state lives in [docs/project/status.md](../../project/status.md).

---

## What This Pass Found and Did

The audit reviewed the full requirement set against the then-current implementation and identified concrete divergences. Many were corrected in-pass:

- Restored `steward outline` as the canonical top-level tree command (had drifted to `tree`).
- Added `config show --effective` and strict YAML field validation to `config validate`.
- Made `ValidationEngine` honor `validation.disabled_rules`.
- Reworked `check --output json` to use explicit DTOs with string-valued severities.
- Surfaced policy roles, `start_here`, and cheap signals in `orient` and `status`.
- Added CLI contract and snapshot tests for corrected public surfaces.

## Key Findings That Shaped Later Work

- **Profile layering not implemented:** Profiles were label-only; defaults were not merged into effective policy. → Fixed in v0.10.0.
- **Policy schema drift from RFC-002:** `governance.frontmatter`, `governance.managed_regions`, `governance.completion_policy` were absent from `RepositoryPolicy`. → Addressed progressively v0.10.0–v0.13.0.
- **State document handling missing:** Artifact roles were free-form strings with no behavioral weight. → Still open; tracked in readiness plan.
- **Scoped validation not implemented:** `--scope changed|staged`, `--fix`, `--dry-run` all absent. → Implemented in v0.10.0 through v0.12.0.

## Canonical Successors

- Current project state: [docs/project/status.md](../../project/status.md)
- Durable synthesis of this review wave: [historical-audit-synthesis.md](../audits/historical-audit-synthesis.md)
- Active readiness tracker: [pre-1-0-readiness-plan.md](../plans/pre-1-0-readiness-plan-2026-06-05.md)
