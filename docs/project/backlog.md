---
type: project
status: Active
summary: Validated enhancement ideas and deferred decisions that are not assigned to a milestone
last_updated: 2026-06-06
review_after: 2026-09-06
---

# Backlog

Items here are intentionally unscheduled. Moving an item into committed scope requires updating the [roadmap](roadmap.md).

## Deferred Decisions

| Item | Why deferred |
|------|--------------|
| [RFC-012 heading-level Markdown refactors](../decisions/rfcs/RFC-012-heading-level-markdown-refactors.md) | Needs mature selector and reference-update infrastructure |
| [RFC-013 governed suppressions](../decisions/rfcs/RFC-013-governed-suppressions-and-expiring-debt.md) | Needs stable policy schema and adoption evidence |

## Validated Enhancements

| Item | Current direction |
|------|-------------------|
| Repository adoption flow | Compose `init`, `config suggest`, validation, doctor, and check into a staged workflow when adoption evidence justifies it |
| Governance-gap explanation | Extend `status` or `explain path` with actionable coverage dimensions |
| Policy evaluation trace | Add precedence and hypothetical-policy detail to `explain path` rather than creating a broad new command family |
| Indexed mode for large repositories | Revisit only if live-scan performance becomes an evidenced problem |
| Consolidated impact surface | Aggregate `check`, `refs`, `explain path`, and maintenance signals for agent workflows |
| Universal JSON expected-failure cleanup | Route the remaining expected-failure paths through the standard envelope before claiming universal JSON coverage |
| Heading selector fuzzy matching | Add a conservative non-exact selector mode |
| Workflow/session modeling | Revisit RFC-008 phase 3 after higher-priority trust work |
| Additional init profiles | Re-enable `mixed` or `knowledge` only when they have distinct, tested contracts |
| Host-specific integrations | Keep outside the offline-first core until a concrete integration is prioritized |

The detailed April 2026 enhancement analysis is preserved in the [historical backlog](../history/plans/future-enhancements-backlog-2026-04-18.md).
