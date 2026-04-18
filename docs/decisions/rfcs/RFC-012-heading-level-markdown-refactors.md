---
type: rfc
status: Deferred
description: Defines heading-level Markdown refactor operations starting with safe, reference-aware heading rename
resolves: REQ-MD-004 and REQ-MD-005 structural editing depth, plus cross-file reference safety for heading changes
last_updated: 2026-04-18
---

# RFC-012: Heading-Level Markdown Refactors

---

## Context

Steward treats Markdown as a first-class structural document type. It can query sections (`md query`), edit them structurally (`md edit`), reason about references (`refs`), perform file-level refactoring (`refactor move`), and plan document decomposition (`md split plan`, `md edit extract-section` per RFC-011).

However, in documentation-heavy repositories, many meaningful changes happen below the file level: renaming a heading, reparenting a subsection, or moving a section between files. These operations change anchor slugs and can silently break inbound Markdown links throughout the repository.

Today, a heading rename requires manually editing the heading text, computing the new anchor slug, finding all inbound references, and updating each one. This is error-prone and tedious, especially for high-fan-in headings in authoritative documents.

## Problem Statement

There is no coherent high-level operation for "refactor this heading safely." Users must stitch together `md edit`, `refs`, and manual link repair. The result is:

- Heading rename is not atomic — references can break between steps.
- Anchor slug changes are the user's burden to compute and propagate.
- Agents must orchestrate multiple primitives without a canonical workflow.
- Section-level change review lacks a clear semantic plan.

The infrastructure for this exists (Markdown parser, selector model, reference graph, deterministic editing). What is missing is a dedicated heading-refactor surface.

## Decision

When this RFC is implemented, Steward will add heading-level refactor operations under the existing `refactor` command group:

```
steward refactor heading rename <target> --to <new-heading-text>
```

### First milestone: rename

The first implementation slice covers heading rename only:

1. Resolve the heading target via anchor-style selector (`file.md#heading-slug`) or MdPath selector.
2. Rename the heading text in the source file.
3. Compute the old and new anchor slugs deterministically.
4. Find all inbound Markdown references affected by the slug change (inline links, reference-style links, same-file anchors, cross-file anchors).
5. Preview both direct edits and reference edits.
6. Apply only when `--apply` is given.

The operation must:

- Fail safely on ambiguous selectors (multiple matching headings).
- Detect and warn when the new heading text would create a duplicate normalized slug (STWD-017 violation).
- Only rewrite structured Markdown links, not free-text mentions.
- Preserve minimal diffs outside the targeted structural change.
- Produce both text and JSON output for human and agent consumers.

### Refactor plan output

A heading rename produces a structured impact plan showing:

- Target identity (file, old heading text, old anchor slug).
- New heading text and new anchor slug.
- Direct file edits (heading text change).
- Reference edits (each affected file and link).
- Unresolved references (links that could not be safely rewritten).
- Warnings (duplicate slug risk, managed-region boundaries).

### Future extensions (not in first milestone)

These are recorded as follow-on scope, not committed:

- **Move within file:** Reorder a section among siblings.
- **Reparent:** Move a section under a different parent heading, adjusting heading level.
- **Split:** Decompose a section into multiple child or sibling sections per a structured plan.
- **Merge:** Combine adjacent or semantically equivalent sections.
- **Cross-file extract:** Bridge heading refactors with file-level `refactor move`.

## Scope and Non-Goals

**In scope:**

- Heading rename with cross-file reference updating.
- Preview-first safety model consistent with other Steward mutation commands.
- Text and JSON output modes.
- Interaction with STWD-017 unique heading validation.

**Non-goals:**

- Freeform content rewriting or natural-language transformation.
- Automatic resolution of ambiguous references without user review.
- Replacing lower-level `md edit` operations.
- Full document semantic understanding.

## Dependencies and Prerequisites

- Requires stable anchor-slug computation (already present via STWD-017 normalization).
- Benefits from RFC-009 typed resource addresses for richer reference handling, but can ship without them using current path-based selectors.
- Builds on existing `refs` infrastructure for reference discovery.

## Rationale

Heading rename is the most common and highest-value section-level refactor. It is also the simplest to implement safely because it is a well-defined text transformation with deterministic anchor-slug consequences. Starting with rename provides immediate value while establishing the contract and safety model for future heading-level operations.

The alternative — leaving heading rename as a manual multi-step process — is adequate but increasingly error-prone as repositories grow and documentation cross-referencing deepens.

## Alternatives Considered

1. **Keep heading rename as manual `md edit` + `refs` combination.** Rejected: powerful but not ergonomic. Users and agents need a single operation with clear impact reporting.
2. **Add only anchor-rewrite helpers.** Rejected: heading refactoring is a structural operation, not just link mutation.
3. **Wait for RFC-009 typed resource addresses.** Rejected: heading rename is already valuable with current selector infrastructure. RFC-009 can enhance it later.

## Risks

- **Ambiguous heading resolution:** Mitigated by requiring exact selectors and failing safely on ambiguity.
- **Incorrect link rewrites:** Mitigated by rewriting only structured Markdown links and previewing all changes.
- **Scope creep:** Mitigated by limiting first milestone to rename only, with future operations phased deliberately.

## Status

Deferred. This RFC is accepted in principle but not scheduled for implementation. It should be revisited after the pre-1.0 trust floor (JSON contract, hosted release evidence) is established and existing Markdown refactoring surfaces have proven stable in production use.
