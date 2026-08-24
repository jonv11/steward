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

| Item | Detail |
|------|--------|
| `config doctor` reports anticipatory artifact families and `path_overrides` as dead config | Same class as the preventive path-rule fix in the 2026-08-24 trial: a family declared before its first instance exists matches zero files, so doctor reports `unreachable-family-pattern` and exits `1`. Governance that is correct but not yet populated is indistinguishable from governance that is wrong. This repo's own `.scratch` ticket families are in exactly that state until the first effort lands. The preventive exemption currently covers `forbidden`/`reserved` path-policy categories only; extend the same reasoning to `artifact_families` and `validation.path_overrides` |

The remaining defects confirmed during the 2026-08-24 adoption trial on two external repositories (`jvcode`, `mdrule`) have all been fixed; see the CHANGELOG for what changed.

## Validated Enhancements

Rule phase-in and baseline has moved into committed scope; see the [roadmap](roadmap.md#current-milestone-rule-phase-in-and-baseline).

| Item | Current direction |
|------|-------------------|
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
| Profile depth and additional init profiles | All 5 profiles (`software`, `docs`, `minimal` public; `mixed`, `knowledge` deferred per ADR-014) only gate file presence/required-optional status today — no archetype-specific behavior beyond that, so most feel like "require a README" with extra steps. Design richer, more differentiated defaults per profile before re-enabling `mixed`/`knowledge` or expanding the public set |
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
