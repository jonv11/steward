---
type: rfc
status: Accepted
resolves: >-
  REQ-MD-012 split/extract workflows in preview-first form for governed Markdown
last_updated: 2026-04-17
---

# RFC-011: Markdown Split and Extract Workflows

---

## Context

Steward already has a real Markdown structural subsystem:

- MdPath selectors
- structural queries
- structural edits
- preview/apply safety
- managed-region ownership enforcement
- line-count and section-size introspection

What it does not yet have is a safe multi-file Markdown refactor workflow. That gap is explicitly reserved in the PRD as `REQ-MD-012`.

The current milestone plan names "split/extract evaluation" as a `v0.15.0` theme. This RFC narrows that theme into something implementable and trustworthy.

## Problem Statement

Large or overgrown Markdown documents eventually need to be decomposed. Today Steward can help users inspect structure and edit sections, but it cannot safely:

- plan a split into smaller documents
- extract a section into a new file with preview-first behavior
- report multi-file Markdown refactors in one deterministic workflow

Without that support, maintainers and agents fall back to manual copy/move edits, which are easy to get wrong and hard to review.

## Goals

1. Add a preview-first workflow for extracting a governed Markdown section into a new file.
2. Add a non-mutating split-planning surface for large documents.
3. Preserve existing safety and ownership guarantees.
4. Keep the first slice narrow enough to earn trust on the pre-1.0 line.

## Non-Goals

1. Full automatic document sharding in `v0.15.0`.
2. Automatic repo-wide link rewriting.
3. Arbitrary free-form content refactoring outside Markdown structural boundaries.
4. Using AI summarization or prose generation to decide extracted content.

## Decision

`v0.15.0` should introduce two related but deliberately different surfaces:

1. `steward md split plan`
2. `steward md edit extract-section`

`md split plan` is non-mutating and advisory. `extract-section` is preview-first and mutating only when `--apply` is given.

## 1. `md split plan`

### Command

```bash
steward md split plan <file> [--max-lines <n>] [--min-section-lines <n>] [--output json|text]
```

### Behavior

- analyzes the existing heading structure and line counts
- proposes candidate sections to extract when the document or section sizes exceed thresholds
- suggests target filenames based on heading names
- does not write files

### Output

Text mode:

- source file summary
- candidate sections in recommended extraction order
- suggested target filenames
- warnings when headings are ambiguous or too small to justify extraction

JSON mode:

- source file
- total lines
- threshold inputs
- candidate extraction list with selector, heading, line counts, and suggested target path

This surface satisfies the "evaluation" part of the milestone without committing Steward to a broad automatic split operation too early.

## 2. `md edit extract-section`

### Command

```bash
steward md edit extract-section <file> --selector <mdpath> --to <target-file> [--replace-with-link] [--apply]
```

### Required Behavior

- selector must resolve to exactly one Markdown section
- preview mode shows the source-file diff and the target-file creation plan
- `--apply` writes both files
- the source-file edit is minimal-diff
- if `--replace-with-link` is used, the extracted section in the source file is replaced with a short link stub to the new file
- if `--replace-with-link` is not used, the section is removed from the source file after extraction

### Safety Rules

- fail if the selector is ambiguous
- fail if the target file already exists and is non-empty
- fail if the selected section intersects content Steward is not allowed to mutate
- do not rewrite unrelated links across the repository

### Target File Rules

The target file should:

- be created as Markdown
- contain the extracted section content
- preserve frontmatter only when explicitly requested by a future follow-on flag

Default `v0.15.0` behavior should not try to infer or synthesize policy-aware frontmatter automatically. That can be added later once the base extract flow is trusted.

## Why This Is Narrow Enough

This proposal intentionally does not include:

- one-shot "split the whole document now" apply mode
- automatic extraction of multiple sections in one destructive command
- automatic repo-wide link updates

That narrower scope matters because split/extract is Steward's first real multi-file Markdown refactor flow. It should earn trust with one precise extraction contract before it grows into broader automation.

## Relationship To Other v0.15.0 Work

- RFC-010 helps by giving the split-plan JSON a standard machine-facing envelope.
- RFC-009 helps by letting split plans and extract previews refer to source sections and target files with canonical addresses rather than only ad hoc path-plus-selector pairs.

## Consequences

### Positive

- Steward gains a meaningful new Markdown stewardship workflow rather than another isolated primitive
- large-document maintenance becomes more reviewable
- agents get a deterministic split-planning surface before mutation

### Negative

- preview and apply now need to describe multi-file effects clearly
- test coverage must expand from single-file edits to coordinated source-and-target changes

## Explicitly Deferred

- whole-document auto-splitting
- automatic frontmatter scaffolding based on artifact families
- automatic inbound/outbound link rewriting across the repo
- non-section extraction primitives such as arbitrary list or table extraction

