---
type: project
status: Active
summary: Validated enhancement ideas and deferred decisions that are not assigned to a milestone
last_updated: 2026-08-24
review_after: 2026-09-06
---

# Backlog

Items here are intentionally unscheduled. Moving an item into committed scope requires updating the [roadmap](roadmap.md).

## Deferred Decisions

| Item | Why deferred |
|------|--------------|
| [RFC-012 heading-level Markdown refactors](../decisions/rfcs/RFC-012-heading-level-markdown-refactors.md) | Needs mature selector and reference-update infrastructure |
| [RFC-013 governed suppressions](../decisions/rfcs/RFC-013-governed-suppressions-and-expiring-debt.md) | Needs stable policy schema and adoption evidence. Present granularity is global disable or glob `path_overrides`; `standalone: true` covers STWD-013 only, so a one-file exception still forces an over-broad glob |

## Defects

Confirmed against source during an adoption trial on two external repositories (`jvcode`, `mdrule`) on 2026-08-24.

| Defect | Evidence |
|--------|----------|
| `config doctor` reports `category: forbidden` path rules as dead config | The unmatched-path-rule loop in `ConfigCommand.cs` filters on pattern match only, with no category filter. A forbidden rule matching zero files is the success condition, not dead configuration. Both trial repositories hit this; one deleted a correct prohibition to get a clean doctor, the other accepted a permanently dirty doctor |
| Governance coverage ignores artifact families | `ComputeCoverage` in `StatusCommand.cs` derives governed paths from artifacts, maintenance scopes, `start_here`, and link reachability only — `policy.ArtifactFamilies` is never consulted. `status` therefore reports a family as matching N files while listing those same files under "Ungoverned Files". On the trial repository this excluded 259 family-matched files from the coverage numerator |
| `maintain` reports maintenance failures as `OK` | In `IndexMaintainer.cs`, both "target file does not exist" and "managed section not found" return `HasChanges = false` with no failure signal, so the CLI renders them as `OK`. A maintenance artifact that silently does nothing is indistinguishable from one that is up to date. `MaintenanceAction` has no status field to carry the distinction |

## Validated Enhancements

| Item | Current direction |
|------|-------------------|
| Rule phase-in and baseline | Highest-impact adoption gap. Enabling a rule currently applies it to all existing content at once, which blocks adoption on repositories that already have history. Explore a baseline snapshot or a "warn on existing, error on new" mode before adding further rules |
| `check --fail-on <severity>` | Let CI gate on warnings without rewriting rule severities in `policy.yaml`. Only `error` affects the exit code today, so most rules are non-blocking by default |
| Shared or inheritable policy | No `extends`/import exists, so multi-repo governance means copying `.steward/` and hand-syncing drift. Evaluate against the offline-first constraint before committing |
| Policy impact preview | Show what a proposed policy change would do before it is committed. `config doctor` covers dead config, not effect |
| Retire the ignored `path-policy` `kind` field | `PathRule.Kind` is parsed and never read, and `config validate` does not flag it. Either give it meaning or reject it |
| Repository adoption flow | Compose `init`, `config suggest`, validation, doctor, and check into a staged workflow when adoption evidence justifies it |
| Governance-gap explanation | Extend `status` or `explain path` with actionable coverage dimensions |
| Policy evaluation trace | Add precedence and hypothetical-policy detail to `explain path` rather than creating a broad new command family. `explain path` currently lists applicable rule IDs with no severity and no config provenance, which leaves the configuration surface undebuggable |
| Indexed mode for large repositories | Revisit only if live-scan performance becomes an evidenced problem |
| Consolidated impact surface | Aggregate `check`, `refs`, `explain path`, and maintenance signals for agent workflows |
| Universal JSON expected-failure cleanup | Route the remaining expected-failure paths through the standard envelope before claiming universal JSON coverage |
| Heading selector fuzzy matching | Add a conservative non-exact selector mode |
| Workflow/session modeling | Revisit RFC-008 phase 3 after higher-priority trust work |
| Additional init profiles | Re-enable `mixed` or `knowledge` only when they have distinct, tested contracts |
| Per-path rule severity | `path_overrides` can only disable rules and `severity_overrides` is global, so phasing in a rule declared for a single family forces a repository-wide downgrade. A `path_overrides[].severity_overrides` would express the real intent and partly addresses the phase-in gap above |
| Family matching on non-YAML metadata | `match.frontmatter` is the only content-based discriminator. Repositories that record `Type:`/`Status:` as body lines rather than YAML cannot split one glob into two families. Both trial repositories hit this and dropped otherwise-justified section schemas because of it |
| `directory-index` degradation | `directory-index` hard-blocks unless every source file has non-empty `frontmatter.description`, which rules it out for any repository being adopted into. `type: index` degrades to an H1-derived link list and worked; consider the same fallback |
| Per-directory `directory_expectations` | `min_count` is repository-wide, so "every effort directory contains a `spec.md`" is inexpressible for multi-instance layouts — only "at least one exists anywhere" |
| STWD-001 and STWD-009 overlap | Reported as emitting verbatim duplicate diagnostics for declared artifacts. STWD-009 appears to add value only for `importance: optional`, which STWD-001 skips. Confirm before scoping |
| Host-specific integrations | Keep outside the offline-first core until a concrete integration is prioritized |

## Documentation Gaps

Found by trial and error during the same adoption trial; none are covered by the [configuration reference](../guide/configuration-reference.md) or the [maintainer guide](../guide/maintainer-guide.md).

| Gap | Detail |
|-----|--------|
| Managed-section marker syntax | The `steward:begin`/`steward:end` marker form required by `maintenance` type `index` is documented nowhere. The markers are also asymmetric: begin carries `id` and `owner`, end must be bare `<!-- steward:end -->`, and an `id` on the end marker fails silently |
| `title_pattern` matches normalized text | Patterns are applied to the normalized H1 text, not the raw Markdown, so `` # `jvcode issues` `` matches as `jvcode issues` and a pattern anchored on the backtick never matches |
| Glob character classes in patterns | Character classes such as `docs/adr/[0-9]*.md` work and are the practical way to exclude an index `README.md` from a family glob, but are not mentioned |

The detailed April 2026 enhancement analysis is preserved in the [historical backlog](../history/plans/future-enhancements-backlog-2026-04-18.md).

Evidence for the phase-in, provenance, and shared-policy items: [maintainer configuration experience audit](../history/audits/maintainer-configuration-experience-audit-2026-08-24.md).
