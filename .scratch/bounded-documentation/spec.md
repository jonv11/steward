# Spec — Bounded documentation contracts

Status: done

## Problem statement

Owner-authorized adoption work for MPES needs declarative content budgets, bounded index pages,
unique document identities, validated metadata relationships and lossless source collections.
Keep existing policies unchanged unless they opt in. Add registered validation rules with
explainable diagnostics, scoped-check behavior, configuration validation and real CLI tests.

## Solution

Three opt-in rules extend the existing validation registry; explicit index globs extend STWD-011.
All contracts are absent by default and preserve existing policies.

## Acceptance criteria

- Content budgets count Markdown source words including code and tables, excluding front matter.
- Per-heading own content and parsed table cells have independent budgets; indexes count links.
- Identity and relation checks inspect the full discovered corpus even during scoped checks.
- Source collections reconstruct exact UTF-8 bodies in declared order and verify SHA-256.
- Bad configuration fails explicitly; good and planted-bad corpora prove CLI exit behavior.
- Documentation, tests and local consumption instructions describe the unreleased capability.
