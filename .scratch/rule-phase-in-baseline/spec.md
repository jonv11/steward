# Spec — Rule phase-in and baseline

Status: needs-triage

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

## Status of the work

As of 2026-11-09 there is no baseline code and the implementation is not yet sliced into
tickets. The next step is to break RFC-017 into vertical-slice tickets under `issues/`.
