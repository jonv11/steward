---
type: audit
status: Historical
last_updated: 2026-04-18
---

# Repository Audit — 2026-04-14 [Historical Stub]

**Original scope:** Full requirement-driven audit and contract-alignment review against the repo state on 2026-04-14. Reviewed PRD, RFC-001–006, ADR-001–009, source code, tests, and runtime behavior.

**Status:** This document has been reduced to a historical stub. It reflects an early repo snapshot. Current authoritative state lives in [implementation-status.md](../implementation-status.md) and the active planning docs.

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

- Current state: [implementation-status.md](../implementation-status.md)
- Durable synthesis of this review wave: [historical-audit-synthesis.md](historical-audit-synthesis.md)
- Active readiness tracker: [pre-1-0-readiness-plan.md](../planning/pre-1-0-readiness-plan.md)
