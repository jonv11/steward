namespace Steward.Core.Markdown;

/// <summary>
/// Structural representation of a Markdown document.
/// </summary>
public sealed class StructuredDocument
{
    public required string FilePath { get; init; }
    public required string RawContent { get; init; }
    public FrontmatterBlock? Frontmatter { get; init; }
    public required IReadOnlyList<Section> Sections { get; init; }
    public IReadOnlyList<ManagedRegion> ManagedRegions { get; init; } = [];
    public int TotalLines { get; init; }
}

public sealed class FrontmatterBlock
{
    public required string RawYaml { get; init; }
    public required Dictionary<string, object?> Fields { get; init; }
    public required LineRange Range { get; init; }
}

public sealed class Section
{
    public required string Heading { get; init; }
    public required int Level { get; init; }
    public required LineRange Range { get; init; }
    public required IReadOnlyList<ContentBlock> ContentBlocks { get; init; }
    public required IReadOnlyList<Section> Children { get; init; }
    public int LineCount => Range.End - Range.Start + 1;
}

public sealed class ContentBlock
{
    public required ContentBlockType Type { get; init; }
    public required LineRange Range { get; init; }
}

public enum ContentBlockType
{
    Paragraph,
    List,
    Table,
    CodeBlock,
    ThematicBreak,
    HtmlBlock,
    Other
}

public sealed record ManagedRegion(string Id, string? Owner, LineRange Range);

public sealed record LineRange(int Start, int End);
