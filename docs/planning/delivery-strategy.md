# Delivery Strategy

- **Document ID:** PLAN-0001
- **Version:** 0.10.0
- **Status:** Accepted
- **Last updated:** 2026-04-15

---

## Approach

Steward continues to follow an incremental milestone model. Each milestone must deliver a coherent, testable slice of functionality without assuming that the first stable release has already been approved.

## Sequencing Principles

1. **Foundation first.** CLI scaffolding, config loading, and file discovery land before higher-level behavior.
2. **Read before write.** Orientation, outline, explainability, and search precede mutation-heavy surfaces.
3. **Validate before maintain.** Deterministic maintenance depends on policy-aware validation and stale-artifact detection.
4. **Preview before apply.** Mutating flows stay preview-first and must surface review-friendly output.
5. **Policy before hardcoding.** Repository-specific workflow semantics belong in config and policy, not in Steward source.
6. **Pre-1.0 honesty.** Planning, packaging, and release signaling must describe the repo as pre-stable until `1.0.0` is explicitly authorized.

## Versioning

Authoritative versioning policy is recorded in [ADR-013](../decisions/adrs/ADR-013-pre-1-0-versioning-and-release-authorization.md).

- `0.10.0` is the current pre-1.0 baseline.
- Future feature work stays on the `0.x.y` line until an explicit release-authorization decision approves `1.0.0`.
- Pre-1.0 patch bumps are reserved for tightly scoped fixes, packaging corrections, and documentation adjustments on the same baseline.
- Pre-1.0 minor bumps are intentional roadmap moves and require written rationale in the active planning/state artifacts.

## Milestone Shape

- Delivered lineage: `v0.1.0` through `v0.10.0`
- Planned pre-stable continuation: `v0.11.0` and later
- First stable release: `v1.0.0`, not yet authorized and not yet scheduled
