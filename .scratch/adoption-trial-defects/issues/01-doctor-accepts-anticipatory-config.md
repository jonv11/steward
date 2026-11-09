# 01 — `config doctor` reports anticipatory governance as dead config

Status: needs-triage
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

A family declared before its first instance exists matches zero files, so `config doctor`
reports `unreachable-family-pattern` and exits `1`. Governance that is correct but not yet
populated is indistinguishable from governance that is wrong. This is the same class as the
preventive path-rule fix from the 2026-08-24 trial, whose exemption covers only the
`forbidden` and `reserved` path-policy categories.

This repository hit it with its own `.scratch` tracker governance (the `ticket`,
`effort-spec` and `effort-map` families, the `.scratch/**` path override and the
`recommended` `scratch-tracker-layout` naming rules): all of it reported as dead config until
the first efforts landed on 2026-11-09. Any repository adopting the same tracker convention
before filing its first ticket hits the same thing.

Extend the same reasoning to `must_match` naming rules, `artifact_families` and
`validation.path_overrides`, without hiding genuinely mistyped patterns.

## Acceptance criteria

- [ ] `config doctor` no longer exits `1` solely because a family, path override or naming rule has not matched a file yet.
- [ ] A pattern that can never match (for example a typo against an existing directory) is still reported.
- [ ] Core and CLI tests cover an anticipatory family, an anticipatory override, an anticipatory naming rule, and a genuinely dead pattern.
- [ ] The configuration reference documents the rule.
