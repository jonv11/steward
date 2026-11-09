# 08 — Resume or drop RFC-013 governed suppressions

Status: needs-info
Type: grilling
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/config-transparency/map.md`

## The question

Should [RFC-013 governed suppressions](../../../docs/decisions/rfcs/RFC-013-governed-suppressions-and-expiring-debt.md)
resume? Present granularity is a global disable or a glob `path_overrides`; `standalone: true`
covers STWD-013 only, so a one-file exception still forces an over-broad glob.

## Why a human decides this

It is a deferred decision record: it needs a stable policy schema and adoption evidence, and
whether baselines (RFC-017) and per-path severity (ticket 05) already cover the need is a
product judgment.

## What is needed to close this

Adoption evidence after rule phase-in ships, then a decision to resume, narrow or supersede
RFC-013, recorded under `## Answer` and in the RFC's status.
