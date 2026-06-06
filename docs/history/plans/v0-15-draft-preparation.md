---
type: planning
version: 0.14.0
status: Completed
last_updated: 2026-04-18
standalone: true
---

# v0.15.0 Draft Preparation

---

## Purpose

This document turns the current `v0.15.0` placeholder scope into a reviewable draft package. It is intentionally design-first: the goal is to make the next pre-1.0 milestone concrete enough to review and sequence before implementation starts.

The current milestone plan names three themes for `v0.15.0`:

1. typed resource-address follow-on work
2. split/extract evaluation
3. JSON output envelope consistency

This document records the repo-grounded evidence for those themes, recommends an implementation order, and links the draft RFCs prepared for review in the same pass.

## Source Evidence

The following artifacts drove this draft set:

- [PRD](../../requirements/PRD.md)
- [Requirements Traceability](../../requirements/requirements-traceability.md)
- [Milestone Plan](milestone-plan-2026-06-05.md)
- [Implementation Instructions](implementation-instructions-2026-06-05.md)
- [Pre-1.0 Readiness Plan](pre-1-0-readiness-plan-2026-06-05.md)
- [RFC-004 Markdown Structural Model](../../decisions/rfcs/RFC-004-markdown-structural-model.md)
- [RFC-008 Convention-Based Discovery and Workflow Modeling](../../decisions/rfcs/RFC-008-convention-based-discovery-and-workflow-modeling.md)
- `src/Steward.Cli/Commands/*.cs`
- `src/Steward.Core/Search/SearchResult.cs`
- `src/Steward.Core/Markdown/MdPathSelector.cs`

Live CLI spot-checks were also used from the built `Steward.Cli.dll` binary:

- `status --output json`
- `search "Release" --mode headings --max 2 --output json`
- `md query README.md frontmatter --output json`
- `refs README.md --output json`

## Current Evidence Summary

### 1. Typed resource addresses are still only a future seam

What is already true:

- The product requirements explicitly keep a typed, URI-like resource-address model on the later pre-1.0 line (`REQ-ADDR-002` through `REQ-ADDR-005`).
- RFC-004 already states that MdPath was designed to stay compatible with that future generalization.
- Current search, refs, and Markdown selectors already expose the conceptual ingredients of an address model:
  - `search` returns repo-relative `path`, `line`, `column`, `kind`, and `headingContext`
  - `refs` returns repo-relative path strings for inbound and outbound links
  - `md query` already accepts MdPath selectors such as `heading[...]`, `frontmatter.*`, and `managed[...]`

What is not true yet:

- There is no shared `ResourceAddress` type in core.
- There is no canonical address string emitted by search or refs.
- Cross-command handoff still depends on stitching together raw paths and selectors manually.
- Search results cannot point at a canonical Markdown section or managed region in a machine-stable way.

Conclusion:

- The repo is ready for an additive address model.
- The next step should be a narrow, path-compatible typed address layer, not a wholesale replacement of existing path arguments.

### 2. JSON output is useful today, but not shaped as one product surface

What is already true:

- JSON output exists across the primary surfaces and is genuinely useful for automation.
- `check` has explicit DTOs and is already the most structured machine-facing contract.
- `status`, `search`, `md query`, and `refs` all return deterministic JSON today.

Current shape differences are material:

- `status --output json` returns a repository-status object directly.
- `search --output json` returns a search-specific object directly.
- `md query --output json` returns selector and match data directly.
- `refs --output json` returns path plus inbound/outbound arrays directly.
- `check --output json` already uses a deeper command-specific response model with `summary`, `completion`, `diagnostics`, `impactSignals`, and `stagedCompleteness`.

What is missing:

- No common envelope for `command`, `schemaVersion`, `exitCode`, or success.
- No standard place for future cross-command metadata such as resource addresses.
- No single way for generic agent code to reason about JSON output before understanding each command-specific body.

Conclusion:

- The current JSON contracts are good enough to preserve, but not coherent enough to grow cleanly.
- A consistent envelope should land before richer machine-facing payload additions spread command by command.

### 3. Split/extract is still deferred and should stay narrow

What is already true:

- RFC-004 defines the Markdown structural model, preview/apply rules, managed-region ownership, and minimal-diff editing expectations.
- `md query`, `md outline`, and `md edit` already provide the structural and safety primitives needed for later split/extract work.
- The PRD explicitly keeps `REQ-MD-012` as future split/extract workflows in preview-first form.

What is not implemented:

- No `split` command family exists.
- No `extract-section` operation exists.
- No preview-first multi-file Markdown refactor flow exists.

Conclusion:

- `v0.15.0` should not try to ship a broad automatic document-sharding system.
- The right next increment is a narrow, preview-first extract flow plus a non-mutating split-planning surface.

### 4. Workflow/session modeling should not be pulled into v0.15.0

RFC-008 still defers workflow/session modeling to `v0.15.0+`, but the current repo evidence says it should remain later:

- the release-process work just stabilized the pre-1.0 line
- resource-address, JSON consistency, and safe Markdown split/extract are already enough for one milestone
- workflow/session modeling is broader and more policy-heavy than the three directly planned `v0.15.0` items

Recommendation:

- Keep workflow/session modeling on `v0.16.0+` unless one of the current `v0.15.0` items is intentionally cut.

## Draft Artifact Set Prepared In This Pass

| Artifact | Purpose |
|----------|---------|
| [RFC-009 Typed Resource Addresses and Search Alignment](../../decisions/rfcs/RFC-009-typed-resource-addresses-and-search-alignment.md) | Defines an additive typed address model for file and Markdown resources and a narrow set of first consumers |
| [RFC-010 Consistent JSON Output Envelope](../../decisions/rfcs/RFC-010-consistent-json-output-envelope.md) | Defines a machine-facing JSON envelope that can wrap existing payloads without destabilizing current consumers |
| [RFC-011 Markdown Split and Extract Workflows](../../decisions/rfcs/RFC-011-markdown-split-and-extract-workflows.md) | Narrows split/extract work to an extract operation plus a split-planning surface |

## Recommended v0.15.0 Execution Order

### 1. JSON output envelope consistency

Why first:

- It is the safest foundation piece.
- It gives later `v0.15.0` work one consistent place for metadata.
- It reduces the risk that address work or extract-plan work will create even more JSON divergence.

Recommended implementation target:

- introduce a standard envelope as an additive mode first
- keep existing legacy JSON payloads available for compatibility during `v0.15.0`
- add contract tests for both legacy and envelope modes on the highest-value commands

### 2. Typed resource addresses and search alignment

Why second:

- It becomes much easier to add `address` fields once the envelope work exists.
- It directly serves both human power users and AI agents by making command handoff less ad hoc.
- It builds on existing path-first and MdPath-first semantics rather than fighting them.

Recommended implementation target:

- add a core `ResourceAddress` parser/formatter
- emit canonical addresses from search JSON
- accept addresses in a narrow set of read-only surfaces first

### 3. Markdown split and extract workflows

Why third:

- It is the highest-risk mutation work in the milestone.
- It benefits from both the new JSON envelope and the new address model.
- The repo already has a good structural engine; the missing piece is the safe multi-file workflow contract.

Recommended implementation target:

- add `md split plan` as a non-mutating planning surface
- add `md edit extract-section` as the first preview/apply multi-file extract operation
- defer any broader automatic split-apply workflow until the extract contract earns trust

## Proposed v0.15.0 Scope Boundary

### In scope

- standard JSON envelope as an additive machine-facing contract
- typed resource addresses for file and Markdown resources
- search/address alignment using canonical emitted addresses
- non-mutating split planning for large Markdown documents
- preview-first section extraction into a new Markdown file

### Explicitly out of scope

- workflow/session modeling from RFC-008 Phase 3
- heading fuzzy matching in MdPath
- external host or permalink address schemes
- automatic cross-repository or host-backed addresses
- automatic repo-wide link rewriting during extract
- full document auto-sharding in one destructive command

## Open Review Questions

1. Should the standard JSON envelope stay opt-in for the whole `0.15.x` line, or should Steward flip the default before `0.16.0` while still pre-1.0?
2. Is the proposed first typed-address scope narrow enough, or should `refs` and `explain path` be required consumers in the first implementation slice?
3. Should `md split plan` and `md edit extract-section` both land in `v0.15.0`, or should the milestone intentionally ship only one of them first?
4. Should extracted files receive policy-aware frontmatter scaffolding automatically when the target path matches a configured artifact family?

## Definition Of Ready For Implementation

`v0.15.0` should be treated as ready to implement when:

- the draft RFCs below have maintainership review comments captured
- one JSON-envelope compatibility policy is chosen explicitly
- one typed-address string format is accepted explicitly
- the split/extract scope boundary is accepted explicitly
- tests are planned before command-surface edits begin
