---
type: rfc
status: Accepted
description: Defines Markdown structural selectors, anchor-compatible heading addressing, managed regions, and preview-first edit operations
resolves: >-
  Selector syntax, managed regions, edit operations, preview/apply, ownership enforcement
---

# RFC-004: Markdown Structural Model

---

## Context

Markdown is a first-class governed document type. The requirements demand structural selectors for querying and editing Markdown content, managed regions with ownership enforcement, preview-before-apply, and minimal-diff edits.

## Decision

### Selector syntax (mdpath)

Structural selectors use a path-like syntax called **mdpath** for addressing Markdown document elements.

```
# Frontmatter
frontmatter                     # The entire frontmatter block
frontmatter.status              # A specific frontmatter field

# Headings by path
heading[Overview]               # First heading matching "Overview"
heading[Overview/Goals]         # "Goals" heading under "Overview"
heading[#2]                     # Second top-level heading (1-indexed)
heading[Overview/#1]            # First child heading under "Overview"

# Managed regions
managed[steward:toc]            # Managed region with id "steward:toc"
managed[steward:index]          # Managed region with id "steward:index"

# Content types (within a heading scope)
heading[Overview].lists         # All lists under "Overview"
heading[Overview].tables        # All tables under "Overview"
heading[Overview].codeblocks    # All code blocks under "Overview"

# Markdown anchor-style heading lookup
#who-is-steward-for             # Heading whose normalized anchor slug is "who-is-steward-for"
```

**Design principles:**

- Selectors read left-to-right as a path into the document.
- Ambiguous selectors that match multiple elements fail with an error by default (REQ-MD-007).
- Indexed selectors (`#N`) provide deterministic addressing when names collide.
- Anchor-style heading selectors normalize text using Markdown-fragment rules: lowercase, trim, drop most punctuation, and collapse whitespace / `-` / `_` into `-`.
- Heading text must therefore be unique within a document after that normalization so anchor-style selectors remain deterministic.

### Managed region markers

Managed regions use HTML comment markers:

```markdown
<!-- steward:managed:begin id="toc" owner="steward" -->
- [Overview](#overview)
- [Details](#details)
<!-- steward:managed:end id="toc" -->
```

| Attribute | Required | Description |
|-----------|----------|-------------|
| `id` | Yes | Unique identifier within the document |
| `owner` | Yes | Who manages this region (`steward`, `manual`, or custom) |

**Rules:**

- The CLI refuses to modify content inside a managed region unless it is the declared owner.
- Content outside managed regions is never modified by maintenance operations.
- Markers are preserved exactly; only the content between them changes.

### Edit operations

| Operation | Command | Behavior |
|-----------|---------|----------|
| `ensure-section` | `steward md edit ensure-section <file> --heading "Section Name" --under "Parent"` | Creates the heading and empty section if it doesn't exist; no-op if it does |
| `set-section` | `steward md edit set-section <file> --heading "Section Name" --content <file-or-stdin>` | Replaces section content under the heading |
| `insert-section` | `steward md edit insert-section <file> --heading "New" --after "Existing"` | Inserts a new section as a sibling after the named heading |
| `append-block` | `steward md edit append-block <file> --under "Section" --content <text>` | Appends content at the end of the named section |
| `prepend-block` | `steward md edit prepend-block <file> --under "Section" --content <text>` | Prepends content at the start of the named section |
| `fm-set` | `steward md edit fm-set <file> --key status --value draft` | Sets a frontmatter field |
| `fm-merge` | `steward md edit fm-merge <file> --input <yaml-file>` | Merges YAML into existing frontmatter |
| `fm-validate` | `steward md edit fm-validate <file>` | Validates frontmatter against policy |

### Heading level inference

When inserting headings, the level is inferred from context (REQ-MD-005):

| Placement | Inferred level |
|-----------|---------------|
| `--under "Parent"` | Parent level + 1 (child) |
| `--before "Sibling"` or `--after "Sibling"` | Same level as sibling |
| Explicit `--level N` | Uses specified level (overrides inference) |

### Preview / Apply model

All edit operations default to **preview mode** unless `--apply` is specified.

- `steward md edit <op> <file> [args]` → Shows what would change (unified diff).
- `steward md edit <op> <file> [args] --apply` → Applies the change.

Preview output is a standard unified diff by default, or structured JSON with `--output json`.

### Query operations

Query does not mutate. It extracts and returns structural content.

```bash
# Get frontmatter as YAML
steward md query README.md frontmatter

# Get a specific section's content
steward md query docs/PRD.md "heading[Goals]"

# Get a section by Markdown anchor slug
steward md query README.md#who-is-steward-for

# Get heading outline
steward md outline docs/PRD.md

# Get all managed regions
steward md query docs/index.md "managed[*]" --output json
```

### Structural validation

`steward check` runs structural validation on governed Markdown documents when policy declares expectations:

| Rule type | Example |
|-----------|---------|
| Required headings | "PRD must have heading 'Goals'" |
| Heading order | "Changelog entries must be newest-first" |
| Frontmatter fields | "All docs must have 'status' field" |
| Managed region integrity | "Managed regions must have matching begin/end markers" |
| Section size thresholds | "Warn when a section exceeds 500 lines" |

### Large-document support

- `steward md outline <file>` shows heading hierarchy with line counts per section.
- `steward outline <file> --lines` shows total line count.
- Policy can define section-size thresholds that trigger warnings.
- Split/extract guidance is informational only in v1.0.0 (per REQ-MD-011). Automated split workflows (REQ-MD-012) are deferred.

## Alternatives considered

1. **XPath-like syntax:** Rejected—overly complex for Markdown's simpler structure.
2. **JSONPath-like syntax:** Rejected—Markdown is not JSON; a document-native syntax is more intuitive.
3. **No managed regions (whole-file ownership only):** Rejected—mixed-ownership documents are a core requirement (REQ-OWN-001).
4. **Default to apply instead of preview:** Rejected—safety-first is a core principle (REQ-SAFE-004).

## Consequences

- Consistent selector syntax across query, edit, and validation.
- Managed regions enable mixed-ownership documents.
- Preview-first model prevents accidental mutations.
- Edit operations are composable and agent-friendly.
- The mdpath syntax is designed to be compatible with future resource-address generalization (REQ-ADDR-006).
