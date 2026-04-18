---
type: rfc
status: Deferred
description: Defines structured, metadata-bearing suppression governance with optional expiry, ownership, and auditability for policy exceptions
resolves: Governance debt visibility gap identified in config-expressiveness stress test and review synthesis SYN-08
last_updated: 2026-04-18
---

# RFC-013: Governed Suppressions and Expiring Debt

---

## Context

Steward provides several suppression mechanisms: `disabled_rules` for global rule disablement, `path_overrides` for scoped rule suppression, and `severity_overrides` for severity adjustment. These mechanisms are functional and necessary — repositories will always need exceptions for migrations, legacy content, generated files, or gradual adoption.

However, current suppressions are anonymous configuration entries. They carry no rationale, no ownership, no lifecycle metadata, and no expiry. Once added, they persist indefinitely unless someone remembers to remove them. `config doctor` can detect dead suppressions (references to rules that no longer exist), but it cannot detect suppressions that are still syntactically valid yet no longer necessary.

The config-expressiveness stress test (2026-04-18) identified ungoverned-zone declaration and graduated/maturity-gradient policy as gaps that are not credibly expressible today. The review synthesis (SYN-08) flagged grandfathering and intentionally ungoverned zones as adoption-oriented needs. Both point to the same underlying problem: policy exceptions need governance of their own.

## Problem Statement

Without structured metadata, suppressions become invisible governance debt:

- A disabled rule lingers after migration completes.
- A path override hides a real problem indefinitely.
- No one knows why a suppression exists or who owns it.
- CI stays green while governance quality silently decays.
- Agents cannot distinguish intentional exceptions from forgotten overrides.

A governance tool should treat suppressions as tracked debt, not anonymous configuration.

## Decision

When this RFC is implemented, Steward will support structured suppression entries alongside the existing lightweight syntax. The structured form adds metadata fields that make each exception reviewable, attributable, and auditable.

### Structured suppression schema

```yaml
suppressions:
  - id: suppress-legacy-docs-naming
    kind: rule-disable
    target:
      rule: STWD-010
      paths: ["docs/legacy/**"]
    reason: "Legacy migration backlog; naming cleanup tracked in issue #42"
    owner: docs-team
    introduced: 2026-04-18
    review_after: 2026-06-01
    expires: 2026-09-01
```

### Metadata fields

| Field | Required | Purpose |
|-------|----------|---------|
| `id` | Strongly recommended | Unique identifier for reference in diagnostics and auditing |
| `kind` | Strongly recommended | Controlled vocabulary: `rule-disable`, `severity-override`, `path-exception`, `coverage-exception` |
| `target` | Required | What the suppression affects (rule ID, path patterns, or both) |
| `reason` | Strongly recommended | Human-readable explanation of why the exception exists |
| `owner` | Optional | Team or individual responsible for reviewing this exception |
| `introduced` | Optional | Date the suppression was added |
| `review_after` | Optional | Date after which the suppression should be flagged for review |
| `expires` | Optional | Date after which the suppression should be treated as expired |

### Lifecycle semantics

- **No dates:** Suppression is indefinite. Visible in audit output as "indefinite suppression."
- **`review_after` passed:** Suppression remains active but is flagged for review in `check` and `config doctor`.
- **`expires` passed:** Suppression is treated as expired. Repository policy determines whether expiry produces info, warning, or error severity.

### Diagnostic surfaces

`steward check` gains new diagnostic types:

- `suppression-review-due` — a suppression has passed its `review_after` date.
- `suppression-expired` — a suppression has passed its `expires` date.
- `suppression-orphaned` — a suppression targets paths or rules that no longer exist.
- `suppression-unused` — a suppression is syntactically valid but matches nothing in the current repository.

`steward config doctor` gains deeper observations:

- Suppressions without `reason`.
- Suppressions with unknown or uncontactable owners.
- Duplicate or overlapping suppressions.
- Suppressions that shadow broader rules more aggressively than necessary.

`steward explain path` and `steward explain <rule-id>` incorporate suppression context: "this rule would apply but is suppressed by X until Y."

### Backward compatibility

Existing lightweight suppression syntax (`disabled_rules`, `path_overrides`, `severity_overrides`) continues to work unchanged. Structured suppressions are additive. Repositories can adopt structured form incrementally, and `config doctor` can suggest migration from legacy to structured form.

## Scope and Non-Goals

**In scope:**

- Structured suppression schema in `policy.yaml`.
- Lifecycle semantics for `review_after` and `expires`.
- New diagnostic types in `check` and `config doctor`.
- Suppression context in `explain path` and `explain <rule-id>`.
- Backward compatibility with existing suppression forms.

**Non-goals:**

- Eliminating all overrides or suppressions.
- Forcing every repository into the same debt-management policy.
- Automatic suppression removal without human review.
- Ticket-system integration.

## Rationale

Suppression governance addresses a class of problems that grows worse over time. Early repositories have few exceptions, but mature repositories accumulate many. Without metadata, the cost of understanding and cleaning up exceptions grows superlinearly with repository age.

The structured form is designed to be ergonomic enough for casual use while providing enough metadata for serious governance. Optional fields allow gradual adoption — a repository can start with just `id` and `reason` and add lifecycle metadata later.

## Alternatives Considered

1. **Leave suppressions as lightweight anonymous config.** Rejected: produces hidden debt that weakens long-term governance trust.
2. **Force all suppressions to be ticket-backed.** Rejected: too heavy for lightweight repositories.
3. **Enforce mandatory expiry on all suppressions.** Rejected: some permanent carve-outs are legitimate (vendor content, generated files).

## Risks

- **Config verbosity:** Mitigated by keeping the simple form supported and providing migration tooling.
- **Fake reasons:** Free text is still better than nothing. Review culture handles quality.
- **Expiry noise:** Mitigated by repository-configurable severity and clear distinction between review-due and expired.

## Dependencies

- Benefits from existing `config doctor` infrastructure for dead-suppression detection.
- Policy schema extension requires `config validate` updates.
- No hard dependency on other pending RFCs.

## Status

Deferred. This RFC is accepted in principle but not scheduled for implementation. It should be revisited after the pre-1.0 trust floor is established. The structured-suppression concept is a governance-depth enhancement that becomes more valuable as Steward is adopted by repositories with mature exception inventories.
