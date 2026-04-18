---
type: rfc
status: Deferred
description: Proposes a typed resource-address model aligned across file, Markdown, search, and reference surfaces
resolves: REQ-ADDR-002 through REQ-ADDR-005 and REQ-SEARCH-012 follow-on alignment on the pre-1.0 line
last_updated: 2026-04-18
---

# RFC-009: Typed Resource Addresses and Search Alignment

---

## Context

Steward is intentionally path-first today, but the product requirements explicitly say the path model is "path-first, not path-only" and reserve a typed, URI-like resource-address model for later pre-1.0 work.

That later work is now the first planned theme of `v0.15.0`.

The current product already contains most of the conceptual ingredients:

- repo-relative file paths are stable across `check`, `status`, `search`, `refs`, and maintenance
- Markdown structural targeting already exists through MdPath selectors
- RFC-004 explicitly states that MdPath should remain compatible with future address generalization
- `search` and `refs` already return machine-readable location data, but only as raw path-centric shapes

The problem is not absence of location data. The problem is the absence of one typed address model that can move across CLI surfaces without command-specific glue logic.

## Problem Statement

Current machine-facing workflows still require manual stitching:

- `search` returns a file path plus line/column and optional heading context, but no canonical address
- `md query` requires a file plus selector, not a reusable address
- `refs` returns string arrays of paths, not typed references
- current JSON output gives no consistent place for future cross-command address metadata

This creates three avoidable gaps:

1. agents cannot pass one canonical location token from search to later Markdown inspection
2. human power users cannot reason about "the same resource" across search, refs, and explain surfaces
3. future features such as split/extract and workflow guidance cannot point at one common address language

## Goals

1. Introduce one typed address model without breaking the current path-first CLI.
2. Keep the first implementation slice additive and reviewable.
3. Make search results and Markdown structural targets converge on one canonical representation.
4. Preserve compatibility with existing MdPath selectors and repo-relative path semantics.

## Non-Goals

1. Replacing all path arguments in `v0.15.0`.
2. Introducing host-specific or web permalink address schemes.
3. Supporting cross-repository addressing.
4. Solving fuzzy matching or workflow modeling in the same milestone.

## Decision

Steward should introduce a first-class `ResourceAddress` model in core and a canonical string format for file and Markdown resources. The first `v0.15.0` slice should keep the current path-based interfaces working while adding typed addresses to read-oriented surfaces first.

### Canonical String Format

The canonical string form is:

`steward://<kind>/<repo-relative-path>[#<selector>]`

Rules:

- `<kind>` is required.
- `<repo-relative-path>` always uses forward slashes.
- `#<selector>` is optional and uses the existing MdPath syntax when present.
- Address parsing normalizes separators and trims leading `./`, but preserves user-facing repo-relative meaning.

Examples:

- `steward://file/README.md`
- `steward://heading/docs/requirements/PRD.md#heading[Goals]`
- `steward://frontmatter/docs/planning/milestone-plan.md#frontmatter.status`
- `steward://managed-region/STRUCTURE.md#managed[steward:structure]`

### Address Kinds In The First Slice

The first implementation slice should support:

- `file`
- `heading`
- `frontmatter`
- `managed-region`

These kinds are enough to connect current search, Markdown query, refs, and future split/extract planning without overextending into workflow or host integration.

### Core Model

Add a `ResourceAddress` value object in core with:

- `Kind`
- `Path`
- `Selector`
- `OriginalText` or equivalent for error reporting

Add parser and formatter helpers so command surfaces do not each invent their own string logic.

### v0.15.0 Consumer Scope

The first implementation slice should require the following consumers:

1. `search --output json`
   - keep existing `path`, `line`, `column`, `snippet`, `kind`, and `headingContext`
   - add canonical `address`
2. `md query`
   - accept `--address <resource-address>` as an alternative to `<file> <selector>`
   - include canonical `address` on each match in JSON output
3. `refs --output json`
   - preserve current path lists in legacy mode
   - emit address-bearing reference objects in the new JSON envelope mode

`explain path` is a desirable follow-on consumer, but it is not required for the first implementation slice if the milestone needs to stay narrow.

## Relationship To JSON Envelope Work

This RFC depends on the JSON-envelope work proposed in RFC-010 for the cleanest rollout.

Why:

- `refs` currently returns inbound and outbound path arrays; changing those arrays directly is needlessly risky
- richer address objects fit cleanly when command-specific payloads already live inside a standard `data` envelope
- address metadata can then grow without each command inventing a new top-level JSON contract

Recommended order:

1. land RFC-010
2. then add RFC-009 address fields inside the envelope payloads

## CLI Behavior

### Parsing

- invalid address strings fail as usage errors with remediation that points to the canonical format
- `--address` and positional file/selector inputs are mutually exclusive where both exist
- if an address includes a selector kind that does not match the address kind, parsing fails early

### Resolution

- `file` addresses resolve to a repo-relative file path only
- `heading`, `frontmatter`, and `managed-region` addresses resolve to Markdown files and then use existing MdPath evaluation rules
- ambiguous selectors still fail safely by default

## Backward Compatibility

This RFC is additive in `v0.15.0`:

- existing path arguments remain supported
- existing path fields remain present in legacy JSON
- address support is added first to read-oriented flows

## Consequences

### Positive

- search results become more reusable across commands
- agent workflows gain one machine-stable handoff token
- future split/extract planning can reference source and target resources consistently
- the product earns more of the "one conceptual address model" promise without a disruptive rewrite

### Negative

- address parsing and validation adds a new correctness surface
- there will be a temporary period where some commands know about addresses and others do not

## Explicitly Deferred

- cross-repository addresses
- host-backed permalinks
- workflow/session resource kinds
- fuzzy or heuristic selector matching
- replacement of all path-first command arguments
