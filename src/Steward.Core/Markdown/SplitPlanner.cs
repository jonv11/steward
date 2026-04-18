namespace Steward.Core.Markdown;

/// <summary>
/// Non-mutating split-planning surface: analyzes a document's heading structure and
/// proposes candidate sections to extract when sizes exceed thresholds.
/// </summary>
public static class SplitPlanner
{
    public const int DefaultMaxLines = 300;
    public const int DefaultMinSectionLines = 20;

    public static SplitPlan Plan(StructuredDocument doc, int maxLines = DefaultMaxLines, int minSectionLines = DefaultMinSectionLines)
    {
        var candidates = new List<SplitCandidate>();
        CollectCandidates(doc.Sections, doc.FilePath, candidates, minSectionLines, doc.TotalLines, maxLines);

        return new SplitPlan
        {
            SourceFile = doc.FilePath,
            TotalLines = doc.TotalLines,
            MaxLines = maxLines,
            MinSectionLines = minSectionLines,
            Candidates = candidates
        };
    }

    private static void CollectCandidates(
        IReadOnlyList<Section> sections,
        string sourceFile,
        List<SplitCandidate> candidates,
        int minSectionLines,
        int totalLines,
        int maxLines)
    {
        foreach (var section in sections)
        {
            if (section.LineCount < minSectionLines)
                continue;

            if (totalLines <= maxLines && section.LineCount < minSectionLines * 2)
                continue;

            var suggestedPath = SuggestTargetPath(sourceFile, section.Heading);
            var warnings = new List<string>();

            if (section.Heading.Length < 3)
                warnings.Add("Heading is very short — suggested filename may not be meaningful.");

            candidates.Add(new SplitCandidate
            {
                Heading = section.Heading,
                Level = section.Level,
                Selector = $"heading[{section.Heading}]",
                LineCount = section.LineCount,
                Range = section.Range,
                SuggestedTargetPath = suggestedPath,
                Warnings = warnings
            });
        }
    }

    internal static string SuggestTargetPath(string sourceFile, string heading)
    {
        var dir = Path.GetDirectoryName(sourceFile) ?? "";
        var slug = MarkdownHeadings.ToAnchorSlug(heading)
            .Replace('/', '-')
            .Replace('\\', '-');
        slug = slug.Trim('-');
        if (string.IsNullOrEmpty(slug)) slug = "extracted";

        var fileName = $"{slug}.md";
        return string.IsNullOrEmpty(dir) ? fileName : Path.Combine(dir, fileName).Replace('\\', '/');
    }
}

public sealed class SplitPlan
{
    public required string SourceFile { get; init; }
    public required int TotalLines { get; init; }
    public required int MaxLines { get; init; }
    public required int MinSectionLines { get; init; }
    public required List<SplitCandidate> Candidates { get; init; }
}

public sealed class SplitCandidate
{
    public required string Heading { get; init; }
    public required int Level { get; init; }
    public required string Selector { get; init; }
    public required int LineCount { get; init; }
    public required LineRange Range { get; init; }
    public required string SuggestedTargetPath { get; init; }
    public List<string> Warnings { get; init; } = [];
}
