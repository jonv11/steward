# ADR-004: Markdown Parser — Markdig

- **Status:** Accepted
- **Category:** Technical

---

## Context

Markdown is a first-class governed document type. The CLI needs to parse Markdown into a structural model, query it, edit it structurally, and render it back with minimal diff.

## Decision

Use **Markdig** as the Markdown parsing library and build a structural facade on top of its AST.

### Why Markdig

- The most mature and widely used .NET Markdown library.
- Full CommonMark compliance with extensive extension support.
- Produces a detailed AST (`MarkdownDocument` with block/inline nodes).
- Supports YAML frontmatter extraction via the `YamlFrontMatterExtension`.
- Round-trip preservation: the AST retains source positions and trivia.
- BSD-2-Clause licensed.
- Actively maintained (Xoofx).

### Structural model layer

Markdig's AST is low-level (block and inline nodes). We build a higher-level **structural model** on top:

```csharp
public sealed class StructuredDocument
{
    public string FilePath { get; }
    public FrontmatterBlock? Frontmatter { get; }
    public IReadOnlyList<Section> Sections { get; }
    public IReadOnlyList<ManagedRegion> ManagedRegions { get; }
    public string RawContent { get; }
}

public sealed class Section
{
    public HeadingInfo Heading { get; }
    public int Level { get; }
    public TextRange Range { get; }  // Start/end line in source
    public IReadOnlyList<Section> Children { get; }
    public IReadOnlyList<ContentBlock> ContentBlocks { get; }  // Lists, tables, code blocks
}
```

This model:
- Provides heading-hierarchy navigation.
- Supports mdpath selector evaluation.
- Tracks source positions for minimal-diff editing.
- Identifies managed regions.

### Editing strategy

Structural edits do **not** serialize the AST back to Markdown. Instead, they operate on the **raw text** guided by source positions from the structural model:

1. Parse the document to build the structural model.
2. Resolve the mdpath selector to identify the target range.
3. Compute the text edit (insert, replace, delete) against the raw text.
4. Apply the text edit to produce the new document.

This ensures:
- Unrelated content is never reformatted.
- Diffs are minimal (only the intended change appears).
- Whitespace, blank lines, and formatting choices are preserved outside the edit range.

### Frontmatter handling

- Markdig's `YamlFrontMatterExtension` identifies the YAML frontmatter block.
- The frontmatter content is parsed separately using YamlDotNet.
- Frontmatter edits (set, merge) modify the YAML content and replace the frontmatter block in the raw text.

## Alternatives considered

1. **CommonMark.NET:** Active but less feature-rich than Markdig. No built-in frontmatter support.
2. **Custom parser:** Enormous effort with no benefit. Markdig is battle-tested.
3. **AST round-trip for editing:** Rejected—AST serialization inevitably reformats content, violating the minimal-diff requirement.
4. **Regex-based editing:** Fragile for complex structural operations. The structural model provides reliable source positions.

## Consequences

- Mature, well-supported Markdown parsing with full CommonMark compliance.
- Structural model provides the abstraction needed for mdpath selectors and structural queries.
- Raw-text editing preserves formatting and achieves minimal diffs.
- Frontmatter is handled consistently using YAML infrastructure (ADR-003).
