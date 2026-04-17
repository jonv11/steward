namespace Steward.Core.Markdown;

/// <summary>
/// Preview-first multi-file section extraction.
/// Extracts one section from a source document into a new target file.
/// Returns an ExtractResult describing both sides of the change; the caller is
/// responsible for writing files when --apply is given.
/// </summary>
public static class SectionExtractor
{
    public static ExtractResult Extract(
        StructuredDocument doc,
        string selector,
        string targetFile,
        bool replaceWithLink = false)
    {
        var selectorResult = MdPathSelector.Evaluate(doc, selector);

        if (selectorResult.IsError)
            return ExtractResult.Error(selectorResult.ErrorMessage!);

        if (selectorResult.IsEmpty)
            return ExtractResult.Error($"Selector '{selector}' matched no content.");

        if (selectorResult.Matches.Count > 1)
            return ExtractResult.Error($"Selector '{selector}' is ambiguous — matched {selectorResult.Matches.Count} sections. Use a more specific selector.");

        var match = selectorResult.Matches[0];

        // Only section matches can be extracted
        if (match.Kind != MatchKind.Section)
            return ExtractResult.Error($"Selector '{selector}' matched a '{match.Kind}' element, not a section. Only heading sections can be extracted.");

        var lines = doc.RawContent.Split('\n');
        var startIdx = match.Range.Start - 1; // 0-based
        var endIdx = match.Range.End - 1;

        if (startIdx < 0 || endIdx >= lines.Length || startIdx > endIdx)
            return ExtractResult.Error("Resolved section range is out of bounds.");

        // Check ownership for the matched section
        foreach (var region in doc.ManagedRegions)
        {
            if (match.Range.Start >= region.Range.Start && match.Range.Start <= region.Range.End)
            {
                if (region.Owner != null && !string.Equals(region.Owner, "steward", StringComparison.OrdinalIgnoreCase))
                    return ExtractResult.Error(
                        $"Section '{match.Label}' is within managed region '{region.Id}' owned by '{region.Owner}'. Cannot extract.");
            }
        }

        var extractedLines = lines[startIdx..(endIdx + 1)];
        var targetContent = string.Join('\n', extractedLines);

        // Build new source content — remove extracted section (or replace with link stub)
        var sourceLines = lines.ToList();
        string? replacementLine = replaceWithLink
            ? $"[{match.Label}]({targetFile})"
            : null;

        sourceLines.RemoveRange(startIdx, endIdx - startIdx + 1);

        if (replacementLine != null)
            sourceLines.Insert(startIdx, replacementLine);

        var newSourceContent = string.Join('\n', sourceLines);

        return ExtractResult.Success(doc.RawContent, newSourceContent, targetContent, match.Label, targetFile);
    }
}

public sealed class ExtractResult
{
    public bool IsError { get; private init; }
    public string? ErrorMessage { get; private init; }

    public string OriginalSourceContent { get; private init; } = "";
    public string NewSourceContent { get; private init; } = "";
    public string TargetContent { get; private init; } = "";
    public string? ExtractedHeading { get; private init; }
    public string? TargetFile { get; private init; }

    public static ExtractResult Error(string message) => new() { IsError = true, ErrorMessage = message };

    public static ExtractResult Success(
        string originalSource, string newSource, string target, string heading, string targetFile) => new()
    {
        IsError = false,
        OriginalSourceContent = originalSource,
        NewSourceContent = newSource,
        TargetContent = target,
        ExtractedHeading = heading,
        TargetFile = targetFile
    };
}
