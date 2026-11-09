# Spec — Rule phase-in and baseline

Status: ready-for-agent

The committed current milestone after `v0.18.1` (see the [roadmap](../../docs/project/roadmap.md)).
The design authority is [RFC-017](../../docs/decisions/rfcs/RFC-017-rule-phase-in-and-baseline.md)
(Accepted); this spec does not restate it.

## Problem statement

Enabling a rule today applies it to all existing content at once. A repository that already
has history and turns a rule on gets an immediate wall of errors across every pre-existing
file, with no way to say "only enforce this going forward." Adopting a new rule into an
established repository is effectively all-or-nothing.

Both external repositories in the 2026-08-24 adoption trial hit this and flagged it as
blocking, not just annoying — the highest-impact adoption gap on current evidence. See the
[maintainer configuration experience audit](../../docs/history/audits/maintainer-configuration-experience-audit-2026-08-24.md).

## Solution

A generated `.steward/baseline.json` snapshots current violations at adoption time, so
`check` suppresses them while still catching new ones, with a drift signal that shows when
grandfathered debt gets fixed. RFC-017 defines the file format, matching, regeneration and
the drift signal.

Until it ships, the [setup-steward](../../.agents/skills/setup-steward/SKILL.md) skill works
around the gap by gating rules only where a repository has zero existing violations.

## Implementation decisions

Decisions taken while slicing, where RFC-017 leaves a choice open:

- **Activation follows the file.** The baseline applies when the file at `baseline.path`
  (default `.steward/baseline.json`) exists. `baseline.enabled: false` turns it off without
  deleting the file; `baseline.enabled: true` with no file is a `config validate` error. This
  keeps RFC-017 §5 literal: deleting the file restores full enforcement, and a repository
  with neither file nor block behaves exactly as today.
- **Identity is rule, path and selector — never line.** A diagnostic's selector is the one the
  rule supplies, or, when it supplies none, the diagnostic message. Rules whose message carries
  a volatile value (a line number, an age, a count) must supply an explicit selector, so an
  unrelated edit cannot invalidate an entry. Diagnostics with no path use the empty path.
- **Matching is a multiset.** Two identical identities in one file are two entries; fixing one
  leaves one baselined and reports one as drift.
- **Baseline runs after severity overrides and path overrides**, as a post-filter in `check`;
  no rule's evaluation changes (RFC-017 §4).
- **The baseline records every severity**, not only errors, so `info` and `warning` noise from
  pre-existing content is grandfathered too.

## Status of the work

As of 2026-11-09 the spec is sliced into tickets under `issues/`; see their `Status:` lines.
